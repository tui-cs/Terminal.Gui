namespace Terminal.Gui.App;

internal partial class ApplicationImpl
{
    private const int MaximumInlineRequests = 256;
    private readonly LinkedList<InlineOutputOperation> _inlineOutputQueue = new ();
    private long _inlineOutputGeneration;
    private int _inlineOutputUnits;
    private int _inlineOutputOutstanding;
    private Exception? _inlineOutputFailure;
    private bool _layoutAndDrawActive;
    private readonly HashSet<SessionToken> _activeLayoutSessions = [];
    private bool _inlineOriginEstablished;
    private bool _settlingInlineOutput;

    internal void SettleInlineOutput (Action settle)
    {
        if (!_layoutAndDrawActive || MainThreadId != Thread.CurrentThread.ManagedThreadId)
        {
            settle ();
            return;
        }
        // MainLoopSyncContext.Post otherwise executes inline on the running UI thread, even
        // when the TCS requests asynchronous continuations. Only this print settlement defers posts.
        bool previous = _settlingInlineOutput;
        _settlingInlineOutput = true;
        try
        {
            settle ();
        }
        finally
        {
            _settlingInlineOutput = previous;
        }
    }

    Task IApplicationInlineOutput.PrintAboveAsync (IReadOnlyList<InlineOutputLine> lines, SessionToken? owner, CancellationToken cancellationToken) =>
        PrepareInlineOutput (() => new InlineOutputLayout (lines), owner, cancellationToken);

    internal Task PrintAboveTextAsync (string text, SessionToken? owner, CancellationToken cancellationToken) =>
        PrepareInlineOutput (() => new InlineOutputLayout (ApplicationInlineOutputExtensions.Split (text)), owner, cancellationToken);

