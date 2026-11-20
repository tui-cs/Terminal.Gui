#nullable enable
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terminal.Gui.Editor;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Dynamic MenuBar", "Demonstrates how to change a MenuBar dynamically.")]
[ScenarioCategory ("Arrangement")]
[ScenarioCategory ("Menus")]
public class DynamicMenuBar : Scenario
{
    private static IApplication? _app;

    public override void Main ()
    {
        using IApplication app = Application.Create ();
        _app = app;
        _app.Init ();
        _app.Run<DynamicMenuBarSample> ();
    }

    public class Binding
    {
        private readonly PropertyInfo? _sourceBindingProperty;
        private readonly object? _sourceDataContext;
        private readonly IValueConverter? _valueConverter;

        public Binding (View source, string sourcePropertyName, View target, string targetPropertyName, IValueConverter? valueConverter = null)
        {
            Target = target;
            Source = source;
            SourcePropertyName = sourcePropertyName;
            TargetPropertyName = targetPropertyName;
            _sourceDataContext = Source.GetType ().GetProperty ("DataContext")?.GetValue (Source);
            _sourceBindingProperty = _sourceDataContext?.GetType ().GetProperty (SourcePropertyName);
            _valueConverter = valueConverter;
            UpdateTarget ();

            if (_sourceDataContext is INotifyPropertyChanged notifier)
            {
                notifier.PropertyChanged += (_, e) =>
                                            {
                                                if (e.PropertyName == SourcePropertyName)
                                                {
                                                    UpdateTarget ();
                                                }
                                            };
            }
        }

        public View Source { get; }
        public string SourcePropertyName { get; }
        public View Target { get; }
        public string TargetPropertyName { get; }

        private void UpdateTarget ()
        {
            try
            {
                if (_sourceBindingProperty is null || _sourceDataContext is null)
                {
                    return;
                }

                object? sourceValue = _sourceBindingProperty.GetValue (_sourceDataContext);

                if (sourceValue == null)
                {
                    return;
                }

                object finalValue = _valueConverter?.Convert (sourceValue) ?? sourceValue;

                PropertyInfo? targetProperty = Target.GetType ().GetProperty (TargetPropertyName);
                targetProperty?.SetValue (Target, finalValue);
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery (_app!, "Binding Error", $"Binding failed: {ex}.", "Ok");
            }
        }
    }

    public class DynamicMenuBarDetails : FrameView
    {
        private bool _hasParent;
        private bool _loadingDetails;
        private MenuItem? _menuItem;
        private Line? _separator;
        private bool _suppressSubMenuPrompts;

        public DynamicMenuBarDetails (MenuItem? menuItem = null, bool hasParent = false) : this ()
        {
            _menuItem = menuItem;
            _hasParent = hasParent;
            Title = menuItem == null ? "Adding New Menu." : "Editing Menu.";
        }

        public DynamicMenuBarDetails ()
        {
            Label lblTitle = new () { Y = 1, Text = "Title:" };
            Add (lblTitle);

            TextTitle = new TextField { X = Pos.Right (lblTitle) + 2, Y = Pos.Top (lblTitle), Width = Dim.Fill () };
            Add (TextTitle);

            Label lblHelp = new () { X = Pos.Left (lblTitle), Y = Pos.Bottom (lblTitle) + 1, Text = "Help:" };
            Add (lblHelp);

            TextHelp = new TextField { X = Pos.Left (TextTitle), Y = Pos.Top (lblHelp), Width = Dim.Fill () };
            Add (TextHelp);

            Label lblAction = new () { X = Pos.Left (lblTitle), Y = Pos.Bottom (lblHelp) + 1, Text = "Action:" };
            Add (lblAction);

            TextAction = new Editor { X = Pos.Left (TextTitle), Y = Pos.Top (lblAction), Width = Dim.Fill (), Height = 5 };
            Add (TextAction);

            // A HotKey can be set here without a HotKeySpecifier in the title.
            // Width 2 fits one character plus the cursor. The field keeps a single entry.
            Label lblHotKey = new () { X = Pos.Left (lblTitle), Y = Pos.Bottom (lblAction) + 5, Text = "HotKey:" };
            Add (lblHotKey);

            TextHotKey = new TextField { X = Pos.Left (TextTitle), Y = Pos.Bottom (lblAction) + 5, Width = 2 };

            TextHotKey.TextChanging += (_, e) =>
                                       {
                                           if (string.IsNullOrEmpty (e.Result))
                                           {
                                               return;
                                           }

                                           // One printable character. A further keystroke replaces it.
                                           Rune hotKey = e.Result.EnumerateRunes ().Last ();

                                           if (hotKey.GetColumns () != 1 || Rune.IsControl (hotKey) || hotKey == (Rune)' ')
                                           {
                                               e.Handled = true;

                                               return;
                                           }

                                           string hotKeyText = hotKey.ToString ();

                                           if (hotKeyText.Length == 1 && char.IsLower (hotKeyText [0]))
                                           {
                                               hotKeyText = hotKeyText.ToUpperInvariant ();
                                           }

                                           e.Result = hotKeyText;
                                       };
            TextHotKey.TextChanged += (_, _) => TextHotKey.SelectAll ();
            TextHotKey.SelectAll ();
            Add (TextHotKey);

            // Line separators belong inside a Menu, not as MenuBar titles.
            CkbSeparator = new CheckBox { X = Pos.Left (lblTitle), Y = Pos.Bottom (lblHotKey) + 1, Text = "Separator" };
            Add (CkbSeparator);

            // Checked keeps the menu item or separator enabled.
            CkbEnabled = new CheckBox
            {
                X = Pos.Right (CkbSeparator) + 2,
                Y = Pos.Top (CkbSeparator),
                Text = "Enabled",
                Value = CheckState.Checked
            };
            Add (CkbEnabled);

            CkbSubMenu = new CheckBox
            {
                X = Pos.Left (lblTitle),
                Y = Pos.Bottom (CkbSeparator),
                Value = (_menuItem == null ? !_hasParent : HasSubMenus (_menuItem)) ? CheckState.Checked : CheckState.UnChecked,
                Text = "Has sub-menus"
            };
            Add (CkbSubMenu);

            // Checked sets true, unchecked sets false, and the empty state sets null.
            CkbAssignHotKeys = new CheckBox
            {
                X = Pos.Right (CkbSubMenu) + 2,
                Y = Pos.Top (CkbSubMenu),
                Text = "AssignHotKeys",
                AllowCheckStateNone = true,
                Value = CheckState.Checked
            };
            Add (CkbAssignHotKeys);

            CkbNullCheck = new CheckBox { X = Pos.Left (lblTitle), Y = Pos.Bottom (CkbSubMenu), Text = "Allow null checked" };
            Add (CkbNullCheck);

            RbChkStyle = new OptionSelector<DynamicMenuCheckStyle>
            {
                X = Pos.Left (lblTitle),
                Y = Pos.Bottom (CkbNullCheck) + 1,
                // Border title needs more width than the option labels alone provide.
                Width = Math.Max (14, "Check Style".GetColumns () + 4),
                Height = Dim.Auto (),
                Value = DynamicMenuCheckStyle.NoCheck,
                Title = "Check Style",
                AssignHotKeys = false,
                BorderStyle = LineStyle.Dotted
            };
            Add (RbChkStyle);
            RbChkStyle.ValueChanged += (_, _) =>
                                      {
                                          UpdateItemFieldsEnabled ();
                                      };

            Label lblShortcut = new () { X = Pos.Right (CkbAssignHotKeys) + 2, Y = Pos.Top (CkbSubMenu), Text = "Shortcut:" };
            Add (lblShortcut);

            TextShortcutKey = new TextField { X = Pos.X (lblShortcut), Y = Pos.Bottom (lblShortcut), Width = Dim.Fill (), ReadOnly = true };

            TextShortcutKey.KeyDown += (_, e) => { TextShortcutKey.Text = e.ToString (); };

            Add (TextShortcutKey);

            BtnClearShortcut = new Button { X = Pos.X (lblShortcut), Y = Pos.Bottom (TextShortcutKey) + 1, Text = "Clear Shortcut" };
            BtnClearShortcut.Accepting += (_, _) => { TextShortcutKey.Text = ""; };
            Add (BtnClearShortcut);

            CkbSeparator.ValueChanging += (_, e) =>
                                          {
                                              if (_loadingDetails
                                                  || e.NewValue != CheckState.Checked
                                                  || CkbSeparator.Value == CheckState.Checked
                                                  || !CurrentMenuHasSubMenus ())
                                              {
                                                  return;
                                              }

                                              int? result = MessageBox.Query (_app!,
                                                                              "Change menu type",
                                                                              "Changing this item to a separator will remove its sub-menus.",
                                                                              "Cancel",
                                                                              "Continue");

                                              if (result != 1)
                                              {
                                                  e.Handled = true;
                                              }
                                          };

            CkbSeparator.ValueChanged += (_, e) =>
                                         {
                                             if (_loadingDetails)
                                             {
                                                 return;
                                             }

                                             // Disabled when not usable; still guard so MenuBar slots never clear Title.
                                             if (!CanUseSeparator ())
                                             {
                                                 if (e.NewValue == CheckState.Checked)
                                                 {
                                                     _loadingDetails = true;

                                                     try
                                                     {
                                                         CkbSeparator.Value = CheckState.UnChecked;
                                                     }
                                                     finally
                                                     {
                                                         _loadingDetails = false;
                                                     }
                                                 }

                                                 UpdateItemFieldsEnabled ();

                                                 return;
                                             }

                                             if (e.NewValue == CheckState.Checked)
                                             {
                                                 _suppressSubMenuPrompts = true;

                                                 try
                                                 {
                                                     CkbSubMenu.Value = CheckState.UnChecked;
                                                 }
                                                 finally
                                                 {
                                                     _suppressSubMenuPrompts = false;
                                                 }

                                                 TextTitle.Text = "";
                                                 TextHelp.Text = "";
                                                 TextAction.Text = "";
                                                 TextHotKey.Text = "";
                                                 TextShortcutKey.Text = "";
                                                 UpdateItemFieldsEnabled ();

                                                 return;
                                             }

                                             // Suggest a title in Menu Details only — Ok commits the conversion.
                                             TextTitle.Text = "New";
                                             UpdateItemFieldsEnabled ();
                                         };

            CkbSubMenu.ValueChanging += (_, e) =>
                                        {
                                            if (_loadingDetails || _suppressSubMenuPrompts)
                                            {
                                                return;
                                            }

                                            if (e.NewValue == CheckState.Checked && CkbSeparator.Value == CheckState.Checked)
                                            {
                                                int? result = MessageBox.Query (_app!,
                                                                                "Change menu type",
                                                                                "Adding sub-menus will disable 'Separator'.",
                                                                                "Cancel",
                                                                                "Continue");

                                                if (result != 1)
                                                {
                                                    e.Handled = true;
                                                }

                                                return;
                                            }

                                            if (e.NewValue != CheckState.UnChecked || !CurrentMenuHasSubMenus ())
                                            {
                                                return;
                                            }

                                            int? removeResult = MessageBox.Query (_app!,
                                                                                  "Remove sub-menus",
                                                                                  "This menu already has sub-menus. Disabling Has sub-menus will remove them and enable the action for this menu.",
                                                                                  "Cancel",
                                                                                  "Continue");

                                            if (removeResult != 1)
                                            {
                                                e.Handled = true;
                                            }
                                        };

            CkbSubMenu.ValueChanged += (_, e) =>
                                       {
                                           if (_loadingDetails)
                                           {
                                               return;
                                           }

                                           if (e.NewValue == CheckState.Checked)
                                           {
                                               if (CkbSeparator.Value == CheckState.Checked)
                                               {
                                                   _loadingDetails = true;

                                                   try
                                                   {
                                                       CkbSeparator.Value = CheckState.UnChecked;
                                                   }
                                                   finally
                                                   {
                                                       _loadingDetails = false;
                                                   }

                                                   CkbSeparator.SetNeedsDraw ();

                                                   // Suggest title in Menu Details only; Ok commits separator → item.
                                                   if (string.IsNullOrEmpty (TextTitle.Text))
                                                   {
                                                       TextTitle.Text = "New";
                                                   }
                                               }

                                               TextHelp.Text = "";
                                               TextAction.Text = "";
                                               TextShortcutKey.Text = "";
                                           }

                                           UpdateItemFieldsEnabled ();
                                       };
        }

        public Button BtnClearShortcut { get; }
        public CheckBox CkbAssignHotKeys { get; }
        public CheckBox CkbEnabled { get; }
        public CheckBox CkbNullCheck { get; }
        public CheckBox CkbSeparator { get; }
        public CheckBox CkbSubMenu { get; }
        public OptionSelector<DynamicMenuCheckStyle> RbChkStyle { get; }
        public Editor TextAction { get; }
        public TextField TextHelp { get; }
        public TextField TextHotKey { get; }
        public TextField TextShortcutKey { get; }
        public TextField TextTitle { get; }

        public DynamicMenuItem CreateDynamicMenuItem () =>
            new ()
            {
                Title = TextTitle.Text,
                Help = TextHelp.Text,
                Action = TextAction.Text,
                HotKey = TextHotKey.Text,
                IsSeparator = CkbSeparator.Value == CheckState.Checked,
                Enabled = CkbEnabled.Value == CheckState.Checked,
                HasSubMenu = CkbSubMenu.Value == CheckState.Checked,
                AssignHotKeys = FromAssignHotKeysCheckState (CkbAssignHotKeys.Value),
                CheckStyle = RbChkStyle.Value ?? DynamicMenuCheckStyle.NoCheck,
                ShortcutKey = TextShortcutKey.Text,
                AllowNullChecked = CkbNullCheck.Enabled && CkbNullCheck.Value == CheckState.Checked
            };

        /// <summary>
        ///     Enables Title/Help/Action/HotKey/Shortcut for normal items; disables them for separators
        ///     and Help/Action/Shortcut for submenu hosts.
        /// </summary>
        private void UpdateItemFieldsEnabled ()
        {
            bool isSeparator = CkbSeparator.Value == CheckState.Checked;
            bool hasSubMenus = CkbSubMenu.Value == CheckState.Checked;
            bool allowAction = !isSeparator && !hasSubMenus;
            bool isMenuBarItem = _menuItem is MenuBarItem { SuperView: MenuBar }
                                 || (_menuItem is null && !_hasParent);
            bool supportsNullCheck = allowAction
                                     && !isMenuBarItem
                                     && RbChkStyle.Value == DynamicMenuCheckStyle.Checked;

            TextTitle.Enabled = !isSeparator;
            TextHelp.Enabled = allowAction;
            TextAction.Enabled = allowAction;
            TextHotKey.Enabled = !isSeparator;
            TextShortcutKey.Enabled = allowAction;
            BtnClearShortcut.Enabled = allowAction;
            CkbSubMenu.Enabled = !isSeparator;
            CkbAssignHotKeys.Enabled = !isSeparator;
            CkbNullCheck.Enabled = supportsNullCheck;
            RbChkStyle.Enabled = allowAction && !isMenuBarItem;

            if (!supportsNullCheck)
            {
                CkbNullCheck.Value = CheckState.UnChecked;
            }

            // Separators only inside a popover/submenu Menu (MenuBar : Menu, so SuperView-is-Menu is not enough).
            CkbSeparator.Enabled = CanUseSeparator ();
        }

