// ReSharper disable AccessToDisposedClosure

#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Transparent", "Demonstrates View Transparency")]
public sealed class Transparent : Scenario
{
    public override void Main ()
    {

        // Init
        using IApplication app = Application.Create ();
        app.Init ();

        // Setup - Create a top-level application window and configure it.
        using Window appWindow = new () { Title = GetQuitKeyAndName () };
        appWindow.BorderStyle = LineStyle.None;
        appWindow.SchemeName = "Error";

        appWindow.Text = "App Text - Centered Vertically and Horizontally.\n2nd Line of Text.\n3rd Line of Text.";
        appWindow.TextAlignment = Alignment.Center;
        appWindow.VerticalTextAlignment = Alignment.Center;

        appWindow.ClearingViewport += (s, e) =>
                                      {
                                          if (s is View sender)
                                          {
                                              sender.FillRect (sender.Viewport, Glyphs.Stipple);
                                          }

                                          e.Cancel = true;
                                      };

        ViewportSettingsEditor viewportSettingsEditor = new ()
        {
            Y = Pos.AnchorEnd (),

            //X = Pos.Right (adornmentsEditor),
            AutoSelectViewToEdit = true,
            AutoSelectAdornments = true,
            ShowViewIdentifier = true
        };
        appWindow.Add (viewportSettingsEditor);

        AdornmentsEditor adornmentsEditor = new ()
        {
            BorderStyle = LineStyle.Single,
            X = Pos.AnchorEnd (),
            AutoSelectViewToEdit = true,
            AutoSelectAdornments = true,
            ShowViewIdentifier = true
        };
        adornmentsEditor.Border.Thickness = new Thickness (1, 2, 1, 1);
        appWindow.Add (adornmentsEditor);

        Button appButton = new () { X = 10, Y = 4, Title = "_AppButton" };

        appButton.Accepting += (sender, args) =>
                               {
                                   MessageBox.Query ((sender as View)?.App!, "AppButton", "Transparency is cool!", Strings.btnOk);
                                   args.Handled = true;
                               };
        appWindow.Add (appButton);

        TransparentView tv = new () { X = 2, Y = 2, Width = Dim.Fill (10), Height = Dim.Fill (10) };

        appWindow.ViewportChanged += (_, _) =>
                                     {
                                         // Little hack to convert the Dim.Fill to actual size
                                         // So resizing works
                                         tv.Width = appWindow.Frame.Width - 10;
                                         tv.Height = appWindow.Frame.Height - 10;
                                     };
        appWindow.Add (tv);

        // Run - Start the application.
        app.Run (appWindow);
    }

    public class TransparentView : FrameView
    {
        public TransparentView ()
        {
            Title = "Transparent View - Move and Resize To See Transparency In Action";
            base.Text = "View.Text.\nThis should be opaque. Note how clipping works?";
            Arrangement = ViewArrangement.Overlapped | ViewArrangement.Resizable | ViewArrangement.Movable;
            ViewportSettings |= ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse;
            BorderStyle = LineStyle.RoundedDotted;
            SchemeName = "Base";

            View transparentSubView = new ()
            {
                Text = "Sizable/Movable SubView with border and shadow.",
                Id = "transparentSubView",
                X = Pos.Center (),
                Y = Pos.Center (),
                Width = 20,
                Height = 8,
                BorderStyle = LineStyle.Dashed,
                Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable,
                ShadowStyle = ShadowStyles.Transparent
            };
            transparentSubView.Border.Thickness = new Thickness (1, 1, 1, 1);
            transparentSubView.SchemeName = "Dialog";

            Button button = new () { Title = "_Opaque Shadow", X = Pos.Center (), Y = 2, SchemeName = "Dialog" };

            button.Accepting += (_, args) =>
                                {
                                    MessageBox.Query (App!, "Clicked!", "Button in Transparent View", Strings.btnOk);
                                    args.Handled = true;
                                };

            Shortcut shortcut = new ()
            {
                Id = "shortcut",
                X = Pos.Center (),
                Y = Pos.AnchorEnd (),
                Title = "A _Shortcut",
                HelpText = "Help!",
                Key = Key.F11,
                SchemeName = "Base"
            };

            button.ClearingViewport += (_, args) => { args.Cancel = true; };

            // Subscribe to DrawingContent event to draw "TUI"
            DrawingContent += TransparentView_DrawingContent;

            Add (button);
            Add (shortcut);
            Add (transparentSubView);

            Padding.Thickness = new Thickness (1);
            Padding.GetOrCreateView ();
            Padding.View?.Text = "This is the Padding";
        }

