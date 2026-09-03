#nullable enable

using System.Collections.ObjectModel;

// ReSharper disable AccessToDisposedClosure

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Themes", "Shows off Themes, Schemes, and VisualRoles.")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Drawing")]
[ScenarioCategory ("Configuration")]
public sealed class Themes : Scenario
{
    private IApplication? _app;
    private View? _view;

    public override void Main ()
    {

        // Init
        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        // Setup - Create a top-level application window and configure it.
        using Runnable appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();
        appWindow.BorderStyle = LineStyle.None;

        string [] options = ThemeManager.GetThemeNames ().Select (option => "_" + option).ToArray ();

        OptionSelector themeOptionSelector = new ()
        {
            Title = "_Themes",
            BorderStyle = LineStyle.Rounded,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Labels = options,
            Value = ThemeManager.GetThemeNames ().IndexOf (ThemeManager.Theme)
        };
        themeOptionSelector.Border.Thickness = new Thickness (0, 1, 0, 0);
        themeOptionSelector.Margin.Thickness = new Thickness (0, 0, 1, 0);

        AttributeViewer defaultAttributeView = new ()
        {
            Title = "Default Attribute",
            BorderStyle = LineStyle.Rounded,
            Y = Pos.Bottom (themeOptionSelector),
            Width = Dim.Width (themeOptionSelector),
            Height = Dim.Auto ()
        };
        defaultAttributeView.Border.Thickness = new Thickness (0, 1, 0, 0);

        FrameView codeRolesPanel = CreateCodeRolesPanel ();
        codeRolesPanel.Y = Pos.Bottom (defaultAttributeView);
        codeRolesPanel.Width = Dim.Width (themeOptionSelector);

        Markdown codeRolesMarkdown = new ()
        {
            Title = "Code Fence",
            BorderStyle = LineStyle.Rounded,
            Y = Pos.Bottom (codeRolesPanel),
            Width = Dim.Width (themeOptionSelector),
            Height = 8,
            SyntaxHighlighter = new TextMateSyntaxHighlighter (),
            Text = """
                   ```cs
                   public sealed class Demo
                   {
                       string Text => "theme";
                   }
                   ```
                   """
        };
        codeRolesMarkdown.Border.Thickness = new Thickness (0, 1, 0, 0);

        themeOptionSelector.ValueChanged += (sender, args) =>
                                            {
                                                if (sender is not OptionSelector optionSelector)
                                                {
                                                    return;
                                                }
                                                string newTheme = optionSelector.Labels! [(int)args.NewValue!];

                                                // strip off the leading underscore
                                                ThemeManager.Theme = newTheme [1..];
                                            };

        ThemeViewer themeViewer = new () { X = Pos.Right (themeOptionSelector) };

        Dictionary<string, Type> viewClasses = GetAllViewClassesCollection ()
                                               .OrderBy (t => t.Name)
                                               .Select (t => new KeyValuePair<string, Type> (t.Name, t))
                                               .ToDictionary (t => t.Key, t => t.Value);

        CheckBox allViewsCheckBox = new () { Title = "_All Views", X = Pos.Right (themeViewer) };

        ListView viewListView = new ()
        {
            X = Pos.Right (themeViewer),
            Y = Pos.Bottom (allViewsCheckBox),
            Title = "_Views",
            BorderStyle = LineStyle.Rounded,
            Width = Dim.Auto (),
            Height = Dim.Fill (),
            Source = new ListWrapper<string> (new ObservableCollection<string> (viewClasses.Keys))
        };
        viewListView.Border.Thickness = new Thickness (0, 1, 0, 0);
        viewListView.Margin.Thickness = new Thickness (0, 0, 1, 0);

        viewListView.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;

        ViewPropertiesEditor viewPropertiesEditor = new () { X = Pos.Right (viewListView), Width = Dim.Fill (), Height = Dim.Auto () };

        FrameView viewFrame = new ()
        {
            X = Pos.Right (viewListView),
            Y = Pos.Bottom (viewPropertiesEditor),
            Title = "The View",
            BorderStyle = LineStyle.Rounded,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            TabStop = TabBehavior.TabStop
        };
        viewFrame.Border.Thickness = new Thickness (0, 1, 0, 0);

        viewListView.ValueChanged += (_, args) =>
                                     {
                                         if (_view is { })
                                         {
                                             viewPropertiesEditor.ViewToEdit = null;
                                             viewFrame.Remove (_view);
                                             _view.Dispose ();
                                             _view = null;
                                         }

                                         if (args.NewValue is null)
                                         {
                                             return;
                                         }

                                         var viewName = (string)viewListView.Source!.ToList () [args.NewValue.Value]!;
                                         _view = CreateView (viewClasses [viewName]);

                                         if (_view is null)
                                         {
                                             return;
                                         }
                                         viewFrame.Add (_view);
                                         viewPropertiesEditor.ViewToEdit = _view;
                                     };

        appWindow.Add (themeOptionSelector,
                       defaultAttributeView,
                       codeRolesPanel,
                       codeRolesMarkdown,
                       themeViewer,
                       allViewsCheckBox,
                       viewListView,
                       viewPropertiesEditor,
                       viewFrame);

        viewListView.SelectedItem = 0;

        themeViewer.SchemeNameChanging += (_, args) =>
                                          {
                                              if (_view is null)
                                              {
                                                  return;
                                              }
                                              _app!.TopRunnableView!.SchemeName = args.NewValue;

                                              if (_view.HasScheme)
                                              {
                                                  _view.SetScheme (null);
                                              }

                                              _view.SchemeName = args.NewValue;
                                          };

        AllViewsView? allViewsView = null;

        allViewsCheckBox.ValueChanged += (_, args) =>
                                         {
                                             if (args.NewValue == CheckState.Checked)
                                             {
                                                 viewListView.Visible = false;
                                                 appWindow.Remove (viewFrame);

                                                 allViewsView = new AllViewsView
                                                 {
                                                     X = Pos.Right (themeViewer),
                                                     Y = Pos.Bottom (viewPropertiesEditor),
                                                     Title = "All Views - Focused: {None}",
                                                     BorderStyle = LineStyle.Rounded,
                                                     Width = Dim.Fill (),
                                                     Height = Dim.Fill (),
                                                     TabStop = TabBehavior.TabStop
                                                 };

                                                 allViewsView.FocusedChanged += (_, a) =>
                                                                                {
                                                                                    allViewsView.Title = $"All Views - Focused: {a.NewFocused?.Title}";
                                                                                    viewPropertiesEditor.ViewToEdit = a.NewFocused?.SubViews.ElementAt (0);
                                                                                };
                                                 appWindow.Add (allViewsView);
                                             }
                                             else
                                             {
                                                 appWindow.Remove (allViewsView);
                                                 allViewsView!.Dispose ();
                                                 allViewsView = null;

                                                 appWindow.Add (viewFrame);
                                                 viewListView.Visible = true;
                                             }
                                         };

        // Run - Start the application.
        app.Run (appWindow);
        viewFrame.Dispose ();
    }

