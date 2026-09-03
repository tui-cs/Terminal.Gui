using System.Collections;
using System.Collections.ObjectModel;
using System.Text;
using UnitTests;

namespace ViewsTests;

public class DropDownListTests (ITestOutputHelper output)
{
    [Fact]
    public void AltDown_OpensDropdown ()
    {
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"])) };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.CursorDown.WithAlt);

        IPopoverView? dropDownListPopover = FindDropDownPopover (app);
        Assert.NotNull (dropDownListPopover);
        Assert.True (dropDownListPopover.Visible);

        dropdown.Dispose ();
    }

    [Fact]
    public void Constructor_HasAltDownKeyBinding ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new ();

        Assert.True (dropdown.KeyBindings.TryGet (Key.CursorDown.WithAlt, out _));
    }

    [Fact]
    public void Constructor_HasF4KeyBinding ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new ();

        Assert.True (dropdown.KeyBindings.TryGet (Key.F4, out _));
    }

    [Fact]
    public void Constructor_HasMouseBinding ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new ();

        Assert.True (dropdown.MouseBindings.TryGet (MouseFlags.LeftButtonPressed, out _));
    }

    [Fact]
    public void Constructor_HasToggleButtonInPadding ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new ();

        Button? toggleButton = dropdown.Padding.View!.SubViews.OfType<Button> ().FirstOrDefault ();
        Assert.NotNull (toggleButton);
        Assert.False (toggleButton.CanFocus);
        Assert.Equal (TabBehavior.NoStop, toggleButton.TabStop);
    }

    [Fact]
    public void Constructor_InitializesDefaults ()
    {
        DropDownList dropdown = new ();

        Assert.NotNull (dropdown);
        Assert.True (dropdown.ReadOnly);
        Assert.Null (dropdown.Source);
    }

    [Fact]
    public void Constructor_PaddingRightIsOneForToggleButton ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new ();

        Assert.Equal (1, dropdown.Padding.Thickness.Right);
    }

    [Fact]
    public void Constructor_ToggleButtonHasDownArrow ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new ();

        Button? toggleButton = dropdown.Padding.View!.SubViews.OfType<Button> ().FirstOrDefault ();
        Assert.NotNull (toggleButton);
        Assert.Equal (Glyphs.DownArrow.ToString (), toggleButton.Text);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow ()
    {
        // Claude - Opus 4.6
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1"])) };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.Dispose ();

        // Second dispose should not throw
        dropdown.Dispose ();
    }

    [Fact]
    public void Dispose_CleansUpResources ()
    {
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"])) };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        // Open dropdown
        dropdown.NewKeyDownEvent (Key.F4);

        Assert.True (app.Popovers!.Popovers.Any ());

        dropdown.Dispose ();

        Assert.Empty (app.Popovers.Popovers);
    }

    [Fact]
    public void Dispose_WhenNotFocused_DoesNotThrow ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1"])) };
        dropdown.BeginInit ();
        dropdown.EndInit ();

        // Dispose without ever focusing - should not throw
        dropdown.Dispose ();
    }

    [Fact]
    public void EditableMode_AllowsTextEditing ()
    {
        DropDownList dropdown = new () { ReadOnly = false, Text = "Initial" };
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.A);

        Assert.NotEqual ("Initial", dropdown.Text);
    }

    [Fact]
    public void EmptySource_OpensWithoutCrash ()
    {
        // Claude - Opus 4.6
        using IApplication app = Application.Create ();

        ObservableCollection<string> items = [];
        DropDownList dropdown = new () { Source = new ListWrapper<string> (items), ReadOnly = true };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.F4);

        IPopoverView? popover = FindDropDownPopover (app);
        Assert.NotNull (popover);
        Assert.True (popover.Visible);

        dropdown.Dispose ();
    }

    [Fact]
    public void EnableForDesign_PopulatesSampleData ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new ();

        bool result = dropdown.EnableForDesign ();

        Assert.True (result);
        Assert.NotNull (dropdown.Source);
        Assert.True (dropdown.Source.Count > 0);
        Assert.Equal ("Germany", dropdown.Value);
    }

    [Fact]
    public void F4_OpensDropdown ()
    {
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"])) };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.F4);

        IPopoverView? dropDownListPopover = FindDropDownPopover (app);
        Assert.NotNull (dropDownListPopover);
        Assert.True (dropDownListPopover.Visible);

        dropdown.Dispose ();
    }

    [Fact]
    public void FocusGain_AutoRegistersPopover ()
    {
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"])) };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();

        // Initially, no popovers registered (not focused yet)
        Assert.Empty (app.Popovers!.Popovers);

        // Set focus - should auto-register
        dropdown.SetFocus ();

        Assert.True (app.Popovers.Popovers.Count () > 0);

        dropdown.Dispose ();
    }

    [Fact]
    public void IValue_InterfaceWorks ()
    {
        DropDownList dropdown = new () { Text = "Initial" };

        IValue<string> value = dropdown;

        Assert.Equal ("Initial", value.Value);

        value.Value = "Updated";

        Assert.Equal ("Updated", dropdown.Text);
        Assert.Equal ("Updated", value.Value);
    }

    [Fact]
    public void NullSource_ToggleDoesNotCrash ()
    {
        // Claude - Opus 4.6
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { ReadOnly = true };
        Assert.Null (dropdown.Source);

        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        // Should not throw with null source
        dropdown.NewKeyDownEvent (Key.F4);

        dropdown.Dispose ();
    }

    [Fact]
    public void OpenDropDown_RegistersPopover_IfNotAlreadyRegistered ()
    {
        // Claude - Opus 4.6
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"])) };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();

        // Don't call SetFocus - popover won't be registered via OnHasFocusChanging

        // Open dropdown via command - it should auto-register
        dropdown.InvokeCommand (Command.Toggle);

        IPopoverView? popover = FindDropDownPopover (app);
        Assert.NotNull (popover);

        dropdown.Dispose ();
    }

    [Fact]
    public void PreSelection_EmptyText_DoesNotCrash ()
    {
        // Claude - Opus 4.6
        using IApplication app = Application.Create ();

        ObservableCollection<string> items = ["Item1", "Item2"];
        DropDownList dropdown = new () { Source = new ListWrapper<string> (items), ReadOnly = true, Text = "" };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.F4);

        IPopoverView? popover = FindDropDownPopover (app);
        Assert.NotNull (popover);
        Assert.True (popover.Visible);

        dropdown.Dispose ();
    }

    [Fact]
    public void PreSelection_NoMatchingItem_KeepsDefault ()
    {
        // Claude - Opus 4.6
        using IApplication app = Application.Create ();

        ObservableCollection<string> items = ["Item1", "Item2", "Item3"];
        DropDownList dropdown = new () { Source = new ListWrapper<string> (items), ReadOnly = true, Text = "NonExistent" };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.F4);

        IPopoverView? popover = FindDropDownPopover (app);
        ListView? listView = (popover as Popover<ListView, string?>)?.ContentView;
        Assert.NotNull (listView);

        // SelectedItem should not have been changed to match any specific item
        // since "NonExistent" doesn't appear in the list
        Assert.True (listView.SelectedItem is null or 0);

        dropdown.Dispose ();
    }

    [Fact]
    public void PreSelection_SelectsLastItem ()
    {
        // Claude - Opus 4.6
        using IApplication app = Application.Create ();

        ObservableCollection<string> items = ["First", "Second", "Third"];
        DropDownList dropdown = new () { Source = new ListWrapper<string> (items), ReadOnly = true, Text = "Third" };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.F4);

        IPopoverView? popover = FindDropDownPopover (app);
        ListView? listView = (popover as Popover<ListView, string?>)?.ContentView;
        Assert.NotNull (listView);
        Assert.Equal (2, listView.SelectedItem);

        dropdown.Dispose ();
    }

    [Fact]
    public void PreSelection_SelectsMatchingItem ()
    {
        using IApplication app = Application.Create ();

        ObservableCollection<string> items = ["Item1", "Item2", "Item3"];
        DropDownList dropdown = new () { Source = new ListWrapper<string> (items), ReadOnly = true, Text = "Item2" };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.F4);

        IPopoverView? popover = FindDropDownPopover (app);
        Assert.NotNull (popover);

        ListView? listView = (popover as Popover<ListView, string?>)?.ContentView;

        Assert.NotNull (listView);
        Assert.Equal (1, listView.SelectedItem);

        dropdown.Dispose ();
    }

    [Fact]
    public void ReadOnly_CanBeSetToFalse ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new () { ReadOnly = false };

        Assert.False (dropdown.ReadOnly);
    }

    [Fact]
    public void ReadOnly_DefaultIsTrue ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new ();

        Assert.True (dropdown.ReadOnly);
    }

    [Fact]
    public void ReadOnlyMode_PreventsTextEditing ()
    {
        DropDownList dropdown = new () { ReadOnly = true, Text = "Initial" };
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.A);

        Assert.Equal ("Initial", dropdown.Text);
    }

    [Fact]
    public void Source_GetSet ()
    {
        DropDownList dropdown = new ();
        ObservableCollection<string> items = ["Item1", "Item2", "Item3"];
        ListWrapper<string> source = new (items);

        dropdown.Source = source;

        Assert.Equal (source, dropdown.Source);
    }

    [Fact]
    public void Source_SetToNull_Works ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new ();
        dropdown.Source = new ListWrapper<string> (["Item1", "Item2"]);
        Assert.NotNull (dropdown.Source);

        dropdown.Source = null;

        Assert.Null (dropdown.Source);
    }

    [Fact]
    public void Text_SetDirectly_UpdatesValue ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Apple", "Banana", "Cherry"])) };

        dropdown.Text = "Banana";

        Assert.Equal ("Banana", dropdown.Text);
        Assert.Equal ("Banana", dropdown.Value);
    }

    [Fact]
    public void ToggleButton_ClickOpensDropdown ()
    {
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"])) };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        Button? toggleButton = dropdown.Padding.View!.SubViews.OfType<Button> ().FirstOrDefault ();
        Assert.NotNull (toggleButton);

        toggleButton.InvokeCommand (Command.Accept);

        IPopoverView? dropDownListPopover = FindDropDownPopover (app);
        Assert.NotNull (dropDownListPopover);
        Assert.True (dropDownListPopover.Visible);

        dropdown.Dispose ();
    }

    [Fact]
    public void ToggleCommand_OpenCloseReopen ()
    {
        // Claude - Opus 4.6
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["A", "B"])) };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        // Open
        dropdown.InvokeCommand (Command.Toggle);
        IPopoverView? popover = FindDropDownPopover (app);
        Assert.True (popover?.Visible);

        // Access the ListView (popover is Popover<ListView, string?>)
        ListView? listView = (popover as Popover<ListView, string?>)?.ContentView;
        Assert.NotNull (listView);

        // Close
        dropdown.InvokeCommand (Command.Toggle);
        Assert.False (popover?.Visible);

        // Re-open
        dropdown.InvokeCommand (Command.Toggle);
        Assert.True (popover?.Visible);

        dropdown.Dispose ();
    }

    [Fact]
    public void ToggleCommand_OpensAndClosesDropdown ()
    {
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"])) };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        // Open with F4
        dropdown.NewKeyDownEvent (Key.F4);

        IPopoverView? dropDownListPopover = FindDropDownPopover (app);
        Assert.NotNull (dropDownListPopover);
        Assert.True (dropDownListPopover.Visible);

        // Close with F4 again
        dropdown.NewKeyDownEvent (Key.F4);

        Assert.False (dropDownListPopover.Visible);

        dropdown.Dispose ();
    }

    [Fact]
    public void Value_SetDirectly_UpdatesText ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Apple", "Banana", "Cherry"])) };

        dropdown.Value = "Cherry";

        Assert.Equal ("Cherry", dropdown.Text);
        Assert.Equal ("Cherry", dropdown.Value);
    }

    [Fact]
    public void ValueChanged_RaisedMultipleTimes ()
    {
        // Claude - Opus 4.6
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2", "Item3"])), ReadOnly = true };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        var changeCount = 0;

        dropdown.ValueChanged += (_, _) => { changeCount++; };

        dropdown.Text = "Item1";
        dropdown.Text = "Item2";
        dropdown.Text = "Item3";

        Assert.Equal (3, changeCount);

        dropdown.Dispose ();
    }

    [Fact]
    public void ValueChanged_RaisedOnSelection ()
    {
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2", "Item3"])), ReadOnly = true };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        string? oldValue = null;
        string? newValue = null;
        var valueChangedCalled = false;

        dropdown.ValueChanged += (_, e) =>
                                 {
                                     valueChangedCalled = true;
                                     oldValue = e.OldValue;
                                     newValue = e.NewValue;
                                 };

        dropdown.Text = "Item2";

        Assert.True (valueChangedCalled);
        Assert.Empty (oldValue ?? "");
        Assert.Equal ("Item2", newValue);

        dropdown.Dispose ();
    }

    [Fact]
    public void ValueChanging_CanCancel ()
    {
        using IApplication app = Application.Create ();

        DropDownList dropdown = new ()
        {
            Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2", "Item3"])), ReadOnly = true, Text = "Item1"
        };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.ValueChanging += (_, e) => { e.Handled = true; };

        dropdown.Text = "Item2";

        Assert.Equal ("Item1", dropdown.Text);

        dropdown.Dispose ();
    }

    [Fact]
    public void ValueChanging_CancelPreventsValueChanged ()
    {
        // Claude - Opus 4.6
        using IApplication app = Application.Create ();

        DropDownList dropdown = new ()
        {
            Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"])), ReadOnly = true, Text = "Item1"
        };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        var valueChangedCalled = false;

        dropdown.ValueChanging += (_, e) => { e.Handled = true; };
        dropdown.ValueChanged += (_, _) => { valueChangedCalled = true; };

        dropdown.Text = "Item2";

        Assert.False (valueChangedCalled);
        Assert.Equal ("Item1", dropdown.Text);

        dropdown.Dispose ();
    }

    [Fact]
    public void ValueChanging_RaisedBeforeChange ()
    {
        using IApplication app = Application.Create ();

        DropDownList dropdown = new ()
        {
            Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2", "Item3"])), ReadOnly = true, Text = "Item1"
        };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        string? currentValue = null;
        string? newValue = null;
        var valueChangingCalled = false;

        dropdown.ValueChanging += (_, e) =>
                                  {
                                      valueChangingCalled = true;
                                      currentValue = e.CurrentValue;
                                      newValue = e.NewValue;
                                  };

        dropdown.Text = "Item2";

        Assert.True (valueChangingCalled);
        Assert.Equal ("Item1", currentValue);
        Assert.Equal ("Item2", newValue);

        dropdown.Dispose ();
    }

    // Claude - Opus 4.5

    /// <summary>
    ///     Regression test: clicking on a list item in the popover should close the popover
    ///     and select the item, but the synthesized Click event must NOT flow through to views underneath.
    ///     The bug: Released on the ListView fires Activate → popover closes. The subsequent synthesized
    ///     Clicked event then finds no popover and falls through to whatever view is below.
    /// </summary>
    [Fact]
    public void ClickListItem_DoesNotActivateViewBelow ()
    {
        // Claude - Opus 4.6
        using IDisposable logging = TestLogging.Verbose (output, TraceCategory.Mouse | TraceCategory.Command);
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 15);

        using Runnable top = new ();
        SessionToken? token = app.Begin (top);

        // DropDownList at the top of the screen
        ObservableCollection<string> items = ["Apple", "Banana", "Cherry"];

        DropDownList dropdown = new ()
        {
            X = 0,
            Y = 0,
            Width = 20,
            Source = new ListWrapper<string> (items),
            ReadOnly = true
        };

        top.Add (dropdown);
        top.Layout ();

        // Give focus to the dropdown and open the popover via key injection
        dropdown.SetFocus ();
        app.InjectKey (Key.F4);

        IPopoverView? popover = FindDropDownPopover (app);
        Assert.NotNull (popover);
        Assert.True (popover.Visible, "Popover should be open");

        // Track whether ANY mouse Click event leaks to the Runnable after the popover closes.
        // The Clicked event at the popover item position should NOT reach views below.
        var runnableReceivedClick = false;

        top.MouseEvent += (_, e) =>
                          {
                              if (e.Flags.HasFlag (MouseFlags.LeftButtonClicked))
                              {
                                  runnableReceivedClick = true;
                              }
                          };

        // Click on the third item ("Cherry") at screen Y=3
        // (screen Y=1 = item 0 "Apple", Y=2 = item 1 "Banana", Y=3 = item 2 "Cherry")
        Point clickPos = new (2, 3);

        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };
        IInputInjector injector = app.GetInputInjector ();

        // Simulate click via input injection: Pressed → Released (system synthesizes Click)
        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, options);

        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) },
                              options);

        // The popover should have closed and the item should be selected
        Assert.False (popover.Visible, "Popover should be closed after clicking a list item");
        Assert.Equal ("Cherry", dropdown.Text);

        // CRITICAL: The synthesized Clicked event should NOT leak through to the Runnable
        Assert.False (runnableReceivedClick, "Runnable should NOT receive the Clicked event that was synthesized after the popover closed");

        app.End (token!);
    }

    [Fact] // Copilot
    public void Scrolling_TallDropdown_TopItemsDraw ()
    {
        // Regression test: when a DropDownList's popover list exceeds available screen space
        // and the user scrolls down, the items at the top of the visible area must still render.
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (30, 10);

        using Runnable top = new ();
        SessionToken? token = app.Begin (top);

        // Create a DropDownList with many items (more than the 10-row screen)
        ObservableCollection<string> items =
        [
            "Item_00",
            "Item_01",
            "Item_02",
            "Item_03",
            "Item_04",
            "Item_05",
            "Item_06",
            "Item_07",
            "Item_08",
            "Item_09",
            "Item_10",
            "Item_11",
            "Item_12",
            "Item_13",
            "Item_14",
            "Item_15",
            "Item_16",
            "Item_17",
            "Item_18",
            "Item_19"
        ];

        DropDownList dropdown = new ()
        {
            X = 0,
            Y = 0,
            Source = new ListWrapper<string> (items),
            ReadOnly = true,
            Text = "Item_00"
        };

        top.Add (dropdown);
        app.LayoutAndDraw ();

        // Open the dropdown
        dropdown.SetFocus ();
        app.InjectKey (Key.F4);
        app.LayoutAndDraw ();

        // Verify the popover is open
        IPopoverView? popover = FindDropDownPopover (app);
        Assert.NotNull (popover);
        Assert.True (popover.Visible);

        // Get the ListView from the popover
        Popover<ListView, string?>? typedPopover = popover as Popover<ListView, string?>;
        Assert.NotNull (typedPopover);
        ListView? listView = typedPopover.ContentView;
        Assert.NotNull (listView);

        // Verify the scrollbar is visible (content exceeds viewport)
        Assert.True (listView.VerticalScrollBar.Visible, "ScrollBar should be visible for tall dropdown");
        Assert.Equal (0, listView.Viewport.Y);

        // Scroll down 5 items
        listView.VerticalScrollBar.Value = 5;

        // Capture buffer when the Runnable starts drawing (AFTER the Popover drew)
        top.DrawingContent += (_, _) =>
                              {
                                  Cell [,]? buf = app.Driver.GetOutputBuffer ().Contents;

                                  if (buf is null)
                                  {
                                      return;
                                  }

                                  output.WriteLine ("Buffer when Runnable draws (after Popover):");

                                  for (var r = 0; r < 10; r++)
                                  {
                                      var t = "";

                                      for (var c = 0; c < 10; c++)
                                      {
                                          t += buf [r, c].Grapheme;
                                      }

                                      output.WriteLine ($"  Row {r}: '{t}'");
                                  }

                                  output.WriteLine ("  Clip: not available");
                              };

        app.LayoutAndDraw ();

        Cell [,]? contents = app.Driver.GetOutputBuffer ().Contents;
        Assert.NotNull (contents);

        output.WriteLine ("Final buffer:");

        for (var row = 0; row < 10; row++)
        {
            var rowText = "";

            for (var col = 0; col < 10; col++)
            {
                rowText += contents [row, col].Grapheme;
            }

            output.WriteLine ($"  Row {row}: '{rowText}'");
        }

        var topRowText = "";

        for (var col = 0; col < 7; col++)
        {
            topRowText += contents [1, col].Grapheme;
        }

        Assert.Contains ("Item_05", topRowText);

        app.End (token!);
    }

    // CoPilot - ChatGPT v4
    [Fact]
    public void VisiblePopover_Repositions_WhenTerminalIsResized ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 15);

        using Runnable top = new ();
        SessionToken? token = app.Begin (top);

        ObservableCollection<string> items = ["Alpha", "Beta", "Gamma"];

        DropDownList dropdown = new ()
        {
            X = Pos.Center (),
            Y = Pos.Center (),
            Width = 12,
            Source = new ListWrapper<string> (items),
            ReadOnly = true
        };

        top.Add (dropdown);
        app.LayoutAndDraw ();

        dropdown.SetFocus ();
        app.InjectKey (Key.F4);
        app.LayoutAndDraw ();

        Popover<ListView, string?>? popover = FindDropDownPopover (app) as Popover<ListView, string?>;
        Assert.NotNull (popover);
        Assert.True (popover.Visible);

        Rectangle initialListFrame = popover.ContentView!.FrameToScreen ();
        Rectangle initialDropDownFrame = dropdown.FrameToScreen ();

        // Act
        app.Driver.SetScreenSize (60, 25);
        app.LayoutAndDraw ();

        // Assert
        Rectangle resizedListFrame = popover.ContentView.FrameToScreen ();
        Rectangle resizedDropDownFrame = dropdown.FrameToScreen ();

        Assert.NotEqual (initialDropDownFrame.Location, resizedDropDownFrame.Location);
        Assert.Equal (resizedDropDownFrame.X, resizedListFrame.X);
        Assert.Equal (resizedDropDownFrame.Bottom, resizedListFrame.Y);
        Assert.NotEqual (initialListFrame.Location, resizedListFrame.Location);

        app.End (token!);
    }

    // CoPilot - ChatGPT v4
    [Fact]
    public void VisiblePopover_LayoutsHeight_WhenTerminalIsResized ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (30, 10);

        using Runnable top = new ();
        SessionToken? token = app.Begin (top);

        ObservableCollection<string> items =
        [
            "Item_00",
            "Item_01",
            "Item_02",
            "Item_03",
            "Item_04",
            "Item_05",
            "Item_06",
            "Item_07",
            "Item_08",
            "Item_09"
        ];

        DropDownList dropdown = new ()
        {
            X = 0,
            Y = 0,
            Width = 12,
            Source = new ListWrapper<string> (items),
            ReadOnly = true
        };

        top.Add (dropdown);
        app.LayoutAndDraw ();

        dropdown.SetFocus ();
        app.InjectKey (Key.F4);
        app.LayoutAndDraw ();

        Popover<ListView, string?>? popover = FindDropDownPopover (app) as Popover<ListView, string?>;
        Assert.NotNull (popover);
        Assert.True (popover.Visible);

        int initialHeight = popover.ContentView!.Frame.Height;
        Assert.Equal (9, initialHeight);

        // Act
        app.Driver.SetScreenSize (30, 5);
        app.LayoutAndDraw ();

        // Assert
        int resizedHeight = popover.ContentView.Frame.Height;
        Assert.Equal (4, resizedHeight);
        Assert.NotEqual (initialHeight, resizedHeight);

        app.End (token!);
    }

    [Fact]
    public void Space_OpensDropdown_WhenReadOnly ()
    {
        // Copilot
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"])), ReadOnly = true };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.Space);

        IPopoverView? popover = FindDropDownPopover (app);
        Assert.NotNull (popover);
        Assert.True (popover.Visible);

        dropdown.Dispose ();
    }

    [Fact]
    public void Down_SelectsNextItem_WhenClosed ()
    {
        // Copilot
        using IApplication app = Application.Create ();

        DropDownList dropdown = new ()
        {
            Source = new ListWrapper<string> (new ObservableCollection<string> (["Apple", "Banana", "Cherry"])),
            ReadOnly = true,
            Text = "Apple"
        };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.CursorDown);

        Assert.Equal ("Banana", dropdown.Text);

        dropdown.Dispose ();
    }

    [Fact]
    public void Up_SelectsPreviousItem_WhenClosed ()
    {
        // Copilot
        using IApplication app = Application.Create ();

        DropDownList dropdown = new ()
        {
            Source = new ListWrapper<string> (new ObservableCollection<string> (["Apple", "Banana", "Cherry"])),
            ReadOnly = true,
            Text = "Banana"
        };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.CursorUp);

        Assert.Equal ("Apple", dropdown.Text);

        dropdown.Dispose ();
    }

    [Fact]
    public void Down_DoesNothing_WhenAtEnd ()
    {
        // Copilot
        using IApplication app = Application.Create ();

        DropDownList dropdown = new ()
        {
            Source = new ListWrapper<string> (new ObservableCollection<string> (["Apple", "Banana", "Cherry"])),
            ReadOnly = true,
            Text = "Cherry"
        };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.CursorDown);

        Assert.Equal ("Cherry", dropdown.Text);

        dropdown.Dispose ();
    }

    [Fact]
    public void Up_DoesNothing_WhenAtStart ()
    {
        // Copilot
        using IApplication app = Application.Create ();

        DropDownList dropdown = new ()
        {
            Source = new ListWrapper<string> (new ObservableCollection<string> (["Apple", "Banana", "Cherry"])),
            ReadOnly = true,
            Text = "Apple"
        };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.CursorUp);

        Assert.Equal ("Apple", dropdown.Text);

        dropdown.Dispose ();
    }

    [Fact]
    public void CollectionNavigation_SelectsMatchingItem_WhenClosed ()
    {
        // Copilot
        using IApplication app = Application.Create ();

        DropDownList dropdown = new ()
        {
            Source = new ListWrapper<string> (new ObservableCollection<string> (["Apple", "Banana", "Cherry"])),
            ReadOnly = true,
            Text = "Apple"
        };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        // Type 'c' to navigate to "Cherry"
        dropdown.NewKeyDownEvent (Key.C);

        Assert.Equal ("Cherry", dropdown.Text);

        dropdown.Dispose ();
    }

    [Fact]
    public void Down_SelectsFirstItem_WhenNoSelection ()
    {
        // Copilot
        using IApplication app = Application.Create ();

        DropDownList dropdown = new ()
        {
            Source = new ListWrapper<string> (new ObservableCollection<string> (["Apple", "Banana", "Cherry"])),
            ReadOnly = true
        };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.CursorDown);

        Assert.Equal ("Apple", dropdown.Text);

        dropdown.Dispose ();
    }

    // Claude - Opus 4.8 - Regression test for https://github.com/gui-cs/Terminal.Gui/issues/5517
    [Fact]
    public void DroppedList_UsesBackgroundDistinctFromSuperView ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 15);

        using Runnable top = new ();
        SessionToken? token = app.Begin (top);

        ObservableCollection<string> items = ["Alpha", "Beta", "Gamma"];

        DropDownList dropdown = new ()
        {
            X = 0,
            Y = 0,
            Width = 12,
            Source = new ListWrapper<string> (items),
            ReadOnly = true
        };

        top.Add (dropdown);
        app.LayoutAndDraw ();

        dropdown.SetFocus ();
        app.InjectKey (Key.F4);
        app.LayoutAndDraw ();

        // Act
        Popover<ListView, string?>? popover = FindDropDownPopover (app) as Popover<ListView, string?>;
        Assert.NotNull (popover);
        Assert.True (popover.Visible);

        // Assert - the dropped list must "float" with its own background, distinct from the SuperView's.
        Attribute superViewNormal = dropdown.SuperView!.GetAttributeForRole (VisualRole.Normal);
        Attribute droppedListNormal = popover.ContentView!.GetAttributeForRole (VisualRole.Normal);

        Assert.NotEqual (superViewNormal.Background, droppedListNormal.Background);

        app.End (token!);
    }

    // CoPilot - Grok 4.6
    /// <summary>
    ///     Regression for https://github.com/tui-cs/Terminal.Gui/issues/5635.
    ///     Keyboard Enter with Accepting Handled and a focus move must not leave the dropped
    ///     list's text in cells the popover previously covered.
    /// </summary>
    [Fact]
    public void Enter_HandledAccepting_DoesNotLeaveStalePopoverCells ()
    {
        using HandledAcceptRepro repro = HandledAcceptRepro.Create ();

        repro.DropDown.SetFocus ();
        repro.App.InjectKey (Key.F4);
        repro.App.LayoutAndDraw ();

        IPopoverView? popover = FindDropDownPopover (repro.App);
        Assert.NotNull (popover);
        Assert.True (popover.Visible);

        string openBuffer = ReadScreen (repro.App);
        output.WriteLine ("Open:\n{0}", openBuffer);
        Assert.Contains ("Equipment", openBuffer, StringComparison.Ordinal);

        repro.App.InjectKey (Key.CursorDown);
        repro.App.InjectKey (Key.Enter);
        repro.App.LayoutAndDraw ();

        Assert.Equal ("Fuel/Energy", repro.DropDown.Text);
        Assert.True (repro.NextView.HasFocus);
        Assert.False (popover.Visible);
        Assert.NotEqual (popover, repro.App.Popovers?.GetActivePopover ());

        string closedBuffer = ReadScreen (repro.App);
        output.WriteLine ("Closed:\n{0}", closedBuffer);
        Assert.Contains ("COVERED-AREA-3", closedBuffer, StringComparison.Ordinal);
        Assert.DoesNotContain ("Equipment", closedBuffer, StringComparison.Ordinal);
    }

    // CoPilot - Grok 4.6
    /// <summary>
    ///     Regression for https://github.com/tui-cs/Terminal.Gui/issues/5635.
    ///     Mouse accept with Accepting Handled must still repaint the covered region.
    /// </summary>
    [Fact]
    public void MouseAccept_HandledAccepting_DoesNotLeaveStalePopoverCells ()
    {
        using HandledAcceptRepro repro = HandledAcceptRepro.Create ();

        repro.DropDown.SetFocus ();
        repro.App.InjectKey (Key.F4);
        repro.App.LayoutAndDraw ();

        Popover<ListView, string?>? popover = FindDropDownPopover (repro.App) as Popover<ListView, string?>;
        Assert.NotNull (popover);
        Assert.True (popover.Visible);
        Assert.NotNull (popover.ContentView);

        Rectangle listFrame = popover.ContentView.FrameToScreen ();
        Point clickPos = new (listFrame.X + 1, listFrame.Y + 1);

        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };
        IInputInjector injector = repro.App.GetInputInjector ();

        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, options);
        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) }, options);

        repro.App.LayoutAndDraw ();

        Assert.Equal ("Fuel/Energy", repro.DropDown.Text);
        Assert.True (repro.NextView.HasFocus);
        Assert.False (popover.Visible);
        Assert.NotEqual (popover, repro.App.Popovers?.GetActivePopover ());

        string closedBuffer = ReadScreen (repro.App);
        output.WriteLine ("Closed:\n{0}", closedBuffer);
        Assert.Contains ("COVERED-AREA-3", closedBuffer, StringComparison.Ordinal);
        Assert.DoesNotContain ("Equipment", closedBuffer, StringComparison.Ordinal);
    }

    // Helper to find the DropDownList popover (excludes the context menu popover)
    private static IPopoverView? FindDropDownPopover (IApplication app) => app.Popovers?.Popovers.OfType<Popover<ListView, string?>> ().FirstOrDefault ();

    private static string ReadScreen (IApplication app)
    {
        Cell [,]? contents = app.Driver!.GetOutputBuffer ().Contents;
        Assert.NotNull (contents);

        int rows = contents.GetLength (0);
        int cols = contents.GetLength (1);
        StringBuilder builder = new ();

        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < cols; col++)
            {
                builder.Append (contents [row, col].Grapheme);
            }

            builder.AppendLine ();
        }

        return builder.ToString ();
    }

    /// <summary>
    ///     Dialog repro for handled Accepting: read-only DropDownList over a Label, ListView as the next focus target.
    /// </summary>
    private sealed class HandledAcceptRepro : IDisposable
    {
        private readonly SessionToken? _token;

        private HandledAcceptRepro (IApplication app, Dialog dialog, DropDownList dropDown, ListView nextView, SessionToken? token)
        {
            App = app;
            Dialog = dialog;
            DropDown = dropDown;
            NextView = nextView;
            _token = token;
        }

        public IApplication App { get; }

        public Dialog Dialog { get; }

        public DropDownList DropDown { get; }

        public ListView NextView { get; }

        public static HandledAcceptRepro Create ()
        {
            IApplication app = Application.Create ();
            app.Init (DriverRegistry.Names.ANSI);
            app.Driver!.SetScreenSize (40, 15);

            Dialog dialog = new ()
            {
                X = 0,
                Y = 0,
                Width = 40,
                Height = 12,
                Title = "Repro",
                ShadowStyle = ShadowStyles.None
            };

            ObservableCollection<string> items = ["Labor", "Fuel/Energy", "Equipment"];

            DropDownList dropDown = new ()
            {
                X = 0,
                Y = 0,
                Width = 20,
                ReadOnly = true,
                Source = new ListWrapper<string> (items),
                Text = "Labor"
            };

            Label covered1 = new () { X = 0, Y = 1, Width = 20, Text = "COVERED-AREA-1" };
            Label covered2 = new () { X = 0, Y = 2, Width = 20, Text = "COVERED-AREA-2" };
            Label covered3 = new () { X = 0, Y = 3, Width = 20, Text = "COVERED-AREA-3" };

            ListView nextView = new ()
            {
                X = 0,
                Y = 5,
                Width = 20,
                Height = 3,
                Source = new ListWrapper<string> (new ObservableCollection<string> (["NextA", "NextB", "NextC"]))
            };

            dropDown.Accepting += (_, e) =>
                                  {
                                      e.Handled = true;
                                      nextView.SetFocus ();
                                  };

            dialog.Add (dropDown, covered1, covered2, covered3, nextView);

            SessionToken? token = app.Begin (dialog);
            app.LayoutAndDraw ();

            return new HandledAcceptRepro (app, dialog, dropDown, nextView, token);
        }

        public void Dispose ()
        {
            if (_token is { })
            {
                App.End (_token);
            }

            Dialog.Dispose ();
            App.Dispose ();
        }
    }
}