        private void TransparentView_DrawingContent (object? sender, DrawEventArgs e)
        {
            // Draw "TUI" text using rectangular regions, positioned after "Hi"
            // Letter "T"
            Rectangle tTop = new (20, 5, 7, 2); // Top horizontal bar
            Rectangle tStem = new (23, 7, 2, 8); // Vertical stem

            // Letter "U"
            Rectangle uLeft = new (30, 5, 2, 8); // Left vertical bar
            Rectangle uBottom = new (32, 13, 3, 2); // Bottom horizontal bar
            Rectangle uRight = new (35, 5, 2, 8); // Right vertical bar

            // Letter "I"
            Rectangle iTop = new (39, 5, 4, 2); // Bar on top
            Rectangle iStem = new (40, 7, 2, 6); // Vertical stem
            Rectangle iBottom = new (39, 13, 4, 2); // Bar on Bottom

            // Draw "TUI" using the HotActive attribute
            SetAttributeForRole (VisualRole.HotActive);
            FillRect (tTop, Glyphs.BlackCircle);
            FillRect (tStem, Glyphs.BlackCircle);
            FillRect (uLeft, Glyphs.BlackCircle);
            FillRect (uBottom, Glyphs.BlackCircle);
            FillRect (uRight, Glyphs.BlackCircle);
            FillRect (iTop, Glyphs.BlackCircle);
            FillRect (iStem, Glyphs.BlackCircle);
            FillRect (iBottom, Glyphs.BlackCircle);

            Region tuiRegion = new (ViewportToScreen (tTop));
            tuiRegion.Union (ViewportToScreen (tStem));
            tuiRegion.Union (ViewportToScreen (uLeft));
            tuiRegion.Union (ViewportToScreen (uBottom));
            tuiRegion.Union (ViewportToScreen (uRight));
            tuiRegion.Union (ViewportToScreen (iTop));
            tuiRegion.Union (ViewportToScreen (iStem));
            tuiRegion.Union (ViewportToScreen (iBottom));

            // Register the drawn region for "TUI" to enable transparency effects
            e.DrawContext?.AddDrawnRegion (tuiRegion);
        }

        /// <inheritdoc/>
        protected override bool OnDrawingContent (DrawContext? context)
        {
            base.OnDrawingContent (context);

            // Draw "Hi" text using rectangular regions
            // Letter "H"
            Rectangle hLeft = new (5, 5, 2, 10); // Left vertical bar
            Rectangle hMiddle = new (7, 9, 3, 2); // Middle horizontal bar
            Rectangle hRight = new (10, 5, 2, 10); // Right vertical bar

            // Letter "i" (with some space between H and i)
            Rectangle iDot = new (15, 5, 2, 2); // Dot on top
            Rectangle iStem = new (15, 9, 2, 6); // Vertical stem

            // Draw "Hi" using the Highlight attribute
            SetAttributeForRole (VisualRole.Highlight);
            FillRect (hLeft, Glyphs.BlackCircle);
            FillRect (hMiddle, Glyphs.BlackCircle);
            FillRect (hRight, Glyphs.BlackCircle);
            FillRect (iDot, Glyphs.BlackCircle);
            FillRect (iStem, Glyphs.BlackCircle);

            // Register the drawn region for "Hi" to enable transparency effects
            Region hiRegion = new (ViewportToScreen (hLeft));
            hiRegion.Union (ViewportToScreen (hMiddle));
            hiRegion.Union (ViewportToScreen (hRight));
            hiRegion.Union (ViewportToScreen (iDot));
            hiRegion.Union (ViewportToScreen (iStem));
            context?.AddDrawnRegion (hiRegion);

            // Return false to allow DrawingContent event to fire
            return false;
        }

        protected override bool OnRenderingLineCanvas ()
        {
            // Draw "dotnet" using LineCanvas
            Point screenPos = ViewportToScreen (new Point (7, 16));
            DrawDotnet (LineCanvas, screenPos.X, screenPos.Y, LineStyle.Single, GetAttributeForRole (VisualRole.Normal));

            return false;
        }

        /// <inheritdoc/>
        protected override bool OnClearingViewport () => false;

        /// <inheritdoc/>
        protected override bool OnMouseEvent (Mouse mouse) => false;

