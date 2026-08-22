using System.Collections.ObjectModel;
using System.Reflection;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("KeyBindings", "Illustrates the KeyBindings API.")]
[ScenarioCategory ("Mouse and Keyboard")]
public sealed class KeyBindings : Scenario
{
    private readonly ObservableCollection<string> _hotkeyBindings = [];
    private readonly ObservableCollection<string> _focusedBindings = [];
    private ListView _hotkeyBindingsListView;
    private ListView _focusedBindingsListView;

    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();

        // ── Left column: App Bindings (top 50%) and View Bindings (bottom 50%) ──

        ListView appBindingsListView = new ()
        {
            Title = "_App Default Bindings",
            BorderStyle = LineStyle.Single,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            CanFocus = true,
            Source = new ListWrapper<string> (FormatDefaultKeyBindings (Application.DefaultKeyBindings)),
            ViewportSettings = ViewportSettingsFlags.HasVerticalScrollBar
        };

        ListView viewBindingsListView = new ()
        {
            Title = "_View Default Bindings",
            BorderStyle = LineStyle.Single,
            Y = Pos.Bottom (appBindingsListView),
            Width = Dim.Auto (),
            Height = Dim.Fill (),
            Source = new ListWrapper<string> (FormatDefaultKeyBindings (View.DefaultKeyBindings)),
            ViewportSettings = ViewportSettingsFlags.HasVerticalScrollBar
        };

        appWindow.Add (appBindingsListView, viewBindingsListView);

        // ── Middle column: All View types ──

        Dictionary<string, Type> viewClasses = GetAllViewClasses ();
        ObservableCollection<string> viewClassNames = new (viewClasses.Keys.OrderBy (k => k));

        ListView viewClassListView = new ()
        {
            Title = "All _View Types",
            BorderStyle = LineStyle.Double,
            X = Pos.Right (viewBindingsListView),
            Width = Dim.Auto (),
            Height = Dim.Fill (),
            CanFocus = true,
            Source = new ListWrapper<string> (viewClassNames),
            ViewportSettings = ViewportSettingsFlags.HasVerticalScrollBar
        };

        appWindow.Add (viewClassListView);

        _focusedBindingsListView = new ListView
        {
            Title = "View Bindings",
            BorderStyle = LineStyle.Single,
            Y = 0,
            X = Pos.Right (viewClassListView),
            Width = Dim.Fill (),
            Height = Dim.Percent (70),
            CanFocus = true,
            Source = new ListWrapper<string> (_focusedBindings)
        };

        _hotkeyBindingsListView = new ListView
        {
            Title = "_HotKey Bindings",
            BorderStyle = LineStyle.Single,
            X = Pos.Left (_focusedBindingsListView),
            Y = Pos.Bottom (_focusedBindingsListView),
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            CanFocus = true,
            Source = new ListWrapper<string> (_hotkeyBindings)
        };

        appWindow.Add (_focusedBindingsListView, _hotkeyBindingsListView);

        // ── Wire up selection changes ──

        viewClassListView.ValueChanged += (_, args) =>
                                        {
                                            if (args.NewValue is null || args.NewValue < 0 || args.NewValue >= viewClassNames.Count)
                                            {
                                                return;
                                            }

                                            string selectedName = viewClassNames [args.NewValue.Value];

                                            if (!viewClasses.TryGetValue (selectedName, out Type selectedType))
                                            {
                                                return;
                                            }
                                            _focusedBindingsListView.Title = $"{selectedName} Bindings";
                                            _hotkeyBindingsListView.Title = $"{selectedName} HotKey Bindings";
                                            PopulateBindingsForType (selectedType, selectedName);
                                        };

        // Select the first view type to populate bindings
        if (viewClassNames.Count > 0)
        {
            viewClassListView.SelectedItem = 0;
            string firstName = viewClassNames [0];

            if (viewClasses.TryGetValue (firstName, out Type firstType))
            {
                PopulateBindingsForType (firstType, firstName);
            }
        }

