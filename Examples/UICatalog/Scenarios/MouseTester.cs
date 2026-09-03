#nullable enable
using System.Collections.ObjectModel;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Mouse Tester", "Illustrates Mouse event flow and handling")]
[ScenarioCategory ("Mouse and Keyboard")]
public class MouseTester : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Runnable runnable = new () { Id = "runnable" };

        MenuBar menuBar = new ();
        menuBar.Add (new MenuBarItem (Strings.menuFile, [new MenuItem { Title = Strings.cmdQuit, Action = () => app.RequestStop () }]));

        FlagSelector<DemoMouseFlags> mouseFlagsFilter = new () { AssignHotKeys = true, Value = DemoMouseFlags.All & ~DemoMouseFlags.PositionReport };

        menuBar.Add (new MenuBarItem ("_Filter", [new MenuItem { CommandView = mouseFlagsFilter }]),
                     new MenuBarItem (runnable, Command.DeleteAll, "_Clear Logs"));
        runnable.Add (menuBar);

        View lastDriverEvent = new () { Height = 1, Width = Dim.Auto (), Y = Pos.Bottom (menuBar), Text = "Last Driver Event: " };

        runnable.Add (lastDriverEvent);

        View lastAppEvent = new () { Height = 1, Width = Dim.Auto (), Y = Pos.Bottom (lastDriverEvent), Text = "Last App Event: " };

        runnable.Add (lastAppEvent);

        View lastViewEvent = new () { Height = 1, Width = Dim.Auto (), Y = Pos.Bottom (lastAppEvent), Text = "Last View Event: " };

        runnable.Add (lastViewEvent);

        FlagSelector<MouseState> mouseHighlightStates = new ()
        {
            BorderStyle = LineStyle.Dotted, Title = "_Highlight States", Y = Pos.Bottom (lastViewEvent), Width = 20
        };
        runnable.Add (mouseHighlightStates);

        CheckBox cbRepeatOnHold = new ()
        {
            X = Pos.Right (mouseHighlightStates) + 1, Y = Pos.Top (mouseHighlightStates), BorderStyle = LineStyle.Dotted, Title = "_Repeat On Hold"
        };

        runnable.Add (cbRepeatOnHold);

        MouseEventDemoView demo = new ()
        {
            Id = "demo",
            Y = Pos.Bottom (mouseHighlightStates),
            Width = Dim.Fill (),
            Height = 15,
            Title = "Enter/Leave Demo"
        };

        MouseEventDemoView demoInPadding = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Func (_ => demo.Padding.Thickness.Top),
            Title = "inPadding",
            Id = "inPadding"
        };
        demo.Padding.GetOrCreateView ().Add (demoInPadding);

        demo.Padding.View!.Initialized += DemoPaddingOnInitialized;

        MouseEventDemoView sub1 = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent (20),
            Height = Dim.Fill (),
            Title = "sub1",
            Id = "sub1"
        };
        demo.Add (sub1);

        MouseEventDemoView sub2 = new ()
        {
            X = Pos.Right (sub1) - 4,
            Y = Pos.Top (sub1) + 1,
            Width = Dim.Percent (20),
            Height = Dim.Fill (1),
            Title = "sub2",
            Id = "sub2"
        };

        demo.Add (sub2);

        runnable.Add (demo);

        mouseHighlightStates.Value = demo.MouseHighlightStates;

        mouseHighlightStates.ValueChanged += (sender, _) =>
                                             {
                                                 if (sender is not FlagSelector<MouseState> optionSelector)
                                                 {
                                                     return;
                                                 }
                                                 demo.MouseHighlightStates = optionSelector.Value!.Value;

                                                 foreach (View subview in demo.SubViews)
                                                 {
                                                     subview.MouseHighlightStates = optionSelector.Value!.Value;
                                                 }

                                                 foreach (View subview in demo.Padding.View?.SubViews ?? [])
                                                 {
                                                     subview.MouseHighlightStates = optionSelector.Value!.Value;
                                                 }
                                             };

        cbRepeatOnHold.ValueChanging += (_, _) =>
                                        {
                                            demo.MouseHoldRepeat = demo.MouseHoldRepeat is null ? MouseFlags.LeftButtonPressed : null;

                                            foreach (View subview in demo.SubViews)
                                            {
                                                subview.MouseHoldRepeat = demo.MouseHoldRepeat;
                                            }

                                            foreach (View subview in demo.Padding.View?.SubViews ?? [])
                                            {
                                                subview.MouseHoldRepeat = demo.MouseHoldRepeat;
                                            }
                                        };

        Label label = new ()
        {
            Text = "Dri_ver Events:",

            //X = Pos.Right (filterSlider),
            Y = Pos.Bottom (demo)
        };

        ObservableCollection<string> driverLogList = new ();

        ListView driverLog = new ()
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = Dim.Auto (minimumContentDim: Dim.Percent (20)),
            Height = Dim.Fill (),
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Base),
            Source = new ListWrapper<string> (driverLogList)
        };
        runnable.Add (label, driverLog);

        app.Driver!.MouseEvent += (_, mouse) =>
                                  {
                                      if (!mouseFlagsFilter.Value.HasValue)
                                      {
                                          return;
                                      }

                                      if (!mouseFlagsFilter.Value.Value.HasFlag ((DemoMouseFlags)mouse.Flags))
                                      {
                                          return;
                                      }
                                      lastDriverEvent.Text = $"Last Driver Event: {mouse}";
                                      Logging.Trace (lastDriverEvent.Text);
                                      driverLogList.Add ($"{mouse.Position}:{mouse.Flags}");
                                      driverLog.MoveEnd ();
                                  };

        label = new Label { Text = "_App Events:", X = Pos.Right (driverLog) + 1, Y = Pos.Bottom (demo) };

        ObservableCollection<string> appLogList = new ();

        ListView appLog = new ()
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = Dim.Auto (minimumContentDim: Dim.Percent (20)),
            Height = Dim.Fill (),
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Base),
            Source = new ListWrapper<string> (appLogList)
        };
        runnable.Add (label, appLog);

        app.Mouse.MouseEvent += (_, mouse) =>
                                {
                                    if (!mouseFlagsFilter.Value.HasValue)
                                    {
                                        return;
                                    }

                                    if (!mouseFlagsFilter.Value.Value.HasFlag ((DemoMouseFlags)mouse.Flags))
                                    {
                                        return;
                                    }
                                    lastAppEvent.Text = $"   Last App Event: {mouse}";
                                    appLogList.Add ($"{mouse.Position}:{mouse.Flags}");
                                    appLog.MoveEnd ();
                                };

        label = new Label { Text = "_View Events:", X = Pos.Right (appLog) + 1, Y = Pos.Top (label) };
        ObservableCollection<string> viewLogList = [];

        ListView viewLog = new ()
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = Dim.Auto (minimumContentDim: Dim.Percent (20)),
            Height = Dim.Fill (),
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Base),
            Source = new ListWrapper<string> (viewLogList)
        };
        runnable.Add (label, viewLog);

        demo.MouseEvent += (_, mouse) =>
                           {
                               if (!mouseFlagsFilter.Value.Value.HasFlag ((DemoMouseFlags)mouse.Flags))
                               {
                                   return;
                               }
                               lastViewEvent.Text = $"  Last View Event: {mouse}";
                               viewLogList.Add ($"{mouse.Position}:{mouse.View!.Id}:{mouse.Flags}");
                               viewLog.MoveEnd ();
                           };

        demoInPadding.MouseEvent += (_, mouse) =>
                                    {
                                        if (!mouseFlagsFilter.Value.Value.HasFlag ((DemoMouseFlags)mouse.Flags))
                                        {
                                            return;
                                        }
                                        lastViewEvent.Text = $"  Last View Event: {mouse}";
                                        viewLogList.Add ($"{mouse.Position}:{mouse.View!.Id}:{mouse.Flags}");
                                        viewLog.MoveEnd ();
                                    };

        sub1.MouseEvent += (_, mouse) =>
                           {
                               if (!mouseFlagsFilter.Value.Value.HasFlag ((DemoMouseFlags)mouse.Flags))
                               {
                                   return;
                               }
                               lastViewEvent.Text = $"  Last View Event: {mouse}";
                               viewLogList.Add ($"{mouse.Position}:{mouse.View!.Id}:{mouse.Flags}");
                               viewLog.MoveEnd ();
                           };

        sub2.MouseEvent += (_, mouse) =>
                           {
                               if (!mouseFlagsFilter.Value.Value.HasFlag ((DemoMouseFlags)mouse.Flags))
                               {
                                   return;
                               }
                               lastViewEvent.Text = $"  Last View Event: {mouse}";
                               viewLogList.Add ($"{mouse.Position}:{mouse.View!.Id}:{mouse.Flags}");
                               viewLog.MoveEnd ();
                           };

        label = new Label { Text = "_Commands:", X = Pos.Right (viewLog) + 1, Y = Pos.Top (label) };
        ObservableCollection<string> commandLogList = [];

        ListView commandLog = new ()
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = Dim.Auto (minimumContentDim: Dim.Percent (15)),
            Height = Dim.Fill (),
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Base),
            Source = new ListWrapper<string> (commandLogList)
        };
        runnable.Add (label, commandLog);

        demo.Activating += (_, args) =>
                           {
                               string sourceId = args.Context?.Source?.TryGetTarget (out View? sourceView) == true ? sourceView.Id : "null";
                               commandLogList.Add ($"{sourceId}:{args.Context!.Command}");
                               commandLog.MoveEnd ();
                               args.Handled = true;
                           };

        demo.Accepting += (_, args) =>
                          {
                              string sourceId = args.Context?.Source?.TryGetTarget (out View? sourceView) == true ? sourceView.Id : "null";
                              commandLogList.Add ($"{sourceId}:{args.Context!.Command}");
                              commandLog.MoveEnd ();
                              args.Handled = true;
                          };

        demo.CommandNotBound += (_, args) =>
                                {
                                    commandLogList.Add ($"{args.Context!.Source.ToIdentifyingString ()}:{args.Context!.Command}");
                                    commandLog.MoveEnd ();
                                    args.Handled = true;
                                };

        demoInPadding.Activating += (_, args) =>
                                    {
                                        string sourceId = args.Context?.Source?.TryGetTarget (out View? sourceView) == true ? sourceView.Id : "null";
                                        commandLogList.Add ($"{sourceId}:{args.Context!.Command}");
                                        commandLog.MoveEnd ();
                                        args.Handled = true;
                                    };

        demoInPadding.Accepting += (_, args) =>
                                   {
                                       string sourceId = args.Context?.Source?.TryGetTarget (out View? sourceView) == true ? sourceView.Id : "null";
                                       commandLogList.Add ($"{sourceId}:{args.Context!.Command}");
                                       commandLog.MoveEnd ();
                                       args.Handled = true;
                                   };

        sub1.Activating += (_, args) =>
                           {
                               string sourceId = args.Context?.Source?.TryGetTarget (out View? sourceView) == true ? sourceView.Id : "null";
                               commandLogList.Add ($"{sourceId}:{args.Context!.Command}");
                               commandLog.MoveEnd ();
                               args.Handled = true;
                           };

        sub1.Accepting += (_, args) =>
                          {
                              string sourceId = args.Context?.Source?.TryGetTarget (out View? sourceView) == true ? sourceView.Id : "null";
                              commandLogList.Add ($"{sourceId}:{args.Context!.Command}");
                              commandLog.MoveEnd ();
                              args.Handled = true;
                          };

        sub2.Activating += (_, args) =>
                           {
                               string sourceId = args.Context?.Source?.TryGetTarget (out View? sourceView) == true ? sourceView.Id : "null";
                               commandLogList.Add ($"{sourceId}:{args.Context!.Command}");
                               commandLog.MoveEnd ();
                               args.Handled = true;
                           };

        sub2.Accepting += (_, args) =>
                          {
                              string sourceId = args.Context?.Source?.TryGetTarget (out View? sourceView) == true ? sourceView.Id : "null";
                              commandLogList.Add ($"{sourceId}:{args.Context!.Command}");
                              commandLog.MoveEnd ();
                              args.Handled = true;
                          };

        runnable.CommandNotBound += (_, args) =>
                                    {
                                        if (args.Context!.Command != Command.DeleteAll)
                                        {
                                            return;
                                        }
                                        driverLogList.Clear ();
                                        driverLog.SetSource (driverLogList);
                                        appLogList.Clear ();
                                        appLog.SetSource (appLogList);
                                        viewLogList.Clear ();
                                        viewLog.SetSource (viewLogList);
                                        commandLogList.Clear ();
                                        commandLog.SetSource (commandLogList);
                                        args.Handled = true;
                                    };

        app.Run (runnable);

        return;

        void DemoPaddingOnInitialized (object? o, EventArgs eventArgs) => demo.Padding.Thickness = demo.Padding.Thickness with { Top = 5 };
    }

    public class MouseEventDemoView : View
    {
        public MouseEventDemoView ()
        {
            CanFocus = true;
            Id = "mouseEventDemoView";

            MouseLeave += (_, _) => { Text = "Leave"; };
            MouseEnter += (_, _) => { Text = "Enter"; };

            // Bind mouse events to commands that will illustrate command handling
            MouseBindings.ReplaceCommands (MouseFlags.LeftButtonPressed, Command.Down);
            MouseBindings.ReplaceCommands (MouseFlags.LeftButtonReleased, Command.Up);
            MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked, Command.Accept);
            MouseBindings.ReplaceCommands (MouseFlags.LeftButtonDoubleClicked, Command.Open);
        }

        /// <inheritdoc/>
        public override void EndInit ()
        {
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Base);

            TextAlignment = Alignment.Center;
            VerticalTextAlignment = Alignment.Center;

            Padding.Thickness = new Thickness (1, 1, 1, 1);
            Padding.GetOrCreateView ().SetScheme (new Scheme (new Attribute (Color.DarkGray)));
            Padding.View!.Id = $"{Id}.Padding";

            Border.Thickness = new Thickness (1);
            Border.LineStyle = LineStyle.Rounded;
            Border.View!.Id = $"{Id}.Border";
            base.EndInit ();
        }

        /// <inheritdoc/>
        protected override void OnMouseStateChanged (EventArgs<MouseState> args)
        {
            base.OnMouseStateChanged (args);
            Border.LineStyle = args.Value.HasFlag (MouseState.PressedOutside) ? LineStyle.Dotted : LineStyle.Single;

            SetNeedsDraw ();
        }

        /// <inheritdoc/>
        protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
        {
            switch (role)
            {
                case VisualRole.Normal when MouseState.HasFlag (MouseState.Pressed) && MouseHighlightStates.HasFlag (MouseState.Pressed):
                    currentAttribute = currentAttribute with { Background = currentAttribute.Foreground.GetBrighterColor () };

                    return true;

                default:
                    return base.OnGettingAttributeForRole (in role, ref currentAttribute);
            }
        }
    }
}