        /// <summary>
        ///     Draws "dotnet" text using LineCanvas. The 'd' is 8 cells high.
        /// </summary>
        /// <param name="canvas">The LineCanvas to draw on</param>
        /// <param name="x">Starting X position</param>
        /// <param name="y">Starting Y position</param>
        /// <param name="style">Line style to use</param>
        /// <param name="attribute">Optional attribute for the lines</param>
        private void DrawDotnet (LineCanvas canvas, int x, int y, LineStyle style = LineStyle.Single, Attribute? attribute = null)
        {
            int currentX = x;
            var letterHeight = 8;
            var letterSpacing = 2;

            // Letter 'd' - lowercase, height 8
            // Vertical stem on right (goes up full 8 cells)
            canvas.AddLine (new Point (currentX + 3, y), letterHeight, Orientation.Vertical, style, attribute);

            // Top horizontal
            canvas.AddLine (new Point (currentX, y + 3), 4, Orientation.Horizontal, style, attribute);

            // Left vertical (only bottom 5 cells, leaving top 3 for ascender space)
            canvas.AddLine (new Point (currentX, y + 3), 5, Orientation.Vertical, style, attribute);

            // Bottom horizontal
            canvas.AddLine (new Point (currentX, y + 7), 4, Orientation.Horizontal, style, attribute);
            currentX += 4 + letterSpacing;

            // Letter 'o' - height 5 (x-height)
            int oY = y + 3; // Align with x-height (leaving 3 cells for ascenders)

            // Top
            canvas.AddLine (new Point (currentX, oY), 4, Orientation.Horizontal, style, attribute);

            // Left
            canvas.AddLine (new Point (currentX, oY), 5, Orientation.Vertical, style, attribute);

            // Right
            canvas.AddLine (new Point (currentX + 3, oY), 5, Orientation.Vertical, style, attribute);

            // Bottom
            canvas.AddLine (new Point (currentX, oY + 4), 4, Orientation.Horizontal, style, attribute);
            currentX += 4 + letterSpacing;

            // Letter 't' - height 7 (has ascender above x-height)
            int tY = y + 1; // Starts 1 cell above x-height

            // Vertical stem
            canvas.AddLine (new Point (currentX + 1, tY), 7, Orientation.Vertical, style, attribute);

            // Top cross bar (at x-height)
            canvas.AddLine (new Point (currentX, tY + 2), 3, Orientation.Horizontal, style, attribute);

            // Bottom horizontal (foot)
            canvas.AddLine (new Point (currentX + 1, tY + 6), 2, Orientation.Horizontal, style, attribute);
            currentX += 3 + letterSpacing;

            // Letter 'n' - height 5 (x-height)
            int nY = y + 3;

            // Left vertical
            canvas.AddLine (new Point (currentX, nY), 5, Orientation.Vertical, style, attribute);

            // Top horizontal
            canvas.AddLine (new Point (currentX + 1, nY), 3, Orientation.Horizontal, style, attribute);

            // Right vertical
            canvas.AddLine (new Point (currentX + 3, nY), 5, Orientation.Vertical, style, attribute);
            currentX += 4 + letterSpacing;

            // Letter 'e' - height 5 (x-height)
            int eY = y + 3;

            // Top
            canvas.AddLine (new Point (currentX, eY), 4, Orientation.Horizontal, style, attribute);

            // Left
            canvas.AddLine (new Point (currentX, eY), 5, Orientation.Vertical, style, attribute);

            // Right
            canvas.AddLine (new Point (currentX + 3, eY), 3, Orientation.Vertical, style, attribute);

            // Middle horizontal bar
            canvas.AddLine (new Point (currentX, eY + 2), 4, Orientation.Horizontal, style, attribute);

            // Bottom
            canvas.AddLine (new Point (currentX, eY + 4), 4, Orientation.Horizontal, style, attribute);
            currentX += 4 + letterSpacing;

            // Letter 't' - height 7 (has ascender above x-height) - second 't'
            int t2Y = y + 1;

            // Vertical stem
            canvas.AddLine (new Point (currentX + 1, t2Y), 7, Orientation.Vertical, style, attribute);

            // Top cross bar (at x-height)
            canvas.AddLine (new Point (currentX, t2Y + 2), 3, Orientation.Horizontal, style, attribute);

            // Bottom horizontal (foot)
            canvas.AddLine (new Point (currentX + 1, t2Y + 6), 2, Orientation.Horizontal, style, attribute);
        }
    }
}