        /// <summary>
        ///     <see langword="true"/> when the current details target can be a <see cref="Line"/> separator
        ///     (nested under a non-<see cref="MenuBar"/> <see cref="Menu"/>).
        /// </summary>
        private bool CanUseSeparator () => _separator is { } || _hasParent;

        internal bool IsEditingSeparator (Line line) => ReferenceEquals (_separator, line);

        public Action CreateAction (MenuItem menuItem, DynamicMenuItem item)
        {
            // Keep DynamicMenuItem on Data so Menu Details can round-trip the action text.
            menuItem.Data = item;

            return () => MessageBox.ErrorQuery (_app!, item.Title, item.Action, "Ok");
        }

        /// <summary>
        ///     Returns the <see cref="MenuItem.Action"/> for a command-view host.
        ///     CheckBox/Radio already toggle in <see cref="CheckBox.OnActivated"/> — Action only runs
        ///     the configured message (and radio peer exclusivity), never a second AdvanceCheckState.
        /// </summary>
        public Action CreateCommandViewAction (MenuItem menuItem, DynamicMenuItem item)
        {
            Action showAction = CreateAction (menuItem, item);

            if (menuItem.CommandView is CheckBox { RadioStyle: true } radio)
            {
                return () =>
                       {
                           SelectRadio (menuItem, radio);
                           showAction.Invoke ();
                       };
            }

            return showAction;
        }

        /// <summary>
        ///     Radio style: one radio <see cref="CheckBox"/> per menu item. Mutual exclusion is limited to the
        ///     contiguous radio run containing <paramref name="menuItem"/> — <see cref="Line"/> separators
        ///     (and any non-radio item) start a new group.
        /// </summary>
        private static void SelectRadio (MenuItem menuItem, CheckBox radio)
        {
            radio.Value = CheckState.Checked;

            if (menuItem.SuperView is not Menu menu)
            {
                return;
            }

            foreach (MenuItem peer in GetRadioGroupPeers (menu, menuItem))
            {
                if (peer == menuItem || peer.CommandView is not CheckBox { RadioStyle: true } peerRadio)
                {
                    continue;
                }

                peerRadio.Value = CheckState.UnChecked;
            }
        }

        /// <summary>
        ///     Radios in the same contiguous block as <paramref name="menuItem"/> (split by <see cref="Line"/> or non-radio items).
        /// </summary>
        internal static IEnumerable<MenuItem> GetRadioGroupPeers (Menu menu, MenuItem menuItem)
        {
            List<View> subViews = [.. menu.SubViews];
            int index = subViews.IndexOf (menuItem);

            if (index < 0 || !IsRadioMenuItem (menuItem))
            {
                yield break;
            }

            int start = index;

            while (start > 0 && IsRadioMenuItem (subViews [start - 1]))
            {
                start--;
            }

            int end = index;

            while (end < subViews.Count - 1 && IsRadioMenuItem (subViews [end + 1]))
            {
                end++;
            }

            for (int i = start; i <= end; i++)
            {
                if (subViews [i] is MenuItem peer)
                {
                    yield return peer;
                }
            }
        }

        private static bool IsRadioMenuItem (View view) =>
            view is MenuItem { CommandView: CheckBox { RadioStyle: true } };

        public void EditMenuBarItem (MenuItem? menuItem)
        {
            if (menuItem == null)
            {
                _hasParent = false;
                _separator = null;
                Enabled = false;
                CleanEditMenuBarItem ();

                return;
            }

            _loadingDetails = true;

            try
            {
                // Nested under a popover/submenu Menu. MenuBar : Menu, so exclude MenuBar slots.
                _hasParent = menuItem.SuperView is Menu and not MenuBar;
                Enabled = true;
                _menuItem = menuItem;
                _separator = null;
                TextTitle.Text = menuItem.Title;
                TextHelp.Text = menuItem.HelpText;

                TextAction.Text = GetActionText (menuItem);
                TextHotKey.Text = menuItem.HotKey != Key.Empty ? menuItem.HotKey.ToString () : "";
                CkbSeparator.Value = CheckState.UnChecked;
                CkbEnabled.Value = menuItem.Enabled ? CheckState.Checked : CheckState.UnChecked;
                CkbSubMenu.Value = HasSubMenus (menuItem) ? CheckState.Checked : CheckState.UnChecked;
                CkbAssignHotKeys.Value = ToAssignHotKeysCheckState (ReadAssignHotKeys (menuItem));
                CkbNullCheck.Value = menuItem.CommandView is CheckBox { AllowCheckStateNone: true } ? CheckState.Checked : CheckState.UnChecked;

                switch (menuItem.CommandView)
                {
                    case CheckBox { RadioStyle: true }:
                        RbChkStyle.Value = DynamicMenuCheckStyle.Radio;

                        break;

                    case CheckBox:
                        RbChkStyle.Value = DynamicMenuCheckStyle.Checked;

                        break;

                    default:
                        RbChkStyle.Value = DynamicMenuCheckStyle.NoCheck;

                        break;
                }

                TextShortcutKey.Text = menuItem.Key.ToString ();
            }
            finally
            {
                _loadingDetails = false;
                UpdateItemFieldsEnabled ();
            }
        }

        public void EditSeparator (Line? line)
        {
            if (line is null)
            {
                _separator = null;
                Enabled = false;
                CleanEditMenuBarItem ();

                return;
            }

            _loadingDetails = true;

            try
            {
                _hasParent = true;
                Enabled = true;
                _menuItem = null;
                _separator = line;
                TextTitle.Text = "";
                TextHelp.Text = "";
                TextAction.Text = "";
                TextHotKey.Text = "";
                TextShortcutKey.Text = "";
                CkbSeparator.Value = CheckState.Checked;
                CkbEnabled.Value = line.Enabled ? CheckState.Checked : CheckState.UnChecked;
                CkbSubMenu.Value = CheckState.UnChecked;
                CkbAssignHotKeys.Value = CheckState.Checked;
                CkbNullCheck.Value = CheckState.UnChecked;
                RbChkStyle.Value = DynamicMenuCheckStyle.NoCheck;
            }
            finally
            {
                _loadingDetails = false;
                UpdateItemFieldsEnabled ();
            }
        }

        public DynamicMenuItem? EnterMenuItem ()
        {
            var valid = false;

            Enabled = true;

            if (_menuItem == null && _separator is null)
            {
                _loadingDetails = true;

                try
                {
                    DynamicMenuItem m = new ();
                    TextTitle.Text = m.Title;
                    TextHelp.Text = m.Help;
                    TextAction.Text = m.Action;
                    TextHotKey.Text = m.HotKey;
                    CkbSeparator.Value = CheckState.UnChecked;
                    CkbEnabled.Value = m.Enabled ? CheckState.Checked : CheckState.UnChecked;
                    CkbSubMenu.Value = !_hasParent ? CheckState.Checked : CheckState.UnChecked;
                    CkbAssignHotKeys.Value = ToAssignHotKeysCheckState (m.AssignHotKeys);
                    CkbNullCheck.Value = CheckState.UnChecked;
                }
                finally
                {
                    _loadingDetails = false;
                    UpdateItemFieldsEnabled ();
                }
            }
            else if (_menuItem is { })
            {
                EditMenuBarItem (_menuItem);
            }

            Button btnOk = new () { Text = "Ok" };

            btnOk.Accepting += (_, e) =>
                               {
                                   if (CkbSeparator.Value != CheckState.Checked && string.IsNullOrEmpty (TextTitle.Text))
                                   {
                                       e.Handled = true;
                                       MessageBox.ErrorQuery (_app!, "Invalid title", "Must enter a valid title!.", "Ok");

                                       return;
                                   }

                                   valid = true;
                                   _app!.RequestStop ();
                               };
            Button btnCancel = new () { Text = "Cancel" };

            btnCancel.Accepting += (_, _) =>
                                   {
                                       TextTitle.Text = string.Empty;
                                       _app!.RequestStop ();
                                   };

            Dialog dialog = new () { Title = "Enter the menu details.", Buttons = [btnOk, btnCancel] };

            Width = Dim.Auto ();
            Height = Dim.Auto ();

            // Set the default accept button for the dialog because AddButton method always sets the last added button as default.
            btnOk.IsDefault = true;

            // Set the default accept view for the dialog explicitly, otherwise the btnOk.Accepting event doesn't get triggered.
            dialog.DefaultAcceptView = btnOk;
            dialog.Add (this);
            _app!.Run (dialog);
            dialog.Dispose ();

            if (!valid)
            {
                return null;
            }

            return CreateDynamicMenuItem ();
        }

        private void CleanEditMenuBarItem ()
        {
            _loadingDetails = true;

            try
            {
                TextTitle.Text = "";
                TextHelp.Text = "";
                TextAction.Text = "";
                TextHotKey.Text = "";
                CkbSeparator.Value = CheckState.UnChecked;
                CkbEnabled.Value = CheckState.Checked;
                CkbSubMenu.Value = CheckState.UnChecked;
                CkbAssignHotKeys.Value = CheckState.Checked;
                RbChkStyle.Value = DynamicMenuCheckStyle.NoCheck;
                TextShortcutKey.Text = "";
                UpdateItemFieldsEnabled ();
            }
            finally
            {
                _loadingDetails = false;
            }
        }

        private string GetActionText (MenuItem menuItem)
        {
            if (menuItem.Data is DynamicMenuItem data)
            {
                return data.Action;
            }

            if (menuItem.Action is null)
            {
                return string.Empty;
            }

            return GetTargetAction (menuItem.Action);
        }

        private string GetTargetAction (Action action)
        {
            object? me = action.Target;

            if (me is null)
            {
                return string.Empty;
            }

            foreach (FieldInfo field in me.GetType ().GetFields ())
            {
                if (field.Name == "item" && field.GetValue (me) is DynamicMenuItem item)
                {
                    return item.Action;
                }
            }

            return string.Empty;
        }

        /// <summary>
        ///     Reads <see cref="View.AssignHotKeys"/> from the submenu this item owns.
        ///     A leaf keeps the value on its <see cref="View.Data"/> until it gains a submenu.
        ///     <see langword="null"/> is the unset property.
        /// </summary>
        private static bool? ReadAssignHotKeys (MenuItem menuItem)
        {
            if (MenuHierarchy.GetMenu (menuItem) is { } menu)
            {
                return menu.AssignHotKeys;
            }

            if (menuItem.Data is DynamicMenuItem data)
            {
                return data.AssignHotKeys;
            }

            return null;
        }

        private static CheckState ToAssignHotKeysCheckState (bool? assignHotKeys) =>
            assignHotKeys switch
            {
                true => CheckState.Checked,
                false => CheckState.UnChecked,
                _ => CheckState.None
            };

        private static bool? FromAssignHotKeysCheckState (CheckState state) =>
            state switch
            {
                CheckState.Checked => true,
                CheckState.UnChecked => false,
                _ => null
            };

        private bool HasSubMenus (MenuItem menuItem)
        {
            if (menuItem is MenuBarItem menuBarItem)
            {
                return menuBarItem.PopoverMenu is { } || menuBarItem.SubMenu is { };
            }

            return menuItem.SubMenu is { };
        }

        private bool CurrentMenuHasSubMenus () { return _menuItem is { } menuItem && HasSubMenus (menuItem); }
    }

    public class DynamicMenuBarSample : Window
    {
        private readonly ListView _lstMenus;
        private MenuItem? _currentEditMenuBarItem;
        private MenuItem? _currentMenuBarItem;
        private int _currentSelectedMenuBar;
        private MenuBar? _dynamicMenuBar;

        public DynamicMenuItemModel DataContext { get; set; }