public class DropDownListGenericTests
{
    private enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    [Fact]
    public void Constructor_PopulatesSourceFromEnum ()
    {
        // Copilot
        DropDownList<Season> dropdown = new ();

        Assert.NotNull (dropdown.Source);
        Assert.Equal (4, dropdown.Source.Count);

        dropdown.Dispose ();
    }

    [Fact]
    public void Constructor_SourceContainsEnumNames ()
    {
        // Copilot
        DropDownList<Season> dropdown = new ();

        IList items = dropdown.Source!.ToList ();
        List<string?> names = items.Cast<object?> ().Select (i => i?.ToString ()).ToList ();
        Assert.Contains ("Spring", names);
        Assert.Contains ("Summer", names);
        Assert.Contains ("Autumn", names);
        Assert.Contains ("Winter", names);

        dropdown.Dispose ();
    }

    [Fact]
    public void Value_Get_ReturnsNullWhenTextIsEmpty ()
    {
        // Copilot
        DropDownList<Season> dropdown = new ();

        Assert.Null (dropdown.Value);

        dropdown.Dispose ();
    }

    [Fact]
    public void Value_Set_UpdatesText ()
    {
        // Copilot
        DropDownList<Season> dropdown = new ();

        dropdown.Value = Season.Summer;

        Assert.Equal ("Summer", dropdown.Text);

        dropdown.Dispose ();
    }