    private static List<Type> GetAllViewClassesCollection ()
    {
        List<Type> types = typeof (View).Assembly.GetTypes ()
                                        .Where (myType => myType is { IsClass: true, IsAbstract: false, IsPublic: true } && myType.IsSubclassOf (typeof (View)))
                                        .ToList ();

        types.Add (typeof (View));

        return types;
    }

    private static FrameView CreateCodeRolesPanel ()
    {
        FrameView panel = new () { Title = "Code roles", BorderStyle = LineStyle.Rounded, Height = Dim.Auto () };
        panel.Border.Thickness = new Thickness (0, 1, 0, 0);

        VisualRoleViewer? previous = null;

        foreach (VisualRole role in Enum.GetValues<VisualRole> ().Where (role => role >= VisualRole.Code))
        {
            VisualRoleViewer roleViewer = new () { Role = role, SchemeName = "Base" };

            if (previous is { })
            {
                roleViewer.Y = Pos.Bottom (previous);
            }

            panel.Add (roleViewer);
            previous = roleViewer;
        }

        return panel;
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
                typeArguments.Add (GetSubstituteType (arg));
            }

            // And change what type we are instantiating from MyClass<T> to MyClass<object> or MyClass<T>
            type = type.MakeGenericType (typeArguments.ToArray ());
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

        view.Initialized += OnViewInitialized;

        return view;
    }

    private Type GetSubstituteType (Type genericParam)
    {
        // If it's a non-nullable value type, keep it as-is
        if (genericParam.IsValueType && Nullable.GetUnderlyingType (genericParam) == null)
        {
            return genericParam;
        }

        // Check constraints (e.g., where TView : View, new())
        Type [] constraints = genericParam.GetGenericParameterConstraints ();

        // Find the most derived base class constraint (ignore interfaces)
        Type? baseConstraint = constraints.Where (c => c.IsClass)
                                          .OrderByDescending (c => c.GetInterfaces ().Length) // rough heuristic for "most derived"
                                          .FirstOrDefault ();

        if (baseConstraint != null)
        {
            // If the constraint itself is abstract or doesn't have a
            // parameterless constructor, this may still fail at activation
            return baseConstraint;
        }

        // No class constraint — fall back to object
        return typeof (object);
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
            view.Height = Dim.Fill ();
        }
    }
}