        public DynamicMenuBarSample ()
        {
            DataContext = new DynamicMenuItemModel ();

            Title = $"{Application.GetDefaultKey (Command.Quit)} to Quit - Scenario: Dynamic MenuBar";

            FrameView frmDelimiter = new ()
            {
                X = Pos.Center (),
                Y = 3,
                Width = 25,
                Height = 4,
                Title = "Shortcut Delimiter:"
            };

            TextField txtDelimiter = new () { X = Pos.Center (), Width = 2, Text = Key.Separator.ToString () };

            FrameView frmMenu = new () { Y = 7, Width = Dim.Percent (50), Height = Dim.Fill (), Title = "Menus:" };

            Button btnAddMenuBar = new () { Y = 1, Text = "Add a MenuBar" };
            frmMenu.Add (btnAddMenuBar);

            Button btnMenuBarUp = new () { X = Pos.Center (), Text = Glyphs.UpArrow.ToString () };
            frmMenu.Add (btnMenuBarUp);

            Button btnMenuBarDown = new () { X = Pos.Center (), Y = Pos.Bottom (btnMenuBarUp), Text = Glyphs.DownArrow.ToString () };
            frmMenu.Add (btnMenuBarDown);

            Button btnRemoveMenuBar = new () { Y = 1, Text = "Remove a MenuBar" };

            btnRemoveMenuBar.X = Pos.AnchorEnd (0) - (Pos.Right (btnRemoveMenuBar) - Pos.Left (btnRemoveMenuBar));
            frmMenu.Add (btnRemoveMenuBar);

            Button btnPrevious = new () { X = Pos.Left (btnAddMenuBar), Y = Pos.Top (btnAddMenuBar) + 2, Text = Glyphs.LeftArrow.ToString () };
            frmMenu.Add (btnPrevious);

            Button btnAdd = new () { Y = Pos.Top (btnPrevious) + 2, Text = " Add  " };
            btnAdd.X = Pos.AnchorEnd ();
            frmMenu.Add (btnAdd);

            Button btnNext = new () { X = Pos.X (btnAdd), Y = Pos.Top (btnPrevious), Text = Glyphs.RightArrow.ToString () };
            frmMenu.Add (btnNext);

            Label lblMenuBar = new ()
            {
                SchemeName = "Dialog",
                TextAlignment = Alignment.Center,
                X = Pos.Right (btnPrevious) + 1,
                Y = Pos.Top (btnPrevious),
                Width = Dim.Fill () - Dim.Func (_ => btnAdd.Frame.Width + 1),
                Height = 1
            };

            lblMenuBar.TextChanged += (_, _) =>
                                      {
                                          // Remove the HotKeySpecifier from the lblMenuBar text to prevent it from being triggered
                                          if (lblMenuBar.Text.Contains (lblMenuBar.HotKeySpecifier.ToString ()))
                                          {
                                              lblMenuBar.Text = lblMenuBar.Text.Replace (lblMenuBar.HotKeySpecifier.ToString (), "");
                                          }
                                      };
            frmMenu.Add (lblMenuBar);
            lblMenuBar.MousePositionTracking = true;
            lblMenuBar.CanFocus = true;

            Label lblParent = new ()
            {
                TextAlignment = Alignment.Center,
                X = Pos.Right (btnPrevious) + 1,
                Y = Pos.Top (btnPrevious) + 1,
                Width = Dim.Fill () - Dim.Width (btnAdd) - 1
            };
            lblParent.TextChanged += (_, _) =>
                                     {
                                         if (lblParent.Text.Contains ('_'))
                                         {
                                             lblParent.Text = lblParent.Text.Replace ("_", "");
                                         }
                                     };
            frmMenu.Add (lblParent);

            Button btnPreviousParent = new () { X = Pos.Left (btnAddMenuBar), Y = Pos.Top (btnPrevious) + 1, Text = ".." };
            frmMenu.Add (btnPreviousParent);

            _lstMenus = new ListView
            {
                SchemeName = "Dialog",
                X = Pos.Right (btnPrevious) + 1,
                Y = Pos.Top (btnPrevious) + 2,
                Width = lblMenuBar.Width,
                Height = Dim.Fill (1),
                Source = new ListWrapper<DynamicMenuItemList> ([])
            };
            frmMenu.Add (_lstMenus);

            frmMenu.Add (new Label { X = Pos.Left (_lstMenus), Y = Pos.Bottom (_lstMenus), Text = "Title, HotKey, Key, SubMenu" });

            Button btnRemove = new () { X = Pos.Left (btnAdd), Y = Pos.Top (btnAdd) + 1, Text = "Remove" };
            frmMenu.Add (btnRemove);

            Button btnUp = new () { X = Pos.Right (_lstMenus) + 2, Y = Pos.Top (btnRemove) + 2, Text = Glyphs.UpArrow.ToString () };
            frmMenu.Add (btnUp);

            Button btnDown = new () { X = Pos.Right (_lstMenus) + 2, Y = Pos.Top (btnUp) + 1, Text = Glyphs.DownArrow.ToString () };
            frmMenu.Add (btnDown);

            Add (frmMenu);

            Button btnHowToUse = new () { Text = "How to use", X = Pos.AnchorEnd () };
            btnHowToUse.Accepted += (_, _) => { ShowHowToUseDialog (); };
            Add (btnHowToUse);

            Button btnImportScript = new () { Text = "Import Script", X = Pos.AnchorEnd () };
            Add (btnImportScript);

            Button btnGenerateScript = new () { Text = "Generate Script", X = Pos.AnchorEnd () };
            Add (btnGenerateScript);

            DynamicMenuBarDetails frmMenuDetails = new ()
            {
                X = Pos.Right (frmMenu),
                Y = Pos.Top (frmMenu),
                Width = Dim.Fill (),
                Height = Dim.Fill (2),
                Title = "Menu Details:"
            };
            Add (frmMenuDetails);

            btnHowToUse.Y = Pos.Top (frmMenuDetails) - 3;
            btnImportScript.Y = Pos.Top (frmMenuDetails) - 2;
            btnGenerateScript.Y = Pos.Top (frmMenuDetails) - 1;

            btnMenuBarUp.Accepting += (_, _) =>
                                      {
                                          int i = _currentSelectedMenuBar;

                                          if (i < 0
                                              || _dynamicMenuBar is not { SubViews.Count: > 0 }
                                              || _dynamicMenuBar.SubViews.ElementAt (i) is not MenuBarItem menuItem
                                              || i <= 0)
                                          {
                                              return;
                                          }

                                          _dynamicMenuBar.MoveSubViewTowardsStart (menuItem);
                                          _currentSelectedMenuBar = i - 1;
                                          _currentMenuBarItem = menuItem;
                                          DataContext.Menus = [];
                                          SetListViewSource (_currentMenuBarItem, true);
                                          DataContext.MenuBar = menuItem.Title;
                                          UpdateParentLabel ();
                                      };

            btnMenuBarDown.Accepting += (_, _) =>
                                        {
                                            int i = _currentSelectedMenuBar;

                                            if (i < 0
                                                || _dynamicMenuBar is not { SubViews.Count: > 0 }
                                                || _dynamicMenuBar.SubViews.ElementAt (i) is not MenuBarItem menuItem
                                                || i >= _dynamicMenuBar.SubViews.Count - 1)
                                            {
                                                return;
                                            }

                                            _dynamicMenuBar.MoveSubViewTowardsEnd (menuItem);
                                            _currentSelectedMenuBar = i + 1;
                                            _currentMenuBarItem = menuItem;
                                            DataContext.Menus = [];
                                            SetListViewSource (_currentMenuBarItem, true);
                                            DataContext.MenuBar = menuItem.Title;
                                            UpdateParentLabel ();
                                        };

            btnUp.Accepting += (_, _) =>
                               {
                                   int? i = _lstMenus.SelectedItem;

                                   if (i is null or <= 0 || i.Value >= DataContext.Menus.Count)
                                   {
                                       return;
                                   }

                                   Menu? menu = GetMenuFromMenuItem (_currentMenuBarItem);
                                   DynamicMenuItemList entry = DataContext.Menus [i.Value];
                                   View? view = entry.MenuItem as View ?? entry.Separator;

                                   if (view is null || menu is null)
                                   {
                                       return;
                                   }

                                   menu.MoveSubViewTowardsStart (view);
                                   SetListViewSource (_currentMenuBarItem, true);
                                   _lstMenus.SelectedItem = i - 1;
                               };

            btnDown.Accepting += (_, _) =>
                                 {
                                     int? i = _lstMenus.SelectedItem;

                                     if (i is null or < 0 || i.Value >= DataContext.Menus.Count - 1)
                                     {
                                         return;
                                     }

                                     Menu? menu = GetMenuFromMenuItem (_currentMenuBarItem);
                                     DynamicMenuItemList entry = DataContext.Menus [i.Value];
                                     View? view = entry.MenuItem as View ?? entry.Separator;

                                     if (view is null || menu is null)
                                     {
                                         return;
                                     }

                                     menu.MoveSubViewTowardsEnd (view);
                                     SetListViewSource (_currentMenuBarItem, true);
                                     _lstMenus.SelectedItem = i + 1;
                                 };

            Button btnOk = new () { X = Pos.Right (frmMenu) + 20, Y = Pos.Bottom (frmMenuDetails), Text = "Ok" };
            Add (btnOk);

            Button btnCancel = new () { X = Pos.Right (btnOk) + 3, Y = Pos.Top (btnOk), Text = "Cancel" };
            btnCancel.Accepting += (_, _) => { SetFrameDetails (_currentEditMenuBarItem); };
            Add (btnCancel);

            // Wire after btnOk/btnCancel exist — import/generate local funcs sync those buttons.
            btnImportScript.Accepting += (_, _) => { ShowImportMenuScriptDialog (); };
            btnGenerateScript.Accepting += (_, _) => { ShowGeneratedMenuScriptDialog (); };

            btnPreviousParent.Accepting += (_, _) =>
                                           {
                                               if (_currentMenuBarItem is null)
                                               {
                                                   return;
                                               }

                                               MenuItem? parentItem = MenuHierarchy.GetParentItem (_currentMenuBarItem);
                                               if (parentItem is null)
                                               {
                                                   // Already at bar entry: show that bar item’s children again
                                                   SelectCurrentMenuBarItem ();
                                                   return;
                                               }

                                               MenuItem? was = _currentMenuBarItem;
                                               _currentMenuBarItem = parentItem;
                                               SetListViewSource (_currentMenuBarItem, true);
                                               UpdateParentLabel ();

                                               int i = DataContext.Menus.IndexOf (m => m.MenuItem == was);
                                               if (i >= 0)
                                               {
                                                   _lstMenus.SelectedItem = i;
                                               }

                                               SetFrameDetails (_currentMenuBarItem);
                                           };

            txtDelimiter.TextChanging += (_, e) =>
                                         {
                                             if (!string.IsNullOrEmpty (e.Result))
                                             {
                                                 Key.Separator = e.Result.ToRunes () [0];
                                             }
                                             else
                                             {
                                                 e.Handled = true;
                                                 txtDelimiter.SelectAll ();
                                             }
                                         };

            txtDelimiter.TextChanged += (_, _) =>
                                        {
                                            txtDelimiter.SelectAll ();
                                            SetFrameDetails ();
                                        };
            frmDelimiter.Add (txtDelimiter);
            txtDelimiter.SelectAll ();
            Add (frmDelimiter);

            _lstMenus.ValueChanged += (_, _) => { SetFrameDetailsFromListSelection (); };

            btnOk.Accepting += (_, e) =>
                               {
                                   DynamicMenuItem details = frmMenuDetails.CreateDynamicMenuItem ();

                                   if (!details.IsSeparator && string.IsNullOrEmpty (details.Title))
                                   {
                                       e.Handled = true;
                                       MessageBox.ErrorQuery (_app!, "Invalid title", "Must enter a valid title!.", "Ok");

                                       return;
                                   }

                                   CommitSelectedDetails ();
                               };

            btnAdd.Accepting += (_, _) =>
                                {
                                    if (_dynamicMenuBar == null)
                                    {
                                        MessageBox.ErrorQuery (_app!, "Menu Bar Error", "Must add a MenuBar first!", "Ok");
                                        btnAddMenuBar.SetFocus ();

                                        return;
                                    }

                                    if (_currentMenuBarItem is null && _dynamicMenuBar.SubViews.Count > 0)
                                    {
                                        _currentMenuBarItem = _dynamicMenuBar.SubViews.ElementAt (_currentSelectedMenuBar) as MenuItem;
                                    }

                                    MenuItem? parentMenuItem = _currentMenuBarItem;

                                    if (parentMenuItem is null)
                                    {
                                        return;
                                    }

                                    // Action-only bar items cannot host children until converted to Has sub-menus.
                                    if (!TryConvertActionOnlyToHasSubMenus (parentMenuItem))
                                    {
                                        return;
                                    }

                                    DynamicMenuBarDetails frameDetails = new (null, true);
                                    DynamicMenuItem? item = frameDetails.EnterMenuItem ();

                                    if (item == null)
                                    {
                                        return;
                                    }

                                    View created = CreateNewMenuEntry (item, parentMenuItem);
                                    SetListViewSource (parentMenuItem, true);

                                    int newIndex = -1;

                                    for (int i = 0; i < DataContext.Menus.Count; i++)
                                    {
                                        DynamicMenuItemList mi = DataContext.Menus [i];

                                        if ((created is not MenuItem menuItem || mi.MenuItem != menuItem) && (created is not Line line || mi.Separator != line))
                                        {
                                            continue;
                                        }
                                        newIndex = i;

                                        break;
                                    }

                                    if (newIndex > -1)
                                    {
                                        _lstMenus.SelectedItem = newIndex;
                                    }

                                    SetFrameDetailsFromListSelection ();
                                    _dynamicMenuBar.SetNeedsDraw ();
                                };

            btnRemove.Accepting += (_, _) =>
                                   {
                                       if (DataContext.Menus.Count == 0
                                           || _lstMenus.SelectedItem is not { } selIdx
                                           || selIdx < 0
                                           || selIdx >= DataContext.Menus.Count)
                                       {
                                           return;
                                       }

                                       DynamicMenuItemList entry = DataContext.Menus [selIdx];

                                       if (entry.Separator is { } line)
                                       {
                                           line.SuperView?.Remove (line);
                                           line.Dispose ();
                                       }

                                       if (entry.MenuItem is { } menuItem)
                                       {
                                           if (_currentEditMenuBarItem == menuItem)
                                           {
                                               _currentEditMenuBarItem = null;
                                           }

                                           ReleaseMenuItemShortcuts (menuItem);
                                           menuItem.SuperView?.Remove (menuItem);
                                           menuItem.Dispose ();
                                       }

                                       DataContext.Menus.RemoveAt (selIdx);

                                       // Same index is the following entry. Past the end, keep the new last one.
                                       if (DataContext.Menus.Count == 0)
                                       {
                                           _lstMenus.SelectedItem = null;
                                           ShowMenuDetails (_currentMenuBarItem);
                                       }
                                       else
                                       {
                                           _lstMenus.SelectedItem = Math.Min (selIdx, DataContext.Menus.Count - 1);
                                           SetFrameDetailsFromListSelection ();
                                       }

                                       _lstMenus.SetNeedsDraw ();
                                   };

            _lstMenus.Accepted += (_, _) =>
                                  {
                                      if (_lstMenus.SelectedItem is not { } idx
                                          || idx < 0
                                          || idx >= DataContext.Menus.Count)
                                      {
                                          return;
                                      }

                                      MenuItem? chosen = DataContext.Menus [idx].MenuItem;

                                      if (chosen is null)
                                      {
                                          return;
                                      }

                                      if (MenuHierarchy.GetMenu (chosen) is null)
                                      {
                                          MessageBox.ErrorQuery (_app!, "Menu Open Error", "Must allow sub menus first!", "Ok");

                                          return;
                                      }

                                      _currentMenuBarItem = chosen;
                                      UpdateParentLabel ();
                                      SetListViewSource (chosen, true);
                                      SetFrameDetailsFromListSelection ();
                                  };

            _lstMenus.HasFocusChanging += (_, e) =>
                                          {
                                              if (e.NewFocused != _lstMenus)
                                              {
                                                  return;
                                              }

                                              SetFrameDetailsFromListSelection ();
                                          };

            btnNext.Accepting += (_, _) =>
                                 {
                                     if (_dynamicMenuBar != null && _currentSelectedMenuBar + 1 < _dynamicMenuBar.SubViews.Count)
                                     {
                                         _currentSelectedMenuBar++;
                                     }

                                     SelectCurrentMenuBarItem ();
                                 };

            btnPrevious.Accepting += (_, _) =>
                                     {
                                         if (_currentSelectedMenuBar - 1 > -1)
                                         {
                                             _currentSelectedMenuBar--;
                                         }

                                         SelectCurrentMenuBarItem ();
                                     };

            lblMenuBar.HasFocusChanging += (_, e) =>
                                           {
                                               if (e.NewFocused == lblMenuBar && _dynamicMenuBar?.SubViews.Count > 0)
                                               {
                                                   _currentMenuBarItem = _dynamicMenuBar.SubViews.ElementAt (_currentSelectedMenuBar) as MenuItem;
                                                   SetFrameDetails (_currentMenuBarItem);
                                               }
                                               else if (e.NewFocused == _lstMenus && _lstMenus.SelectedItem > -1)
                                               {
                                                   SetFrameDetailsFromListSelection ();
                                               }
                                           };

            btnAddMenuBar.Accepting += (_, _) =>
                                       {
                                           DynamicMenuBarDetails frameDetails = new (null);
                                           DynamicMenuItem? item = frameDetails.EnterMenuItem ();

                                           if (item == null)
                                           {
                                               return;
                                           }

                                           if (_dynamicMenuBar == null)
                                           {
                                               _dynamicMenuBar = new MenuBar { AssignHotKeys = true };
                                            //    _dynamicMenuBar.AssignHotKeysToSubViews ();
                                               Add (_dynamicMenuBar);
                                           }

                                           MenuItem newMenu = CreateNewMenu (item);
                                           _dynamicMenuBar?.Add (newMenu);

                                           _currentMenuBarItem = newMenu;

                                           // After Add so App is available for BindKeyToApplication registration.
                                           ApplyMenuItemShortcut (newMenu, item.ShortcutKey);

                                           _currentSelectedMenuBar = _dynamicMenuBar!.SubViews.Count - 1;
                                           DataContext.MenuBar = newMenu.Title;
                                           UpdateParentLabel ();
                                           SetListViewSource (_currentMenuBarItem, true);
                                           SetFrameDetails ((_dynamicMenuBar.SubViews.ElementAt (_currentSelectedMenuBar) as MenuItem)!);
                                           _dynamicMenuBar.SetNeedsDraw ();
                                       };

            btnRemoveMenuBar.Accepting += (_, _) =>
                                          {
                                              if (_dynamicMenuBar is not { SubViews.Count: > 0 })
                                              {
                                                  return;
                                              }

                                              MenuItem? barItemToRemove = _dynamicMenuBar.SubViews.ElementAtOrDefault (_currentSelectedMenuBar) as MenuItem;

                                              if (barItemToRemove is null)
                                              {
                                                  return;
                                              }

                                              ReleaseMenuItemShortcuts (barItemToRemove);
                                              _dynamicMenuBar.Remove (barItemToRemove);

                                              if (barItemToRemove is MenuBarItem menuBarItem)
                                              {
                                                  ClearBarItemDropdown (menuBarItem);
                                              }

                                              barItemToRemove.Dispose ();

                                              // Same index is the following bar item. Past the end, keep the new last one.
                                              if (_currentSelectedMenuBar >= _dynamicMenuBar.SubViews.Count)
                                              {
                                                  _currentSelectedMenuBar = _dynamicMenuBar.SubViews.Count - 1;
                                              }

                                              _currentMenuBarItem = _currentSelectedMenuBar >= 0 && _dynamicMenuBar.SubViews.Count > 0
                                                                        ? _dynamicMenuBar.SubViews.ElementAt (_currentSelectedMenuBar) as MenuItem
                                                                        : null;
                                              _currentEditMenuBarItem = null;
                                              RemoveMenuBar ();
                                              SetListViewSource (_currentMenuBarItem, true);
                                              ShowMenuDetails (_currentMenuBarItem);
                                          };

            SetFrameDetails ();

            _ = new Binding (this, "MenuBar", lblMenuBar, "Text");
            _ = new Binding (this, "Parent", lblParent, "Text");
            _ = new Binding (this, "Menus", _lstMenus, "Source", new ListWrapperConverter<DynamicMenuItemList> ());

            return;

            void ShowHowToUseDialog ()
            {
                Label helpLabel = new ()
                {

                    Width = 68,
                    Text = """
                           Select a MenuBar or menu item before you add or remove entries.

                           Menus: Add a MenuBar first. Arrows choose and reorder bars.
                           Add inserts under the current menu. Remove deletes the selected row.
                           .. returns to the parent menu. Up and down reorder items.

                           Menu Details (press Ok to apply):
                           Title, Help, and Action describe the item.
                           HotKey is one character. An underscore in the title is optional.
                           Shortcut runs while the menu is closed. Delimiter sets its glyph.
                           Separator is a line inside a menu, not a bar title.
                           Has sub-menus lets that item hold children. Add then opens them.
                           Enabled shows the item. AssignHotKeys fills free letters in that menu.

                           Scripts: Generate Script writes C# and a DYNAMIC_MENUBAR_IMPORT block.
                           Import Script reads that block and rebuilds the MenuBar.
                           """,
                    // The import marker name contains underscores. Leave them visible.
                    HotKeySpecifier = new Rune ('\uFFFF')

                };
                helpLabel.TextFormatter.WordWrap = true;
                helpLabel.TextFormatter.MultiLine = true;

                Button btnClose = new () { Text = "Close", IsDefault = true };

                Dialog dialog = new ()
                {
                    Title = "How to use Dynamic MenuBar",
                    Buttons = [btnClose]
                };
                dialog.Add (helpLabel);
                App!.Run (dialog);
                dialog.Dispose ();
            }

            void ShowGeneratedMenuScriptDialog ()
            {
                if (_dynamicMenuBar is null || _dynamicMenuBar.SubViews.Count == 0)
                {
                    MessageBox.ErrorQuery (App!, "Generate Script", "Add at least one MenuBar item first.", "Ok");

                    return;
                }

                string script = MenuScriptGenerator.Generate (_dynamicMenuBar);

                Editor scriptView = new ()
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill (),
                    Height = Dim.Fill (1),
                    Text = script,
                    ReadOnly = true,
                    WordWrap = false
                };

                Button btnCopy = new () { Text = "Copy" };
                btnCopy.Accepting += (_, e) =>
                                     {
                                         e.Handled = true;

                                         if (App?.Clipboard?.TrySetClipboardData (scriptView.Text) == true)
                                         {
                                             return;
                                         }

                                         MessageBox.ErrorQuery (App!, "Clipboard", "Could not copy to the clipboard.", "Ok");
                                     };

                Button btnClose = new () { Text = "Close", IsDefault = true };

                Dialog dialog = new ()
                {
                    Title = "Generated Menu Script",
                    Width = Dim.Percent (80),
                    Height = Dim.Percent (80),
                    Buttons = [btnCopy, btnClose]
                };
                dialog.Add (scriptView);
                App!.Run (dialog);
                dialog.Dispose ();
            }

