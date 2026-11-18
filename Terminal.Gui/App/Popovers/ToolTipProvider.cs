namespace Terminal.Gui.App;

/// <summary>
/// Provides a mechanism for supplying tooltip content for ToolTipHost.
/// </summary>
/// <remarks>ToolTipProvider enables flexible creation of tooltips by allowing content to be specified as a static
/// string, a dynamic string factory, a custom view, or a view factory delegate. This allows tooltips to be customized
/// or generated dynamically based on application state. The tooltip content is created on demand when needed by the
/// UI.</remarks>
public sealed class ToolTipProvider
{
    private readonly Func<View> _factory;

    /// <summary>
    /// Creates tooltip content.
    /// </summary>
    public View GetContent () => _factory ();

    /// <summary>
    /// Initializes a new instance of the ToolTipProvider class using the specified factory function to generate tooltip
    /// content.
    /// </summary>
    /// <param name="contentFactory">A function that returns a View representing the content to display in the tooltip. This function is invoked each
    /// time a tooltip is shown.</param>
    /// <exception cref="ArgumentNullException">Thrown if the contentFactory parameter is null.</exception>
    public ToolTipProvider (Func<View> contentFactory)
    {
        _factory = contentFactory ?? throw new ArgumentNullException (nameof (contentFactory));
    }

    /// <summary>
    /// Initializes a new instance of the ToolTipProvider class with the specified tooltip text.
    /// </summary>
    /// <param name="text">The text to display in the tooltip. This value is used as the content of the tooltip label.</param>
    public ToolTipProvider (string text)
        : this (() => new Label { Text = text })
    {
    }

    /// <summary>
    /// Initializes a new instance of the ToolTipProvider class using a delegate that supplies the tooltip text.
    /// </summary>
    /// <remarks>Use this constructor when you want the tooltip text to be generated dynamically at runtime.
    /// The provided delegate allows the tooltip content to reflect the current state or context when
    /// displayed.</remarks>
    /// <param name="textFactory">A delegate that returns the text to display in the tooltip. The delegate is invoked each time the tooltip is
    /// shown.</param>
    public ToolTipProvider (Func<string> textFactory)
        : this (() => new Label { Text = textFactory () })
    {
    }
}