#nullable enable
namespace UICatalog.Scenarios;

public class AllViewsView : View
{
    private const int MAX_VIEW_FRAME_HEIGHT = 30;

    public AllViewsView ()
    {
        CanFocus = true;
        BorderStyle = LineStyle.Heavy;
        Arrangement = ViewArrangement.Resizable;
        ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;

        ViewportChanged += (sender, _) =>
                           {
                               if (sender is View sendingView)
                               {
                                   sendingView.SetContentHeight (sendingView.GetHeightRequiredForSubViews ());
                               }
                           };

        AddCommand (Command.Up, () => ScrollVertical (-1));
        AddCommand (Command.Down, () => ScrollVertical (1));
        AddCommand (Command.PageUp, () => ScrollVertical (-SubViews.OfType<FrameView> ().First ().Frame.Height));
        AddCommand (Command.PageDown, () => ScrollVertical (SubViews.OfType<FrameView> ().First ().Frame.Height));

        AddCommand (Command.Start,
                    () =>
                    {
                        Viewport = Viewport with { Y = 0 };

                        return true;
                    });

        AddCommand (Command.End,
                    () =>
                    {
                        Viewport = Viewport with { Y = GetContentHeight () };

                        return true;
                    });

        AddCommand (Command.ScrollDown, () => ScrollVertical (1));
        AddCommand (Command.ScrollUp, () => ScrollVertical (-1));
        AddCommand (Command.ScrollRight, () => ScrollHorizontal (1));
        AddCommand (Command.ScrollLeft, () => ScrollHorizontal (-1));

        KeyBindings.Add (Key.CursorUp, Command.Up);
        KeyBindings.Add (Key.CursorDown, Command.Down);
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.PageUp, Command.PageUp);
        KeyBindings.Add (Key.PageDown, Command.PageDown);
        KeyBindings.Add (Key.Home, Command.Start);
        KeyBindings.Add (Key.End, Command.End);
        KeyBindings.Add (PopoverMenu.DefaultKey, Command.Context);

        MouseBindings.Add (MouseFlags.LeftButtonDoubleClicked, Command.Accept);
        MouseBindings.ReplaceCommands (MouseFlags.RightButtonClicked, Command.Context);
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked | MouseFlags.Ctrl, Command.Context);
        MouseBindings.Add (MouseFlags.WheeledDown, Command.ScrollDown);
        MouseBindings.Add (MouseFlags.WheeledUp, Command.ScrollUp);
        MouseBindings.Add (MouseFlags.WheeledLeft, Command.ScrollLeft);
        MouseBindings.Add (MouseFlags.WheeledRight, Command.ScrollRight);
    }

    /// <inheritdoc/>
    public override void EndInit ()
    {
        base.EndInit ();

        List<Type> allClasses = GetAllViewClassesCollection ();

        View? previousView = null;

        foreach (Type type in allClasses)
        {
            View? view = CreateView (type);

            if (view is null)
            {
                continue;
            }

            FrameView frame = new ()
            {
                CanFocus = true,
                Title = type.Name,
                Y = previousView is { } ? Pos.Bottom (previousView) : 0,
                Width = Dim.Fill (),
                Height = Dim.Auto (DimAutoStyle.Content, maximumContentDim: MAX_VIEW_FRAME_HEIGHT)
            };
            frame.Add (view);
            Add (frame);
            previousView = frame;
        }
    }

    private View? CreateView (Type type)
    {
        // If we are to create a generic Type
        if (type.IsGenericType)
        {
            // For each of the <T> arguments
            List<Type> typeArguments = new ();

            // use <object> or the original type if applicable
            foreach (Type arg in type.GetGenericArguments ())
            {
                if (arg.IsValueType && Nullable.GetUnderlyingType (arg) == null)
                {
                    typeArguments.Add (arg);
                }
                else
                {
                    // Check if the generic parameter has constraints
                    Type [] constraints = arg.GetGenericParameterConstraints ();

                    // Use the first constraint type to satisfy the constraint
                    typeArguments.Add (constraints.Length > 0 ? constraints [0] : typeof (object));
                }
            }

            // And change what type we are instantiating from MyClass<T> to MyClass<object> or MyClass<T>
            try
            {
                type = type.MakeGenericType (typeArguments.ToArray ());
            }
            catch (ArgumentException ex)
            {
                Logging.Warning ($"Cannot create generic type {type} with arguments [{string.Join (", ", typeArguments.Select (t => t.Name))}]: {ex.Message}");

                return null;
            }
        }

        // Ensure the type does not contain any generic parameters
        if (type.ContainsGenericParameters)
        {
            Logging.Warning ($"Cannot create an instance of {type} because it contains generic parameters.");

            //throw new ArgumentException ($"Cannot create an instance of {type} because it contains generic parameters.");
            return null;
        }

        // Instantiate view
        var view = (View)Activator.CreateInstance (type)!;
        var demoText = "This, that, and the other thing.";

        if (view is IDesignable designable)
        {
            designable.EnableForDesign (ref demoText);
        }
        else
        {
            view.Text = demoText;
            view.Title = "_Test Title";
        }

        view.X = 0;
        view.Y = 0;

        view.Initialized += OnViewInitialized;

        return view;
    }

    private static List<Type> GetAllViewClassesCollection ()
    {
        List<Type> types = typeof (View).Assembly.GetTypes ()
                                        .Where (myType => myType is { IsClass: true, IsAbstract: false, IsPublic: true } && myType.IsSubclassOf (typeof (View)))
                                        .ToList ();

        types.Add (typeof (View));

        return types;
    }

    private void OnViewInitialized (object? sender, EventArgs e)
    {
        if (sender is not View view)
        {
            return;
        }

        if (view.Width == Dim.Absolute (0))
        {
            view.Width = Dim.Fill ();
        }

        if (view.Height == Dim.Absolute (0))
        {
            view.Height = MAX_VIEW_FRAME_HEIGHT - 2;
        }

        if (!view.Width.Has<DimAuto> (out _))
        {
            view.Width = Dim.Fill (0, 100);
        }

        if (!view.Height.Has<DimAuto> (out _))
        {
            view.Height = Dim.Auto (minimumContentDim: MAX_VIEW_FRAME_HEIGHT - 2,
                                    maximumContentDim: MAX_VIEW_FRAME_HEIGHT - view.GetAdornmentsThickness ().Vertical);
        }
    }
}