            void ShowImportMenuScriptDialog ()
            {
                Editor scriptView = new ()
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill (),
                    Height = Dim.Fill (1),
                    Text = "",
                    WordWrap = false
                };

                if (App?.Clipboard?.TryGetClipboardData (out string clip) == true
                    && !string.IsNullOrWhiteSpace (clip)
                    && (clip.Contains (MenuScriptGenerator.ImportBeginMarker, StringComparison.Ordinal)
                        || clip.TrimStart ().StartsWith ('{')))
                {
                    scriptView.Text = clip;
                }

                var imported = false;

                Button btnImport = new () { Text = "Import", IsDefault = true };
                btnImport.Accepting += (_, e) =>
                                       {
                                           if (!MenuScriptImporter.TryParse (scriptView.Text, out MenuScriptDocument? document)
                                               || document is null)
                                           {
                                               e.Handled = true;
                                               MessageBox.ErrorQuery (
                                                                      App!,
                                                                      "Import Script",
                                                                      "Could not find a Dynamic MenuBar import payload.\nPaste a script from Generate Script (includes the DYNAMIC_MENUBAR_IMPORT block).",
                                                                      "Ok");

                                               return;
                                           }

                                           ApplyImportedMenuBar (document);
                                           imported = true;
                                       };

                Button btnCancelImport = new () { Text = "Cancel" };

                Dialog dialog = new ()
                {
                    Title = "Import Menu Script",
                    Width = Dim.Percent (80),
                    Height = Dim.Percent (80),
                    Buttons = [btnImport, btnCancelImport]
                };
                dialog.Add (scriptView);
                App!.Run (dialog);
                dialog.Dispose ();

                if (imported)
                {
                    MessageBox.Query (App!, "Import Script", "MenuBar imported successfully.", "Ok");
                }
            }

            void ApplyImportedMenuBar (MenuScriptDocument document)
            {
                MenuBar replacement = BuildImportedMenuBar (document);

                if (_dynamicMenuBar is { })
                {
                    ReleaseMenuBarShortcuts (_dynamicMenuBar);
                    Remove (_dynamicMenuBar);
                    _dynamicMenuBar.Dispose ();
                }

                _dynamicMenuBar = replacement;
                Add (_dynamicMenuBar);

                _currentSelectedMenuBar = _dynamicMenuBar.SubViews.Count > 0 ? 0 : -1;
                _currentMenuBarItem = null;
                _currentEditMenuBarItem = null;
                SelectCurrentMenuBarItem ();
                _dynamicMenuBar.SetNeedsDraw ();
            }

            MenuBar BuildImportedMenuBar (MenuScriptDocument document)
            {
                MenuBar menuBar = new () { AssignHotKeys = true };

                foreach (MenuScriptBarItemModel barModel in document.BarItems)
                {
                    DynamicMenuItem barItem = MenuScriptImporter.ToDynamicMenuItem (barModel);
                    MenuBarItem created = (MenuBarItem)CreateNewMenuEntry (barItem);
                    menuBar.Add (created);
                    ApplyMenuItemShortcut (created, barModel.ShortcutKey);

                    if (barModel.Children is { Count: > 0 })
                    {
                        ImportMenuChildren (created, barModel.Children);
                    }
                }

                return menuBar;
            }

            void ImportMenuChildren (MenuItem parent, List<MenuScriptEntryModel> children)
            {
                foreach (MenuScriptEntryModel child in children)
                {
                    DynamicMenuItem item = MenuScriptImporter.ToDynamicMenuItem (child);
                    View created = CreateNewMenuEntry (item, parent);

                    if (child.Children is not { Count: > 0 } || created is not MenuItem menuItem)
                    {
                        continue;
                    }

                    ImportMenuChildren (menuItem, child.Children);
                }
            }

            void RemoveMenuBar ()
            {
                if (_dynamicMenuBar != null && _currentMenuBarItem == null && _dynamicMenuBar?.SubViews.Count == 0)
                {
                    Remove (_dynamicMenuBar);
                    _dynamicMenuBar.Dispose ();
                    _dynamicMenuBar = null;
                    DataContext.Menus = [];
                    _currentSelectedMenuBar = -1;
                    DataContext.MenuBar = string.Empty;
                    UpdateParentLabel ();
                }
                else
                {
                    DataContext.MenuBar = _dynamicMenuBar?.SubViews.ElementAt (_currentSelectedMenuBar).Title!;
                    UpdateParentLabel ();
                }
            }

            // Shows <paramref name="menuItem"/> in Menu Details. A null item clears the form.
            void ShowMenuDetails (MenuItem? menuItem)
            {
                if (menuItem is null)
                {
                    _currentEditMenuBarItem = null;
                    frmMenuDetails.EditMenuBarItem (null);
                    SyncDetailsButtonsEnabled ();

                    return;
                }

                SetFrameDetails (menuItem);
            }

            void SetFrameDetailsFromListSelection ()
            {
                if (_lstMenus.SelectedItem is { } idx and >= 0
                    && idx < DataContext.Menus.Count)
                {
                    DynamicMenuItemList entry = DataContext.Menus [idx];

                    if (entry.Separator is { } line)
                    {
                        _currentEditMenuBarItem = null;
                        frmMenuDetails.EditSeparator (line);
                        SyncDetailsButtonsEnabled ();

                        return;
                    }

                    SetFrameDetails (entry.MenuItem);

                    return;
                }

                SetFrameDetails (_currentEditMenuBarItem);
            }

            void SetFrameDetails (MenuItem? menuBarItem = null)
            {
                MenuItem? menuItem;

                if (menuBarItem == null)
                {
                    if (_lstMenus.SelectedItem is { } idx and >= 0
                        && idx < DataContext.Menus.Count
                        && DataContext.Menus [idx].Separator is { } line)
                    {
                        _currentEditMenuBarItem = null;
                        frmMenuDetails.EditSeparator (line);
                        SyncDetailsButtonsEnabled ();

                        return;
                    }

                    menuItem = _lstMenus.SelectedItem > -1 && DataContext.Menus.Count > 0 && _lstMenus.SelectedItem.HasValue
                                   ? DataContext.Menus [_lstMenus.SelectedItem.Value].MenuItem
                                   : _currentEditMenuBarItem;
                }
                else
                {
                    menuItem = menuBarItem;
                }

                _currentEditMenuBarItem = menuItem;
                frmMenuDetails.EditMenuBarItem (menuItem);
                SyncDetailsButtonsEnabled ();
            }

            void SyncDetailsButtonsEnabled ()
            {
                if (btnOk.Enabled == frmMenuDetails.Enabled)
                {
                    return;
                }

                btnOk.Enabled = frmMenuDetails.Enabled;
                btnCancel.Enabled = frmMenuDetails.Enabled;
            }

            void SelectCurrentMenuBarItem ()
            {
                MenuBarItem? menuBarItem = null;

                if (_dynamicMenuBar?.SubViews is { Count: > 0 })
                {
                    menuBarItem = GetElementAt<MenuBarItem> (_dynamicMenuBar, _currentSelectedMenuBar);

                    if (menuBarItem is { })
                    {
                        DataContext.MenuBar = menuBarItem.Title;
                    }
                }

                SetFrameDetails (menuBarItem);
                _currentMenuBarItem = menuBarItem;
                DataContext.Menus = [];
                SetListViewSource (_currentMenuBarItem, true);
                UpdateParentLabel ();

                if (_currentMenuBarItem is null)
                {
                    DataContext.MenuBar = string.Empty;
                }
            }

            T? GetElementAt<T> (View view, int index)
            {
                if (index < 0 || index >= view.SubViews.Count)
                {
                    return default (T?);
                }

                if (view.SubViews.ElementAt (index) is T item)
                {
                    return item;
                }

                return default (T?);
            }

            Menu? GetMenuFromMenuItem (MenuItem? menuItem)
            {
                if (menuItem is MenuBarItem menuBarItem)
                {
                    return menuBarItem.PopoverMenu?.Root ?? menuBarItem.SubMenu;
                }

                return menuItem?.SubMenu;
            }

            void SetListViewSource (MenuItem? current, bool fill = false)
            {
                DataContext.Menus = [];
                if (!fill || current is null)
                {
                    return;
                }

                Menu? menu = MenuHierarchy.GetMenu (current);

                if (menu is null)
                {
                    return;
                }

                foreach (View sub in menu.SubViews)
                {
                    switch (sub)
                    {
                        case MenuItem child:
                            DataContext.Menus.Add (new DynamicMenuItemList
                            {
                                Title = child.Title,
                                MenuItem = child
                            });

                            break;

                        case Line line:
                            DataContext.Menus.Add (new DynamicMenuItemList
                            {
                                Title = "─ Separator ─",
                                Separator = line
                            });

                            break;
                    }
                }
            }