        app.Run (appWindow);
    }

    /// <summary>
    ///     Creates an instance of the given <see cref="View"/> type and populates the HotKey
    ///     and Focused binding lists from it.
    /// </summary>
    private void PopulateBindingsForType (Type viewType, string displayName)
    {
        _hotkeyBindings.Clear ();
        _focusedBindings.Clear ();

        View view;

        try
        {
            Type typeToCreate = viewType;

            if (viewType.IsGenericType)
            {
                Type [] genericArgs = viewType.GetGenericArguments ();
                Type [] typeArguments = new Type [genericArgs.Length];

                for (var i = 0; i < genericArgs.Length; i++)
                {
                    typeArguments [i] = typeof (object);
                }

                typeToCreate = viewType.MakeGenericType (typeArguments);
            }

            view = (View)Activator.CreateInstance (typeToCreate)!;

            if (view is IDesignable designable)
            {
                string demoText = "Sample text";
                designable.EnableForDesign (ref demoText);
            }
        }
        catch (Exception)
        {
            _hotkeyBindings.Add ("(could not instantiate)");
            _focusedBindings.Add ("(could not instantiate)");
            _hotkeyBindingsListView.Title = $"_HotKey Bindings - {displayName}";
            _focusedBindingsListView.Title = $"_Focused Bindings - {displayName}";

            return;
        }

        // HotKey bindings
        _hotkeyBindingsListView.Title = $"_HotKey Bindings - {displayName}";
        List<KeyValuePair<Key, KeyBinding>> hotKeyBindings = view.HotKeyBindings.GetBindings ().ToList ();

        if (hotKeyBindings.Count == 0)
        {
            _hotkeyBindings.Add ("(none)");
        }
        else
        {
            foreach (KeyValuePair<Key, KeyBinding> binding in hotKeyBindings)
            {
                string commands = string.Join (",", binding.Value.Commands);
                _hotkeyBindings.Add ($"{commands,-22} {binding.Key}");
            }
        }

        // Focused (KeyBindings) bindings
        _focusedBindingsListView.Title = $"_Focused Bindings - {displayName}";
        List<KeyValuePair<Key, KeyBinding>> focusedBindings = view.KeyBindings.GetBindings ().ToList ();

        if (focusedBindings.Count == 0)
        {
            _focusedBindings.Add ("(none)");
        }
        else
        {
            foreach (KeyValuePair<Key, KeyBinding> binding in focusedBindings)
            {
                string commands = string.Join (",", binding.Value.Commands);
                _focusedBindings.Add ($"{commands,-22} {binding.Key}");
            }
        }

        view.Dispose ();
    }

    /// <summary>
    ///     Formats a <see cref="PlatformKeyBinding"/> dictionary for display, one line per key.
    ///     Each line: "CommandName   KeyString (Platform)"
    /// </summary>
    private static ObservableCollection<string> FormatDefaultKeyBindings (Dictionary<Command, PlatformKeyBinding> dict)
    {
        ObservableCollection<string> items = [];

        if (dict is null)
        {
            return items;
        }

        foreach ((Command command, PlatformKeyBinding pkb) in dict)
        {
            string cmd = command.ToString ();

            foreach (Key key in pkb.All ?? [])
            {
                items.Add ($"{cmd,-22} {key} (All)");
            }

            foreach (Key key in pkb.Windows ?? [])
            {
                items.Add ($"{cmd,-22} {key} (Win)");
            }

            foreach (Key key in pkb.Linux ?? [])
            {
                items.Add ($"{cmd,-22} {key} (Linux)");
            }

            foreach (Key key in pkb.Macos ?? [])
            {
                items.Add ($"{cmd,-22} {key} (macOS)");
            }
        }

        return items;
    }

    /// <summary>
    ///     Gets all concrete, public <see cref="View"/> subclasses (plus <see cref="View"/> itself),
    ///     keyed by formatted display name.
    /// </summary>
    private static Dictionary<string, Type> GetAllViewClasses ()
    {
        List<Type> types = typeof (View).Assembly.GetTypes ()
                                        .Where (t => t is { IsClass: true, IsAbstract: false, IsPublic: true }
                                                     && t.IsSubclassOf (typeof (View)))
                                        .ToList ();

        types.Add (typeof (View));

        return types
               .Where (CanSatisfyGenericConstraints)
               .OrderBy (GetFormattedTypeName)
               .ToDictionary (GetFormattedTypeName, t => t);

        static string GetFormattedTypeName (Type type)
        {
            if (!type.IsGenericType)
            {
                return type.Name;
            }

            string baseName = type.Name [..type.Name.IndexOf ('`')];
            string [] typeParams = type.GetGenericArguments ().Select (t => t.Name).ToArray ();

            return $"{baseName}<{string.Join (", ", typeParams)}>";
        }

        static bool CanSatisfyGenericConstraints (Type type)
        {
            if (!type.IsGenericType)
            {
                return true;
            }

            Type genericTypeDef = type.GetGenericTypeDefinition ();
            Type [] genericArgs = genericTypeDef.GetGenericArguments ();

            return genericArgs.SelectMany (arg => arg.GetGenericParameterConstraints ())
                              .All (constraint => !constraint.IsClass || constraint == typeof (object));
        }
    }
}