    [Fact]
    public void Value_Get_ReturnsCorrectEnum ()
    {
        // Copilot
        DropDownList<Season> dropdown = new ();
        dropdown.Text = "Autumn";

        Assert.Equal (Season.Autumn, dropdown.Value);

        dropdown.Dispose ();
    }

    [Fact]
    public void Value_SetNull_ClearsText ()
    {
        // Copilot
        DropDownList<Season> dropdown = new ();
        dropdown.Value = Season.Winter;

        dropdown.Value = null;

        Assert.Equal (string.Empty, dropdown.Text);

        dropdown.Dispose ();
    }

    [Fact]
    public void ValueChanged_RaisedWithTypedEnum ()
    {
        // Copilot
        DropDownList<Season> dropdown = new ();

        Season? receivedValue = null;
        dropdown.ValueChanged += (_, e) => { receivedValue = e.Value; };

        dropdown.Value = Season.Spring;

        Assert.Equal (Season.Spring, receivedValue);

        dropdown.Dispose ();
    }

    [Fact]
    public void ValueChanged_RaisedNullWhenTextCleared ()
    {
        // Copilot
        DropDownList<Season> dropdown = new ();
        dropdown.Value = Season.Winter;

        Season? receivedValue = Season.Winter; // set to non-null sentinel
        dropdown.ValueChanged += (_, e) => { receivedValue = e.Value; };

        dropdown.Value = null;

        Assert.Null (receivedValue);

        dropdown.Dispose ();
    }

    [Fact]
    public void GetValue_ReturnsBoxedEnum ()
    {
        // Copilot
        DropDownList<Season> dropdown = new ();
        dropdown.Value = Season.Autumn;

        object? boxed = ((IValue)dropdown).GetValue ();

        Assert.Equal (Season.Autumn, boxed);

        dropdown.Dispose ();
    }

    [Fact]
    public void IsInterchangeableWithOptionSelector_SameValueInterface ()
    {
        // Copilot - demonstrates that both implement IValue with the same TEnum type
        DropDownList<Season> dropDown = new ();
        OptionSelector<Season> optionSelector = new ();

        dropDown.Value = Season.Summer;
        optionSelector.Value = Season.Summer;

        Assert.Equal (optionSelector.Value, dropDown.Value);

        dropDown.Dispose ();
        optionSelector.Dispose ();
    }
}