            void UpdateParentLabel ()
            {
                // lblMenuBar = current MenuBar slot. lblParent = nested host you drilled into
                // (e.g. New1 after Accepted on a submenu item). Empty when viewing a bar entry.
                if (_currentMenuBarItem is null || _currentMenuBarItem.SuperView is MenuBar)
                {
                    DataContext.Parent = string.Empty;

                    return;
                }

                DataContext.Parent = _currentMenuBarItem.Title;
            }

            void ClearBarItemDropdown (MenuBarItem barItem)
            {
                // Detach any PopoverMenu/SubMenu so Activate runs the Action instead of opening a
                // (possibly empty) dropdown. Remove SubViews before Dispose — same pattern as
                // MenuBarItem.EndInit — to avoid cascade dispose into nested SubMenus.
                PopoverMenu? popover = barItem.PopoverMenu;
                barItem.PopoverMenu = null;

                if (popover is { })
                {
                    if (popover.Visible)
                    {
                        popover.Visible = false;
                    }

                    if (popover.Root is { } root)
                    {
                        ReleaseMenuShortcuts (root);
                        List<View> items = [.. root.SubViews];
                        root.RemoveAll ();

                        foreach (View child in items)
                        {
                            child.Dispose ();
                        }
                    }

                    popover.RemoveAll ();
                    barItem.App?.Popovers?.DeRegister (popover);
                    popover.Dispose ();
                }

                ClearMenuItemSubMenu (barItem);
            }

            // Detaches and disposes <see cref="MenuItem.SubMenu"/> (and its SubViews) so the item becomes action-only.
            void ClearMenuItemSubMenu (MenuItem menuItem)
            {
                if (menuItem.SubMenu is not { } subMenu)
                {
                    return;
                }

                menuItem.SubMenu = null;

                List<View> items = [.. subMenu.SubViews];
                ReleaseMenuShortcuts (subMenu);
                subMenu.RemoveAll ();

                foreach (View child in items)
                {
                    child.Dispose ();
                }

                subMenu.Dispose ();
            }

            void ApplyActionOnlyBarItem (MenuBarItem barItem, DynamicMenuItem item)
            {
                // Action-only MenuBarItem: no PopoverMenu/SubMenu. MenuBar BubblingUp cancels
                // Activate before RaiseActivated when there is no open dropdown, so Shortcut.Action
                // would never run. Invoke Action from Activating and mark Handled.
                ClearBarItemDropdown (barItem);

                barItem.Title = item.Title;
                barItem.HelpText = item.Help;
                barItem.Action = frmMenuDetails.CreateAction (barItem, item);
                barItem.Activating -= OnActionOnlyBarItemActivating;
                barItem.Activating += OnActionOnlyBarItemActivating;

                if (barItem.CommandView is not { } commandView)
                {
                    return;
                }
                commandView.CanFocus = false;
                commandView.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;

                // Shortcut is applied by the caller after Add so App is available for BindKeyToApplication.
            }

            // Empty text drops a programmatic HotKey and lets the title specifier (or AssignHotKeys) decide.
            // A non-empty value is applied even when the title has no HotKeySpecifier.
            void ApplyMenuItemHotKey (MenuItem menuItem, string? hotKeyText)
            {
                if (string.IsNullOrWhiteSpace (hotKeyText))
                {
                    ClearExplicitHotKey (menuItem);

                    return;
                }

                if (!TryParseMenuHotKey (hotKeyText, out Key key))
                {
                    return;
                }

                // CommandView can disable the item specifier after copying a '_' title. Still drop a
                // marker that points at a different letter than the explicit HotKey.
                Rune specifier = menuItem.HotKeySpecifier == (Rune)'\xffff' ? (Rune)'_' : menuItem.HotKeySpecifier;

                if (TextFormatter.FindHotKey (menuItem.Title, specifier, out int hotKeyPos, out Key titleHotKey) && titleHotKey != key)
                {
                    menuItem.Title = TextFormatter.RemoveHotKeySpecifier (menuItem.Title, hotKeyPos, specifier);
                }

                menuItem.HotKey = key;
            }

            void ClearExplicitHotKey (MenuItem menuItem)
            {
                if (menuItem.HotKeySpecifier != (Rune)'\xffff'
                    && TextFormatter.FindHotKey (menuItem.Title, menuItem.HotKeySpecifier, out _, out Key titleHotKey)
                    && titleHotKey == menuItem.HotKey)
                {
                    return;
                }

                if (menuItem.HotKey != Key.Empty)
                {
                    menuItem.HotKey = Key.Empty;
                }

                string title = menuItem.Title;

                if (string.IsNullOrEmpty (title))
                {
                    return;
                }

                // Setting Title again re-reads a user-written specifier after the programmatic key is cleared.
                menuItem.Title = string.Empty;
                menuItem.Title = title;

                if (menuItem.SuperView is { AssignHotKeys: true } superView)
                {
                    superView.AssignHotKeysToSubViews ();
                }
            }

            // MenuItem.Key defaults to view-local HotKeyBindings (only while that item can receive hotkeys).
            // Shortcuts must use <see cref="Shortcut.BindKeyToApplication"/> so they fire while the menu is closed.
            // PopoverMenu items are not always in an App SuperView chain at construction — set <see cref="View.App"/>
            // explicitly so <c>AddApp</c> registers the binding.
            void ApplyMenuItemShortcut (MenuItem menuItem, string? shortcutKey)
            {
                ReleaseApplicationShortcut (menuItem);

                if (string.IsNullOrEmpty (shortcutKey) || !Key.TryParse (shortcutKey, out Key key) || key == Key.Empty)
                {
                    menuItem.Key = Key.Empty;

                    return;
                }

                IApplication? app = menuItem.App ?? App;

                if (app is { })
                {
                    menuItem.App = app;
                }

                menuItem.BindKeyToApplication = true;
                menuItem.Key = key;
            }

            void ReleaseMenuBarShortcuts (MenuBar menuBar)
            {
                foreach (MenuItem menuItem in menuBar.SubViews.OfType<MenuItem> ())
                {
                    ReleaseMenuItemShortcuts (menuItem);
                }
            }

            void ReleaseMenuItemShortcuts (MenuItem menuItem)
            {
                ReleaseApplicationShortcut (menuItem);

                if (menuItem is MenuBarItem { PopoverMenu.Root: { } root })
                {
                    ReleaseMenuShortcuts (root);
                }

                if (menuItem.SubMenu is { } subMenu)
                {
                    ReleaseMenuShortcuts (subMenu);
                }
            }

            void ReleaseMenuShortcuts (Menu menu)
            {
                foreach (MenuItem menuItem in menu.SubViews.OfType<MenuItem> ())
                {
                    ReleaseMenuItemShortcuts (menuItem);
                }
            }

            void ReleaseApplicationShortcut (MenuItem menuItem)
            {
                if (!menuItem.BindKeyToApplication || menuItem.Key == Key.Empty)
                {
                    return;
                }

                IApplication? app = menuItem.App ?? App;

                if (app?.Keyboard.KeyBindings.TryGet (menuItem.Key, out KeyBinding binding) != true
                    || !ReferenceEquals (binding.Target, menuItem))
                {
                    return;
                }

                app.Keyboard.KeyBindings.Remove (menuItem.Key);
            }

            // Enabled is copied onto the item. AssignHotKeys is stored on the submenu Menu, or on Data until one exists.
            void ApplyMenuItemFlags (MenuItem menuItem, DynamicMenuItem item)
            {
                menuItem.Data = item;
                menuItem.Enabled = item.Enabled;

                if (MenuHierarchy.GetMenu (menuItem) is not { } menu)
                {
                    return;
                }

                menu.AssignHotKeys = item.AssignHotKeys;
            }

            bool IsActionOnlyBarItem (MenuItem? item) =>
                item is MenuBarItem { SuperView: MenuBar, PopoverMenu: null, SubMenu: null, Action: not null };

            bool TryConvertActionOnlyToHasSubMenus (MenuItem parent)
            {
                if (!IsActionOnlyBarItem (parent))
                {
                    return true;
                }

                int? result = MessageBox.Query (_app!,
                                                "Change menu type",
                                                "Adding sub-menus will remove the action for this menu.",
                                                "Cancel",
                                                "Continue");

                if (result != 1)
                {
                    return false;
                }

                ConvertActionOnlyToHasSubMenus ((MenuBarItem)parent);

                return true;
            }

            void ConvertActionOnlyToHasSubMenus (MenuBarItem barItem)
            {
                barItem.Action = null;
                barItem.Activating -= OnActionOnlyBarItemActivating;
                MenuHierarchy.EnsureParentMenu (barItem);
                _currentMenuBarItem = barItem;
                _currentEditMenuBarItem = barItem;
                DataContext.MenuBar = barItem.Title;
                SetListViewSource (barItem, true);
                UpdateParentLabel ();
                SetFrameDetails (barItem);
            }

            View CreateNewMenuEntry (DynamicMenuItem item, MenuItem? parent = null)
            {
                if (parent is null)
                {
                    // Always a MenuBarItem on the MenuBar — never a plain MenuItem.
                    MenuBarItem barItem = new ()
                    {
                        Title = item.Title,
                        HelpText = item.Help
                    };

                    if (item.HasSubMenu)
                    {
                        Menu root = new ();
                        PopoverMenu popover = new () { Root = root };
                        barItem.PopoverMenu = popover;
                        root.SuperMenuItem = barItem;
                    }
                    else
                    {
                        ApplyActionOnlyBarItem (barItem, item);
                    }

                    // After Title is set. Add (below) keeps a programmatic HotKey when AssignHotKeys is on.
                    if (!string.IsNullOrWhiteSpace (item.HotKey))
                    {
                        ApplyMenuItemHotKey (barItem, item.HotKey);
                    }

                    ApplyMenuItemFlags (barItem, item);

                    return barItem;
                }

                MenuHierarchy.EnsureParentMenu (parent);
                Menu host = MenuHierarchy.GetMenu (parent)!;
                host.SuperMenuItem = parent;

                if (item.IsSeparator)
                {
                    Line separator = new ()
                    {
                        Enabled = item.Enabled
                    };
                    host.Add (separator);

                    return separator;
                }

                MenuItem menuItem = new ()
                {
                    Title = item.Title,
                    HelpText = item.Help
                };

                if (item.HasSubMenu)
                {
                    menuItem.SubMenu = new Menu ();
                    menuItem.Action = null;
                }
                else
                {
                    menuItem.CommandView = item.CreateCommandView ();
                    menuItem.Action = frmMenuDetails.CreateCommandViewAction (menuItem, item);

                    if (menuItem.CommandView is CheckBox { RadioStyle: false } checkBox)
                    {
                        checkBox.AllowCheckStateNone = item.AllowNullChecked;
                        checkBox.Value = item.AllowNullChecked ? CheckState.None : CheckState.UnChecked;
                    }
                }

                host.Add (menuItem);

                // After Add so App is available for BindKeyToApplication registration.
                if (!item.HasSubMenu)
                {
                    ApplyMenuItemShortcut (menuItem, item.ShortcutKey);
                }

                EnsureSoleRadioIsChecked (menuItem);

                // After Title and CommandView, so an explicit HotKey is kept without a title specifier.
                if (!string.IsNullOrWhiteSpace (item.HotKey))
                {
                    ApplyMenuItemHotKey (menuItem, item.HotKey);
                }

                ApplyMenuItemFlags (menuItem, item);

                return menuItem;
            }

            void EnsureSoleRadioIsChecked (MenuItem menuItem)
            {
                if (menuItem.CommandView is not CheckBox { RadioStyle: true } radio
                    || menuItem.SuperView is not Menu menu)
                {
                    return;
                }

                // First radio in a separator-bounded group starts checked.
                List<MenuItem> group = [.. DynamicMenuBarDetails.GetRadioGroupPeers (menu, menuItem)];

                if (group.Count != 1)
                {
                    return;
                }

                radio.Value = CheckState.Checked;
            }

            // Back-compat name used by Add MenuBar.
            MenuItem CreateNewMenu (DynamicMenuItem item, MenuItem? parent = null) =>
                (MenuItem)CreateNewMenuEntry (item, parent);

            void CommitSelectedDetails ()
            {
                DynamicMenuItem details = frmMenuDetails.CreateDynamicMenuItem ();

                if (_lstMenus.SelectedItem is { } idx and >= 0
                    && idx < DataContext.Menus.Count)
                {
                    DynamicMenuItemList entry = DataContext.Menus [idx];

                    if (entry.Separator is { } existingLine && frmMenuDetails.IsEditingSeparator (existingLine))
                    {
                        if (details.IsSeparator)
                        {
                            existingLine.Enabled = details.Enabled;

                            return;
                        }

                        // Title "New" in Menu Details is only a suggestion until Ok.
                        if (string.IsNullOrEmpty (details.Title))
                        {
                            return;
                        }

                        // Convert separator → MenuItem
                        Menu? host = existingLine.SuperView as Menu;
                        int lineIdx = host?.SubViews.IndexOf (existingLine) ?? -1;
                        host?.Remove (existingLine);
                        existingLine.Dispose ();

                        if (host is null || _currentMenuBarItem is null)
                        {
                            return;
                        }

                        MenuItem? parentItem = host.SuperMenuItem;
                        View created = CreateNewMenuEntry (details, parentItem ?? _currentMenuBarItem);
                        SetListViewSource (_currentMenuBarItem, true);

                        if (lineIdx >= 0 && created.SuperView is Menu createdHost)
                        {
                            while (createdHost.SubViews.IndexOf (created) > lineIdx)
                            {
                                createdHost.MoveSubViewTowardsStart (created);
                            }
                        }

                        SetListViewSource (_currentMenuBarItem, true);
                        SetFrameDetailsFromListSelection ();

                        return;
                    }

                    if (entry.MenuItem is { } menuItem && details.IsSeparator)
                    {
                        // Convert MenuItem → separator (MenuBar : Menu — never add Line to the bar).
                        if (menuItem.SuperView is not Menu host || host is MenuBar || _currentMenuBarItem is null)
                        {
                            return;
                        }

                        int itemIdx = host.SubViews.IndexOf (menuItem);
                        ReleaseMenuItemShortcuts (menuItem);
                        host.Remove (menuItem);
                        menuItem.Dispose ();

                        Line separator = new ()
                        {
                            Enabled = details.Enabled
                        };
                        host.Add (separator);

                        if (itemIdx >= 0)
                        {
                            while (host.SubViews.IndexOf (separator) > itemIdx)
                            {
                                host.MoveSubViewTowardsStart (separator);
                            }
                        }

                        SetListViewSource (_currentMenuBarItem, true);
                        _lstMenus.SelectedItem = Math.Min (idx, DataContext.Menus.Count - 1);
                        SetFrameDetailsFromListSelection ();

                        return;
                    }
                }

                if (_currentEditMenuBarItem is { } current)
                {
                    UpdateMenuItem (current, details, _lstMenus.SelectedItem);
                }
            }