// All the MouseFlags we can set for filtering
[Flags]
internal enum DemoMouseFlags
{
    /// <summary>
    ///     No mouse event. This is the default value for <see cref="Mouse.Flags"/> when no mouse event is being
    ///     No mouse event. This is the default value for <see cref="Mouse.Flags"/> when no mouse event is being
    ///     reported.
    /// </summary>
    None = 0,

    /// <summary>The first mouse button was pressed.</summary>
    LeftButtonPressed = 0x2,

    /// <summary>The first mouse button was released.</summary>
    LeftButtonReleased = 0x1,

    /// <summary>The first mouse button was clicked (press+release).</summary>
    LeftButtonClicked = 0x4,

    /// <summary>The first mouse button was double-clicked.</summary>
    LeftButtonDoubleClicked = 0x8,

    /// <summary>The first mouse button was triple-clicked.</summary>
    LeftButtonTripleClicked = 0x10,

    /// <summary>The second mouse button was pressed.</summary>
    MiddleButtonPressed = 0x80,

    /// <summary>The second mouse button was released.</summary>
    MiddleButtonReleased = 0x40,

    /// <summary>The second mouse button was clicked (press+release).</summary>
    MiddleButtonClicked = 0x100,

    /// <summary>The second mouse button was double-clicked.</summary>
    MiddleButtonDoubleClicked = 0x200,

