namespace Terminal.Gui.App;

/// <summary>Event arguments for events about <see cref="SessionToken"/></summary>
public class SessionTokenEventArgs : EventArgs
{
    /// <summary>Creates a new instance of the <see cref="SessionTokenEventArgs"/> class</summary>
    /// <param name="state"></param>
    public SessionTokenEventArgs (SessionToken state) { State = state; }

    /// <summary>The state being reported on by the event</summary>
    public SessionToken State { get; }
}