            void UpdateMenuItem (MenuItem currentEditMenuBarItem, DynamicMenuItem menuItem, int? index)
            {
                currentEditMenuBarItem.Title = menuItem.Title;
                currentEditMenuBarItem.HelpText = menuItem.Help;

                if (currentEditMenuBarItem is MenuBarItem { SuperView: MenuBar } barItem)
                {
                    if (menuItem.HasSubMenu)
                    {
                        barItem.Action = null;
                        barItem.Activating -= OnActionOnlyBarItemActivating;
                        barItem.Data = menuItem;
                        MenuHierarchy.EnsureParentMenu (barItem);
                    }
                    else
                    {
                        // Action-only bar entry (Has sub-menus unchecked).
                        ApplyActionOnlyBarItem (barItem, menuItem);
                    }

                    ApplyMenuItemShortcut (barItem, menuItem.ShortcutKey);
                    ApplyMenuItemHotKey (barItem, menuItem.HotKey);
                    ApplyMenuItemFlags (barItem, menuItem);

                    DataContext.MenuBar = barItem.Title;
                    SetListViewSource (barItem, true);
                    UpdateParentLabel ();
                    SetFrameDetails (barItem);

                    return;
                }

                if (!menuItem.HasSubMenu)
                {
                    currentEditMenuBarItem.CommandView = menuItem.CreateCommandView ();
                }

                if (menuItem.HasSubMenu)
                {
                    currentEditMenuBarItem.Action = null;
                    ApplyMenuItemShortcut (currentEditMenuBarItem, string.Empty);

                    if (currentEditMenuBarItem.SubMenu is null)
                    {
                        currentEditMenuBarItem.SubMenu = new Menu ();
                    }

                    SetListViewSource (_currentMenuBarItem, true);
                }
                else
                {
                    // Must clear SubMenu before SetFrameDetails; otherwise EditMenuBarItem sees
                    // HasSubMenus still true and re-checks "Has sub-menus".
                    ClearMenuItemSubMenu (currentEditMenuBarItem);

                    currentEditMenuBarItem.Action = frmMenuDetails.CreateCommandViewAction (currentEditMenuBarItem, menuItem);
                    ApplyMenuItemShortcut (currentEditMenuBarItem, menuItem.ShortcutKey);

                    if (currentEditMenuBarItem.CommandView is CheckBox { RadioStyle: true })
                    {
                        EnsureSoleRadioIsChecked (currentEditMenuBarItem);
                    }
                    else if (currentEditMenuBarItem.CommandView is CheckBox checkBox)
                    {
                        checkBox.AllowCheckStateNone = menuItem.AllowNullChecked;
                        checkBox.Value = menuItem.AllowNullChecked ? CheckState.None : CheckState.UnChecked;
                    }

                    SetListViewSource (_currentMenuBarItem, true);
                }

                ApplyMenuItemHotKey (currentEditMenuBarItem, menuItem.HotKey);
                ApplyMenuItemFlags (currentEditMenuBarItem, menuItem);

                if (index is > -1 && index.Value < DataContext.Menus.Count)
                {
                    DataContext.Menus [index.Value] = new DynamicMenuItemList
                    {
                        Title = currentEditMenuBarItem.Title,
                        MenuItem = currentEditMenuBarItem
                    };
                }

                SetFrameDetails (currentEditMenuBarItem);
            }
        }

        private void OnActionOnlyBarItemActivating (object? sender, CommandEventArgs e)
        {
            if (sender is not MenuBarItem barItem)
            {
                return;
            }

            if (e.Context?.Routing == CommandRouting.Bridged)
            {
                return;
            }

            // Action-only bar entries always have Action set. Dropdown hosts clear Action.
            if (barItem.Action is null)
            {
                return;
            }

            // Prefer opening a real dropdown if one still has items (handler should not be attached
            // in that case; this is a safety check).
            Menu? root = barItem.PopoverMenu?.Root ?? barItem.SubMenu;

            if (root?.SubViews.OfType<MenuItem> ().Any () == true)
            {
                return;
            }

            // OnActivating may have toggled an empty leftover PopoverMenu open before this event.
            if (barItem.PopoverMenu is { Visible: true } popover)
            {
                popover.Visible = false;
            }

            if (barItem.SubMenu is { Visible: true } subMenu)
            {
                subMenu.HideMenu ();
            }

            barItem.Action.Invoke ();
            e.Handled = true;
        }
    }

    public enum DynamicMenuCheckStyle
    {
        NoCheck,
        Checked,
        Radio
    }

    public class DynamicMenuItem
    {
        public string Action { get; set; } = string.Empty;
        public bool AllowNullChecked { get; set; }
        public DynamicMenuCheckStyle CheckStyle { get; set; }

        /// <summary>
        ///     Value copied to <see cref="View.AssignHotKeys"/> on the submenu <see cref="Menu"/> this item owns.
        ///     <see langword="true"/> assigns hotkeys to that menu's subviews, <see langword="false"/> declines
        ///     automatic assignment, and <see langword="null"/> leaves the property unset.
        /// </summary>
        public bool? AssignHotKeys { get; set; } = true;

        /// <summary>
        ///     When <see langword="true"/>, the menu item or separator is created with <see cref="View.Enabled"/>
        ///     set to <see langword="true"/>.
        /// </summary>
        public bool Enabled { get; set; } = true;

        public bool HasSubMenu { get; set; }
        public string Help { get; set; } = string.Empty;
        public string HotKey { get; set; } = string.Empty;

        /// <summary>
        ///     When true, the entry is a <see cref="Line"/> separator inside a <see cref="Menu"/> (not a MenuBar slot).
        /// </summary>
        public bool IsSeparator { get; set; }

        public string ShortcutKey { get; set; } = string.Empty;
        public string Title { get; set; } = "New";

        public View CreateCommandView ()
        {
            switch (CheckStyle)
            {
                case DynamicMenuCheckStyle.NoCheck:
                    return new View { Id = "CommandView", Width = Dim.Auto (), Height = Dim.Auto (), Text = Title };

                case DynamicMenuCheckStyle.Checked:
                    return new CheckBox { Id = "CommandView", Width = Dim.Auto (), Height = Dim.Auto (), Text = Title };

                case DynamicMenuCheckStyle.Radio:
                    // One radio per menu item; peer exclusivity is handled in the scenario (SelectRadio).
                    return new CheckBox
                    {
                        Id = "CommandView",
                        Width = Dim.Auto (),
                        Height = Dim.Auto (),
                        RadioStyle = true,
                        CanFocus = false,
                        Text = Title
                    };

                default:
                    throw new ArgumentOutOfRangeException (nameof (CheckStyle), CheckStyle, null);
            }
        }
    }

    public class DynamicMenuItemList
    {
        public MenuItem? MenuItem { get; set; }

        /// <summary>
        ///     When set, this list entry represents a <see cref="Line"/> separator (mutually exclusive with <see cref="MenuItem"/>).
        /// </summary>
        public Line? Separator { get; set; }

        public string Title { get; set; } = string.Empty;

        public override string ToString ()
        {
            if (Separator is { })
            {
                return Title;
            }

            return $"{Title}, {MenuItem?.HotKey}, {MenuItem?.Key}, {MenuItem?.SubMenu is { }} ";
        }
    }

    public class DynamicMenuItemModel : INotifyPropertyChanged
    {
        public DynamicMenuItemModel () => Menus = [];

        public string MenuBar
        {
            get;
            set
            {
                if (value == field)
                {
                    return;
                }

                field = value;

                PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (GetPropertyName ()));
            }
        } = string.Empty;

        public ObservableCollection<DynamicMenuItemList> Menus
        {
            get;
            set
            {
                if (value == field)
                {
                    return;
                }

                field = value;

                PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (GetPropertyName ()));
            }
        }

        public string Parent
        {
            get;
            set
            {
                if (value == field)
                {
                    return;
                }

                field = value;

                PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (GetPropertyName ()));
            }
        } = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;
        public string? GetPropertyName ([CallerMemberName] string? propertyName = null) { return propertyName; }
    }

    public interface IValueConverter
    {
        object Convert (object value, object? parameter = null);
    }

    public class ListWrapperConverter<T> : IValueConverter
    {
        public object Convert (object value, object? parameter = null) { return new ListWrapper<T> ((ObservableCollection<T>)value); }
    }

    /// <summary>
    ///     Machine-readable payload embedded in generated scripts for Import Script round-trip.
    /// </summary>
    public class MenuScriptDocument
    {
        public int Version { get; set; } = 1;
        public List<MenuScriptBarItemModel> BarItems { get; set; } = [];
    }

    public class MenuScriptBarItemModel
    {
        public string Title { get; set; } = string.Empty;
        public string Help { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string ShortcutKey { get; set; } = string.Empty;

        /// <summary>
        ///     Hot key that is not encoded by a <see cref="View.HotKeySpecifier"/> in <see cref="Title"/>.
        /// </summary>
        public string HotKey { get; set; } = string.Empty;

        /// <summary>When <see langword="false"/>, the bar item is created with <see cref="View.Enabled"/> cleared.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///     Value copied to <see cref="View.AssignHotKeys"/> on the dropdown <see cref="Menu"/>.
        ///     <see langword="null"/> leaves the property unset.
        /// </summary>
        public bool? AssignHotKeys { get; set; }

        public bool HasSubMenu { get; set; }
        public List<MenuScriptEntryModel> Children { get; set; } = [];
    }

    public class MenuScriptEntryModel
    {
        public bool IsSeparator { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Help { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string ShortcutKey { get; set; } = string.Empty;

        /// <summary>
        ///     Hot key that is not encoded by a <see cref="View.HotKeySpecifier"/> in <see cref="Title"/>.
        /// </summary>
        public string HotKey { get; set; } = string.Empty;

        /// <summary>When <see langword="false"/>, the entry is created with <see cref="View.Enabled"/> cleared.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///     Value copied to <see cref="View.AssignHotKeys"/> on the submenu <see cref="Menu"/>.
        ///     <see langword="null"/> leaves the property unset.
        /// </summary>
        public bool? AssignHotKeys { get; set; }

        public bool HasSubMenu { get; set; }
        public bool AllowNullChecked { get; set; }
        public DynamicMenuCheckStyle CheckStyle { get; set; }
        public List<MenuScriptEntryModel> Children { get; set; } = [];
    }

    /// <summary>
    ///     Parses a hot key typed in Menu Details. Empty text is not a hot key.
    ///     Ctrl is rejected. Alt and Shift are removed so the value matches <see cref="View.HotKey"/>.
    /// </summary>
    private static bool TryParseMenuHotKey (string? text, out Key key)
    {
        key = Key.Empty;

        if (string.IsNullOrWhiteSpace (text))
        {
            return false;
        }

        if (!Key.TryParse (text.Trim (), out Key parsed))
        {
            return false;
        }

        if (parsed == Key.Empty || parsed.IsCtrl)
        {
            return false;
        }

        Key baseKey = parsed.NoAlt.NoCtrl;

        if (baseKey.IsKeyCodeAtoZ)
        {
            baseKey = baseKey.NoShift;
        }

        if (baseKey == Key.Empty || Rune.IsControl (baseKey.AsRune))
        {
            return false;
        }

        key = baseKey;

        return true;
    }

    /// <summary>
    ///     <see langword="true"/> when <paramref name="menuItem"/> has a <see cref="View.HotKey"/> that is not
    ///     produced by a <see cref="View.HotKeySpecifier"/> in its title.
    /// </summary>
    private static bool TryGetHotKeyWithoutSpecifier (MenuItem menuItem, out string hotKeyText)
    {
        hotKeyText = string.Empty;

        if (menuItem.HotKey == Key.Empty)
        {
            return false;
        }

        Rune specifier = menuItem.HotKeySpecifier == (Rune)'\xffff' ? (Rune)'_' : menuItem.HotKeySpecifier;

        if (TextFormatter.FindHotKey (menuItem.Title, specifier, out _, out Key titleHotKey) && titleHotKey == menuItem.HotKey)
        {
            return false;
        }

        hotKeyText = menuItem.HotKey.ToString ();

        return true;
    }

    /// <summary>
    ///     Emits pasteable Terminal.Gui v2 C# in the UICatalogRunnable style:
    ///     <c>new MenuBar ([ new MenuBarItem (title, CreateXxxMenuItems ()), ... ])</c>
    ///     with local helper functions returning <c>View []</c>, plus an import JSON block.
    /// </summary>
    private static class MenuScriptGenerator
    {
        // ReSharper disable once InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles

        public const string ImportBeginMarker = "DYNAMIC_MENUBAR_IMPORT_BEGIN";
#pragma warning restore IDE1006 // Naming Styles
        // ReSharper disable once InconsistentNaming

#pragma warning disable IDE1006 // Naming Styles

        public const string ImportEndMarker = "DYNAMIC_MENUBAR_IMPORT_END";
#pragma warning restore IDE1006 // Naming Styles

        // ReSharper disable once InconsistentNaming
        private static readonly JsonSerializerOptions JsonOptions = new ()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // Default encoder turns '+' into \u002B (e.g. Ctrl+F5). Keep keys human-readable.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new JsonStringEnumConverter () }
        };

        public static string Generate (MenuBar menuBar)
        {
            StringBuilder sb = new ();
            StringBuilder helpers = new ();
            int id = 0;
            List<string> barEntries = [];
            bool needsActionOnlyHelper = false;
            bool needsBarItemOptionsHelper = false;
            bool needsRadioHelper = false;

            foreach (View sub in menuBar.SubViews)
            {
                if (sub is not MenuBarItem barItem)
                {
                    continue;
                }

                Menu? dropdown = MenuHierarchy.GetMenu (barItem);

                if (dropdown is null)
                {
                    needsActionOnlyHelper = true;
                }
                else if (dropdown.AssignHotKeys is not null)
                {
                    needsBarItemOptionsHelper = true;
                }

                if (dropdown is { } && ContainsRadio (dropdown))
                {
                    needsRadioHelper = true;
                }

                barEntries.Add (FormatMenuBarItemEntry (barItem, helpers, ref id, indent: 27));
            }

            sb.AppendLine ("// Generated by UICatalog → Dynamic MenuBar (UICatalogRunnable style)");
            sb.AppendLine ("// Requires: using Terminal.Gui.App; using Terminal.Gui.Views; using Terminal.Gui.Input; using Terminal.Gui.ViewBase;");
            sb.AppendLine ("// Import Script reads the DYNAMIC_MENUBAR_IMPORT block at the end of this file.");
            sb.AppendLine ();
            sb.AppendLine ("MenuBar CreateMenuBar (IApplication app)");
            sb.AppendLine ("{");
            sb.AppendLine ("    MenuBar menuBar = new ([");

            for (int i = 0; i < barEntries.Count; i++)
            {
                string comma = i < barEntries.Count - 1 ? "," : "";
                sb.AppendLine ($"{barEntries [i]}{comma}");
            }

            sb.AppendLine ("                           ]);");
            sb.AppendLine ();
            sb.AppendLine ("    return menuBar;");

            if (helpers.Length > 0)
            {
                sb.AppendLine ();
                sb.Append (helpers);
            }

            if (needsActionOnlyHelper)
            {
                sb.AppendLine ();
                AppendActionOnlyHelpers (sb);
            }

            if (needsBarItemOptionsHelper)
            {
                sb.AppendLine ();
                AppendBarItemOptionsHelper (sb);
            }

            if (needsRadioHelper)
            {
                sb.AppendLine ();
                AppendRadioHelper (sb);
            }

            sb.AppendLine ();
            sb.AppendLine ("    Key ParseKey (string s) => Key.TryParse (s, out Key key) ? key : Key.Empty;");
            sb.AppendLine ("}");
            sb.AppendLine ();
            sb.AppendLine ("/*");
            sb.AppendLine (ImportBeginMarker);
            string json = JsonSerializer.Serialize (BuildDocument (menuBar), JsonOptions)
                                        .Replace ("*/", "*\\/", StringComparison.Ordinal);
            sb.AppendLine (json);
            sb.AppendLine (ImportEndMarker);
            sb.AppendLine ("*/");

            return sb.ToString ();
        }

        private static MenuScriptDocument BuildDocument (MenuBar menuBar)
        {
            MenuScriptDocument document = new ();

            foreach (View sub in menuBar.SubViews)
            {
                if (sub is not MenuBarItem barItem)
                {
                    continue;
                }

                document.BarItems.Add (ExportBarItem (barItem));
            }

            return document;
        }

        private static MenuScriptBarItemModel ExportBarItem (MenuBarItem barItem)
        {
            Menu? menu = MenuHierarchy.GetMenu (barItem);
            MenuScriptBarItemModel model = new ()
            {
                Title = barItem.Title,
                Help = barItem.HelpText,
                Action = GetStoredActionText (barItem),
                ShortcutKey = barItem.Key == Key.Empty ? string.Empty : barItem.Key.ToString (),
                HotKey = TryGetHotKeyWithoutSpecifier (barItem, out string hotKeyText) ? hotKeyText : string.Empty,
                Enabled = barItem.Enabled,
                AssignHotKeys = ReadStoredAssignHotKeys (barItem),
                HasSubMenu = menu is { }
            };

            if (menu is { })
            {
                model.Children = ExportMenuEntries (menu);
            }

            return model;
        }

        private static List<MenuScriptEntryModel> ExportMenuEntries (Menu menu)
        {
            List<MenuScriptEntryModel> entries = [];

            foreach (View sub in menu.SubViews)
            {
                switch (sub)
                {
                    case Line line:
                        entries.Add (new MenuScriptEntryModel
                        {
                            IsSeparator = true,
                            Enabled = line.Enabled
                        });

                        break;

                    case MenuItem menuItem:
                        entries.Add (ExportMenuItem (menuItem));

                        break;
                }
            }

            return entries;
        }

        private static MenuScriptEntryModel ExportMenuItem (MenuItem menuItem)
        {
            MenuScriptEntryModel model = new ()
            {
                Title = menuItem.Title,
                Help = menuItem.HelpText,
                Action = GetStoredActionText (menuItem),
                ShortcutKey = menuItem.Key == Key.Empty ? string.Empty : menuItem.Key.ToString (),
                HotKey = TryGetHotKeyWithoutSpecifier (menuItem, out string hotKeyText) ? hotKeyText : string.Empty,
                Enabled = menuItem.Enabled,
                AssignHotKeys = ReadStoredAssignHotKeys (menuItem),
                HasSubMenu = menuItem.SubMenu is { },
                CheckStyle = menuItem.CommandView switch
                {
                    CheckBox { RadioStyle: true } => DynamicMenuCheckStyle.Radio,
                    CheckBox => DynamicMenuCheckStyle.Checked,
                    _ => DynamicMenuCheckStyle.NoCheck
                },
                AllowNullChecked = menuItem.CommandView is CheckBox { AllowCheckStateNone: true }
            };

            if (menuItem.SubMenu is { } subMenu)
            {
                model.Children = ExportMenuEntries (subMenu);
            }

            return model;
        }

        private static string FormatMenuBarItemEntry (MenuBarItem barItem, StringBuilder helpers, ref int id, int indent)
        {
            string pad = new (' ', indent);
            Menu? dropdown = MenuHierarchy.GetMenu (barItem);

            if (dropdown is null)
            {
                return FormatActionOnlyBarItem (barItem, pad);
            }

            string helperName = NextHelperName ("Create", barItem.Title, "MenuItems", ref id);
            AppendViewArrayHelper (helpers, helperName, dropdown, ref id);

            string creation = WithBarItemInitializer ($"{pad}new MenuBarItem (\"{Escape (barItem.Title)}\", {helperName} ())", barItem);

            if (dropdown.AssignHotKeys is not { } assignHotKeys)
            {
                return creation;
            }

            // AssignHotKeys lives on the dropdown Menu, which MenuBarItem creates internally.
            return $"{pad}ApplyBarItemOptions ({creation.TrimStart ()}, {ToBoolLiteral (assignHotKeys)})";
        }

        private static string WithBarItemInitializer (string creation, MenuBarItem barItem)
        {
            List<string> parts = [];

            if (!barItem.Enabled)
            {
                parts.Add ("Enabled = false");
            }

            if (TryGetHotKeyWithoutSpecifier (barItem, out string hotKeyText))
            {
                parts.Add ($"HotKey = ParseKey (\"{Escape (hotKeyText)}\")");
            }

            if (parts.Count == 0)
            {
                return creation;
            }

            return $"{creation} {{ {string.Join (", ", parts)} }}";
        }

        private static string FormatActionOnlyBarItem (MenuBarItem barItem, string pad)
        {
            string actionText = GetStoredActionText (barItem);
            string helpArg = string.IsNullOrEmpty (barItem.HelpText) ? "null" : $"\"{Escape (barItem.HelpText)}\"";
            string keyArg = barItem.Key == Key.Empty ? "Key.Empty" : $"ParseKey (\"{Escape (barItem.Key.ToString ())}\")";
            string hotKeyArg = TryGetHotKeyWithoutSpecifier (barItem, out string hotKeyText) ? $", ParseKey (\"{Escape (hotKeyText)}\")" : "";
            string enabledArg = barItem.Enabled ? "" : ", enabled: false";

            // MenuBar cancels Activate before RaiseActivated when there is no dropdown — Action alone
            // never runs. CreateActionOnlyBarItem wires Activating → Action (see helper below).
            return $"{pad}CreateActionOnlyBarItem (\"{Escape (barItem.Title)}\", {helpArg}, () => MessageBox.ErrorQuery (app, \"{Escape (barItem.Title)}\", \"{Escape (actionText)}\", \"Ok\"), {keyArg}{hotKeyArg}{enabledArg})";
        }

        private static void AppendActionOnlyHelpers (StringBuilder sb)
        {
            sb.AppendLine ("    // Action-only MenuBarItem (no PopoverMenu/SubMenu): MenuBar BubblingUp cancels");
            sb.AppendLine ("    // Activate before RaiseActivated, so Shortcut.Action would never run. Invoke");
            sb.AppendLine ("    // Action from Activating and mark Handled.");
            sb.AppendLine ("    MenuBarItem CreateActionOnlyBarItem (string title, string? helpText, Action action, Key key, Key? hotKey = null, bool enabled = true)");
            sb.AppendLine ("    {");
            sb.AppendLine ("        MenuBarItem barItem = new ()");
            sb.AppendLine ("        {");
            sb.AppendLine ("            Title = title,");
            sb.AppendLine ("            HelpText = helpText ?? string.Empty,");
            sb.AppendLine ("            Action = action,");
            sb.AppendLine ("            // Required for shortcuts to work while the menu is closed (Key alone is view-local).");
            sb.AppendLine ("            BindKeyToApplication = true,");
            sb.AppendLine ("            Key = key");
            sb.AppendLine ("        };");
            sb.AppendLine ();
            sb.AppendLine ("        // HotKey without a title HotKeySpecifier. Null leaves title-based assignment alone.");
            sb.AppendLine ("        if (hotKey is { } explicitHotKey && explicitHotKey != Key.Empty)");
            sb.AppendLine ("        {");
            sb.AppendLine ("            barItem.HotKey = explicitHotKey;");
            sb.AppendLine ("        }");
            sb.AppendLine ();
            sb.AppendLine ("        barItem.Activating += OnActionOnlyBarItemActivating;");
            sb.AppendLine ();
            sb.AppendLine ("        if (barItem.CommandView is { } commandView)");
            sb.AppendLine ("        {");
            sb.AppendLine ("            commandView.CanFocus = false;");
            sb.AppendLine ("            commandView.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;");
            sb.AppendLine ("        }");
            sb.AppendLine ();
            sb.AppendLine ("        barItem.Enabled = enabled;");
            sb.AppendLine ();
            sb.AppendLine ("        return barItem;");
            sb.AppendLine ("    }");
            sb.AppendLine ();
            sb.AppendLine ("    void OnActionOnlyBarItemActivating (object? sender, CommandEventArgs e)");
            sb.AppendLine ("    {");
            sb.AppendLine ("        if (sender is not MenuBarItem barItem)");
            sb.AppendLine ("        {");
            sb.AppendLine ("            return;");
            sb.AppendLine ("        }");
            sb.AppendLine ();
            sb.AppendLine ("        if (e.Context?.Routing == CommandRouting.Bridged)");
            sb.AppendLine ("        {");
            sb.AppendLine ("            return;");
            sb.AppendLine ("        }");
            sb.AppendLine ();
            sb.AppendLine ("        if (barItem.Action is null)");
            sb.AppendLine ("        {");
            sb.AppendLine ("            return;");
            sb.AppendLine ("        }");
            sb.AppendLine ();
            sb.AppendLine ("        Menu? root = barItem.PopoverMenu?.Root ?? barItem.SubMenu;");
            sb.AppendLine ();
            sb.AppendLine ("        if (root?.SubViews.OfType<MenuItem> ().Any () == true)");
            sb.AppendLine ("        {");
            sb.AppendLine ("            return;");
            sb.AppendLine ("        }");
            sb.AppendLine ();
            sb.AppendLine ("        if (barItem.PopoverMenu is { Visible: true } popover)");
            sb.AppendLine ("        {");
            sb.AppendLine ("            popover.Visible = false;");
            sb.AppendLine ("        }");
            sb.AppendLine ();
            sb.AppendLine ("        if (barItem.SubMenu is { Visible: true } subMenu)");
            sb.AppendLine ("        {");
            sb.AppendLine ("            subMenu.Visible = false;");
            sb.AppendLine ("        }");
            sb.AppendLine ();
            sb.AppendLine ("        barItem.Action.Invoke ();");
            sb.AppendLine ("        e.Handled = true;");
            sb.AppendLine ("    }");
        }

        private static void AppendBarItemOptionsHelper (StringBuilder sb)
        {
            sb.AppendLine ("    // The dropdown Menu is created inside MenuBarItem. AssignHotKeys is set on that Menu.");
            sb.AppendLine ("    MenuBarItem ApplyBarItemOptions (MenuBarItem barItem, bool assignHotKeys)");
            sb.AppendLine ("    {");
            sb.AppendLine ("        if ((barItem.PopoverMenu?.Root ?? barItem.SubMenu) is { } menu)");
            sb.AppendLine ("        {");
            sb.AppendLine ("            menu.AssignHotKeys = assignHotKeys;");
            sb.AppendLine ("        }");
            sb.AppendLine ();
            sb.AppendLine ("        return barItem;");
            sb.AppendLine ("    }");
        }

        private static bool ContainsRadio (Menu menu)
        {
            foreach (View subView in menu.SubViews)
            {
                if (subView is not MenuItem menuItem)
                {
                    continue;
                }

                if (menuItem.CommandView is CheckBox { RadioStyle: true }
                    || (menuItem.SubMenu is { } subMenu && ContainsRadio (subMenu)))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AppendRadioHelper (StringBuilder sb)
        {
            sb.AppendLine ("    MenuItem ConfigureRadioMenuItem (MenuItem menuItem, Action action)");
            sb.AppendLine ("    {");
            sb.AppendLine ("        menuItem.Action = () =>");
            sb.AppendLine ("        {");
            sb.AppendLine ("            if (menuItem.CommandView is not CheckBox { RadioStyle: true } radio)");
            sb.AppendLine ("            {");
            sb.AppendLine ("                action ();");
            sb.AppendLine ("                return;");
            sb.AppendLine ("            }");
            sb.AppendLine ();
            sb.AppendLine ("            radio.Value = CheckState.Checked;");
            sb.AppendLine ();
            sb.AppendLine ("            if (menuItem.SuperView is Menu menu)");
            sb.AppendLine ("            {");
            sb.AppendLine ("                List<View> views = [.. menu.SubViews];");
            sb.AppendLine ("                int index = views.IndexOf (menuItem);");
            sb.AppendLine ("                int start = index;");
            sb.AppendLine ("                int end = index;");
            sb.AppendLine ();
            sb.AppendLine ("                while (start > 0 && IsRadio (views [start - 1]))");
            sb.AppendLine ("                {");
            sb.AppendLine ("                    start--;");
            sb.AppendLine ("                }");
            sb.AppendLine ();
            sb.AppendLine ("                while (end < views.Count - 1 && IsRadio (views [end + 1]))");
            sb.AppendLine ("                {");
            sb.AppendLine ("                    end++;");
            sb.AppendLine ("                }");
            sb.AppendLine ();
            sb.AppendLine ("                for (int i = start; i <= end; i++)");
            sb.AppendLine ("                {");
            sb.AppendLine ("                    if (views [i] is MenuItem peer");
            sb.AppendLine ("                        && peer != menuItem");
            sb.AppendLine ("                        && peer.CommandView is CheckBox { RadioStyle: true } peerRadio)");
            sb.AppendLine ("                    {");
            sb.AppendLine ("                        peerRadio.Value = CheckState.UnChecked;");
            sb.AppendLine ("                    }");
            sb.AppendLine ("                }");
            sb.AppendLine ("            }");
            sb.AppendLine ();
            sb.AppendLine ("            action ();");
            sb.AppendLine ("        };");
            sb.AppendLine ();
            sb.AppendLine ("        return menuItem;");
            sb.AppendLine ("    }");
            sb.AppendLine ();
            sb.AppendLine ("    bool IsRadio (View view) => view is MenuItem { CommandView: CheckBox { RadioStyle: true } };");
        }

        private static void AppendViewArrayHelper (StringBuilder helpers, string helperName, Menu menu, ref int id)
        {
            List<View> subs = [.. menu.SubViews];
            List<string> entries = [];

            for (int i = 0; i < subs.Count; i++)
            {
                entries.Add (FormatMenuChild (subs [i], helpers, ref id, indent: 8));
            }

            helpers.AppendLine ($"    View [] {helperName} () =>");
            helpers.AppendLine ("    [");

            for (int i = 0; i < entries.Count; i++)
            {
                string comma = i < entries.Count - 1 ? "," : "";
                helpers.AppendLine ($"{entries [i]}{comma}");
            }

            helpers.AppendLine ("    ];");
            helpers.AppendLine ();
        }

        private static string FormatMenuChild (View view, StringBuilder helpers, ref int id, int indent)
        {
            string pad = new (' ', indent);

            return view switch
            {
                Line line => FormatSeparator (line, pad),
                MenuItem menuItem => FormatMenuItem (menuItem, helpers, ref id, indent),
                _ => $"{pad}// unsupported: {view.GetType ().Name}"
            };
        }

        private static string FormatSeparator (Line line, string pad)
        {
            if (line.Enabled)
            {
                return $"{pad}new Line ()";
            }

            return $"{pad}new Line () {{ Enabled = false }}";
        }

        private static string FormatMenuItem (MenuItem menuItem, StringBuilder helpers, ref int id, int indent)
        {
            string pad = new (' ', indent);
            StringBuilder entry = new ();

            if (menuItem.SubMenu is { } subMenu)
            {
                entry.AppendLine ($"{pad}new MenuItem");
                entry.AppendLine ($"{pad}{{");
                entry.AppendLine ($"{pad}    Title = \"{Escape (menuItem.Title)}\",");
                AppendInitializerEnabled (entry, menuItem, pad);
                AppendInitializerHelpKey (entry, menuItem, pad);
                AppendInitializerHotKey (entry, menuItem, pad);

                string assignHotKeys = AssignHotKeysSuffix (subMenu);

                if (subMenu.SubViews.Count == 0)
                {
                    entry.AppendLine ($"{pad}    SubMenu = new Menu ([]){assignHotKeys}");
                }
                else if (subMenu.SubViews.Count <= 3)
                {
                    entry.AppendLine ($"{pad}    SubMenu = new Menu ([");
                    List<View> children = [.. subMenu.SubViews];

                    for (int i = 0; i < children.Count; i++)
                    {
                        string comma = i < children.Count - 1 ? "," : "";
                        entry.AppendLine ($"{FormatMenuChild (children [i], helpers, ref id, indent + 8)}{comma}");
                    }

                    entry.AppendLine ($"{pad}                        ]){assignHotKeys}");
                }
                else
                {
                    string helperName = NextHelperName ("Create", menuItem.Title, "SubMenuItems", ref id);
                    AppendViewArrayHelper (helpers, helperName, subMenu, ref id);
                    entry.AppendLine ($"{pad}    SubMenu = new Menu ({helperName} ()){assignHotKeys}");
                }

                entry.Append ($"{pad}}}");

                return entry.ToString ().TrimEnd ();
            }

            entry.AppendLine ($"{pad}new MenuItem");
            entry.AppendLine ($"{pad}{{");
            entry.AppendLine ($"{pad}    Title = \"{Escape (menuItem.Title)}\",");
            AppendInitializerEnabled (entry, menuItem, pad);
            AppendInitializerHelpKey (entry, menuItem, pad);
            AppendInitializerCommandView (entry, menuItem, pad);
            AppendInitializerHotKey (entry, menuItem, pad);

            string actionText = GetStoredActionText (menuItem);

            if (menuItem.CommandView is not CheckBox { RadioStyle: true })
            {
                entry.AppendLine ($"{pad}    Action = () => MessageBox.ErrorQuery (app, \"{Escape (menuItem.Title)}\", \"{Escape (actionText)}\", \"Ok\")");
            }

            entry.Append ($"{pad}}}");

            if (menuItem.CommandView is CheckBox { RadioStyle: true })
            {
                return $"""
                        {pad}ConfigureRadioMenuItem (
                        {entry.ToString ().TrimEnd ()},
                        {pad}    () => MessageBox.ErrorQuery (app, "{Escape (menuItem.Title)}", "{Escape (actionText)}", "Ok"))
                        """;
            }

            return entry.ToString ().TrimEnd ();
        }

        private static void AppendInitializerEnabled (StringBuilder entry, View view, string pad)
        {
            if (view.Enabled)
            {
                return;
            }

            entry.AppendLine ($"{pad}    Enabled = false,");
        }

        private static string AssignHotKeysSuffix (Menu menu) =>
            menu.AssignHotKeys switch
            {
                true => " { AssignHotKeys = true }",
                false => " { AssignHotKeys = false }",
                _ => ""
            };

        private static bool? ReadStoredAssignHotKeys (MenuItem menuItem)
        {
            if (MenuHierarchy.GetMenu (menuItem) is { } menu)
            {
                return menu.AssignHotKeys;
            }

            if (menuItem.Data is DynamicMenuItem data)
            {
                return data.AssignHotKeys;
            }

            return null;
        }

        private static void AppendInitializerHotKey (StringBuilder entry, MenuItem menuItem, string pad)
        {
            if (!TryGetHotKeyWithoutSpecifier (menuItem, out string hotKeyText))
            {
                return;
            }

            entry.AppendLine ($"{pad}    HotKey = ParseKey (\"{Escape (hotKeyText)}\"),");
        }

        private static void AppendInitializerHelpKey (StringBuilder entry, MenuItem menuItem, string pad)
        {
            if (!string.IsNullOrEmpty (menuItem.HelpText))
            {
                entry.AppendLine ($"{pad}    HelpText = \"{Escape (menuItem.HelpText)}\",");
            }

            if (menuItem.Key == Key.Empty)
            {
                return;
            }
            entry.AppendLine ($"{pad}    BindKeyToApplication = true,");
            entry.AppendLine ($"{pad}    Key = ParseKey (\"{Escape (menuItem.Key.ToString ())}\"),");
        }

        private static void AppendInitializerCommandView (StringBuilder entry, MenuItem menuItem, string pad)
        {
            switch (menuItem.CommandView)
            {
                case CheckBox { RadioStyle: true } radio:
                    entry.AppendLine (
                                      $"{pad}    CommandView = new CheckBox {{ Text = \"{Escape (menuItem.Title)}\", RadioStyle = true, CanFocus = false, Value = CheckState.{radio.Value} }},");

                    break;

                case CheckBox checkBox:
                    entry.AppendLine (
                                      $"{pad}    CommandView = new CheckBox {{ Text = \"{Escape (menuItem.Title)}\", AllowCheckStateNone = {ToBoolLiteral (checkBox.AllowCheckStateNone)}, CanFocus = false }},");

                    break;
            }
        }

        private static string GetStoredActionText (MenuItem menuItem)
        {
            if (menuItem.Data is DynamicMenuItem data)
            {
                return data.Action;
            }

            return string.Empty;
        }

        private static string NextHelperName (string prefix, string title, string suffix, ref int id)
        {
            id++;
            string cleaned = new (title.Where (static ch => char.IsAsciiLetterOrDigit (ch)).ToArray ());

            if (string.IsNullOrEmpty (cleaned))
            {
                cleaned = "Items";
            }

            if (char.IsAsciiDigit (cleaned [0]))
            {
                cleaned = "N" + cleaned;
            }

            return $"{prefix}{cleaned}{suffix}{id}";
        }

        private static string Escape (string? text) =>
            (text ?? string.Empty)
            // ReSharper disable once UseVerbatimString
            .Replace ("\\", "\\\\", StringComparison.Ordinal)
                .Replace ("\"", "\\\"", StringComparison.Ordinal)
                .Replace ("\r", "\\r", StringComparison.Ordinal)
                .Replace ("\n", "\\n", StringComparison.Ordinal);

        private static string ToBoolLiteral (bool value) => value ? "true" : "false";
    }

    /// <summary>
    ///     Parses the <see cref="MenuScriptGenerator.ImportBeginMarker"/> JSON block (or raw JSON) from a generated script.
    /// </summary>
    private static class MenuScriptImporter
    {
        // ReSharper disable once InconsistentNaming
        private static readonly JsonSerializerOptions JsonOptions = new ()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter () }
        };

        public static bool TryParse (string? script, out MenuScriptDocument? document)
        {
            document = null;

            if (string.IsNullOrWhiteSpace (script))
            {
                return false;
            }

            if (!TryExtractJson (script, out string json))
            {
                return false;
            }

            try
            {
                document = JsonSerializer.Deserialize<MenuScriptDocument> (json, JsonOptions);
            }
            catch (JsonException)
            {
                return false;
            }

            if (!IsValidDocument (document))
            {
                document = null;

                return false;
            }

            return true;
        }

        public static DynamicMenuItem ToDynamicMenuItem (MenuScriptBarItemModel model) =>
            new ()
            {
                Title = model.Title,
                Help = model.Help,
                Action = model.Action,
                ShortcutKey = model.ShortcutKey,
                HotKey = model.HotKey,
                Enabled = model.Enabled,
                AssignHotKeys = model.AssignHotKeys,
                HasSubMenu = model.HasSubMenu || model.Children is { Count: > 0 }
            };

        public static DynamicMenuItem ToDynamicMenuItem (MenuScriptEntryModel model) =>
            new ()
            {
                Title = model.Title,
                Help = model.Help,
                Action = model.Action,
                ShortcutKey = model.ShortcutKey,
                HotKey = model.HotKey,
                Enabled = model.Enabled,
                AssignHotKeys = model.AssignHotKeys,
                HasSubMenu = model.HasSubMenu || model.Children is { Count: > 0 },
                IsSeparator = model.IsSeparator,
                AllowNullChecked = model.AllowNullChecked,
                CheckStyle = model.CheckStyle
            };

        private static bool TryExtractJson (string script, out string json)
        {
            int beginMarker = FindMarkerLine (script, MenuScriptGenerator.ImportBeginMarker, 0);
            int endMarker = beginMarker < 0
                                ? -1
                                : FindMarkerLine (script, MenuScriptGenerator.ImportEndMarker, beginMarker + MenuScriptGenerator.ImportBeginMarker.Length);

            if (beginMarker >= 0 && endMarker > beginMarker)
            {
                int begin = script.IndexOf ('\n', beginMarker);

                if (begin < 0 || begin >= endMarker)
                {
                    json = string.Empty;

                    return false;
                }

                json = script [(begin + 1)..endMarker].Trim ();

                return json.Length > 0;
            }

            string trimmed = script.TrimStart ();

            if (trimmed.StartsWith ('{'))
            {
                json = trimmed;

                return true;
            }

            json = string.Empty;

            return false;
        }

        private static int FindMarkerLine (string text, string marker, int startIndex)
        {
            int index = startIndex;

            while ((index = text.IndexOf (marker, index, StringComparison.Ordinal)) >= 0)
            {
                int lineStart = text.LastIndexOf ('\n', index) + 1;
                int lineEnd = text.IndexOfAny (['\r', '\n'], index + marker.Length);

                if (lineEnd < 0)
                {
                    lineEnd = text.Length;
                }

                bool hasOnlyWhitespaceBefore = text.AsSpan (lineStart, index - lineStart).Trim ().IsEmpty;
                bool hasOnlyWhitespaceAfter = text.AsSpan (index + marker.Length, lineEnd - index - marker.Length).Trim ().IsEmpty;

                if (hasOnlyWhitespaceBefore && hasOnlyWhitespaceAfter)
                {
                    return index;
                }

                index += marker.Length;
            }

            return -1;
        }

        private static bool IsValidDocument (MenuScriptDocument? document)
        {
            if (document is not { Version: 1, BarItems: { Count: > 0 } barItems })
            {
                return false;
            }

            foreach (MenuScriptBarItemModel? barItem in barItems)
            {
                if (!IsValidBarItem (barItem))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidBarItem (MenuScriptBarItemModel? model)
        {
            if (model is null
                || model.Title is null
                || model.Title.Length == 0
                || model.Help is null
                || model.Action is null
                || !IsValidShortcut (model.ShortcutKey)
                || !IsValidHotKey (model.HotKey)
                || model.Children is null)
            {
                return false;
            }

            return AreValidEntries (model.Children);
        }

        private static bool AreValidEntries (List<MenuScriptEntryModel>? entries)
        {
            if (entries is null)
            {
                return false;
            }

            foreach (MenuScriptEntryModel? entry in entries)
            {
                if (!IsValidEntry (entry))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidEntry (MenuScriptEntryModel? model)
        {
            if (model is null
                || model.Title is null
                || model.Help is null
                || model.Action is null
                || model.Children is null
                || !Enum.IsDefined (model.CheckStyle)
                || !IsValidShortcut (model.ShortcutKey)
                || !IsValidHotKey (model.HotKey)
                || (!model.IsSeparator && model.Title.Length == 0)
                || (model.IsSeparator && model.Children.Count > 0))
            {
                return false;
            }

            return AreValidEntries (model.Children);
        }

        private static bool IsValidShortcut (string? shortcut)
        {
            if (shortcut is null)
            {
                return false;
            }

            return shortcut.Length == 0 || Key.TryParse (shortcut, out _);
        }

        private static bool IsValidHotKey (string? hotKey) =>
            hotKey is not null && (hotKey.Length == 0 || TryParseMenuHotKey (hotKey, out _));
    }

    private static class MenuHierarchy
    {
        public static Menu? GetMenu (MenuItem? item) =>
            item switch
            {
                null => null,
                MenuBarItem mbi => mbi.PopoverMenu?.Root ?? mbi.SubMenu,
                _ => item.SubMenu
            };

        public static MenuItem? GetParentItem (MenuItem item) =>
            item.SuperView is Menu menu ? menu.SuperMenuItem : null;

        public static void EnsureParentMenu (MenuItem parent)
        {
            if (parent is MenuBarItem bar)
            {
                if (bar.UsePopoverMenu)
                {
                    bar.PopoverMenu ??= new PopoverMenu ();
                    bar.PopoverMenu.Root ??= new Menu ();
                }
                else
                {
                    bar.SubMenu ??= new Menu ();
                }

                return;
            }

            parent.SubMenu ??= new Menu ();
        }
    }
}
