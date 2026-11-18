#nullable enable

using System.Text;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("WideGlyphs", "Demonstrates wide glyphs with overlapped views & clipping")]
[ScenarioCategory ("Unicode")]
[ScenarioCategory ("Drawing")]
public sealed class WideGlyphs : Scenario
{
    private Rune [,]? _codepoints;

    public override void Main ()
    {

        // Init
        using IApplication app = Application.Create ();
        app.Init ();

        // Setup - Create a top-level application window and configure it.
        using Window appWindow = new () { Title = GetQuitKeyAndName (), BorderStyle = LineStyle.None };

        // Add Editors

        AdornmentsEditor adornmentsEditor = new ()
        {
            BorderStyle = LineStyle.Single,
            X = Pos.AnchorEnd (),
            AutoSelectViewToEdit = true,
            AutoSelectAdornments = false,
            ShowViewIdentifier = true
        };
        appWindow.Add (adornmentsEditor);

        ViewportSettingsEditor viewportSettingsEditor = new ()
        {
            BorderStyle = LineStyle.Single, Y = Pos.AnchorEnd (), X = Pos.AnchorEnd (), AutoSelectViewToEdit = true
        };
        appWindow.Add (viewportSettingsEditor);

        // Build the array of codepoints once when subviews are laid out
        appWindow.SubViewsLaidOut += (s, _) =>
                                     {
                                         var view = s as View;

                                         if (view is null)
                                         {
                                             return;
                                         }

                                         // Only rebuild if size changed or array is null
                                         if (_codepoints is null
                                             || _codepoints.GetLength (0) != view.Viewport.Height
                                             || _codepoints.GetLength (1) != view.Viewport.Width)
                                         {
                                             _codepoints = new Rune [view.Viewport.Height, view.Viewport.Width];

                                             for (var r = 0; r < view.Viewport.Height; r++)
                                             {
                                                 for (var c = 0; c < view.Viewport.Width; c += 2)
                                                 {
                                                     _codepoints [r, c] = GetRandomWideCodepoint ();
                                                 }
                                             }
                                         }
                                     };

        // Fill the window with the pre-built codepoints array
        // For detailed documentation on the draw code flow from Application.Run to this event,
        // see WideGlyphs.DrawFlow.md in this directory
        appWindow.DrawingContent += (s, e) =>
                                    {
                                        var view = s as View;

                                        if (view is null || _codepoints is null)
                                        {
                                            return;
                                        }

                                        // Traverse the Viewport, using the pre-built array
                                        for (var r = 0; r < view.Viewport.Height && r < _codepoints.GetLength (0); r++)
                                        {
                                            for (var c = 0; c < view.Viewport.Width && c < _codepoints.GetLength (1); c += 2)
                                            {
                                                Rune codepoint = _codepoints [r, c];

                                                if (codepoint != default (Rune))
                                                {
                                                    view.Move (c, r);
                                                    Attribute attr = view.GetAttributeForRole (VisualRole.Normal);
                                                    view.SetAttribute (attr with { Background = attr.Background + r * 5 });
                                                    view.AddRune (codepoint);
                                                }
                                            }
                                        }
                                        e.DrawContext?.AddDrawnRectangle (view.Viewport);
                                    };

        View arrangeableViewAtEven = new ()
        {
            CanFocus = true,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable,
            X = 30,
            Y = 5,
            Width = 15,
            Height = 5

            //BorderStyle = LineStyle.Dashed
        };

        arrangeableViewAtEven.SetScheme (new Scheme { Normal = new Attribute (Color.Black, Color.Green) });

        // Proves it's not LineCanvas related
        arrangeableViewAtEven.Border.Thickness = new Thickness (1);
        arrangeableViewAtEven.Border.GetOrCreateView ().Add (new View { Height = Dim.Auto (), Width = Dim.Auto (), Text = "Even" });
        appWindow.Add (arrangeableViewAtEven);

        Button arrangeableViewAtOdd = new ()
        {
            Title = $"你 {Glyphs.Apple}",
            CanFocus = true,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable,
            X = 31,
            Y = 11,
            Width = 15,
            Height = 5,
            BorderStyle = LineStyle.Dashed,
            SchemeName = "error"
        };
        arrangeableViewAtOdd.Accepted += (sender, _) => { MessageBox.Query ((sender as View)?.App!, "Button Pressed", "You Pressed it!"); };
        appWindow.Add (arrangeableViewAtOdd);

        View superView = new ()
        {
            CanFocus = true,
            X = 30, // on an even column to start
            Y = Pos.Center (),
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            BorderStyle = LineStyle.Single,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable,
            ShadowStyle = ShadowStyles.Transparent
        };
        (superView.Margin.View as MarginView)?.ShadowSize = (superView.Margin.View as MarginView)!.ShadowSize with { Width = 2 };

        Rune codepoint = Glyphs.Apple;

        superView.DrawingContent += (s, e) =>
                                    {
                                        var view = s as View;

                                        for (var r = 0; r < view!.Viewport.Height; r++)
                                        {
                                            for (var c = 0; c < view.Viewport.Width; c += 2)
                                            {
                                                if (codepoint != default (Rune))
                                                {
                                                    view.AddRune (c, r, codepoint);
                                                }
                                            }
                                        }
                                        e.DrawContext?.AddDrawnRectangle (view.Viewport);
                                        e.Cancel = true;
                                    };
        appWindow.Add (superView);

        View viewWithBorderAtX0 = new ()
        {
            Text = "viewWithBorderAtX0",
            BorderStyle = LineStyle.Dashed,
            X = 0,
            Y = 1,
            Width = Dim.Auto (),
            Height = 3
        };

        View viewWithBorderAtX1 = new ()
        {
            Text = "viewWithBorderAtX1",
            BorderStyle = LineStyle.Dashed,
            X = 1,
            Y = Pos.Bottom (viewWithBorderAtX0) + 1,
            Width = Dim.Auto (),
            Height = 3
        };

        View viewWithBorderAtX2 = new ()
        {
            Text = "viewWithBorderAtX2",
            BorderStyle = LineStyle.Dashed,
            X = 2,
            Y = Pos.Bottom (viewWithBorderAtX1) + 1,
            Width = Dim.Auto (),
            Height = 3
        };

        superView.Add (viewWithBorderAtX0, viewWithBorderAtX1, viewWithBorderAtX2);

        // Run - Start the application.
        app.Run (appWindow);
    }

    private Rune GetRandomWideCodepoint ()
    {
        Random random = new ();
        int codepoint = random.Next (0x4E00, 0x9FFF);

        return new Rune (codepoint);
    }
}
