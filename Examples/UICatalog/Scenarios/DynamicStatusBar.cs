#nullable enable
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Terminal.Gui.Editor;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Dynamic StatusBar", "Demonstrates how to add and remove a StatusBar and change items dynamically.")]
[ScenarioCategory ("Arrangement")]
public class DynamicStatusBar : Scenario
{
    private static IApplication? _app;

    public override void Main ()
    {
        using IApplication app = Application.Create ();
        _app = app;
        _app.Init ();
        _app.Run<DynamicStatusBarSample> ();
    }

    public class Binding
    {
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

            var notifier = (INotifyPropertyChanged)_sourceDataContext!;

            notifier.PropertyChanged += (_, e) =>
                                        {
                                            if (e.PropertyName == SourcePropertyName)
                                            {
                                                UpdateTarget ();
                                            }
                                        };
        }

        private readonly PropertyInfo? _sourceBindingProperty;
        private readonly object? _sourceDataContext;
        private readonly IValueConverter? _valueConverter;

        public View Source { get; }
        public string SourcePropertyName { get; }
        public View Target { get; }
        public string TargetPropertyName { get; }

        private void UpdateTarget ()
        {
            try
            {
                object? sourceValue = _sourceBindingProperty?.GetValue (_sourceDataContext);

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
                MessageBox.ErrorQuery (Source.App!, "Binding Error", $"Binding failed: {ex}.", "Ok");
            }
        }
    }

    public class DynamicStatusBarDetails : FrameView
    {
        public DynamicStatusBarDetails (Shortcut? statusItem = null) : this ()
        {
            _statusItem = statusItem;
            Title = statusItem == null ? "Adding New StatusBar Item." : "Editing StatusBar Item.";
        }

        public DynamicStatusBarDetails ()
        {
            var lblTitle = new Label { Y = 1, Text = "Title:" };
            Add (lblTitle);

            TextTitle = new TextField { X = Pos.Right (lblTitle) + 4, Y = Pos.Top (lblTitle), Width = Dim.Fill () };
            Add (TextTitle);

            var lblAction = new Label { X = Pos.Left (lblTitle), Y = Pos.Bottom (lblTitle) + 1, Text = "Action:" };
            Add (lblAction);

            TextAction = new Editor { X = Pos.Left (TextTitle), Y = Pos.Top (lblAction), Width = Dim.Fill (), Height = 5 };
            Add (TextAction);

            var lblKey = new Label { X = Pos.Left (lblTitle), Y = Pos.Bottom (TextAction) + 1, Text = "Key:" };
            Add (lblKey);

            TextKey = new TextField { X = Pos.X (TextAction), Y = Pos.Y (lblKey), Width = Dim.Fill (), ReadOnly = true };

            TextKey.KeyDown += (_, e) => { TextKey.Text = e.ToString (); };
            Add (TextKey);

            var btnKey = new Button { X = Pos.X (lblKey), Y = Pos.Bottom (TextKey) + 1, Text = "Clear Key" };

            btnKey.Accepting += (_, e) =>
                                {
                                    TextKey.Text = "";
                                    e.Handled = true;
                                };
            Add (btnKey);
        }

        private Shortcut? _statusItem;
        public Action CreateAction (DynamicStatusItem item) => () => MessageBox.ErrorQuery (_app!, item.Title, item.Action, "Ok");

        public void EditStatusItem (Shortcut? statusItem)
        {
            if (statusItem == null)
            {
                Enabled = false;
                CleanEditStatusItem ();

                return;
            }

            Enabled = true;
            _statusItem = statusItem;
            TextTitle.Text = statusItem.Title;

            TextAction.Text = statusItem.Action != null ? GetTargetAction (statusItem.Action) : string.Empty;

            TextKey.Text = statusItem.Key == Key.Empty ? "" : statusItem.Key;
        }

        public DynamicStatusItem? EnterStatusItem ()
        {
            var valid = false;

            if (_statusItem == null)
            {
                var m = new DynamicStatusItem ();
                TextTitle.Text = m.Title;
                TextAction.Text = m.Action;
            }
            else
            {
                EditStatusItem (_statusItem);
            }

            var btnOk = new Button { IsDefault = true, Text = "OK" };

            btnOk.Accepting += (_, _) =>
                               {
                                   if (string.IsNullOrEmpty (TextTitle.Text))
                                   {
                                       MessageBox.ErrorQuery (_app!, "Invalid title", "Must enter a valid title!.", "Ok");
                                   }
                                   else
                                   {
                                       valid = true;
                                       _app?.RequestStop ();
                                   }
                               };
            var btnCancel = new Button { Text = "Cancel" };

            btnCancel.Accepting += (_, _) =>
                                   {
                                       TextTitle.Text = string.Empty;
                                       _app?.RequestStop ();
                                   };

            Dialog dialog = new () { Title = "Enter the Shortcut details.", Buttons = [btnOk, btnCancel] };

            Width = Dim.Auto ();
            Height = Dim.Auto ();
            dialog.Add (this);
            TextTitle.SetFocus ();
            TextTitle.InsertionPoint = TextTitle.Text.Length;
            _app?.Run (dialog);
            dialog.Dispose ();

            return valid ? new DynamicStatusItem { Title = TextTitle.Text, Action = TextAction.Text, Key = TextKey.Text } : null;
        }

        public Editor TextAction { get; }
        public TextField TextKey { get; }
        public TextField TextTitle { get; }

        private void CleanEditStatusItem ()
        {
            TextTitle.Text = "";
            TextAction.Text = "";
            TextKey.Text = "";
        }

        private string GetTargetAction (Action action)
        {
            object? me = action.Target;

            if (me == null)
            {
                throw new ArgumentException ();
            }

            var v = new object ();

            foreach (FieldInfo field in me.GetType ().GetFields ())
            {
                if (field.Name == "item")
                {
                    v = field.GetValue (me);
                }
            }

            return v is not DynamicStatusItem item ? string.Empty : item.Action;
        }
    }

    public class DynamicStatusBarSample : Window
    {
        public DynamicStatusBarSample ()
        {
            DataContext = new DynamicStatusItemModel ();

            Title = $"{Application.GetDefaultKey (Command.Quit)} to Quit - Scenario: Dynamic StatusBar";

            var frmStatusBar = new FrameView { Y = 5, Width = Dim.Percent (50), Height = Dim.Fill (2), Title = "Items:" };

            var btnAddStatusBar = new Button { Y = 1, Text = "Add a StatusBar" };
            frmStatusBar.Add (btnAddStatusBar);

            var btnRemoveStatusBar = new Button { Y = 1, Text = "Remove a StatusBar" };

            btnRemoveStatusBar.X = Pos.AnchorEnd ();
            frmStatusBar.Add (btnRemoveStatusBar);

            var btnAdd = new Button { Y = Pos.Top (btnRemoveStatusBar) + 2, Text = " Add  " };
            btnAdd.X = Pos.AnchorEnd ();
            frmStatusBar.Add (btnAdd);

            _lstItems = new ListView
            {
                SchemeName = "Dialog",
                Y = Pos.Top (btnAddStatusBar) + 2,
                Width = Dim.Fill () - Dim.Width (btnAdd) - 1,
                Height = Dim.Fill (),
                Source = new ListWrapper<DynamicStatusItemList> ([])
            };
            frmStatusBar.Add (_lstItems);

            var btnRemove = new Button { X = Pos.Left (btnAdd), Y = Pos.Top (btnAdd) + 1, Text = "Remove" };
            frmStatusBar.Add (btnRemove);

            var btnUp = new Button { X = Pos.Right (_lstItems) + 2, Y = Pos.Top (btnRemove) + 2, Text = Glyphs.UpArrow.ToString () };
            frmStatusBar.Add (btnUp);

            var btnDown = new Button { X = Pos.Right (_lstItems) + 2, Y = Pos.Top (btnUp) + 1, Text = Glyphs.DownArrow.ToString () };
            frmStatusBar.Add (btnDown);

            Add (frmStatusBar);

            var frmStatusBarDetails = new DynamicStatusBarDetails
            {
                X = Pos.Right (frmStatusBar),
                Y = Pos.Top (frmStatusBar),
                Width = Dim.Fill (),
                Height = Dim.Fill (4),
                Title = "Shortcut Details:"
            };
            Add (frmStatusBarDetails);

            btnUp.Accepting += (_, _) =>
                               {
                                   if (_lstItems.SelectedItem is null)
                                   {
                                       return;
                                   }
                                   int i = _lstItems.SelectedItem.Value;

                                   Shortcut? statusItem = DataContext.Items.Count > 0 ? DataContext.Items [i].Shortcut : null;

                                   if (statusItem == null)
                                   {
                                       return;
                                   }
                                   Shortcut [] items = _statusBar!.SubViews.OfType<Shortcut> ().ToArray ();

                                   if (i <= 0)
                                   {
                                       return;
                                   }
                                   items [i] = items [i - 1];
                                   items [i - 1] = statusItem;
                                   DataContext.Items [i] = DataContext.Items [i - 1];

                                   DataContext.Items [i - 1] = new DynamicStatusItemList (statusItem.Title, statusItem);
                                   _lstItems.SelectedItem = _currentSelectedStatusBar = i - 1;
                                   Shortcut toMove = _statusBar.RemoveShortcut (i)!;
                                   _statusBar.AddShortcutAt (i - 1, toMove);
                                   _statusBar.SetNeedsLayout ();
                               };

            btnDown.Accepting += (_, _) =>
                                 {
                                     if (_lstItems.SelectedItem is null)
                                     {
                                         return;
                                     }
                                     int i = _lstItems.SelectedItem.Value;

                                     Shortcut? statusItem = DataContext.Items.Count > 0 ? DataContext.Items [i].Shortcut : null;

                                     if (statusItem == null)
                                     {
                                         return;
                                     }
                                     Shortcut [] items = _statusBar!.SubViews.OfType<Shortcut> ().ToArray ();

                                     if (i >= items.Length - 1)
                                     {
                                         return;
                                     }
                                     items [i] = items [i + 1];
                                     items [i + 1] = statusItem;
                                     DataContext.Items [i] = DataContext.Items [i + 1];

                                     DataContext.Items [i + 1] = new DynamicStatusItemList (statusItem.Title, statusItem);
                                     _lstItems.SelectedItem = _currentSelectedStatusBar = i + 1;
                                     Shortcut toMove = _statusBar.RemoveShortcut (i)!;
                                     _statusBar.AddShortcutAt (i + 1, toMove);
                                     _statusBar.SetNeedsLayout ();
                                 };

            var btnOk = new Button { X = Pos.Right (frmStatusBar) + 20, Y = Pos.Bottom (frmStatusBarDetails), Text = "Ok" };
            Add (btnOk);

            var btnCancel = new Button { X = Pos.Right (btnOk) + 3, Y = Pos.Top (btnOk), Text = "Cancel" };
            btnCancel.Accepting += (_, _) => { SetFrameDetails (_currentEditStatusItem); };
            Add (btnCancel);

            _lstItems.ValueChanged += (_, e) =>
                                      {
                                          _currentSelectedStatusBar = e.NewValue ?? -1;
                                          SetFrameDetails ();
                                      };

            btnOk.Accepting += (_, _) =>
                               {
                                   if (string.IsNullOrEmpty (frmStatusBarDetails.TextTitle.Text) && _currentEditStatusItem != null)
                                   {
                                       MessageBox.ErrorQuery (_app!, "Invalid title", "Must enter a valid title!.", "Ok");
                                   }
                                   else if (_currentEditStatusItem != null)
                                   {
                                       var statusItem = new DynamicStatusItem
                                       {
                                           Title = frmStatusBarDetails.TextTitle.Text,
                                           Action = frmStatusBarDetails.TextAction.Text,
                                           Key = frmStatusBarDetails.TextKey.Text
                                       };

                                       if (_lstItems.SelectedItem is { } selectedItem)
                                       {
                                           UpdateStatusItem (_currentEditStatusItem, statusItem, selectedItem);
                                       }
                                   }
                               };

            btnAdd.Accepting += (_, _) =>
                                {
                                    if (_statusBar == null)
                                    {
                                        MessageBox.ErrorQuery (_app!, "StatusBar Bar Error", "Must add a StatusBar first!", "Ok");
                                        btnAddStatusBar.SetFocus ();

                                        return;
                                    }

                                    var frameDetails = new DynamicStatusBarDetails ();
                                    DynamicStatusItem? item = frameDetails.EnterStatusItem ();

                                    if (item == null)
                                    {
                                        return;
                                    }

                                    Shortcut newStatusItem = CreateNewStatusBar (item);
                                    _currentSelectedStatusBar++;
                                    _statusBar.AddShortcutAt (_currentSelectedStatusBar, newStatusItem);
                                    DataContext.Items.Add (new DynamicStatusItemList (newStatusItem.Title, newStatusItem));
                                    _lstItems.MoveDown ();
                                    SetFrameDetails ();
                                };

            btnRemove.Accepting += (_, _) =>
                                   {
                                       Shortcut? statusItem = DataContext.Items.Count > 0 ? DataContext.Items [_lstItems.SelectedItem!.Value].Shortcut : null;

                                       if (statusItem == null)
                                       {
                                           return;
                                       }
                                       Shortcut? removed = _statusBar?.RemoveShortcut (_currentSelectedStatusBar);
                                       removed?.Dispose ();
                                       DataContext.Items.RemoveAt (_lstItems.SelectedItem!.Value);

                                       if (_lstItems.Source.Count > 0 && _lstItems.SelectedItem > _lstItems.Source.Count - 1)
                                       {
                                           _lstItems.SelectedItem = _lstItems.Source.Count - 1;
                                       }
                                       _currentSelectedStatusBar = _lstItems.SelectedItem ?? -1;
                                       _lstItems.SetNeedsDraw ();
                                       SetFrameDetails ();
                                   };

            _lstItems.HasFocusChanging += (_, _) =>
                                          {
                                              Shortcut? statusItem = DataContext.Items.Count > 0
                                                                         ? DataContext.Items [_lstItems.SelectedItem!.Value].Shortcut
                                                                         : null;
                                              SetFrameDetails (statusItem);
                                          };

            btnAddStatusBar.Accepting += (_, _) =>
                                         {
                                             if (_statusBar != null)
                                             {
                                                 return;
                                             }

                                             _statusBar = new StatusBar ();
                                             Add (_statusBar);
                                         };

            btnRemoveStatusBar.Accepting += (_, _) =>
                                            {
                                                if (_statusBar == null)
                                                {
                                                    return;
                                                }

                                                Remove (_statusBar);
                                                _statusBar.Dispose ();
                                                _statusBar = null;
                                                DataContext.Items = [];
                                                Shortcut? currentStatusItem1 = null;
                                                _currentSelectedStatusBar = -1;
                                                SetListViewSource (currentStatusItem1, true);
                                                SetFrameDetails ();
                                            };

            SetFrameDetails ();

            _ = new Binding (this, "Items", _lstItems, "Source", new ListWrapperConverter<DynamicStatusItemList> ());

            return;

            void SetFrameDetails (Shortcut? statusItem = null)
            {
                Shortcut? newStatusItem;

                if (statusItem == null)
                {
                    newStatusItem = DataContext.Items.Count > 0 ? DataContext.Items [_lstItems.SelectedItem!.Value].Shortcut : null;
                }
                else
                {
                    newStatusItem = statusItem;
                }

                _currentEditStatusItem = newStatusItem;
                frmStatusBarDetails.EditStatusItem (newStatusItem);
                bool f = btnOk.Enabled == frmStatusBarDetails.Enabled;

                if (f)
                {
                    return;
                }
                btnOk.Enabled = frmStatusBarDetails.Enabled;
                btnCancel.Enabled = frmStatusBarDetails.Enabled;
            }

            void SetListViewSource (Shortcut? currentStatusItem, bool fill = false)
            {
                DataContext.Items = [];

                if (!fill)
                {
                    return;
                }

                if (currentStatusItem == null)
                {
                    return;
                }

                foreach (Shortcut si in _statusBar?.SubViews.OfType<Shortcut> ()!)
                {
                    DataContext.Items.Add (new DynamicStatusItemList (si.Title, si));
                }
            }

            Shortcut CreateNewStatusBar (DynamicStatusItem item)
            {
                var newStatusItem = new Shortcut (item.Key, item.Title, frmStatusBarDetails.CreateAction (item));

                return newStatusItem;
            }

            void UpdateStatusItem (Shortcut currentEditStatusItem, DynamicStatusItem statusItem, int index)
            {
                _statusBar?.SubViews.ElementAt (index).Title = statusItem.Title;
                ((Shortcut)_statusBar?.SubViews.ElementAt (index)!).Action = frmStatusBarDetails.CreateAction (statusItem);
                ((Shortcut)_statusBar.SubViews.ElementAt (index)).Key = statusItem.Key;

                if (DataContext.Items.Count == 0)
                {
                    DataContext.Items.Add (new DynamicStatusItemList (currentEditStatusItem.Title, currentEditStatusItem));
                }

                DataContext.Items [index] = new DynamicStatusItemList (currentEditStatusItem.Title, currentEditStatusItem);
                SetFrameDetails (currentEditStatusItem);
            }

            // _frmStatusBarDetails.Initialized += (s, e) => _frmStatusBarDetails.Enabled = false;
        }

        private readonly ListView _lstItems;
        private Shortcut? _currentEditStatusItem;
        private int _currentSelectedStatusBar = -1;
        private StatusBar? _statusBar;

        public DynamicStatusItemModel DataContext { get; set; }
    }

    public class DynamicStatusItem
    {
        public string Action { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = "New";
    }

    public class DynamicStatusItemList (string title, Shortcut statusItem)
    {
        public Shortcut Shortcut { get; set; } = statusItem;
        public string Title { get; set; } = title;
        public override string ToString () => $"{Title}, {Shortcut.Key}";
    }

    public class DynamicStatusItemModel : INotifyPropertyChanged
    {
        public DynamicStatusItemModel () => Items = [];

        public event PropertyChangedEventHandler? PropertyChanged;
        public string? GetPropertyName ([CallerMemberName] string? propertyName = null) => propertyName;

        public ObservableCollection<DynamicStatusItemList> Items
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
    }

    public interface IValueConverter
    {
        object Convert (object value, object? parameter = null);
    }

    public class ListWrapperConverter<T> : IValueConverter
    {
        public object Convert (object value, object? parameter = null) => new ListWrapper<T> ((ObservableCollection<T>)value);
    }

    public class UStringValueConverter : IValueConverter
    {
        public object Convert (object value, object? parameter = null)
        {
            byte [] data = Encoding.ASCII.GetBytes (value.ToString () ?? string.Empty);

            return StringExtensions.ToString (data);
        }
    }
}
