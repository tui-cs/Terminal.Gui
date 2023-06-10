using System.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Dynamic StatusBar", Description: "Demonstrates how to add and remove a StatusBar and change items dynamically.")]
	[ScenarioCategory ("Top Level Windows")]
	public class DynamicStatusBar : Scenario {
		public override void Init ()
		{
			Application.Init ();
			Application.Top.Add (new DynamicStatusBarSample () { Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}" });
		}

		public class DynamicStatusItemList {
			public string Title { get; set; }
			public StatusItem StatusItem { get; set; }

			public DynamicStatusItemList () { }

			public DynamicStatusItemList (string title, StatusItem statusItem)
			{
				Title = title;
				StatusItem = statusItem;
			}

			public override string ToString () => $"{Title}, {StatusItem}";
		}

		public class DynamicStatusItem {
			public string title = "New";
			public string action = "";
			public string shortcut;

			public DynamicStatusItem () { }

			public DynamicStatusItem (string title)
			{
				this.title = title;
			}

			public DynamicStatusItem (string title, string action, string shortcut = null)
			{
				this.title = title;
				this.action = action;
				this.shortcut = shortcut;
			}
		}

		public class DynamicStatusBarSample : Window {
			StatusBar _statusBar;
			StatusItem _currentStatusItem;
			int _currentSelectedStatusBar = -1;
			StatusItem _currentEditStatusItem;
			ListView _lstItems;

			public DynamicStatusItemModel DataContext { get; set; }

			public DynamicStatusBarSample () : base ()
			{
				DataContext = new DynamicStatusItemModel ();

				var _frmDelimiter = new FrameView ("Shortcut Delimiter:") {
					X = Pos.Center (),
					Y = 0,
					Width = 25,
					Height = 4
				};

				var _txtDelimiter = new TextField (StatusBar.ShortcutDelimiter) {
					X = Pos.Center (),
					Width = 2,
				};
				_txtDelimiter.TextChanged += (s, _) => StatusBar.ShortcutDelimiter = _txtDelimiter.Text;
				_frmDelimiter.Add (_txtDelimiter);

				Add (_frmDelimiter);

				var _frmStatusBar = new FrameView ("Items:") {
					Y = 5,
					Width = Dim.Percent (50),
					Height = Dim.Fill (2)
				};

				var _btnAddStatusBar = new Button ("Add a StatusBar") {
					Y = 1,
				};
				_frmStatusBar.Add (_btnAddStatusBar);

				var _btnRemoveStatusBar = new Button ("Remove a StatusBar") {
					Y = 1
				};
				_btnRemoveStatusBar.X = Pos.AnchorEnd () - (Pos.Right (_btnRemoveStatusBar) - Pos.Left (_btnRemoveStatusBar));
				_frmStatusBar.Add (_btnRemoveStatusBar);

				var _btnAdd = new Button (" Add  ") {
					Y = Pos.Top (_btnRemoveStatusBar) + 2,
				};
				_btnAdd.X = Pos.AnchorEnd () - (Pos.Right (_btnAdd) - Pos.Left (_btnAdd));
				_frmStatusBar.Add (_btnAdd);

				_lstItems = new ListView (new List<DynamicStatusItemList> ()) {
					ColorScheme = Colors.Dialog,
					Y = Pos.Top (_btnAddStatusBar) + 2,
					Width = Dim.Fill () - Dim.Width (_btnAdd) - 1,
					Height = Dim.Fill (),
				};
				_frmStatusBar.Add (_lstItems);

				var _btnRemove = new Button ("Remove") {
					X = Pos.Left (_btnAdd),
					Y = Pos.Top (_btnAdd) + 1
				};
				_frmStatusBar.Add (_btnRemove);

				var _btnUp = new Button ("^") {
					X = Pos.Right (_lstItems) + 2,
					Y = Pos.Top (_btnRemove) + 2
				};
				_frmStatusBar.Add (_btnUp);

				var _btnDown = new Button ("v") {
					X = Pos.Right (_lstItems) + 2,
					Y = Pos.Top (_btnUp) + 1
				};
				_frmStatusBar.Add (_btnDown);

				Add (_frmStatusBar);

				var _frmStatusBarDetails = new DynamicStatusBarDetails ("StatusBar Item Details:") {
					X = Pos.Right (_frmStatusBar),
					Y = Pos.Top (_frmStatusBar),
					Width = Dim.Fill (),
					Height = Dim.Fill (4)
				};
				Add (_frmStatusBarDetails);

				_btnUp.Clicked += (s,e) => {
					var i = _lstItems.SelectedItem;
					var statusItem = DataContext.Items.Count > 0 ? DataContext.Items [i].StatusItem : null;
					if (statusItem != null) {
						var items = _statusBar.Items;
						if (i > 0) {
							items [i] = items [i - 1];
							items [i - 1] = statusItem;
							DataContext.Items [i] = DataContext.Items [i - 1];
							DataContext.Items [i - 1] = new DynamicStatusItemList (statusItem.Title, statusItem);
							_lstItems.SelectedItem = i - 1;
							_statusBar.SetNeedsDisplay ();
						}
					}
				};

				_btnDown.Clicked += (s,e) => {
					var i = _lstItems.SelectedItem;
					var statusItem = DataContext.Items.Count > 0 ? DataContext.Items [i].StatusItem : null;
					if (statusItem != null) {
						var items = _statusBar.Items;
						if (i < items.Length - 1) {
							items [i] = items [i + 1];
							items [i + 1] = statusItem;
							DataContext.Items [i] = DataContext.Items [i + 1];
							DataContext.Items [i + 1] = new DynamicStatusItemList (statusItem.Title, statusItem);
							_lstItems.SelectedItem = i + 1;
							_statusBar.SetNeedsDisplay ();
						}
					}
				};

				var _btnOk = new Button ("Ok") {
					X = Pos.Right (_frmStatusBar) + 20,
					Y = Pos.Bottom (_frmStatusBarDetails),
				};
				Add (_btnOk);

				var _btnCancel = new Button ("Cancel") {
					X = Pos.Right (_btnOk) + 3,
					Y = Pos.Top (_btnOk),
				};
				_btnCancel.Clicked += (s,e) => {
					SetFrameDetails (_currentEditStatusItem);
				};
				Add (_btnCancel);

				_lstItems.SelectedItemChanged += (s, e) => {
					SetFrameDetails ();
				};

				_btnOk.Clicked += (s,e) => {
					if (string.IsNullOrEmpty (_frmStatusBarDetails._txtTitle.Text) && _currentEditStatusItem != null) {
						MessageBox.ErrorQuery ("Invalid title", "Must enter a valid title!.", "Ok");
					} else if (_currentEditStatusItem != null) {
						_frmStatusBarDetails._txtTitle.Text = SetTitleText (
							_frmStatusBarDetails._txtTitle.Text, _frmStatusBarDetails._txtShortcut.Text);
						var statusItem = new DynamicStatusItem (_frmStatusBarDetails._txtTitle.Text,
							_frmStatusBarDetails._txtAction.Text,
							_frmStatusBarDetails._txtShortcut.Text);
						UpdateStatusItem (_currentEditStatusItem, statusItem, _lstItems.SelectedItem);
					}
				};

				_btnAdd.Clicked += (s,e) => {
					if (StatusBar == null) {
						MessageBox.ErrorQuery ("StatusBar Bar Error", "Must add a StatusBar first!", "Ok");
						_btnAddStatusBar.SetFocus ();
						return;
					}

					var frameDetails = new DynamicStatusBarDetails ();
					var item = frameDetails.EnterStatusItem ();
					if (item == null) {
						return;
					}

					StatusItem newStatusItem = CreateNewStatusBar (item);
					_currentSelectedStatusBar++;
					_statusBar.AddItemAt (_currentSelectedStatusBar, newStatusItem);
					DataContext.Items.Add (new DynamicStatusItemList (newStatusItem.Title, newStatusItem));
					_lstItems.MoveDown ();
					SetFrameDetails ();
				};

				_btnRemove.Clicked += (s,e) => {
					var statusItem = DataContext.Items.Count > 0 ? DataContext.Items [_lstItems.SelectedItem].StatusItem : null;
					if (statusItem != null) {
						_statusBar.RemoveItem (_currentSelectedStatusBar);
						DataContext.Items.RemoveAt (_lstItems.SelectedItem);
						if (_lstItems.Source.Count > 0 && _lstItems.SelectedItem > _lstItems.Source.Count - 1) {
							_lstItems.SelectedItem = _lstItems.Source.Count - 1;
						}
						_lstItems.SetNeedsDisplay ();
						SetFrameDetails ();
					}
				};

				_lstItems.Enter += (s, e) => {
					var statusItem = DataContext.Items.Count > 0 ? DataContext.Items [_lstItems.SelectedItem].StatusItem : null;
					SetFrameDetails (statusItem);
				};

				_btnAddStatusBar.Clicked += (s,e) => {
					if (_statusBar != null) {
						return;
					}

					_statusBar = new StatusBar ();
					Add (_statusBar);
				};

				_btnRemoveStatusBar.Clicked += (s,e) => {
					if (_statusBar == null) {
						return;
					}

					Remove (_statusBar);
					_statusBar = null;
					DataContext.Items = new List<DynamicStatusItemList> ();
					_currentStatusItem = null;
					_currentSelectedStatusBar = -1;
					SetListViewSource (_currentStatusItem, true);
					SetFrameDetails (null);
				};

				SetFrameDetails ();

				var ustringConverter = new UStringValueConverter ();
				var listWrapperConverter = new ListWrapperConverter ();

				var lstItems = new Binding (this, "Items", _lstItems, "Source", listWrapperConverter);

				void SetFrameDetails (StatusItem statusItem = null)
				{
					StatusItem newStatusItem;

					if (statusItem == null) {
						newStatusItem = DataContext.Items.Count > 0 ? DataContext.Items [_lstItems.SelectedItem].StatusItem : null;
					} else {
						newStatusItem = statusItem;
					}

					_currentEditStatusItem = newStatusItem;
					_frmStatusBarDetails.EditStatusItem (newStatusItem);
					var f = _btnOk.Enabled == _frmStatusBarDetails.Enabled;
					if (!f) {
						_btnOk.Enabled = _frmStatusBarDetails.Enabled;
						_btnCancel.Enabled = _frmStatusBarDetails.Enabled;
					}
				}

				void SetListViewSource (StatusItem _currentStatusItem, bool fill = false)
				{
					DataContext.Items = new List<DynamicStatusItemList> ();
					var statusItem = _currentStatusItem;
					if (!fill) {
						return;
					}
					if (statusItem != null) {
						foreach (var si in _statusBar.Items) {
							DataContext.Items.Add (new DynamicStatusItemList (si.Title, si));
						}
					}
				}

				StatusItem CreateNewStatusBar (DynamicStatusItem item)
				{
					var newStatusItem = new StatusItem (ShortcutHelper.GetShortcutFromTag (
						item.shortcut, StatusBar.ShortcutDelimiter),
						item.title, _frmStatusBarDetails.CreateAction (item));

					return newStatusItem;
				}

				void UpdateStatusItem (StatusItem _currentEditStatusItem, DynamicStatusItem statusItem, int index)
				{
					_currentEditStatusItem = CreateNewStatusBar (statusItem);
					_statusBar.Items [index] = _currentEditStatusItem;
					if (DataContext.Items.Count == 0) {
						DataContext.Items.Add (new DynamicStatusItemList (_currentEditStatusItem.Title, _currentEditStatusItem));
					}
					DataContext.Items [index] = new DynamicStatusItemList (_currentEditStatusItem.Title, _currentEditStatusItem);
					SetFrameDetails (_currentEditStatusItem);
				}

				//_frmStatusBarDetails.Initialized += (s, e) => _frmStatusBarDetails.Enabled = false;
			}

			public static string SetTitleText (string title, string shortcut)
			{
				var txt = title;
				var split = title.Split ('~');
				if (split.Length > 1) {
					txt = split [2].Trim (); ;
				}
				if (string.IsNullOrEmpty (shortcut)) {
					return txt;
				}

				return $"~{shortcut}~ {txt}";
			}
		}

		public class DynamicStatusBarDetails : FrameView {
			public StatusItem _statusItem;
			public TextField _txtTitle;
			public TextView _txtAction;
			public TextField _txtShortcut;

			public DynamicStatusBarDetails (StatusItem statusItem = null) : this (statusItem == null ? "Adding New StatusBar Item." : "Editing StatusBar Item.")
			{
				_statusItem = statusItem;
			}

			public DynamicStatusBarDetails (string title) : base (title)
			{
				var _lblTitle = new Label ("Title:") {
					Y = 1
				};
				Add (_lblTitle);

				_txtTitle = new TextField () {
					X = Pos.Right (_lblTitle) + 4,
					Y = Pos.Top (_lblTitle),
					Width = Dim.Fill ()
				};
				Add (_txtTitle);

				var _lblAction = new Label ("Action:") {
					X = Pos.Left (_lblTitle),
					Y = Pos.Bottom (_lblTitle) + 1
				};
				Add (_lblAction);

				_txtAction = new TextView () {
					X = Pos.Left (_txtTitle),
					Y = Pos.Top (_lblAction),
					Width = Dim.Fill (),
					Height = 5
				};
				Add (_txtAction);

				var _lblShortcut = new Label ("Shortcut:") {
					X = Pos.Left (_lblTitle),
					Y = Pos.Bottom (_txtAction) + 1
				};
				Add (_lblShortcut);

				_txtShortcut = new TextField () {
					X = Pos.X (_txtAction),
					Y = Pos.Y (_lblShortcut),
					Width = Dim.Fill (),
					ReadOnly = true
				};
				_txtShortcut.KeyDown += (s, e) => {
					if (!ProcessKey (e.KeyEvent)) {
						return;
					}

					var k = ShortcutHelper.GetModifiersKey (e.KeyEvent);
					if (CheckShortcut (k, true)) {
						e.Handled = true;
					}
				};

				bool ProcessKey (KeyEvent ev)
				{
					switch (ev.Key) {
					case Key.CursorUp:
					case Key.CursorDown:
					case Key.Tab:
					case Key.BackTab:
						return false;
					}

					return true;
				}

				bool CheckShortcut (Key k, bool pre)
				{
					var m = _statusItem != null ? _statusItem : new StatusItem (k, "", null);
					if (pre && !ShortcutHelper.PreShortcutValidation (k)) {
						_txtShortcut.Text = "";
						return false;
					}
					if (!pre) {
						if (!ShortcutHelper.PostShortcutValidation (ShortcutHelper.GetShortcutFromTag (
							_txtShortcut.Text, StatusBar.ShortcutDelimiter))) {
							_txtShortcut.Text = "";
							return false;
						}
						return true;
					}
					_txtShortcut.Text = ShortcutHelper.GetShortcutTag (k, StatusBar.ShortcutDelimiter);

					return true;
				}

				_txtShortcut.KeyUp += (s, e) => {
					var k = ShortcutHelper.GetModifiersKey (e.KeyEvent);
					if (CheckShortcut (k, false)) {
						e.Handled = true;
					}
				};
				Add (_txtShortcut);

				var _btnShortcut = new Button ("Clear Shortcut") {
					X = Pos.X (_lblShortcut),
					Y = Pos.Bottom (_txtShortcut) + 1
				};
				_btnShortcut.Clicked += (s,e) => {
					_txtShortcut.Text = "";
				};
				Add (_btnShortcut);
			}

			public DynamicStatusItem EnterStatusItem ()
			{
				var valid = false;

				if (_statusItem == null) {
					var m = new DynamicStatusItem ();
					_txtTitle.Text = m.title;
					_txtAction.Text = m.action;
				} else {
					EditStatusItem (_statusItem);
				}

				var _btnOk = new Button ("Ok") {
					IsDefault = true,
				};
				_btnOk.Clicked += (s,e) => {
					if (string.IsNullOrEmpty (_txtTitle.Text)) {
						MessageBox.ErrorQuery ("Invalid title", "Must enter a valid title!.", "Ok");
					} else {
						if (!string.IsNullOrEmpty (_txtShortcut.Text)) {
							_txtTitle.Text = DynamicStatusBarSample.SetTitleText (
								_txtTitle.Text, _txtShortcut.Text);
						}
						valid = true;
						Application.RequestStop ();
					}
				};
				var _btnCancel = new Button ("Cancel");
				_btnCancel.Clicked += (s,e) => {
					_txtTitle.Text = string.Empty;
					Application.RequestStop ();
				};
				var _dialog = new Dialog (_btnOk, _btnCancel) { Title = "Enter the menu details." };

				Width = Dim.Fill ();
				Height = Dim.Fill () - 1;
				_dialog.Add (this);
				_txtTitle.SetFocus ();
				_txtTitle.CursorPosition = _txtTitle.Text.Length;
				Application.Run (_dialog);

				if (valid) {
					return new DynamicStatusItem (_txtTitle.Text, _txtAction.Text, _txtShortcut.Text);
				} else {
					return null;
				}
			}

			public void EditStatusItem (StatusItem statusItem)
			{
				if (statusItem == null) {
					Enabled = false;
					CleanEditStatusItem ();
					return;
				} else {
					Enabled = true;
				}
				_statusItem = statusItem;
				_txtTitle.Text = statusItem?.Title ?? "";
				_txtAction.Text = statusItem != null && statusItem.Action != null ? GetTargetAction (statusItem.Action) : string.Empty;
				_txtShortcut.Text = ShortcutHelper.GetShortcutTag (statusItem.Shortcut, StatusBar.ShortcutDelimiter) ?? "";
			}

			void CleanEditStatusItem ()
			{
				_txtTitle.Text = "";
				_txtAction.Text = "";
				_txtShortcut.Text = "";
			}

			string GetTargetAction (Action action)
			{
				var me = action.Target;

				if (me == null) {
					throw new ArgumentException ();
				}
				object v = new object ();
				foreach (var field in me.GetType ().GetFields ()) {
					if (field.Name == "item") {
						v = field.GetValue (me);
					}
				}
				return v == null || !(v is DynamicStatusItem item) ? string.Empty : item.action;
			}

			public Action CreateAction (DynamicStatusItem item)
			{
				return new Action (() => MessageBox.ErrorQuery (item.title, item.action, "Ok"));
			}
		}

		public class DynamicStatusItemModel : INotifyPropertyChanged {
			public event PropertyChangedEventHandler PropertyChanged;

			private string statusBar;
			private List<DynamicStatusItemList> items;

			public string StatusBar {
				get => statusBar;
				set {
					if (value != statusBar) {
						statusBar = value;
						PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (GetPropertyName ()));
					}
				}
			}

			public List<DynamicStatusItemList> Items {
				get => items;
				set {
					if (value != items) {
						items = value;
						PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (GetPropertyName ()));
					}
				}
			}

			public DynamicStatusItemModel ()
			{
				Items = new List<DynamicStatusItemList> ();
			}

			public string GetPropertyName ([CallerMemberName] string propertyName = null)
			{
				return propertyName;
			}
		}

		public interface IValueConverter {
			object Convert (object value, object parameter = null);
		}

		public class Binding {
			public View Target { get; private set; }
			public View Source { get; private set; }

			public string SourcePropertyName { get; private set; }
			public string TargetPropertyName { get; private set; }

			private object sourceDataContext;
			private PropertyInfo sourceBindingProperty;
			private IValueConverter valueConverter;

			public Binding (View source, string sourcePropertyName, View target, string targetPropertyName, IValueConverter valueConverter = null)
			{
				Target = target;
				Source = source;
				SourcePropertyName = sourcePropertyName;
				TargetPropertyName = targetPropertyName;
				sourceDataContext = Source.GetType ().GetProperty ("DataContext").GetValue (Source);
				sourceBindingProperty = sourceDataContext.GetType ().GetProperty (SourcePropertyName);
				this.valueConverter = valueConverter;
				UpdateTarget ();

				var notifier = ((INotifyPropertyChanged)sourceDataContext);
				if (notifier != null) {
					notifier.PropertyChanged += (s, e) => {
						if (e.PropertyName == SourcePropertyName) {
							UpdateTarget ();
						}
					};
				}
			}

			private void UpdateTarget ()
			{
				try {
					var sourceValue = sourceBindingProperty.GetValue (sourceDataContext);
					if (sourceValue == null) {
						return;
					}

					var finalValue = valueConverter?.Convert (sourceValue) ?? sourceValue;

					var targetProperty = Target.GetType ().GetProperty (TargetPropertyName);
					targetProperty.SetValue (Target, finalValue);
				} catch (Exception ex) {
					MessageBox.ErrorQuery ("Binding Error", $"Binding failed: {ex}.", "Ok");
				}
			}
		}

		public class ListWrapperConverter : IValueConverter {
			public object Convert (object value, object parameter = null)
			{
				return new ListWrapper ((IList)value);
			}
		}

		public class UStringValueConverter : IValueConverter {
			public object Convert (object value, object parameter = null)
			{
				var data = Encoding.ASCII.GetBytes (value.ToString ());
				return StringExtensions.ToString (data);
			}
		}
	}
}
