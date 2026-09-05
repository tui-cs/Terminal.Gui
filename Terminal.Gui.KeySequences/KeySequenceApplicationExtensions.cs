using Terminal.Gui.App;
using Terminal.Gui.Input;

namespace Terminal.Gui.KeySequences;

/// <summary>Provides extension methods for attaching key sequences to applications.</summary>
public static class KeySequenceApplicationExtensions
{
    /// <summary>Attaches sequence bindings to an application.</summary>
    public static IDisposable UseKeySequences (this IApplication application, KeySequenceBindings bindings)
    {
        ArgumentNullException.ThrowIfNull (application);
        ArgumentNullException.ThrowIfNull (bindings);

        return new ApplicationKeySequenceRegistration (application, bindings);
    }

    /// <summary>Creates and attaches sequence bindings to an application.</summary>
    public static IDisposable UseKeySequences (this IApplication application, Action<KeySequenceBindings> configure)
    {
        ArgumentNullException.ThrowIfNull (configure);

        KeySequenceBindings bindings = new ();
        configure (bindings);

        return application.UseKeySequences (bindings);
    }

    private sealed class ApplicationKeySequenceRegistration : IDisposable
    {
        private readonly IApplication _application;
        private readonly KeySequenceBindings _bindings;

        private bool _disposed;

        public ApplicationKeySequenceRegistration (IApplication application, KeySequenceBindings bindings)
        {
            _application = application;
            _bindings = bindings;
            _application.Keyboard.KeyDown += Keyboard_KeyDown;
        }

        public void Dispose ()
        {
            if (_disposed)
            {
                return;
            }

            _application.Keyboard.KeyDown -= Keyboard_KeyDown;
            _disposed = true;
        }

        private void Keyboard_KeyDown (object? sender, Key key)
        {
            if (_application.TopRunnableView is not { } target)
            {
                return;
            }

            KeySequenceResult result = _bindings.ProcessKey (target, key);

            if (result != KeySequenceResult.NotLeader && result != KeySequenceResult.TimedOut)
            {
                key.Handled = true;
            }
        }
    }
}
