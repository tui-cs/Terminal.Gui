using Terminal.Gui.App;
using Terminal.Gui.Drivers;

namespace Terminal.Gui.Views;

public partial class Markdown
{
    private const int MAX_RENDER_WIDTH = 4096;
    private static readonly Lock _renderToAnsiLock = new ();

    /// <summary>
    ///     Renders the current <see cref="Text"/> (or the supplied <paramref name="markdown"/>)
    ///     to an ANSI escape-sequence string suitable for writing directly to a terminal.
    /// </summary>
    /// <param name="markdown">
    ///     Optional markdown text to render. If <see langword="null"/>, uses the current <see cref="Text"/>.
    /// </param>
    /// <param name="width">
    ///     The target column width for word-wrapping. Defaults to 80. Clamped to
    ///     the range [<see cref="MIN_WRAP_WIDTH"/>, 4096].
    /// </param>
    /// <returns>A string containing ANSI escape sequences that reproduce the styled markdown output.</returns>
    /// <remarks>
    ///     <para>
    ///         This method does not require <see cref="Application"/> to be initialized. It creates a
    ///         temporary headless ANSI driver internally, performs layout and drawing into an off-screen
    ///         buffer, and returns the ANSI representation.
    ///     </para>
    ///     <para>
    ///         Configuration properties set on this instance — <see cref="SyntaxHighlighter"/>,
    ///         <see cref="MarkdownPipeline"/>, <see cref="UseThemeBackground"/>, and
    ///         <see cref="ShowHeadingPrefix"/> — are copied to the temporary view used for rendering.
    ///         Copy buttons (<see cref="ShowCopyButtons"/>) are always disabled for ANSI output because
    ///         they are interactive-only controls.
    ///     </para>
    /// </remarks>
    public string RenderToAnsi (string? markdown = null, int width = 80)
    {
        if (width < MIN_WRAP_WIDTH)
        {
            width = MIN_WRAP_WIDTH;
        }
        else if (width > MAX_RENDER_WIDTH)
        {
            width = MAX_RENDER_WIDTH;
        }

        string text = markdown ?? Text;

        if (string.IsNullOrEmpty (text))
        {
            return string.Empty;
        }

        // A static lock guards the process-wide DisableRealDriverIO environment variable
        // so concurrent calls do not race on set/restore.
        lock (_renderToAnsiLock)
        {
            string? previousValue = Environment.GetEnvironmentVariable ("DisableRealDriverIO");
            Environment.SetEnvironmentVariable ("DisableRealDriverIO", "1");

            try
            {
                using IApplication app = Application.Create ().Init (DriverRegistry.Names.ANSI);

                // Use a small initial height for the first layout pass (just enough to compute content height)
                app.Driver!.SetScreenSize (width, 1);

                using Markdown renderView = new ()
                {
                    Text = text,
                    Width = Dim.Fill (),
                    Height = Dim.Fill (),
                    SyntaxHighlighter = SyntaxHighlighter,
                    MarkdownPipeline = MarkdownPipeline,
                    UseThemeBackground = UseThemeBackground,
                    ShowHeadingPrefix = ShowHeadingPrefix,
                    ShowCopyButtons = false // Copy buttons are interactive-only
                };

                renderView.App = app;
                renderView.SetRelativeLayout (app.Screen.Size);
                renderView.Layout ();

                int contentHeight = renderView.GetContentHeight ();

                if (contentHeight < 1)
                {
                    return string.Empty;
                }

                // Resize to the full content height so the entire document is drawn
                app.Driver.SetScreenSize (width, contentHeight);
                renderView.Frame = app.Screen with { X = 0, Y = 0 };
                renderView.Layout ();
                app.Driver.ClearContents ();
                renderView.Draw ();

                return app.Driver.ToAnsi ();
            }
            finally
            {
                Environment.SetEnvironmentVariable ("DisableRealDriverIO", previousValue);
            }
        }
    }
}
