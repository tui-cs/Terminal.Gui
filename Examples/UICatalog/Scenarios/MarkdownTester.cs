// ReSharper disable AccessToDisposedClosure

using Terminal.Gui.Editor;
using Terminal.Gui.Editor.Highlighting;
using TextMateSharp.Grammars;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Markdown Tester", "Edit Markdown in an Editor and see it rendered in a MarkdownView.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Text and Formatting")]
public class MarkdownTester : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        Window window = new () { Title = "Markdown Tester", Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        // --- Source editor (top half) ---
        FrameView editorFrame = new ()
        {
            Title = "Markdown Source",
            TabStop = TabBehavior.TabStop,
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Percent (40)
        };
        editorFrame.Border.Thickness = new Thickness (0, 2, 0, 0);

        Editor editor = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            HighlightingDefinition = HighlightingManager.Instance.GetDefinition ("MarkDown")
        };

        editor.Text = Markdown.DefaultMarkdownSample;

        editorFrame.Add (editor);

        // --- Preview (bottom half) ---
        FrameView previewFrame = new ()
        {
            Title = "Rendered Preview",
            TabStop = TabBehavior.TabStop,
            X = 0,
            Y = Pos.Bottom (editorFrame),
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        previewFrame.Border.Thickness = new Thickness (0, 2, 0, 0);

        Markdown preview = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill () - 1,
            SyntaxHighlighter = new TextMateSyntaxHighlighter ()
        };

        previewFrame.Add (preview);

        // Update preview when editor text changes
        editor.Document!.Changed += (_, _) => { preview.Text = editor.Text; };

        window.Add (editorFrame, previewFrame);

        StatusBar statusBar = new ();

        Shortcut quitShortcut = new () { Title = "Quit", Key = Key.Esc, Action = app.RequestStop };

        DropDownList<ThemeName> themeDropDown = new ()
        {
            Value = (preview.SyntaxHighlighter as TextMateSyntaxHighlighter)?.ThemeName ?? ThemeName.DarkPlus, ReadOnly = true, CanFocus = false
        };

        themeDropDown.ValueChanged += (_, e) =>
                                      {
                                          if (e.Value is not { } themeName)
                                          {
                                              return;
                                          }
                                          preview.SyntaxHighlighter = new TextMateSyntaxHighlighter (themeName);
                                          preview.Text = editor.Text;
                                      };

        Shortcut themeShortcut = new () { Title = "Theme", CommandView = themeDropDown };

        // Auto-select a light or dark syntax theme based on the terminal's actual background color.
        app.Driver!.DefaultAttributeChanged += (_, e) =>
                                               {
                                                   if (e.NewValue is not { } attr)
                                                   {
                                                       return;
                                                   }

                                                   ThemeName autoTheme = TextMateSyntaxHighlighter.GetThemeForBackground (attr.Background);
                                                   preview.SyntaxHighlighter = new TextMateSyntaxHighlighter (autoTheme);
                                                   themeDropDown.Value = autoTheme;
                                               };

        CheckBox themeBgCheckBox = new () { Text = "Theme _BG", Value = preview.UseThemeBackground ? CheckState.Checked : CheckState.UnChecked };

        themeBgCheckBox.ValueChanged += (_, e) => { preview.UseThemeBackground = e.NewValue == CheckState.Checked; };

        Shortcut themeBgShortcut = new () { CommandView = themeBgCheckBox };

        statusBar.Add (themeShortcut, themeBgShortcut, quitShortcut);
        window.Add (statusBar);

        preview.Text = editor.Text;

        app.Run (window);
        window.Dispose ();
    }
}
