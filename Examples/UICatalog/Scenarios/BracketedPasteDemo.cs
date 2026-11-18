#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Bracketed Paste", "Logs bracketed-paste payloads delivered by the terminal")]
[ScenarioCategory ("Text and Formatting")]
public sealed class BracketedPasteDemo : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window appWindow = new ()
        {
            Title = GetQuitKeyAndName ()
        };
        appWindow.AssignHotKeys = true;

        Label hint = CreateHintLabel ();

        TextField field = new ()
        {
            X = 0,
            Y = Pos.Bottom (hint) + 1,
            Width = Dim.Fill (),
            Title = "Paste into the focused TextField — the default Command.Paste handler inserts the text."
        };

        Label log = new ()
        {
            X = 0,
            Y = Pos.Bottom (field) + 1,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Text = "Application.Paste log (only bracketed paste events appear here):\n"
        };

        int counter = 0;

        app.Paste += (_, args) =>
                     {
                          counter++;
                          log.Text += $"{FormatPasteLogEntry (counter, args.Text)}\n";
                      };

        appWindow.Add (hint, field, log);

        app.Run (appWindow);
    }

    public static Label CreateHintLabel ()
    {
        Label hint = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Auto (DimAutoStyle.Text),
            Text = "Paste into the TextField below.\n"
                   + "If Terminal.Gui detects bracketed paste, the paste is logged below as a single bracketed-paste event.\n"
                   + "If text appears in the field but no new log entry is added, your terminal delivered it as normal input instead."
        };

        hint.TextFormatter.WordWrap = true;

        return hint;
    }

    public static string FormatPasteLogEntry (int counter, string text)
    {
        return $"[{counter}] Bracketed paste event: {text.Length} chars: {Truncate (text)}";
    }

    private static string Truncate (string text)
    {
        // Replace control chars so the display stays one line per paste.
        string flattened = text.Replace ("\r", "\\r").Replace ("\n", "\\n").Replace ("\t", "\\t");

        if (flattened.Length <= 60)
        {
            return flattened;
        }

        return flattened [..60] + "…";
    }
}
