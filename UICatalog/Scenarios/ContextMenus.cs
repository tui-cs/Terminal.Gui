using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "ContextMenus", Description: "Context Menu Sample.")]
	[ScenarioCategory ("Menus")]
	public class ContextMenus : Scenario {
		private ContextMenu contextMenu = new ContextMenu ();
		private readonly List<CultureInfo> cultureInfos = Application.SupportedCultures;
		private MenuItem miForceMinimumPosToZero;
		private bool forceMinimumPosToZero = true;
		private TextField tfTopLeft, tfTopRight, tfMiddle, tfBottomLeft, tfBottomRight;
		private MenuItem miUseSubMenusSingleFrame;
		private bool useSubMenusSingleFrame;

		public override void Setup ()
		{
			var text = "Context Menu";
			var width = 20;

			Win.Add (new Label ("Press 'Ctrl + Space' to open the Window context menu.") {
				X = Pos.Center (),
				Y = 1
			});

			tfTopLeft = new TextField (text) {
				Width = width
			};
			Win.Add (tfTopLeft);

			tfTopRight = new TextField (text) {
				X = Pos.AnchorEnd (width),
				Width = width
			};
			Win.Add (tfTopRight);

			tfMiddle = new TextField (text) {
				X = Pos.Center (),
				Y = Pos.Center (),
				Width = width
			};
			Win.Add (tfMiddle);

			tfBottomLeft = new TextField (text) {
				Y = Pos.AnchorEnd (1),
				Width = width
			};
			Win.Add (tfBottomLeft);

			tfBottomRight = new TextField (text) {
				X = Pos.AnchorEnd (width),
				Y = Pos.AnchorEnd (1),
				Width = width
			};
			Win.Add (tfBottomRight);

			Point mousePos = default;

			Win.KeyPress += (s, e) => {
				if (e.KeyEvent.Key == (Key.Space | Key.CtrlMask)) {
					ShowContextMenu (mousePos.X, mousePos.Y);
					e.Handled = true;
				}
			};

			Win.MouseClick += (s, e) => {
				if (e.MouseEvent.Flags == contextMenu.MouseFlags) {
					ShowContextMenu (e.MouseEvent.X, e.MouseEvent.Y);
					e.Handled = true;
				}
			};

			Application.RootMouseEvent += Application_RootMouseEvent;

			void Application_RootMouseEvent (MouseEvent me)
			{
				mousePos = new Point (me.X, me.Y);
			}

			Win.WantMousePositionReports = true;

			Application.Top.Closed += (s,e) => {
				Thread.CurrentThread.CurrentUICulture = new CultureInfo ("en-US");
				Application.RootMouseEvent -= Application_RootMouseEvent;
			};
		}

		private void ShowContextMenu (int x, int y)
		{
			contextMenu = new ContextMenu (x, y,
				new MenuBarItem (new MenuItem [] {
					new MenuItem ("_Configuration", "Show configuration", () => MessageBox.Query (50, 5, "Info", "This would open settings dialog", "Ok")),
					new MenuBarItem ("More options", new MenuItem [] {
						new MenuItem ("_Setup", "Change settings", () => MessageBox.Query (50, 5, "Info", "This would open setup dialog", "Ok")),
						new MenuItem ("_Maintenance", "Maintenance mode", () => MessageBox.Query (50, 5, "Info", "This would open maintenance dialog", "Ok")),
					}),
					new MenuBarItem ("_Languages", GetSupportedCultures ()),
					miForceMinimumPosToZero = new MenuItem ("ForceMinimumPosToZero", "", () => {
						miForceMinimumPosToZero.Checked = forceMinimumPosToZero = !forceMinimumPosToZero;
						tfTopLeft.ContextMenu.ForceMinimumPosToZero = forceMinimumPosToZero;
						tfTopRight.ContextMenu.ForceMinimumPosToZero = forceMinimumPosToZero;
						tfMiddle.ContextMenu.ForceMinimumPosToZero = forceMinimumPosToZero;
						tfBottomLeft.ContextMenu.ForceMinimumPosToZero = forceMinimumPosToZero;
						tfBottomRight.ContextMenu.ForceMinimumPosToZero = forceMinimumPosToZero;
					}) { CheckType = MenuItemCheckStyle.Checked, Checked = forceMinimumPosToZero },
					miUseSubMenusSingleFrame = new MenuItem ("Use_SubMenusSingleFrame", "",
						() => contextMenu.UseSubMenusSingleFrame = (bool)(miUseSubMenusSingleFrame.Checked = useSubMenusSingleFrame = !useSubMenusSingleFrame)) {
							CheckType = MenuItemCheckStyle.Checked, Checked = useSubMenusSingleFrame
						},
					null,
					new MenuItem ("_Quit", "", () => Application.RequestStop ())
				})
			) { ForceMinimumPosToZero = forceMinimumPosToZero, UseSubMenusSingleFrame = useSubMenusSingleFrame };

			tfTopLeft.ContextMenu.ForceMinimumPosToZero = forceMinimumPosToZero;
			tfTopRight.ContextMenu.ForceMinimumPosToZero = forceMinimumPosToZero;
			tfMiddle.ContextMenu.ForceMinimumPosToZero = forceMinimumPosToZero;
			tfBottomLeft.ContextMenu.ForceMinimumPosToZero = forceMinimumPosToZero;
			tfBottomRight.ContextMenu.ForceMinimumPosToZero = forceMinimumPosToZero;

			contextMenu.Show ();
		}

		private MenuItem [] GetSupportedCultures ()
		{
			List<MenuItem> supportedCultures = new List<MenuItem> ();
			var index = -1;

			foreach (var c in cultureInfos) {
				var culture = new MenuItem {
					CheckType = MenuItemCheckStyle.Checked
				};
				if (index == -1) {
					culture.Title = "_English";
					culture.Help = "en-US";
					culture.Checked = Thread.CurrentThread.CurrentUICulture.Name == "en-US";
					CreateAction (supportedCultures, culture);
					supportedCultures.Add (culture);
					index++;
					culture = new MenuItem {
						CheckType = MenuItemCheckStyle.Checked
					};
				}
				culture.Title = $"_{c.Parent.EnglishName}";
				culture.Help = c.Name;
				culture.Checked = Thread.CurrentThread.CurrentUICulture.Name == c.Name;
				CreateAction (supportedCultures, culture);
				supportedCultures.Add (culture);
			}
			return supportedCultures.ToArray ();

			void CreateAction (List<MenuItem> supportedCultures, MenuItem culture)
			{
				culture.Action += () => {
					Thread.CurrentThread.CurrentUICulture = new CultureInfo (culture.Help.ToString ());
					culture.Checked = true;
					foreach (var item in supportedCultures) {
						item.Checked = item.Help.ToString () == Thread.CurrentThread.CurrentUICulture.Name;
					}
				};
			}
		}
	}
}