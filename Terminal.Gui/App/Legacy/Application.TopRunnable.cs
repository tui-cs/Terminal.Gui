using System.Collections.Concurrent;

namespace Terminal.Gui.App;

public static partial class Application // TopRunnable handling
{
    /// <summary>The <see cref="View"/> that is on the top of the <see cref="IApplication.SessionStack"/>.</summary>
    /// <value>The top runnable.</value>
    [Obsolete ("The legacy static Application object is going away.")]
    public static View? TopRunnableView => ApplicationImpl.Instance.TopRunnableView;

    /// <summary>The <see cref="View"/> that is on the top of the <see cref="IApplication.SessionStack"/>.</summary>
    /// <value>The top runnable.</value>
    [Obsolete ("The legacy static Application object is going away.")]
    public static IRunnable? TopRunnable => ApplicationImpl.Instance.TopRunnable;
}