    /// <summary>The second mouse button was triple-clicked.</summary>
    MiddleButtonTripleClicked = 0x400,

    /// <summary>The third mouse button was pressed.</summary>
    RightButtonPressed = 0x2000,

    /// <summary>The third mouse button was released.</summary>
    RightButtonReleased = 0x1000,

    /// <summary>The third mouse button was clicked (press+release).</summary>
    RightButtonClicked = 0x4000,

    /// <summary>The third mouse button was double-clicked.</summary>
    RightButtonDoubleClicked = 0x8000,

    /// <summary>The third mouse button was triple-clicked.</summary>
    RightButtonTripleClicked = 0x10000,

    /// <summary>The fourth mouse button was pressed.</summary>
    Button4Pressed = 0x80000,

    /// <summary>The fourth mouse button was released.</summary>
    Button4Released = 0x40000,

    /// <summary>The fourth mouse button was clicked.</summary>
    Button4Clicked = 0x100000,

    /// <summary>The fourth mouse button was double-clicked.</summary>
    Button4DoubleClicked = 0x200000,

    /// <summary>The fourth mouse button was triple-clicked.</summary>
    Button4TripleClicked = 0x400000,

    /// <summary>The mouse position is being reported in this event.</summary>
    PositionReport = 0x8000000,

    /// <summary>Vertical button wheeled up.</summary>
    WheeledUp = 0x10000000,

    /// <summary>Vertical button wheeled down.</summary>
    WheeledDown = 0x20000000,

    /// <summary>Vertical button wheeled up while pressing Ctrl.</summary>
    WheeledLeft = 0x1000000 | WheeledUp,

    /// <summary>Vertical button wheeled down while pressing Ctrl.</summary>
    WheeledRight = 0x1000000 | WheeledDown,

    All = -1
}
