namespace Terminal.Gui.App;

internal sealed class InlineOutputOperation
{
    private readonly ApplicationImpl _app;
    private readonly CancellationToken _token;
    private readonly TaskCompletionSource _completion = new (TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Lock _registrationLock = new ();
    private CancellationTokenRegistration _registration;
    private int _state;

    internal InlineOutputOperation (ApplicationImpl app, InlineOutputLayout layout, SessionToken? owner, CancellationToken token, long generation)
    {
        _app = app;
        Layout = layout;
        Owner = owner;
        _token = token;
        Generation = generation;
    }
    internal InlineOutputLayout Layout { get; }
    internal SessionToken? Owner { get; }
    internal long Generation { get; }
    internal LinkedListNode<InlineOutputOperation>? QueueNode { get; set; }
    internal Task Task => _completion.Task;
    internal bool IsPending => Volatile.Read (ref _state) == 0;
    internal bool TryStart () => !_token.IsCancellationRequested && Interlocked.CompareExchange (ref _state, 1, 0) == 0;

    internal void RegisterCancellation ()
    {
        if (!_token.CanBeCanceled)
        {
            return;
        }
        CancellationTokenRegistration registration = _token.Register (static state => ((InlineOutputOperation)state!).Cancel (), this);
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

    internal void Cancel ()
    {
        if (Interlocked.CompareExchange (ref _state, 2, 0) != 0)
        {
            return;
        }
        Release ();
        _app.SettleInlineOutput (() => _completion.TrySetCanceled (_token.IsCancellationRequested ? _token : new CancellationToken (true)));
    }

    internal void Finish (Exception? error = null)
    {
        if (Interlocked.Exchange (ref _state, 2) == 2)
        {
            return;
        }
        Release ();
        if (error is { })
        {
            _app.SettleInlineOutput (() => _completion.TrySetException (error));
            return;
        }
        _app.SettleInlineOutput (() => _completion.TrySetResult ());
    }

    private void Release ()
    {
        CancellationTokenRegistration registration;
        lock (_registrationLock)
        {
            registration = _registration;
            _registration = default;
        }
        registration.Unregister ();
        _app.CompleteInlineOutput (this);
    }
}
