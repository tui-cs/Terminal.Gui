using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using static UICatalog.Scenario;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "Tab View", Description: "Demos TabView control with limited screen space in Absolute layout.")]
	[ScenarioCategory ("Controls"), ScenarioCategory ("TabView")]
	public class TabViewExample : Scenario {

		TabView tabView;

		MenuItem miShowTopLine;
		MenuItem miShowBorder;
		MenuItem miTabsOnBottom;

		public override void Setup ()
		{
			Win.Title = this.GetName ();
			Win.Y = 1; // menu
			Win.Height = Dim.Fill (1); // status bar
			Application.Top.LayoutSubviews ();

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {

					new MenuItem ("_Add Blank Tab", "", () => AddBlankTab()),

					new MenuItem ("_Clear SelectedTab", "", () => tabView.SelectedTab=null),
					new MenuItem ("_Quit", "", () => Quit()),
				}),
				new MenuBarItem ("_View", new MenuItem [] {
					miShowTopLine = new MenuItem ("_Show Top Line", "", () => ShowTopLine()){
						Checked = true,
						CheckType = MenuItemCheckStyle.Checked
					},
					miShowBorder = new MenuItem ("_Show Border", "", () => ShowBorder()){
						Checked = true,
						CheckType = MenuItemCheckStyle.Checked
					},
					miTabsOnBottom = new MenuItem ("_Tabs On Bottom", "", () => SetTabsOnBottom()){
						Checked = false,
						CheckType = MenuItemCheckStyle.Checked
					}

					})
				});
			Application.Top.Add (menu);

			tabView = new TabView () {
				X = 0,
				Y = 0,
				Width = 60,
				Height = 20,
			};

			tabView.AddTab (new TabView.Tab ("Tab1", new Label ("hodor!")), false);
			tabView.AddTab (new TabView.Tab ("Tab2", new Label ("durdur")), false);
			tabView.AddTab (new TabView.Tab ("Interactive Tab", GetInteractiveTab ()), false);
			tabView.AddTab (new TabView.Tab ("Big Text", GetBigTextFileTab ()), false);
			tabView.AddTab (new TabView.Tab (
				"Long name Tab, I mean seriously long.  Like you would not believe how long this tab's name is its just too much really woooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooowwww thats long",
				 new Label ("This tab has a very long name which should be truncated.  See TabView.MaxTabTextWidth")),
				 false);
			tabView.AddTab (new TabView.Tab ("Les Mise" + Char.ConvertFromUtf32 (Int32.Parse ("0301", NumberStyles.HexNumber)) + "rables", new Label ("This tab name is unicode")), false);

			for (int i = 0; i < 100; i++) {
				tabView.AddTab (new TabView.Tab ($"Tab{i}", new Label ($"Welcome to tab {i}")), false);
			}

			tabView.SelectedTab = tabView.Tabs.First ();

			Win.Add (tabView);

			var frameRight = new FrameView ("About") {
				X = Pos.Right (tabView),
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};

			frameRight.Add (new TextView () {
				Text = "This demos the tabs control\nSwitch between tabs using cursor keys",
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			});

			Win.Add (frameRight);

			var frameBelow = new FrameView ("Bottom Frame") {
				X = 0,
				Y = Pos.Bottom (tabView),
				Width = tabView.Width,
				Height = Dim.Fill (),
			};

			frameBelow.Add (new TextView () {
				Text = "This frame exists to check you can still tab here\nand that the tab control doesn't overspill it's bounds",
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			});

			Win.Add (frameBelow);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Application.QuitKey, $"{Application.QuitKey} to Quit", () => Quit()),
			});
			Application.Top.Add (statusBar);
		}

		private void AddBlankTab ()
		{
			tabView.AddTab (new TabView.Tab (), false);
		}

		private View GetInteractiveTab ()
		{

			var interactiveTab = new View () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			var lblName = new Label ("Name:");
			interactiveTab.Add (lblName);

			var tbName = new TextField () {
				X = Pos.Right (lblName),
				Width = 10
			};
			interactiveTab.Add (tbName);

			var lblAddr = new Label ("Address:") {
				Y = 1
			};
			interactiveTab.Add (lblAddr);

			var tbAddr = new TextField () {
				X = Pos.Right (lblAddr),
				Y = 1,
				Width = 10
			};
			interactiveTab.Add (tbAddr);

			return interactiveTab;
		}

		private View GetBigTextFileTab ()
		{

			var text = new TextView () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};

			var sb = new System.Text.StringBuilder ();

			for (int y = 0; y < 300; y++) {
				for (int x = 0; x < 500; x++) {
					sb.Append ((x + y) % 2 == 0 ? '1' : '0');
				}
				sb.AppendLine ();
			}
			text.Text = sb.ToString ();

			return text;
		}

		private void ShowTopLine ()
		{
			miShowTopLine.Checked = !miShowTopLine.Checked;

			tabView.Style.ShowTopLine = (bool)miShowTopLine.Checked;
			tabView.ApplyStyleChanges ();
		}
		private void ShowBorder ()
		{
			miShowBorder.Checked = !miShowBorder.Checked;

			tabView.Style.ShowBorder = (bool)miShowBorder.Checked;
			tabView.ApplyStyleChanges ();
		}
		private void SetTabsOnBottom ()
		{
			miTabsOnBottom.Checked = !miTabsOnBottom.Checked;

			tabView.Style.TabsOnBottom = (bool)miTabsOnBottom.Checked;
			tabView.ApplyStyleChanges ();
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}
