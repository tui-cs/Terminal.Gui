// ReSharper disable AccessToDisposedClosure
#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Selectors", "Demonstrates OptionSelector and FlagSelector.")]
[ScenarioCategory ("Controls")]
public sealed class Selectors : Scenario
{
    public override void Main ()
    {

        // Init
        using IApplication app = Application.Create ();
        app.Init ();

        // Setup - Create a top-level application window and configure it.
        using Window appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();
        appWindow.BorderStyle = LineStyle.None;

        EventLog eventLog = new ()
        {
            Id = "eventLog",
            X = Pos.AnchorEnd (),
            Height = Dim.Fill (),
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent),
            BorderStyle = LineStyle.Double,
            Title = "E_vents",
            Arrangement = ViewArrangement.LeftResizable
        };

        FrameView? optionSelectorsFrame = null;
        FrameView? flagSelectorsFrame = null;

        OptionSelector<Orientation> orientationSelector = new ()
        {
            Orientation = Orientation.Horizontal, BorderStyle = LineStyle.Dotted, Title = "Selector Or_ientation", Value = Orientation.Vertical
        };
        orientationSelector.ValueChanged += OrientationSelectorOnSelectedItemChanged;

        FlagSelector<SelectorStyles> stylesSelector = new ()
        {
            X = Pos.Right (orientationSelector) + 1, Orientation = Orientation.Horizontal, BorderStyle = LineStyle.Dotted, Title = "Selector St_yles"
        };
        stylesSelector.ValueChanged += StylesSelectorOnValueChanged;

        NumericUpDown<int> horizontalSpace = new ()
        {
            X = 0,
            Y = Pos.Bottom (orientationSelector),
            Width = 11,
            Title = "H_. Space",
            Value = stylesSelector.HorizontalSpace,
            BorderStyle = LineStyle.Dotted
        };
        horizontalSpace.ValueChanging += HorizontalSpaceOnValueChanging;

        CheckBox showBorderAndTitle = new ()
        {
            X = Pos.Right (horizontalSpace) + 1,
            Y = Pos.Top (horizontalSpace),
            Title = "Border _& Title",
            Value = CheckState.Checked,
            BorderStyle = LineStyle.Dotted
        };
        showBorderAndTitle.ValueChanged += ShowBorderAndTitleOnValueChanged;

        CheckBox canFocus = new ()
        {
            X = Pos.Right (showBorderAndTitle) + 1,
            Y = Pos.Top (horizontalSpace),
            Title = "_CanFocus",
            Value = CheckState.Checked,
            BorderStyle = LineStyle.Dotted
        };
        canFocus.ValueChanged += CanFocusOnValueChanged;

        OptionSelector<TabBehavior> tabBehaviorSelector = new ()
        {
            X = Pos.Right (canFocus) + 1,
            Y = Pos.Top (canFocus),
            Orientation = Orientation.Horizontal,
            BorderStyle = LineStyle.Dotted,
            Title = "_Tab Behavior",
            Value = orientationSelector.TabStop
        };

        tabBehaviorSelector.ValueChanged += TabBehaviorSelectorOnValueChanged;

        optionSelectorsFrame = new FrameView { Y = Pos.Bottom (canFocus), Height = Dim.Fill (), Title = "O_ptionSelectors", TabStop = TabBehavior.TabStop };

        Label label = new () { Title = "Fo_ur Options:" };

        OptionSelector optionSelector = new ()
        {
            X = Pos.Right (label) + 1,
            Title = "Fou_r Options",
            BorderStyle = LineStyle.Dotted,
            UsedHotKeys = { label.HotKey },
            AssignHotKeys = true,
            Labels = ["Option _1 (0)", "Option _2 (1)", "Option _3 (5) 你", "Option _Quattro (4) 你"],
            Values = [0, 1, 5, 4],
            Arrangement = ViewArrangement.Resizable
        };
        optionSelectorsFrame.Add (label, optionSelector);

        label = new Label { Y = Pos.Bottom (optionSelector), Title = "<VisualRole_>:" };