    private Task PrepareInlineOutput (Func<InlineOutputLayout> prepare, SessionToken? owner, CancellationToken cancellationToken)
    {
        long generation;
        lock (_dispatchLock)
        {
            CheckInlineOutputLifetime ();
            generation = _inlineOutputGeneration;
        }
        InlineOutputLayout layout = prepare ();
        InlineOutputOperation operation;
        lock (_dispatchLock)
        {
            if (generation != _inlineOutputGeneration || !Initialized || _dispatchStopping)
            {
                return Task.FromCanceled (new CancellationToken (true));
            }
            CheckInlineOutputLifetime ();
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled (cancellationToken);
            }
            if (owner is { } && (owner.IsDispatchClosed || owner.Runnable is null || SessionStack?.Contains (owner) != true)
                || owner is null && HasEndedSession && !HasRunningSession)
            {
                return Task.FromCanceled (new CancellationToken (true));
            }
            if (_inlineOutputFailure is { } failure)
            {
                return Task.FromException (failure);
            }
            if (layout.IsEmpty)
            {
                return Task.CompletedTask;
            }
            if (_inlineOutputOutstanding >= MaximumInlineRequests || layout.Units > InlineOutputLayout.MaximumUnits - _inlineOutputUnits)
            {
                throw new InvalidOperationException ("Inline output queue is full. Await smaller batches before submitting more.");
            }
            operation = new (this, layout, owner, cancellationToken, generation);
            operation.QueueNode = _inlineOutputQueue.AddLast (operation);
            _inlineOutputUnits += layout.Units;
            _inlineOutputOutstanding++;
        }
        operation.RegisterCancellation ();
        return operation.Task;
    }

    private void CheckInlineOutputLifetime ()
    {
        if (!Initialized || _dispatchStopping)
        {
            throw new NotInitializedException (nameof (ApplicationInlineOutputExtensions.PrintAboveAsync));
        }
        if (AppModel != AppModel.Inline)
        {
            throw new InvalidOperationException ("PrintAboveAsync requires Inline mode selected before Init.");
        }
        if (Driver is DriverImpl ansiDriver && ansiDriver.GetOutput () is AnsiOutput { AppModel: not AppModel.Inline })
        {
            throw new InvalidOperationException ("The ANSI output was not initialized in Inline mode.");
        }
        if (Driver is not DriverImpl driver || driver.GetOutput () is not IInlineOutput { IsInlineOutputAvailable: true })
        {
            throw new NotSupportedException ("PrintAboveAsync requires attached built-in ANSI inline output.");
        }
    }

    internal void CompleteInlineOutput (InlineOutputOperation operation)
    {
        lock (_dispatchLock)
        {
            if (operation.QueueNode is { } node)
            {
                _inlineOutputQueue.Remove (node);
                operation.QueueNode = null;
            }
            if (operation.Generation != _inlineOutputGeneration)
            {
                return;
            }
            _inlineOutputUnits -= operation.Layout.Units;
            _inlineOutputOutstanding--;
        }
    }

    private void EndInlineOutput (SessionToken? owner = null, bool fault = false)
    {
        InlineOutputOperation [] operations;
        lock (_dispatchLock)
        {
            operations = _inlineOutputQueue.Where (operation => owner is null || ReferenceEquals (operation.Owner, owner)).ToArray ();
        }
        foreach (InlineOutputOperation operation in operations)
        {
            if (fault)
            {
                // Claim pending work to settle it as a fault, preserving any cancellation that already won.
                if (operation.TryStart ())
                {
                    operation.Finish (_inlineOutputFailure);
                }
                else
                {
                    operation.Cancel ();
                }
                continue;
            }
            operation.Cancel ();
        }
    }

    private InlineOutputOperation? CaptureInlineOutput ()
    {
        lock (_dispatchLock)
        {
            return _inlineOutputQueue.First?.Value;
        }
    }

    private bool DrainInlineOutput (InlineOutputOperation? operation)
    {
        if (operation is null || !operation.IsPending)
        {
            return false;
        }
        if (!Initialized || _dispatchStopping || !HasRunningSession)
        {
            return false;
        }
        if (AppModel != AppModel.Inline || Driver is not DriverImpl driver || driver.GetOutput () is not IInlineOutput output)
        {
            operation.Finish (new InvalidOperationException ("Inline output mode or driver changed before insertion."));
            return false;
        }
        if (Driver.AnsiStartupGate is { IsReady: false } && !ForceInlinePosition.HasValue)
        {
            return false;
        }
        if (!_inlineScreenSized || _screen is not { } region)
        {
            return false;
        }
        Size terminal = Driver.Screen.Size;
        if (output is AnsiOutput ansiOutput && ansiOutput.HasPendingInlineSizeChange (terminal))
        {
            return false;
        }
        if (terminal.Width <= 0 || terminal.Height <= 0 || region.Width <= 0 || region.Height <= 0)
        {
            return false;
        }
        try
        {
            if (region.X != 0 || region.Width != terminal.Width || region.Y < 0 || (long)region.Y + region.Height > terminal.Height)
            {
                throw new InvalidOperationException ("PrintAboveAsync requires a valid full-width inline region.");
            }
            if (driver.GetOutput () is AnsiOutput { AppModel: not AppModel.Inline })
            {
                throw new InvalidOperationException ("The ANSI output mode changed before insertion.");
            }
            output.ValidateInsertion (driver.GetOutputBuffer ());
        }
        catch (Exception error)
        {
            operation.Finish (error);
            return false;
        }
        lock (_dispatchLock)
        {
            if (!CanStartUiWork || operation.Owner is { IsDispatchActive: false } || operation.Generation != _inlineOutputGeneration
                || !ReferenceEquals (_inlineOutputQueue.First?.Value, operation) || !operation.TryStart ())
            {
                return false;
            }
            _inlineOutputQueue.RemoveFirst ();
            operation.QueueNode = null;
        }
        int newY;
        try
        {
            newY = output.InsertAbove (operation.Layout, region, terminal);
        }
        catch (Exception error)
        {
            lock (_dispatchLock)
            {
                _inlineOutputFailure = error;
            }
            operation.Finish (error);
            EndInlineOutput (fault: true);
            return false;
        }
        // Publish geometry before notifying user code; completed output cannot become unprinted on callback failure.
        _screen = region with { Y = newY };
        driver.InlinePosition = new Point (0, newY);
        try
        {
            foreach (View view in SessionStack!.Select (token => token.Runnable).OfType<View> ())
            {
                view.SetNeedsDraw ();
            }
            Popovers?.GetActivePopover ()?.SetNeedsDraw ();
            driver.ClearContents ();
        }
        finally
        {
            // Physical insertion already succeeded even if a redraw callback threw.
            operation.Finish ();
        }
        RaiseScreenChangedEvent (Screen);
        return true;
    }

    private bool InlinePlacementLost => Driver is DriverImpl driver && driver.GetOutput () is IInlineOutput { IsInlinePlacementKnown: false };

    private bool WriteInlineMovement (string text, Size? expectedSize = null)
    {
        if (InlinePlacementLost)
        {
            return false;
        }
        if (Driver is not DriverImpl driver || driver.GetOutput () is not IInlineOutput { IsInlineOutputAvailable: true } output)
        {
            Driver?.WriteRaw (text);
            return true;
        }
        if (output is AnsiOutput ansiOutput)
        {
            if (_inlineResize is { } resize
                    ? ObserveNativeInlineResizeSize (resize, ansiOutput)
                    : ansiOutput.HasPendingInlineSizeChange (expectedSize ?? driver.Screen.Size))
            {
                return false;
            }
            ansiOutput.InlineCursorRow = null;
        }
        if (driver.TryWriteRaw (text))
        {
            return true;
        }
        output.LoseInlinePlacement ();
        lock (_dispatchLock)
        {
            _inlineOutputFailure = new IOException ("Inline placement was lost while reserving the live region.");
        }
        EndInlineOutput (fault: true);
        return false;
    }
}
