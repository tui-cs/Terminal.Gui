#nullable enable

using System;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("RuneWidthGreaterThanOne", "Test rune width greater than one")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Tests")]
[ScenarioCategory ("Unicode")]
public class RuneWidthGreaterThanOne : Scenario
{
    private IApplication? _app;
    private Button? _button;
    private Label? _label;
    private Label? _labelR;
    private Label? _labelV;
    private string? _lastRunesUsed;
    private TextField? _text;
    private Window? _win;

    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        // Window (top-level)
        using Window win = new ()
        {
            X = 5,
            Y = 5,
            Width = Dim.Fill (22),
            Height = Dim.Fill (5),
            Arrangement = ViewArrangement.Overlapped | ViewArrangement.Movable
        };
        _win = win;

        // MenuBar
        MenuBar menu = new ();

        // Controls
        _label = new ()
        {
            X = Pos.Center (),
            Y = 1
        };

        _text = new ()
        {
            X = Pos.Center (),
            Y = 3,
            Width = 20
        };

        _button = new ()
        {
            X = Pos.Center (),
            Y = 5
        };

        _labelR = new ()
        {
            X = Pos.AnchorEnd (30),
            Y = 18
        };

        _labelV = new ()
        {
            TextDirection = TextDirection.TopBottom_LeftRight,
            X = Pos.AnchorEnd (30),
            Y = Pos.Bottom (_labelR)
        };

        menu.Add (
            new MenuBarItem (
                "Padding",
                [
                    new MenuItem
                    {
                        Title = "With Padding",
                        Action = () =>
                        {
                            if (_win is not null)
                            {
                                _win.Padding.Thickness = new (1);
                            }
                        }
                    },
                    new MenuItem
                    {
                        Title = "Without Padding",
                        Action = () =>
                        {
                            if (_win is not null)
                            {
                                _win.Padding.Thickness = new (0);
                            }
                        }
                    }
                ]
            )
        );

        menu.Add (
            new MenuBarItem (
                "BorderStyle",
                [
                    new MenuItem
                    {
                        Title = "Single",
                        Action = () =>
                        {
                            if (_win is not null)
                            {
                                _win.BorderStyle = LineStyle.Single;
                            }
                        }
                    },
                    new MenuItem
                    {
                        Title = "None",
                        Action = () =>
                        {
                            if (_win is not null)
                            {
                                _win.BorderStyle = LineStyle.None;
                            }
                        }
                    }
                ]
            )
        );

        menu.Add (
            new MenuBarItem (
                "Runes length",
                [
                    new MenuItem
                    {
                        Title = "Wide",
                        Action = WideRunes
                    },
                    new MenuItem
                    {
                        Title = "Narrow",
                        Action = NarrowRunes
                    },
                    new MenuItem
                    {
                        Title = "Mixed",
                        Action = MixedRunes
                    }
                ]
            )
        );

        // Add views in order of visual appearance
        win.Add (menu, _label, _text, _button, _labelR, _labelV);

        WideRunes ();

        app.Run (win);
    }

    private void MixedMessage (object? sender, EventArgs e)
    {
        if (_text is not null)
        {
            MessageBox.Query (_text.App!, "Say Hello 你", $"Hello {_text.Text}", "Ok");
        }
    }

    private void MixedRunes ()
    {
        if (_label is null || _text is null || _button is null || _labelR is null || _labelV is null || _win is null)
        {
            return;
        }

        UnsetClickedEvent ();
        _label.Text = "Enter your name 你:";
        _text.Text = "gui.cs 你:";
        _button.Text = "Say Hello 你";
        _button.Accepting += MixedMessage;
        _labelR.X = Pos.AnchorEnd (21);
        _labelR.Y = 18;
        _labelR.Text = "This is a test text 你";
        _labelV.X = Pos.AnchorEnd (21);
        _labelV.Y = Pos.Bottom (_labelR);
        _labelV.Text = "This is a test text 你";
        _win.Title = "HACC Demo 你";
        _lastRunesUsed = "Mixed";
        _app?.LayoutAndDraw ();
    }

    private void NarrowMessage (object? sender, EventArgs e)
    {
        if (_text is not null)
        {
            MessageBox.Query (_text.App!, "Say Hello", $"Hello {_text.Text}", "Ok");
        }
    }

    private void NarrowRunes ()
    {
        if (_label is null || _text is null || _button is null || _labelR is null || _labelV is null || _win is null)
        {
            return;
        }

        UnsetClickedEvent ();
        _label.Text = "Enter your name:";
        _text.Text = "gui.cs";
        _button.Text = "Say Hello";
        _button.Accepting += NarrowMessage;
        _labelR.X = Pos.AnchorEnd (19);
        _labelR.Y = 18;
        _labelR.Text = "This is a test text";
        _labelV.X = Pos.AnchorEnd (19);
        _labelV.Y = Pos.Bottom (_labelR);
        _labelV.Text = "This is a test text";
        _win.Title = "HACC Demo";
        _lastRunesUsed = "Narrow";
        _app?.LayoutAndDraw ();
    }

    private void UnsetClickedEvent ()
    {
        if (_button is null)
        {
            return;
        }

        switch (_lastRunesUsed)
        {
            case "Narrow":
                _button.Accepting -= NarrowMessage;

                break;
            case "Mixed":
                _button.Accepting -= MixedMessage;

                break;
            case "Wide":
                _button.Accepting -= WideMessage;

                break;
        }
    }

    private void WideMessage (object? sender, EventArgs e)
    {
        if (_text is not null)
        {
            MessageBox.Query (_text.App!, "こんにちはと言う", $"こんにちは {_text.Text}", "Ok");
        }
    }

    private void WideRunes ()
    {
        if (_label is null || _text is null || _button is null || _labelR is null || _labelV is null || _win is null)
        {
            return;
        }

        UnsetClickedEvent ();
        _label.Text = "あなたの名前を入力してください：";
        _text.Text = "ティラミス";
        _button.Text = "こんにちはと言う";
        _button.Accepting += WideMessage;
        _labelR.X = Pos.AnchorEnd (29);
        _labelR.Y = 18;
        _labelR.Text = "あなたの名前を入力してください";
        _labelV.X = Pos.AnchorEnd (29);
        _labelV.Y = Pos.Bottom (_labelR);
        _labelV.Text = "あなたの名前を入力してください";
        _win.Title = "デモエムポンズ";
        _lastRunesUsed = "Wide";
        _app?.LayoutAndDraw ();
    }
}