using NStack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Dynamic MenuBar", Description: "Demonstrates how to change a MenuBar dynamically.")]
	[ScenarioCategory ("Top Level Windows")]
	[ScenarioCategory ("Menus")]
	public class DynamicMenuBar : Scenario {
		public override void Init ()
		{
			Application.Init ();
			Application.Top.Add (new DynamicMenuBarSample () { Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}" });
		}

		public class DynamicMenuItemList {
			public ustring Title { get; set; }
			public MenuItem MenuItem { get; set; }

			public DynamicMenuItemList () { }

			public DynamicMenuItemList (ustring title, MenuItem menuItem)
			{
				Title = title;
				MenuItem = menuItem;
			}

			public override string ToString () => $"{Title}, {MenuItem}";
		}

		public class DynamicMenuItem {
			public ustring title = "_New";
			public ustring help = "";
			public ustring action = "";
			public bool isTopLevel;
			public bool hasSubMenu;
			public MenuItemCheckStyle checkStyle;
			public ustring shortcut;
			public bool allowNullChecked;

			public DynamicMenuItem () { }

			public DynamicMenuItem (ustring title, bool hasSubMenu = false)
			{
				this.title = title;
				this.hasSubMenu = hasSubMenu;
			}

			public DynamicMenuItem (ustring title, ustring help, ustring action, bool isTopLevel, bool hasSubMenu, MenuItemCheckStyle checkStyle = MenuItemCheckStyle.NoCheck, ustring shortcut = null, bool allowNullChecked = false)
			{
				this.title = title;
				this.help = help;
				this.action = action;
				this.isTopLevel = isTopLevel;
				this.hasSubMenu = hasSubMenu;
				this.checkStyle = checkStyle;
				this.shortcut = shortcut;
				this.allowNullChecked = allowNullChecked;
			}
		}

		public class DynamicMenuBarSample : Window {
			MenuBar _menuBar;
			MenuItem _currentMenuBarItem;
			int _currentSelectedMenuBar;
			MenuItem _currentEditMenuBarItem;
			ListView _lstMenus;

			public DynamicMenuItemModel DataContext { get; set; }

			public DynamicMenuBarSample () : base ()
			{
				DataContext = new DynamicMenuItemModel ();

				var _frmDelimiter = new FrameView ("Shortcut Delimiter:") {
					X = Pos.Center (),
					Y = 3,
					Width = 25,
					Height = 4
				};

				var _txtDelimiter = new TextField (MenuBar.ShortcutDelimiter.ToString ()) {
					X = Pos.Center (),
					Width = 2,
				};
				_txtDelimiter.TextChanged += (s, _) => MenuBar.ShortcutDelimiter = _txtDelimiter.Text;
				_frmDelimiter.Add (_txtDelimiter);

				Add (_frmDelimiter);

				var _frmMenu = new FrameView ("Menus:") {
					Y = 7,
					Width = Dim.Percent (50),
					Height = Dim.Fill ()
				};

				var _btnAddMenuBar = new Button ("Add a MenuBar") {
					Y = 1,
				};
				_frmMenu.Add (_btnAddMenuBar);

				var _btnMenuBarUp = new Button ("^") {
					X = Pos.Center ()
				};
				_frmMenu.Add (_btnMenuBarUp);

				var _btnMenuBarDown = new Button ("v") {
					X = Pos.Center (),
					Y = Pos.Bottom (_btnMenuBarUp)
				};
				_frmMenu.Add (_btnMenuBarDown);

				var _btnRemoveMenuBar = new Button ("Remove a MenuBar") {
					Y = 1
				};
				_btnRemoveMenuBar.X = Pos.AnchorEnd () - (Pos.Right (_btnRemoveMenuBar) - Pos.Left (_btnRemoveMenuBar));
				_frmMenu.Add (_btnRemoveMenuBar);

				var _btnPrevious = new Button ("<") {
					X = Pos.Left (_btnAddMenuBar),
					Y = Pos.Top (_btnAddMenuBar) + 2
				};
				_frmMenu.Add (_btnPrevious);

				var _btnAdd = new Button (" Add  ") {
					Y = Pos.Top (_btnPrevious) + 2,
				};
				_btnAdd.X = Pos.AnchorEnd () - (Pos.Right (_btnAdd) - Pos.Left (_btnAdd));
				_frmMenu.Add (_btnAdd);

				var _btnNext = new Button (">") {
					X = Pos.X (_btnAdd),
					Y = Pos.Top (_btnPrevious),
				};
				_frmMenu.Add (_btnNext);

				var _lblMenuBar = new Label () {
					ColorScheme = Colors.Dialog,
					TextAlignment = TextAlignment.Centered,
					X = Pos.Right (_btnPrevious) + 1,
					Y = Pos.Top (_btnPrevious),
					Width = Dim.Fill () - Dim.Function (() => _btnAdd.Frame.Width + 1),
					Height = 1
				};
				_frmMenu.Add (_lblMenuBar);
				_lblMenuBar.WantMousePositionReports = true;
				_lblMenuBar.CanFocus = true;

				var _lblParent = new Label () {
					TextAlignment = TextAlignment.Centered,
					X = Pos.Right (_btnPrevious) + 1,
					Y = Pos.Top (_btnPrevious) + 1,
					Width = Dim.Fill () - Dim.Width (_btnAdd) - 1
				};
				_frmMenu.Add (_lblParent);

				var _btnPreviowsParent = new Button ("..") {
					X = Pos.Left (_btnAddMenuBar),
					Y = Pos.Top (_btnPrevious) + 1
				};
				_frmMenu.Add (_btnPreviowsParent);

				_lstMenus = new ListView (new List<DynamicMenuItemList> ()) {
					ColorScheme = Colors.Dialog,
					X = Pos.Right (_btnPrevious) + 1,
					Y = Pos.Top (_btnPrevious) + 2,
					Width = _lblMenuBar.Width,
					Height = Dim.Fill (),
				};
				_frmMenu.Add (_lstMenus);

				_lblMenuBar.TabIndex = _btnPrevious.TabIndex + 1;
				_lstMenus.TabIndex = _lblMenuBar.TabIndex + 1;
				_btnNext.TabIndex = _lstMenus.TabIndex + 1;
				_btnAdd.TabIndex = _btnNext.TabIndex + 1;

				var _btnRemove = new Button ("Remove") {
					X = Pos.Left (_btnAdd),
					Y = Pos.Top (_btnAdd) + 1
				};
				_frmMenu.Add (_btnRemove);

				var _btnUp = new Button ("^") {
					X = Pos.Right (_lstMenus) + 2,
					Y = Pos.Top (_btnRemove) + 2
				};
				_frmMenu.Add (_btnUp);

				var _btnDown = new Button ("v") {
					X = Pos.Right (_lstMenus) + 2,
					Y = Pos.Top (_btnUp) + 1
				};
				_frmMenu.Add (_btnDown);

				Add (_frmMenu);

				var _frmMenuDetails = new DynamicMenuBarDetails ("Menu Details:") {
					X = Pos.Right (_frmMenu),
					Y = Pos.Top (_frmMenu),
					Width = Dim.Fill (),
					Height = Dim.Fill (2)
				};
				Add (_frmMenuDetails);

				_btnMenuBarUp.Clicked += (s,e) => {
					var i = _currentSelectedMenuBar;
					var menuItem = _menuBar != null && _menuBar.Menus.Length > 0 ? _menuBar.Menus [i] : null;
					if (menuItem != null) {
						var menus = _menuBar.Menus;
						if (i > 0) {
							menus [i] = menus [i - 1];
							menus [i - 1] = menuItem;
							_currentSelectedMenuBar = i - 1;
							_menuBar.SetNeedsDisplay ();
						}
					}
				};

				_btnMenuBarDown.Clicked += (s,e) => {
					var i = _currentSelectedMenuBar;
					var menuItem = _menuBar != null && _menuBar.Menus.Length > 0 ? _menuBar.Menus [i] : null;
					if (menuItem != null) {
						var menus = _menuBar.Menus;
						if (i < menus.Length - 1) {
							menus [i] = menus [i + 1];
							menus [i + 1] = menuItem;
							_currentSelectedMenuBar = i + 1;
							_menuBar.SetNeedsDisplay ();
						}
					}
				};

				_btnUp.Clicked += (s,e) => {
					var i = _lstMenus.SelectedItem;
					var menuItem = DataContext.Menus.Count > 0 ? DataContext.Menus [i].MenuItem : null;
					if (menuItem != null) {
						var childrens = ((MenuBarItem)_currentMenuBarItem).Children;
						if (i > 0) {
							childrens [i] = childrens [i - 1];
							childrens [i - 1] = menuItem;
							DataContext.Menus [i] = DataContext.Menus [i - 1];
							DataContext.Menus [i - 1] = new DynamicMenuItemList (menuItem.Title, menuItem);
							_lstMenus.SelectedItem = i - 1;
						}
					}
				};

				_btnDown.Clicked += (s,e) => {
					var i = _lstMenus.SelectedItem;
					var menuItem = DataContext.Menus.Count > 0 ? DataContext.Menus [i].MenuItem : null;
					if (menuItem != null) {
						var childrens = ((MenuBarItem)_currentMenuBarItem).Children;
						if (i < childrens.Length - 1) {
							childrens [i] = childrens [i + 1];
							childrens [i + 1] = menuItem;
							DataContext.Menus [i] = DataContext.Menus [i + 1];
							DataContext.Menus [i + 1] = new DynamicMenuItemList (menuItem.Title, menuItem);
							_lstMenus.SelectedItem = i + 1;
						}
					}
				};

				_btnPreviowsParent.Clicked += (s,e) => {
					if (_currentMenuBarItem != null && _currentMenuBarItem.Parent != null) {
						var mi = _currentMenuBarItem;
						_currentMenuBarItem = _currentMenuBarItem.Parent as MenuBarItem;
						SetListViewSource (_currentMenuBarItem, true);
						var i = ((MenuBarItem)_currentMenuBarItem).GetChildrenIndex (mi);
						if (i > -1) {
							_lstMenus.SelectedItem = i;
						}
						if (_currentMenuBarItem.Parent != null) {
							DataContext.Parent = _currentMenuBarItem.Title;
						} else {
							DataContext.Parent = ustring.Empty;
						}
					} else {
						DataContext.Parent = ustring.Empty;
					}
				};

				var _btnOk = new Button ("Ok") {
					X = Pos.Right (_frmMenu) + 20,
					Y = Pos.Bottom (_frmMenuDetails),
				};
				Add (_btnOk);

				var _btnCancel = new Button ("Cancel") {
					X = Pos.Right (_btnOk) + 3,
					Y = Pos.Top (_btnOk),
				};
				_btnCancel.Clicked += (s,e) => {
					SetFrameDetails (_currentEditMenuBarItem);
				};
				Add (_btnCancel);

				_lstMenus.SelectedItemChanged += (s,e) => {
					SetFrameDetails ();
				};

				_btnOk.Clicked += (s,e) => {
					if (ustring.IsNullOrEmpty (_frmMenuDetails._txtTitle.Text) && _currentEditMenuBarItem != null) {
						MessageBox.ErrorQuery ("Invalid title", "Must enter a valid title!.", "Ok");
					} else if (_currentEditMenuBarItem != null) {
						var menuItem = new DynamicMenuItem (_frmMenuDetails._txtTitle.Text, _frmMenuDetails._txtHelp.Text,
							_frmMenuDetails._txtAction.Text,
							_frmMenuDetails._ckbIsTopLevel != null ? (bool)_frmMenuDetails._ckbIsTopLevel.Checked : false,
							_frmMenuDetails._ckbSubMenu != null ? (bool)_frmMenuDetails._ckbSubMenu.Checked : false,
							_frmMenuDetails._rbChkStyle.SelectedItem == 0 ? MenuItemCheckStyle.NoCheck :
							_frmMenuDetails._rbChkStyle.SelectedItem == 1 ? MenuItemCheckStyle.Checked :
							MenuItemCheckStyle.Radio,
							_frmMenuDetails._txtShortcut.Text);
						UpdateMenuItem (_currentEditMenuBarItem, menuItem, _lstMenus.SelectedItem);
					}
				};

				_btnAdd.Clicked += (s,e) => {
					if (MenuBar == null) {
						MessageBox.ErrorQuery ("Menu Bar Error", "Must add a MenuBar first!", "Ok");
						_btnAddMenuBar.SetFocus ();
						return;
					}

					var frameDetails = new DynamicMenuBarDetails (null, _currentMenuBarItem != null);
					var item = frameDetails.EnterMenuItem ();
					if (item == null) {
						return;
					}

					if (!(_currentMenuBarItem is MenuBarItem)) {
						var parent = _currentMenuBarItem.Parent as MenuBarItem;
						var idx = parent.GetChildrenIndex (_currentMenuBarItem);
						_currentMenuBarItem = new MenuBarItem (_currentMenuBarItem.Title, new MenuItem [] { }, _currentMenuBarItem.Parent);
						_currentMenuBarItem.CheckType = item.checkStyle;
						parent.Children [idx] = _currentMenuBarItem;
					} else {
						MenuItem newMenu = CreateNewMenu (item, _currentMenuBarItem);
						var menuBarItem = _currentMenuBarItem as MenuBarItem;
						if (menuBarItem == null) {
							menuBarItem = new MenuBarItem (_currentMenuBarItem.Title, new MenuItem [] { newMenu }, _currentMenuBarItem.Parent);
						} else if (menuBarItem.Children == null) {
							menuBarItem.Children = new MenuItem [] { newMenu };
						} else {
							var childrens = menuBarItem.Children;
							Array.Resize (ref childrens, childrens.Length + 1);
							childrens [childrens.Length - 1] = newMenu;
							menuBarItem.Children = childrens;
						}
						DataContext.Menus.Add (new DynamicMenuItemList (newMenu.Title, newMenu));
						_lstMenus.MoveDown ();
					}
				};

				_btnRemove.Clicked += (s,e) => {
					var menuItem = DataContext.Menus.Count > 0 ? DataContext.Menus [_lstMenus.SelectedItem].MenuItem : null;
					if (menuItem != null) {
						var childrens = ((MenuBarItem)_currentMenuBarItem).Children;
						childrens [_lstMenus.SelectedItem] = null;
						int i = 0;
						foreach (var c in childrens) {
							if (c != null) {
								childrens [i] = c;
								i++;
							}
						}
						Array.Resize (ref childrens, childrens.Length - 1);
						if (childrens.Length == 0) {
							if (_currentMenuBarItem.Parent == null) {
								((MenuBarItem)_currentMenuBarItem).Children = null;
								//_currentMenuBarItem.Action = _frmMenuDetails.CreateAction (_currentEditMenuBarItem, new DynamicMenuItem (_currentMenuBarItem.Title));
							} else {
								_currentMenuBarItem = new MenuItem (_currentMenuBarItem.Title, _currentMenuBarItem.Help, _frmMenuDetails.CreateAction (_currentEditMenuBarItem, new DynamicMenuItem (_currentEditMenuBarItem.Title)), null, _currentMenuBarItem.Parent);
							}
						} else {
							((MenuBarItem)_currentMenuBarItem).Children = childrens;
						}
						DataContext.Menus.RemoveAt (_lstMenus.SelectedItem);
						if (_lstMenus.Source.Count > 0 && _lstMenus.SelectedItem > _lstMenus.Source.Count - 1) {
							_lstMenus.SelectedItem = _lstMenus.Source.Count - 1;
						}
						_lstMenus.SetNeedsDisplay ();
						SetFrameDetails ();
					}
				};

				_lstMenus.OpenSelectedItem += (s,e) => {
					_currentMenuBarItem = DataContext.Menus [e.Item].MenuItem;
					if (!(_currentMenuBarItem is MenuBarItem)) {
						MessageBox.ErrorQuery ("Menu Open Error", "Must allows sub menus first!", "Ok");
						return;
					}
					DataContext.Parent = _currentMenuBarItem.Title;
					DataContext.Menus = new List<DynamicMenuItemList> ();
					SetListViewSource (_currentMenuBarItem, true);
					var menuBarItem = DataContext.Menus.Count > 0 ? DataContext.Menus [0].MenuItem : null;
					SetFrameDetails (menuBarItem);
				};

				_lstMenus.Enter += (s, e) => {
					var menuBarItem = DataContext.Menus.Count > 0 ? DataContext.Menus [_lstMenus.SelectedItem].MenuItem : null;
					SetFrameDetails (menuBarItem);
				};

				_btnNext.Clicked += (s,e) => {
					if (_menuBar != null && _currentSelectedMenuBar + 1 < _menuBar.Menus.Length) {
						_currentSelectedMenuBar++;
					}
					SelectCurrentMenuBarItem ();
				};

				_btnPrevious.Clicked += (s,e) => {
					if (_currentSelectedMenuBar - 1 > -1) {
						_currentSelectedMenuBar--;
					}
					SelectCurrentMenuBarItem ();
				};

				_lblMenuBar.Enter += (s, e) => {
					if (_menuBar?.Menus != null) {
						_currentMenuBarItem = _menuBar.Menus [_currentSelectedMenuBar];
						SetFrameDetails (_menuBar.Menus [_currentSelectedMenuBar]);
					}
				};

				_btnAddMenuBar.Clicked += (s,e) => {
					var frameDetails = new DynamicMenuBarDetails (null, false);
					var item = frameDetails.EnterMenuItem ();
					if (item == null) {
						return;
					}

					if (MenuBar == null) {
						_menuBar = new MenuBar ();
						Add (_menuBar);
					}
					var newMenu = CreateNewMenu (item) as MenuBarItem;

					var menus = _menuBar.Menus;
					Array.Resize (ref menus, menus.Length + 1);
					menus [^1] = newMenu;
					_menuBar.Menus = menus;
					_currentMenuBarItem = newMenu;
					_currentMenuBarItem.CheckType = item.checkStyle;
					_currentSelectedMenuBar = menus.Length - 1;
					_menuBar.Menus [_currentSelectedMenuBar] = newMenu;
					_lblMenuBar.Text = newMenu.Title;
					SetListViewSource (_currentMenuBarItem, true);
					SetFrameDetails (_menuBar.Menus [_currentSelectedMenuBar]);
					_menuBar.SetNeedsDisplay ();
				};

				_btnRemoveMenuBar.Clicked += (s,e) => {
					if (_menuBar == null || _menuBar.Menus.Length == 0) {
						return;
					}

					if (_menuBar != null && _menuBar.Menus.Length > 0) {
						_menuBar.Menus [_currentSelectedMenuBar] = null;
						int i = 0;
						foreach (var m in _menuBar.Menus) {
							if (m != null) {
								_menuBar.Menus [i] = m;
								i++;
							}
						}
						var menus = _menuBar.Menus;
						Array.Resize (ref menus, menus.Length - 1);
						_menuBar.Menus = menus;
						if (_currentSelectedMenuBar - 1 >= 0 && _menuBar.Menus.Length > 0) {
							_currentSelectedMenuBar--;
						}
						_currentMenuBarItem = _menuBar.Menus?.Length > 0 ? _menuBar.Menus [_currentSelectedMenuBar] : null;
					}
					if (MenuBar != null && _currentMenuBarItem == null && _menuBar.Menus.Length == 0) {
						Remove (_menuBar);
						_menuBar = null;
						DataContext.Menus = new List<DynamicMenuItemList> ();
						_currentMenuBarItem = null;
						_currentSelectedMenuBar = -1;
						_lblMenuBar.Text = ustring.Empty;
					} else {
						_lblMenuBar.Text = _menuBar.Menus [_currentSelectedMenuBar].Title;
					}
					SetListViewSource (_currentMenuBarItem, true);
					SetFrameDetails (null);
				};

				SetFrameDetails ();

				var ustringConverter = new UStringValueConverter ();
				var listWrapperConverter = new ListWrapperConverter ();

				var lblMenuBar = new Binding (this, "MenuBar", _lblMenuBar, "Text", ustringConverter);
				var lblParent = new Binding (this, "Parent", _lblParent, "Text", ustringConverter);
				var lstMenus = new Binding (this, "Menus", _lstMenus, "Source", listWrapperConverter);

				void SetFrameDetails (MenuItem menuBarItem = null)
				{
					MenuItem menuItem;

					if (menuBarItem == null) {
						menuItem = DataContext.Menus.Count > 0 ? DataContext.Menus [_lstMenus.SelectedItem].MenuItem : null;
					} else {
						menuItem = menuBarItem;
					}

					_currentEditMenuBarItem = menuItem;
					_frmMenuDetails.EditMenuBarItem (menuItem);
					var f = _btnOk.Enabled == _frmMenuDetails.Enabled;
					if (!f) {
						_btnOk.Enabled = _frmMenuDetails.Enabled;
						_btnCancel.Enabled = _frmMenuDetails.Enabled;
					}
				}

				void SelectCurrentMenuBarItem ()
				{
					MenuBarItem menuBarItem = null;
					if (_menuBar?.Menus != null) {
						menuBarItem = _menuBar.Menus [_currentSelectedMenuBar];
						_lblMenuBar.Text = menuBarItem.Title;
					}
					SetFrameDetails (menuBarItem);
					_currentMenuBarItem = menuBarItem;
					DataContext.Menus = new List<DynamicMenuItemList> ();
					SetListViewSource (_currentMenuBarItem, true);
					_lblParent.Text = ustring.Empty;
				}

				void SetListViewSource (MenuItem _currentMenuBarItem, bool fill = false)
				{
					DataContext.Menus = new List<DynamicMenuItemList> ();
					var menuBarItem = _currentMenuBarItem as MenuBarItem;
					if (menuBarItem != null && menuBarItem?.Children == null) {
						return;
					}
					if (!fill) {
						return;
					}
					if (menuBarItem != null) {
						foreach (var child in menuBarItem?.Children) {
							var m = new DynamicMenuItemList (child.Title, child);
							DataContext.Menus.Add (m);
						}
					}
				}

				MenuItem CreateNewMenu (DynamicMenuItem item, MenuItem parent = null)
				{
					MenuItem newMenu;
					if (item.hasSubMenu) {
						newMenu = new MenuBarItem (item.title, new MenuItem [] { }, parent);
					} else if (parent != null) {
						newMenu = new MenuItem (item.title, item.help, null, null, parent);
						newMenu.CheckType = item.checkStyle;
						newMenu.Action = _frmMenuDetails.CreateAction (newMenu, item);
						newMenu.Shortcut = ShortcutHelper.GetShortcutFromTag (item.shortcut);
						newMenu.AllowNullChecked = item.allowNullChecked;
					} else if (item.isTopLevel) {
						newMenu = new MenuBarItem (item.title, item.help, null);
						newMenu.Action = _frmMenuDetails.CreateAction (newMenu, item);
					} else {
						newMenu = new MenuBarItem (item.title, item.help, null);
						((MenuBarItem)newMenu).Children [0].Action = _frmMenuDetails.CreateAction (newMenu, item);
						((MenuBarItem)newMenu).Children [0].Shortcut = ShortcutHelper.GetShortcutFromTag (item.shortcut);
					}

					return newMenu;
				}

				void UpdateMenuItem (MenuItem _currentEditMenuBarItem, DynamicMenuItem menuItem, int index)
				{
					_currentEditMenuBarItem.Title = menuItem.title;
					_currentEditMenuBarItem.Help = menuItem.help;
					_currentEditMenuBarItem.CheckType = menuItem.checkStyle;
					var parent = _currentEditMenuBarItem.Parent as MenuBarItem;
					if (parent != null && parent.Children.Length == 1 && _currentEditMenuBarItem.CheckType == MenuItemCheckStyle.Radio) {
						_currentEditMenuBarItem.Checked = true;
					}
					if (menuItem.isTopLevel && _currentEditMenuBarItem is MenuBarItem) {
						((MenuBarItem)_currentEditMenuBarItem).Children = null;
						_currentEditMenuBarItem.Action = _frmMenuDetails.CreateAction (_currentEditMenuBarItem, menuItem);
						SetListViewSource (_currentEditMenuBarItem, true);
					} else if (menuItem.hasSubMenu) {
						_currentEditMenuBarItem.Action = null;
						if (_currentEditMenuBarItem is MenuBarItem && ((MenuBarItem)_currentEditMenuBarItem).Children == null) {
							((MenuBarItem)_currentEditMenuBarItem).Children = new MenuItem [] { };
						} else if (_currentEditMenuBarItem.Parent != null) {
							_frmMenuDetails.UpdateParent (ref _currentEditMenuBarItem);
						} else {
							_currentEditMenuBarItem = new MenuBarItem (_currentEditMenuBarItem.Title, new MenuItem [] { }, _currentEditMenuBarItem.Parent);
						}
						SetListViewSource (_currentEditMenuBarItem, true);
					} else if (_currentEditMenuBarItem is MenuBarItem && _currentEditMenuBarItem.Parent != null) {
						_frmMenuDetails.UpdateParent (ref _currentEditMenuBarItem);
						_currentEditMenuBarItem = new MenuItem (menuItem.title, menuItem.help, _frmMenuDetails.CreateAction (_currentEditMenuBarItem, menuItem), null, _currentEditMenuBarItem.Parent);
					} else {
						if (_currentEditMenuBarItem is MenuBarItem) {
							((MenuBarItem)_currentEditMenuBarItem).Children = null;
							DataContext.Menus = new List<DynamicMenuItemList> ();
						}
						_currentEditMenuBarItem.Action = _frmMenuDetails.CreateAction (_currentEditMenuBarItem, menuItem);
						_currentEditMenuBarItem.Shortcut = ShortcutHelper.GetShortcutFromTag (menuItem.shortcut);
					}

					if (_currentEditMenuBarItem.Parent == null) {
						DataContext.MenuBar = _currentEditMenuBarItem.Title;
					} else {
						if (DataContext.Menus.Count == 0) {
							DataContext.Menus.Add (new DynamicMenuItemList (_currentEditMenuBarItem.Title, _currentEditMenuBarItem));
						}
						DataContext.Menus [index] = new DynamicMenuItemList (_currentEditMenuBarItem.Title, _currentEditMenuBarItem);
					}
					_currentEditMenuBarItem.CheckType = menuItem.checkStyle;
					SetFrameDetails (_currentEditMenuBarItem);
				}

				//_frmMenuDetails.Initialized += (s, e) => _frmMenuDetails.Enabled = false;
			}
		}

		public class DynamicMenuBarDetails : FrameView {
			public MenuItem _menuItem;
			public TextField _txtTitle;
			public TextField _txtHelp;
			public TextView _txtAction;
			public CheckBox _ckbIsTopLevel;
			public CheckBox _ckbSubMenu;
			public CheckBox _ckbNullCheck;
			public RadioGroup _rbChkStyle;
			public TextField _txtShortcut;

			bool hasParent;

			public DynamicMenuBarDetails (MenuItem menuItem = null, bool hasParent = false) : this (menuItem == null ? "Adding New Menu." : "Editing Menu.")
			{
				_menuItem = menuItem;
				this.hasParent = hasParent;
			}

			public DynamicMenuBarDetails (ustring title) : base (title)
			{
				var _lblTitle = new Label ("Title:") {
					Y = 1
				};
				Add (_lblTitle);

				_txtTitle = new TextField () {
					X = Pos.Right (_lblTitle) + 2,
					Y = Pos.Top (_lblTitle),
					Width = Dim.Fill ()
				};
				Add (_txtTitle);

				var _lblHelp = new Label ("Help:") {
					X = Pos.Left (_lblTitle),
					Y = Pos.Bottom (_lblTitle) + 1
				};
				Add (_lblHelp);

				_txtHelp = new TextField () {
					X = Pos.Left (_txtTitle),
					Y = Pos.Top (_lblHelp),
					Width = Dim.Fill ()
				};
				Add (_txtHelp);

				var _lblAction = new Label ("Action:") {
					X = Pos.Left (_lblTitle),
					Y = Pos.Bottom (_lblHelp) + 1
				};
				Add (_lblAction);

				_txtAction = new TextView () {
					X = Pos.Left (_txtTitle),
					Y = Pos.Top (_lblAction),
					Width = Dim.Fill (),
					Height = 5
				};
				Add (_txtAction);

				_ckbIsTopLevel = new CheckBox ("IsTopLevel") {
					X = Pos.Left (_lblTitle),
					Y = Pos.Bottom (_lblAction) + 5
				};
				Add (_ckbIsTopLevel);

				_ckbSubMenu = new CheckBox ("Has sub-menus") {
					X = Pos.Left (_lblTitle),
					Y = Pos.Bottom (_ckbIsTopLevel),
					Checked = _menuItem == null ? !hasParent : HasSubMenus (_menuItem)
				};
				Add (_ckbSubMenu);

				_ckbNullCheck = new CheckBox ("Allow null checked") {
					X = Pos.Left (_lblTitle),
					Y = Pos.Bottom (_ckbSubMenu)
				};
				Add (_ckbNullCheck);

				var _rChkLabels = new ustring [] { "NoCheck", "Checked", "Radio" };
				_rbChkStyle = new RadioGroup (_rChkLabels) {
					X = Pos.Left (_lblTitle),
					Y = Pos.Bottom (_ckbSubMenu) + 1,
				};
				Add (_rbChkStyle);

				var _lblShortcut = new Label ("Shortcut:") {
					X = Pos.Right (_ckbSubMenu) + 10,
					Y = Pos.Top (_ckbSubMenu)
				};
				Add (_lblShortcut);

				_txtShortcut = new TextField () {
					X = Pos.X (_lblShortcut),
					Y = Pos.Bottom (_lblShortcut),
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
					var m = _menuItem != null ? _menuItem : new MenuItem ();
					if (pre && !ShortcutHelper.PreShortcutValidation (k)) {
						_txtShortcut.Text = "";
						return false;
					}
					if (!pre) {
						if (!ShortcutHelper.PostShortcutValidation (ShortcutHelper.GetShortcutFromTag (_txtShortcut.Text))) {
							_txtShortcut.Text = "";
							return false;
						}
						return true;
					}
					_txtShortcut.Text = ShortcutHelper.GetShortcutTag (k);

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

				_ckbIsTopLevel.Toggled += (s, e) => {
					if ((_menuItem != null && _menuItem.Parent != null && (bool)_ckbIsTopLevel.Checked) ||
						_menuItem == null && hasParent && (bool)_ckbIsTopLevel.Checked) {
						MessageBox.ErrorQuery ("Invalid IsTopLevel", "Only menu bar can have top level menu item!", "Ok");
						_ckbIsTopLevel.Checked = false;
						return;
					}
					if (_ckbIsTopLevel.Checked == true) {
						_ckbSubMenu.Checked = false;
						_ckbSubMenu.SetNeedsDisplay ();
						_txtHelp.Enabled = true;
						_txtAction.Enabled = true;
						_txtShortcut.Enabled = _ckbIsTopLevel.Checked == false && _ckbSubMenu.Checked == false;
					} else {
						if (_menuItem == null && !hasParent || _menuItem.Parent == null) {
							_ckbSubMenu.Checked = true;
							_ckbSubMenu.SetNeedsDisplay ();
							_txtShortcut.Enabled = false;
						}
						_txtHelp.Text = "";
						_txtHelp.Enabled = false;
						_txtAction.Text = "";
						_txtAction.Enabled = false;
					}
				};
				_ckbSubMenu.Toggled += (s, e) => {
					if ((bool)_ckbSubMenu.Checked) {
						_ckbIsTopLevel.Checked = false;
						_ckbIsTopLevel.SetNeedsDisplay ();
						_txtHelp.Text = "";
						_txtHelp.Enabled = false;
						_txtAction.Text = "";
						_txtAction.Enabled = false;
						_txtShortcut.Text = "";
						_txtShortcut.Enabled = false;
					} else {
						if (!hasParent) {
							_ckbIsTopLevel.Checked = true;
							_ckbIsTopLevel.SetNeedsDisplay ();
							_txtShortcut.Enabled = false;
						}
						_txtHelp.Enabled = true;
						_txtAction.Enabled = true;
						_txtShortcut.Enabled = _ckbIsTopLevel.Checked == false && _ckbSubMenu.Checked == false;
					}
				};
				_ckbNullCheck.Toggled += (s,e) => {
					if (_menuItem != null) {
						_menuItem.AllowNullChecked = (bool)_ckbNullCheck.Checked;
					}
				};

				//Add (_frmMenuDetails);

			}

			public DynamicMenuItem EnterMenuItem ()
			{
				var valid = false;

				if (_menuItem == null) {
					var m = new DynamicMenuItem ();
					_txtTitle.Text = m.title;
					_txtHelp.Text = m.help;
					_txtAction.Text = m.action;
					_ckbIsTopLevel.Checked = false;
					_ckbSubMenu.Checked = !hasParent;
					_ckbNullCheck.Checked = false;
					_txtHelp.Enabled = hasParent;
					_txtAction.Enabled = hasParent;
					_txtShortcut.Enabled = hasParent;
				} else {
					EditMenuBarItem (_menuItem);
				}

				var _btnOk = new Button ("Ok") {
					IsDefault = true,
				};
				_btnOk.Clicked += (s,e) => {
					if (ustring.IsNullOrEmpty (_txtTitle.Text)) {
						MessageBox.ErrorQuery ("Invalid title", "Must enter a valid title!.", "Ok");
					} else {
						valid = true;
						Application.RequestStop ();
					}
				};
				var _btnCancel = new Button ("Cancel");
				_btnCancel.Clicked += (s,e) => {
					_txtTitle.Text = ustring.Empty;
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
					return new DynamicMenuItem (_txtTitle.Text, _txtHelp.Text, _txtAction.Text,
						_ckbIsTopLevel != null ? (bool)_ckbIsTopLevel.Checked : false,
						_ckbSubMenu != null ? (bool)_ckbSubMenu.Checked : false,
						_rbChkStyle.SelectedItem == 0 ? MenuItemCheckStyle.NoCheck :
						_rbChkStyle.SelectedItem == 1 ? MenuItemCheckStyle.Checked : MenuItemCheckStyle.Radio,
						_txtShortcut.Text, (bool)_ckbNullCheck.Checked);
				} else {
					return null;
				}
			}

			public void EditMenuBarItem (MenuItem menuItem)
			{
				if (menuItem == null) {
					hasParent = false;
					Enabled = false;
					CleanEditMenuBarItem ();
					return;
				} else {
					hasParent = menuItem.Parent != null;
					Enabled = true;
				}
				_menuItem = menuItem;
				_txtTitle.Text = menuItem?.Title ?? "";
				_txtHelp.Text = menuItem?.Help ?? "";
				_txtAction.Text = menuItem != null && menuItem.Action != null ? GetTargetAction (menuItem.Action) : ustring.Empty;
				_ckbIsTopLevel.Checked = IsTopLevel (menuItem);
				_ckbSubMenu.Checked = HasSubMenus (menuItem);
				_ckbNullCheck.Checked = menuItem.AllowNullChecked;
				_txtHelp.Enabled = (bool)!_ckbSubMenu.Checked;
				_txtAction.Enabled = (bool)!_ckbSubMenu.Checked;
				_rbChkStyle.SelectedItem = (int)(menuItem?.CheckType ?? MenuItemCheckStyle.NoCheck);
				_txtShortcut.Text = menuItem?.ShortcutTag ?? "";
				_txtShortcut.Enabled = _ckbIsTopLevel.Checked == false && _ckbSubMenu.Checked == false;
			}

			void CleanEditMenuBarItem ()
			{
				_txtTitle.Text = "";
				_txtHelp.Text = "";
				_txtAction.Text = "";
				_ckbIsTopLevel.Checked = false;
				_ckbSubMenu.Checked = false;
				_rbChkStyle.SelectedItem = (int)MenuItemCheckStyle.NoCheck;
				_txtShortcut.Text = "";
			}

			ustring GetTargetAction (Action action)
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
				return v == null || !(v is DynamicMenuItem item) ? ustring.Empty : item.action;
			}

			bool IsTopLevel (MenuItem menuItem)
			{
				var topLevel = menuItem as MenuBarItem;
				if (topLevel != null && topLevel.Parent == null && (topLevel.Children == null || topLevel.Children.Length == 0) && topLevel.Action != null) {
					return true;
				} else {
					return false;
				}
			}

			bool HasSubMenus (MenuItem menuItem)
			{
				var menuBarItem = menuItem as MenuBarItem;
				if (menuBarItem != null && menuBarItem.Children != null && (menuBarItem.Children.Length > 0 || menuBarItem.Action == null)) {
					return true;
				} else {
					return false;
				}
			}

			public Action CreateAction (MenuItem menuItem, DynamicMenuItem item)
			{
				switch (menuItem.CheckType) {
				case MenuItemCheckStyle.NoCheck:
					return new Action (() => MessageBox.ErrorQuery (item.title, item.action, "Ok"));
				case MenuItemCheckStyle.Checked:
					return new Action (menuItem.ToggleChecked);
				case MenuItemCheckStyle.Radio:
					break;
				}
				return new Action (() => {
					menuItem.Checked = true;
					var parent = menuItem?.Parent as MenuBarItem;
					if (parent != null) {
						var childrens = parent.Children;
						for (int i = 0; i < childrens.Length; i++) {
							var child = childrens [i];
							if (child != menuItem) {
								child.Checked = false;
							}
						}
					}
				});
			}

			public void UpdateParent (ref MenuItem menuItem)
			{
				var parent = menuItem.Parent as MenuBarItem;
				var idx = parent.GetChildrenIndex (menuItem);
				if (!(menuItem is MenuBarItem)) {
					menuItem = new MenuBarItem (menuItem.Title, new MenuItem [] { }, menuItem.Parent);
					if (idx > -1) {
						parent.Children [idx] = menuItem;
					}
				} else {
					menuItem = new MenuItem (menuItem.Title, menuItem.Help, CreateAction (menuItem, new DynamicMenuItem ()), null, menuItem.Parent);
					if (idx > -1) {
						parent.Children [idx] = menuItem;
					}
				}
			}
		}

		public class DynamicMenuItemModel : INotifyPropertyChanged {
			public event PropertyChangedEventHandler PropertyChanged;

			private ustring menuBar;
			private ustring parent;
			private List<DynamicMenuItemList> menus;

			public ustring MenuBar {
				get => menuBar;
				set {
					if (value != menuBar) {
						menuBar = value;
						PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (GetPropertyName ()));
					}
				}
			}

			public ustring Parent {
				get => parent;
				set {
					if (value != parent) {
						parent = value;
						PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (GetPropertyName ()));
					}
				}
			}

			public List<DynamicMenuItemList> Menus {
				get => menus;
				set {
					if (value != menus) {
						menus = value;
						PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (GetPropertyName ()));
					}
				}
			}

			public DynamicMenuItemModel ()
			{
				Menus = new List<DynamicMenuItemList> ();
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
				return ustring.Make (data);
			}
		}
	}
}
