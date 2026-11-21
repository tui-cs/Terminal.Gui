namespace Terminal.Gui.App;

internal sealed class UiDispatchOperation
{
    private readonly Action<IApplication> _action;
    private readonly ApplicationImpl _app;
    private readonly CancellationToken _cancellationToken;
    private readonly TaskCompletionSource _completion = new (TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Lock _registrationLock = new ();
    private CancellationTokenRegistration _registration;
    private int _state;

    internal UiDispatchOperation (ApplicationImpl app, Action<IApplication> action, SessionToken? owner, CancellationToken cancellationToken)
    {
        _app = app;
        _action = action;
        Owner = owner;
        _cancellationToken = cancellationToken;
    }

    internal SessionToken? Owner { get; }

    internal LinkedListNode<UiDispatchOperation>? QueueNode { get; set; }

    internal Task Task => _completion.Task;

    internal bool IsPending => Volatile.Read (ref _state) == 0;

    internal bool TryStart () => Interlocked.CompareExchange (ref _state, 1, 0) == 0;

    internal void RegisterCancellation ()
    {
        if (!_cancellationToken.CanBeCanceled)
        {
            return;
        }

        CancellationTokenRegistration registration = _cancellationToken.Register (static state => ((UiDispatchOperation)state!).Cancel (), this);

        lock (_registrationLock)
        {
            if (IsPending)
            {
                _registration = registration;

                return;
            }
        }

        registration.Unregister ();
    }

    internal void Execute ()
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            Cancel ();

            return;
        }

        // Work that cannot start now stays queued until a later drain starts it or teardown cancels it.
        if (!_app.TryStartDispatch (this))
        {
            return;
        }

        try
        {
            _action (_app);
            _completion.TrySetResult ();
        }
        catch (OperationCanceledException ex) when (_cancellationToken.IsCancellationRequested && ex.CancellationToken == _cancellationToken)
        {
            _completion.TrySetCanceled (_cancellationToken);
        }
        catch (Exception ex)
        {
            _completion.TrySetException (ex);
        }
        finally
        {
            Volatile.Write (ref _state, 2);
            UnregisterCancellation ();
            _app.CompleteDispatch (this);
        }
    }

    internal void Cancel ()
    {
        if (Interlocked.CompareExchange (ref _state, 2, 0) != 0)
        {
            return;
        }

        if (_cancellationToken.IsCancellationRequested)
        {
            _completion.TrySetCanceled (_cancellationToken);
        }
        else
        {
            _completion.TrySetCanceled ();
        }

        UnregisterCancellation ();
        _app.CompleteDispatch (this);
    }

    private void UnregisterCancellation ()
    {
        CancellationTokenRegistration registration;

        lock (_registrationLock)
        {
            registration = _registration;
            _registration = default;
        }

        registration.Unregister ();
    }
}