        OptionSelector<VisualRole> optionSelectorT = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Bottom (optionSelector),
            Title = "<Vi_sualRole>",
            BorderStyle = LineStyle.Dotted,
            UsedHotKeys = optionSelector.UsedHotKeys,
            AssignHotKeys = true
        };

        optionSelectorsFrame.Add (label, optionSelectorT);

        flagSelectorsFrame = new FrameView
        {
            Y = Pos.Top (optionSelectorsFrame),
            X = Pos.Right (optionSelectorsFrame),
            Width = Dim.Fill (eventLog),
            Height = Dim.Fill (),
            Title = "_FlagSelectors",
            TabStop = TabBehavior.TabStop
        };

        label = new Label { Title = "FlagSelector _(uint):" };

        FlagSelector flagSelector = new ()
        {
            X = Pos.Right (label) + 1,
            BorderStyle = LineStyle.Dotted,
            Title = "FlagSe_lector (uint)",
            AssignHotKeys = true,
            Values = [0b_0001, 0b_0010, 0b_0100, 0b_1000, 0b_1111],
            Labels = ["0x0001 One", "0x0010 Two", "0x0100 Quattro", "0x1000 8", "0x1111 Fifteen"]
        };
        flagSelectorsFrame.Add (label, flagSelector);

        label = new Label { Y = Pos.Bottom (flagSelector), Title = "_<ViewDiagnosticFlags>:" };

        FlagSelector<ViewDiagnosticFlags> flagSelectorT = new ()
        {
            X = Pos.Right (label) + 1,
            BorderStyle = LineStyle.Dotted,
            Title = "<ViewD_iagnosticFlags>",
            Y = Pos.Bottom (flagSelector),
            UsedHotKeys = flagSelector.UsedHotKeys,
            AssignHotKeys = true,
            Value = View.Diagnostics
        };
        flagSelectorsFrame.Add (label, flagSelectorT);
        flagSelectorT.ValueChanged += (_, a) => { View.Diagnostics = (ViewDiagnosticFlags)a.Value!; };

        optionSelectorsFrame.Width = Dim.Func (_ => (appWindow.Viewport.Width - eventLog.Frame.Width) / 2);

        appWindow.Add (orientationSelector,
                       stylesSelector,
                       horizontalSpace,
                       showBorderAndTitle,
                       canFocus,
                       tabBehaviorSelector,
                       optionSelectorsFrame,
                       flagSelectorsFrame,
                       eventLog);

        eventLog.SetViewToLog (orientationSelector);
        eventLog.SetViewToLog (stylesSelector);
        eventLog.SetViewToLog (tabBehaviorSelector);

        foreach (SelectorBase selector in GetAllSelectors ())
        {
            eventLog.SetViewToLog (selector);
        }

        // Run - Start the application.
        app.Run (appWindow);

        return;

        void TabBehaviorSelectorOnValueChanged (object? sender, EventArgs<TabBehavior?> e)
        {
            if (sender is not OptionSelector<TabBehavior> s)
            {
                return;
            }
            List<SelectorBase> selectors = GetAllSelectors ();

            foreach (SelectorBase selector in selectors)
            {
                selector.TabBehavior = s.Value!.Value;
            }
        }

        void OrientationSelectorOnSelectedItemChanged (object? sender, EventArgs<Orientation?> e)
        {
            if (sender is not OptionSelector<Orientation> s)
            {
                return;
            }

            List<SelectorBase> selectors = GetAllSelectors ();

            foreach (SelectorBase selector in selectors)
            {
                selector.Orientation = s.Value!.Value;
            }
        }

        void StylesSelectorOnValueChanged (object? sender, EventArgs<SelectorStyles?> e)
        {
            if (sender is not FlagSelector<SelectorStyles> s)
            {
                return;
            }

            List<SelectorBase> selectors = GetAllSelectors ();

            foreach (SelectorBase selector in selectors)
            {
                selector.Styles = s.Value!.Value;
            }
        }

        void HorizontalSpaceOnValueChanging (object? sender, ValueChangingEventArgs<int> e)
        {
            if (sender is not NumericUpDown<int> || e.NewValue < 0)
            {
                e.Handled = true;

                return;
            }

            List<SelectorBase> selectors = GetAllSelectors ();

            foreach (SelectorBase selector in selectors)
            {
                selector.HorizontalSpace = e.NewValue;
            }
        }

        void ShowBorderAndTitleOnValueChanged (object? sender, ValueChangedEventArgs<CheckState> e)
        {
            if (sender is not CheckBox cb)
            {
                return;
            }

            List<SelectorBase> selectors = GetAllSelectors ();

            foreach (SelectorBase selector in selectors)
            {
                selector.Border.Thickness = cb.Value == CheckState.Checked ? new Thickness (1) : new Thickness (0);
            }
        }

        void CanFocusOnValueChanged (object? sender, ValueChangedEventArgs<CheckState> e)
        {
            if (sender is not CheckBox cb)
            {
                return;
            }

            List<SelectorBase> selectors = GetAllSelectors ();

            foreach (SelectorBase selector in selectors)
            {
                selector.CanFocus = cb.Value == CheckState.Checked;
            }
        }

        List<SelectorBase> GetAllSelectors ()
        {
            List<SelectorBase> optionSelectors = [];

            // ReSharper disable once AccessToModifiedClosure
            optionSelectors.AddRange (optionSelectorsFrame!.SubViews.OfType<SelectorBase> ());

            // ReSharper disable once AccessToModifiedClosure
            optionSelectors.AddRange (flagSelectorsFrame!.SubViews.OfType<FlagSelector> ());

            return optionSelectors;
        }
    }
}
