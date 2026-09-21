using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace Terminal.Gui.KeySequences;

internal sealed class KeySequenceRegistration : IDisposable
{
    private readonly KeySequenceBindings _bindings;
    private readonly KeySequenceInterceptionMode _mode;
    private readonly View _view;

    private bool _disposed;

    public KeySequenceRegistration (View view, KeySequenceBindings bindings, KeySequenceInterceptionMode mode)
    {
        _view = view;
        _bindings = bindings;
        _mode = mode;

        _view.KeyDown += View_KeyDown;
        _view.KeyDownNotHandled += View_KeyDownNotHandled;
    }

    public void Dispose ()
    {
        if (_disposed)
        {
            return;
        }

        _view.KeyDown -= View_KeyDown;
        _view.KeyDownNotHandled -= View_KeyDownNotHandled;
        _disposed = true;
    }

    private void View_KeyDown (object? sender, Key key)
    {
        if (_bindings.IsCapturing)
        {
            Process (key);
            return;
        }

        if (_mode != KeySequenceInterceptionMode.Preemptive)
        {
            return;
        }

        Process (key);
    }

    private void View_KeyDownNotHandled (object? sender, Key key)
    {
        if (_bindings.IsCapturing || _mode == KeySequenceInterceptionMode.Preemptive)
        {
            return;
        }

        Process (key);
    }

    private void Process (Key key)
    {
        KeySequenceResult result = _bindings.ProcessKey (_view, key);

        if (result != KeySequenceResult.NotLeader && result != KeySequenceResult.TimedOut)
        {
            key.Handled = true;
        }
    }
}
