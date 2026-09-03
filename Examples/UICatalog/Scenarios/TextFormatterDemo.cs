#nullable enable
using System.Text;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("TextFormatter Demo", "Demos and tests the TextFormatter class.")]
[ScenarioCategory ("Text and Formatting")]
public class TextFormatterDemo : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window window = new ();
        window.Title = GetQuitKeyAndName ();

        // Make Win smaller so sizing the window horizontally will make the
        // labels shrink to zero-width
        window.X = 10;
        window.Width = Dim.Fill (10);

        const string TEXT = "Hello world, how are you today? Pretty neat!\nSecond line\n\nFourth Line.";

        const string UNICODE =
            "Τὴ γλῶσσα μοῦ ἔδωσαν ἑλληνικὴ\nτὸ σπίτι φτωχικὸ στὶς ἀμμουδιὲς τοῦ Ὁμήρου.\nΜονάχη ἔγνοια ἡ γλῶσσα μου στὶς ἀμμουδιὲς τοῦ Ὁμήρου.";

        Label blockText = new ()
        {
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent),
            X = 0,
            Y = 0,

            Height = 10,
            Width = Dim.Fill ()
        };

        StringBuilder block = new ();
        block.AppendLine ("  ▄████  █    ██  ██▓      ▄████▄    ██████ ");
        block.AppendLine (" ██▒ ▀█▒ ██  ▓██▒▓██▒     ▒██▀ ▀█  ▒██    ▒ ");
        block.AppendLine ("▒██░▄▄▄░▓██  ▒██░▒██▒     ▒▓█    ▄ ░ ▓██▄   ");
        block.AppendLine ("░▓█  ██▓▓▓█  ░██░░██░     ▒▓▓▄ ▄██▒  ▒   ██▒");
        block.AppendLine ("░▒▓███▀▒▒▒█████▓ ░██░ ██▓ ▒ ▓███▀ ░▒██████▒▒");
        block.AppendLine (" ░▒   ▒ ░▒▓▒ ▒ ▒ ░▓   ▒▓▒ ░ ░▒ ▒  ░▒ ▒▓▒ ▒ ░");
        block.AppendLine ("  ░   ░ ░░▒░ ░ ░  ▒ ░ ░▒    ░  ▒   ░ ░▒  ░ ░");
        block.AppendLine ("░ ░   ░  ░░░ ░ ░  ▒ ░ ░   ░        ░  ░  ░  ");
        block.AppendLine ("      ░    ░      ░    ░  ░ ░            ░  ");
        block.AppendLine ("                       ░  ░                 ");
        blockText.Text = block.ToString (); // .Replace(" ", "\u00A0"); // \u00A0 is 'non-breaking space
        window.Add (blockText);

        CheckBox unicodeCheckBox = new ()
        {
            X = 0,
            Y = Pos.Bottom (blockText) + 1,
            Text = "Unicode",
            Value = window.HotKeySpecifier == (Rune)' ' ? CheckState.Checked : CheckState.UnChecked
        };

        window.Add (unicodeCheckBox);

        List<Alignment> alignments = [Alignment.Start, Alignment.End, Alignment.Center, Alignment.Fill];
        Label [] singleLines = new Label [alignments.Count];
        Label [] multipleLines = new Label [alignments.Count];

        var multiLineHeight = 5;

        for (var i = 0; i < alignments.Count; i++)
        {
            singleLines [i] = new ()
            {
                TextAlignment = alignments [i],
                X = 0,

                Width = Dim.Fill (),
                Height = 1,
                SchemeName = "Dialog",
                Text = TEXT
            };

            multipleLines [i] = new ()
            {
                TextAlignment = alignments [i],
                X = 0,

                Width = Dim.Fill (),
                Height = multiLineHeight,
                SchemeName = "Dialog",
                Text = TEXT
            };
        }

        Label label = new ()
        {
            Y = Pos.Bottom (unicodeCheckBox) + 1, Text = "Demonstrating multi-line and word wrap:"
        };
        window.Add (label);

        for (var i = 0; i < alignments.Count; i++)
        {
            label = new () { Y = Pos.Bottom (label), Text = $"{alignments [i]}:" };
            window.Add (label);
            singleLines [i].Y = Pos.Bottom (label);
            window.Add (singleLines [i]);
            label = singleLines [i];
        }

        label = new () { Y = Pos.Bottom (label), Text = "Demonstrating multi-line and word wrap:" };
        window.Add (label);

        for (var i = 0; i < alignments.Count; i++)
        {
            label = new () { Y = Pos.Bottom (label), Text = $"{alignments [i]}:" };
            window.Add (label);
            multipleLines [i].Y = Pos.Bottom (label);
            window.Add (multipleLines [i]);
            label = multipleLines [i];
        }

        unicodeCheckBox.ValueChanging += (_, e) =>
                                                {
                                                    for (var i = 0; i < alignments.Count; i++)
                                                    {
                                                        singleLines [i].Text = e.NewValue == CheckState.Checked ? TEXT : UNICODE;
                                                        multipleLines [i].Text = e.NewValue == CheckState.Checked ? TEXT : UNICODE;
                                                    }
                                                };

        app.Run (window);
    }
}
