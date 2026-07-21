namespace Terminal.Gui.KeySequences;

/// <summary>Describes when a view registration starts sequence capture.</summary>
public enum KeySequenceInterceptionMode
{
    /// <summary>Starts capture from keys that normal view handling did not consume.</summary>
    AfterUnhandled,

    /// <summary>Starts capture before normal view key bindings run.</summary>
    Preemptive
}
