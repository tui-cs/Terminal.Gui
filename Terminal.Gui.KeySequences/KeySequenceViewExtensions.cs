using System.Runtime.CompilerServices;
using Terminal.Gui.ViewBase;

namespace Terminal.Gui.KeySequences;

/// <summary>Provides extension methods for attaching key sequences to views.</summary>
public static class KeySequenceViewExtensions
{
    private static readonly ConditionalWeakTable<View, KeySequenceBindings> Bindings = new ();

    /// <summary>Gets the sequence bindings associated with a view.</summary>
    public static KeySequenceBindings GetKeySequences (this View view)
    {
        ArgumentNullException.ThrowIfNull (view);
        return Bindings.GetValue (view, _ => new KeySequenceBindings ());
    }

    /// <summary>Attaches sequence bindings to a view.</summary>
    public static IDisposable UseKeySequences (
        this View view,
        KeySequenceBindings bindings,
        KeySequenceInterceptionMode mode = KeySequenceInterceptionMode.AfterUnhandled)
    {
        ArgumentNullException.ThrowIfNull (view);
        ArgumentNullException.ThrowIfNull (bindings);

        Bindings.Remove (view);
        Bindings.Add (view, bindings);

        return new KeySequenceRegistration (view, bindings, mode);
    }

    /// <summary>Creates and attaches sequence bindings to a view.</summary>
    public static IDisposable UseKeySequences (
        this View view,
        Action<KeySequenceBindings> configure,
        KeySequenceInterceptionMode mode = KeySequenceInterceptionMode.AfterUnhandled)
    {
        ArgumentNullException.ThrowIfNull (configure);

        KeySequenceBindings bindings = view.GetKeySequences ();
        configure (bindings);

        return view.UseKeySequences (bindings, mode);
    }
}
