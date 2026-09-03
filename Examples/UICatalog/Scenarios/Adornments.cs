#nullable enable
using Terminal.Gui.Editor;

// ReSharper disable AccessToDisposedClosure

// ReSharper disable AssignNullToNotNullAttribute

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Adornments Demo", "Demonstrates Margin, Border, and Padding on Views.")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Adornments")]
public class Adornments : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();
        appWindow.BorderStyle = LineStyle.None;

        AdornmentsEditor adornmentsEditor = new ()
        {
            BorderStyle = LineStyle.Single,
            AutoSelectViewToEdit = true,

            // This is for giggles, to show that the editor can be moved around.
            Arrangement = ViewArrangement.Movable,
            X = Pos.AnchorEnd ()
        };

        adornmentsEditor.Border.Thickness = new Thickness (1, 2, 1, 1);

        ViewportSettingsEditor viewportSettingsEditor = new ()
        {
            BorderStyle = LineStyle.Single,
            AutoSelectViewToEdit = true,

            // This is for giggles, to show that the editor can be moved around.
            Arrangement = ViewArrangement.Movable,
            Y = Pos.AnchorEnd ()
        };

        viewportSettingsEditor.Border.Thickness = new Thickness (1, 2, 1, 1);

        Button appButton = new () { X = Pos.Center (), Y = 1, Text = "_SubView of Window" };
        appWindow.Add (appButton);

        Window window = new ()
        {
            Title = "The _Window - The Title is long",
            Arrangement = ViewArrangement.Overlapped | ViewArrangement.Movable | ViewArrangement.Resizable,
            X = 5,
            Y = 5,
            Width = Dim.Fill (adornmentsEditor) - 10,
            Height = Dim.Fill (viewportSettingsEditor) - 10
        };
        appWindow.Add (window);

        TextField tf1 = new () { Width = 10, Text = "TextField" };
        ColorPicker16 color = new () { Title = "BG", BoxHeight = 1, BoxWidth = 1, X = Pos.AnchorEnd () };
        color.BorderStyle = LineStyle.RoundedDotted;

        color.ValueChanged += (_, e) =>
                              {
                                  color.SuperView!.SetScheme (new Scheme (color.SuperView.GetScheme ())
                                  {
                                      Normal =
                                          new Attribute (color.SuperView.GetAttributeForRole (VisualRole.Normal).Foreground,
                                                         e.NewValue,
                                                         color.SuperView.GetAttributeForRole (VisualRole.Normal).Style)
                                  });
                              };

        Button button = new () { X = Pos.Center (), Y = Pos.Center (), Text = "Press me!" };

        button.Accepted += (_, _) => MessageBox.Query (appWindow.App!, "Hi", $"Am I a {window.GetType ().Name}?", Strings.btnNo, Strings.btnYes);

        Editor editor = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (button),
            Title = "Title",
            Width = 40,
            Height = 6 // TODO: Use Dim.Auto
        };
        editor.Text = "I have a 3 row top border.\nMy border inherits from the SuperView.";
        editor.Border.Thickness = new Thickness (1, 3, 1, 1);

        Button btnButtonInWindow = new () { X = Pos.AnchorEnd (), Y = Pos.AnchorEnd (), Text = "Button" };

        Label labelAnchorEnd = new ()
        {
            Y = Pos.AnchorEnd (),
            Width = 40,
            Height = Dim.Percent (20),
            Text = "Label\nY=AnchorEnd(),Height=Dim.Percent(10)",
            SchemeName = "Dialog"
        };

        window.Margin.GetOrCreateView ();
        window.Margin.View?.Text = "Margin Text";
        window.Margin.Thickness = new Thickness (0);

        window.Border.GetOrCreateView ();

        // window.Border.View.Text = "Border Text";
        window.Border.Thickness = new Thickness (3);

        // window.Border.View?.SetScheme (SchemeManager.GetScheme (Schemes.Dialog));

        window.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        window.BorderStyle = LineStyle.Rounded;

        // Enable dragging the tab title to slide it by changing TabOffset.
        // Hook into TitleView.MouseEvent — it fires before the Arranger, so no conflict.
        Point? dragStart = null;
        var dragStartOffset = 0;
        var tabDragHooked = false;

        window.DrawComplete += (_, _) =>
                               {
                                   if (tabDragHooked || ((BorderView)window.Border.View!).TitleView is not { } tabTitle)
                                   {
                                       return;
                                   }

                                   tabDragHooked = true;
                                   var bv = (BorderView)window.Border.View!;

                                   tabTitle.MouseEvent += (_, mouse) =>
                                                          {
                                                              // Start drag
                                                              if (!dragStart.HasValue && mouse.Flags.HasFlag (MouseFlags.LeftButtonPressed))
                                                              {
                                                                  dragStart = mouse.ScreenPosition;
                                                                  dragStartOffset = bv.TabOffset;
                                                                  window.App?.Mouse.GrabMouse (tabTitle);
                                                                  mouse.Handled = true;
                                                              }

                                                              // Dragging
                                                              if (dragStart.HasValue
                                                                  && mouse.Flags is (MouseFlags.LeftButtonPressed | MouseFlags.PositionReport))
                                                              {
                                                                  int delta = bv.TabSide is Side.Top or Side.Bottom
                                                                                  ? mouse.ScreenPosition.X - dragStart.Value.X
                                                                                  : mouse.ScreenPosition.Y - dragStart.Value.Y;

                                                                  int tabLen = bv.TabLength ?? 0;

                                                                  // Content border edge length (the side the tab slides along)
                                                                  int edgeLen = bv.TabSide is Side.Top or Side.Bottom
                                                                                    ? window.Frame.Width
                                                                                      - window.Border.Thickness.Left
                                                                                      - window.Border.Thickness.Right
                                                                                    : window.Frame.Height
                                                                                      - window.Border.Thickness.Top
                                                                                      - window.Border.Thickness.Bottom;

                                                                  int newOffset = Math.Clamp (dragStartOffset + delta, 1 - tabLen, edgeLen - 1);

                                                                  bv.TabOffset = newOffset;
                                                                  mouse.Handled = true;
                                                              }

                                                              // Release
                                                              if (!mouse.Flags.HasFlag (MouseFlags.LeftButtonReleased) || !dragStart.HasValue)
                                                              {
                                                                  return;
                                                              }
                                                              dragStart = null;
                                                              window.App?.Mouse.UngrabMouse ();
                                                              mouse.Handled = true;
                                                          };
                               };
        window.Padding.View?.Text = "Padding Text line 1\nPadding Text line 3\nPadding Text line 3\nPadding Text line 4\nPadding Text line 5";
        window.Padding.Thickness = new Thickness (1);
        window.Padding.View?.SetScheme (SchemeManager.GetScheme (Schemes.Menu));
        window.Padding.View?.CanFocus = true;

        Label longLabel = new () { X = 40, Y = 5, Title = "This is long text (in a label) that should clip." };
        longLabel.TextFormatter.WordWrap = true;
        window.Add (tf1, color, button, editor, btnButtonInWindow, labelAnchorEnd, longLabel);

        window.Initialized += (_, _) =>
                              {
                                  adornmentsEditor.ViewToEdit = window;

                                  adornmentsEditor.ShowViewIdentifier = true;

                                  // NOTE: Adding SubViews to Margin is not supported

                                  Button btnButtonInBorder = new () { X = Pos.Center (), Y = Pos.AnchorEnd (), Text = "Button in Border Y = AnchorEnd" };

                                  btnButtonInBorder.Accepting += (_, args) =>
                                                                 {
                                                                     MessageBox.Query (appWindow.App!, 20, 7, "Hi", "Button in Border Pressed!", "Ok");
                                                                     args.Handled = true;
                                                                 };
                                  window.Border.GetOrCreateView ().Add (btnButtonInBorder);

                                  Label labelInPadding = new () { X = 0, Y = 1, Title = "_Text:" };
                                  window.Padding.GetOrCreateView ().Add (labelInPadding);

                                  TextField textFieldInPadding = new ()
                                  {
                                      X = Pos.Right (labelInPadding) + 1, Y = Pos.Top (labelInPadding), Width = 10, Text = "text (Y = 1)"
                                  };

                                  textFieldInPadding.Accepting +=
                                      (_, _) => MessageBox.Query (appWindow.App!, 20, 7, "TextField", textFieldInPadding.Text, "Ok");
                                  window.Padding.GetOrCreateView ().Add (textFieldInPadding);

                                  Button btnButtonInPadding = new () { X = Pos.Center (), Y = 1, Text = "_Button in Padding Y = 1", CanFocus = true };

                                  btnButtonInPadding.Accepting += (_, args) =>
                                                                  {
                                                                      MessageBox.Query (appWindow.App!, 20, 7, "Hi", "Button in Padding Pressed!", "Ok");
                                                                      args.Handled = true;
                                                                  };
                                  window.Padding.GetOrCreateView ().Add (btnButtonInPadding);

#if SUBVIEW_BASED_BORDER
                                btnButtonInPadding.Border.CloseButton.Visible = true;

                                view.Border.CloseButton.Visible = true;
                                view.Border.CloseButton.Accept += (_, _) =>
                                                                  {
                                                                      MessageBox.Query (20, 7, "Hi", "Window Close Button Pressed!", "Ok");
                                                                      e.Handled = true;
                                                                  };

                                view.Accept += (_, _) => MessageBox.Query (20, 7, "Hi", "Window Close Button Pressed!", "Ok");
#endif
                              };

        adornmentsEditor.AutoSelectViewToEdit = true;
        adornmentsEditor.AutoSelectSuperView = window;
        adornmentsEditor.AutoSelectAdornments = true;

        viewportSettingsEditor.AutoSelectViewToEdit = true;
        viewportSettingsEditor.AutoSelectSuperView = window;
        viewportSettingsEditor.AutoSelectAdornments = true;
        viewportSettingsEditor.ShowViewIdentifier = true;

        appWindow.Add (adornmentsEditor, viewportSettingsEditor);

        appWindow.DrawingContent += (_, e) =>
                                    {
                                        appWindow.FillRect (appWindow.Viewport, Glyphs.Diamond);
                                        e.Cancel = true;
                                    };

        app.Run (appWindow);
    }
}
