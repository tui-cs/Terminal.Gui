using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Terminal.Gui;
using Xunit;
using System.Globalization;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests {

	public class TabViewTests {
		readonly ITestOutputHelper output;

		public TabViewTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		private TabView GetTabView ()
		{
			return GetTabView (out _, out _);
		}

		private TabView GetTabView (out Tab tab1, out Tab tab2, bool initFakeDriver = true)
		{
			if (initFakeDriver)
				InitFakeDriver ();

			var tv = new TabView ();
			tv.ColorScheme = new ColorScheme ();
			tv.AddTab (tab1 = new Tab ("Tab1", new TextField ("hi")), false);
			tv.AddTab (tab2 = new Tab ("Tab2", new Label ("hi2")), false);
			return tv;
		}

		[Fact]
		public void AddTwoTabs_SecondIsSelected ()
		{
			InitFakeDriver ();

			var tv = new TabView ();
			Tab tab1;
			Tab tab2;
			tv.AddTab (tab1 = new Tab ("Tab1", new TextField ("hi")), false);
			tv.AddTab (tab2 = new Tab ("Tab1", new Label ("hi2")), true);

			Assert.Equal (2, tv.Tabs.Count);
			Assert.Equal (tab2, tv.SelectedTab);

			Application.Shutdown ();
		}

		[Fact]
		public void EnsureSelectedTabVisible_NullSelect ()
		{
			var tv = GetTabView ();

			tv.SelectedTab = null;

			Assert.Null (tv.SelectedTab);
			Assert.Equal (0, tv.TabScrollOffset);

			tv.EnsureSelectedTabIsVisible ();

			Assert.Null (tv.SelectedTab);
			Assert.Equal (0, tv.TabScrollOffset);

			Application.Shutdown ();
		}

		[Fact]
		public void EnsureSelectedTabVisible_MustScroll ()
		{
			var tv = GetTabView (out var tab1, out var tab2);

			// Make tab width small to force only one tab visible at once
			tv.Width = 4;

			tv.SelectedTab = tab1;
			Assert.Equal (0, tv.TabScrollOffset);
			tv.EnsureSelectedTabIsVisible ();
			Assert.Equal (0, tv.TabScrollOffset);

			// Asking to show tab2 should automatically move scroll offset accordingly
			tv.SelectedTab = tab2;
			Assert.Equal (1, tv.TabScrollOffset);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void SelectedTabChanged_Called ()
		{
			var tv = GetTabView (out var tab1, out var tab2);

			tv.SelectedTab = tab1;

			Tab oldTab = null;
			Tab newTab = null;
			int called = 0;

			tv.SelectedTabChanged += (s, e) => {
				oldTab = e.OldTab;
				newTab = e.NewTab;
				called++;
			};

			tv.SelectedTab = tab2;

			Assert.Equal (1, called);
			Assert.Equal (tab1, oldTab);
			Assert.Equal (tab2, newTab);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void RemoveTab_ChangesSelection ()
		{
			var tv = GetTabView (out var tab1, out var tab2);

			tv.SelectedTab = tab1;
			tv.RemoveTab (tab1);

			Assert.Equal (tab2, tv.SelectedTab);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void RemoveTab_MultipleCalls_NotAnError ()
		{
			var tv = GetTabView (out var tab1, out var tab2);

			tv.SelectedTab = tab1;

			// Repeated calls to remove a tab that is not part of
			// the collection should be ignored
			tv.RemoveTab (tab1);
			tv.RemoveTab (tab1);
			tv.RemoveTab (tab1);
			tv.RemoveTab (tab1);

			Assert.Equal (tab2, tv.SelectedTab);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void RemoveAllTabs_ClearsSelection ()
		{
			var tv = GetTabView (out var tab1, out var tab2);

			tv.SelectedTab = tab1;
			tv.RemoveTab (tab1);
			tv.RemoveTab (tab2);

			Assert.Null (tv.SelectedTab);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void SwitchTabBy_NormalUsage ()
		{
			var tv = GetTabView (out var tab1, out var tab2);

			Tab tab3;
			Tab tab4;
			Tab tab5;

			tv.AddTab (tab3 = new Tab (), false);
			tv.AddTab (tab4 = new Tab (), false);
			tv.AddTab (tab5 = new Tab (), false);

			tv.SelectedTab = tab1;

			int called = 0;
			tv.SelectedTabChanged += (s, e) => { called++; };

			tv.SwitchTabBy (1);

			Assert.Equal (1, called);
			Assert.Equal (tab2, tv.SelectedTab);

			//reset called counter
			called = 0;

			// go right 2
			tv.SwitchTabBy (2);

			// even though we go right 2 indexes the event should only be called once
			Assert.Equal (1, called);
			Assert.Equal (tab4, tv.SelectedTab);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void AddTab_SameTabMoreThanOnce ()
		{
			var tv = GetTabView (out var tab1, out var tab2);

			Assert.Equal (2, tv.Tabs.Count);

			// Tab is already part of the control so shouldn't result in duplication
			tv.AddTab (tab1, false);
			tv.AddTab (tab1, false);
			tv.AddTab (tab1, false);
			tv.AddTab (tab1, false);

			Assert.Equal (2, tv.Tabs.Count);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void SwitchTabBy_OutOfTabsRange ()
		{
			var tv = GetTabView (out var tab1, out var tab2);

			tv.SelectedTab = tab1;
			tv.SwitchTabBy (500);

			Assert.Equal (tab2, tv.SelectedTab);

			tv.SwitchTabBy (-500);

			Assert.Equal (tab1, tv.SelectedTab);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact, AutoInitShutdown]
		public void ShowTopLine_True_TabsOnBottom_False_TestThinTabView_WithLongNames ()
		{
			var tv = GetTabView (out var tab1, out var tab2, false);
			tv.Width = 10;
			tv.Height = 5;

			// Ensures that the tab bar subview gets the bounds of the parent TabView
			tv.LayoutSubviews ();

			// Test two tab names that fit 
			tab1.Text = "12";
			tab2.Text = "13";

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──┐      
│12│13    
│  └─────┐
│hi      │
└────────┘", output);

			tv.SelectedTab = tab2;

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
   ┌──┐   
 12│13│   
┌──┘  └──┐
│hi2     │
└────────┘", output);

			tv.SelectedTab = tab1;
			// Test first tab name too long
			tab1.Text = "12345678910";
			tab2.Text = "13";

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌───────┐ 
│1234567│ 
│       └►
│hi      │
└────────┘", output);

			//switch to tab2
			tv.SelectedTab = tab2;
			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──┐      
│13│      
◄  └─────┐
│hi2     │
└────────┘", output);

			// now make both tabs too long
			tab1.Text = "12345678910";
			tab2.Text = "abcdefghijklmnopq";

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌───────┐ 
│abcdefg│ 
◄       └┐
│hi2     │
└────────┘", output);
		}

		[Fact, AutoInitShutdown]
		public void ShowTopLine_False_TabsOnBottom_False_TestThinTabView_WithLongNames ()
		{
			var tv = GetTabView (out var tab1, out var tab2, false);
			tv.Width = 10;
			tv.Height = 5;
			tv.Style = new TabStyle { ShowTopLine = false };
			tv.ApplyStyleChanges ();

			// Ensures that the tab bar subview gets the bounds of the parent TabView
			tv.LayoutSubviews ();

			// Test two tab names that fit 
			tab1.Text = "12";
			tab2.Text = "13";

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
│12│13    
│  └─────┐
│hi      │
│        │
└────────┘", output);

			tv.SelectedTab = tab2;

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
 12│13│   
┌──┘  └──┐
│hi2     │
│        │
└────────┘", output);

			tv.SelectedTab = tab1;

			// Test first tab name too long
			tab1.Text = "12345678910";
			tab2.Text = "13";

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
│1234567│ 
│       └►
│hi      │
│        │
└────────┘", output);

			//switch to tab2
			tv.SelectedTab = tab2;
			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
│13│      
◄  └─────┐
│hi2     │
│        │
└────────┘", output);

			// now make both tabs too long
			tab1.Text = "12345678910";
			tab2.Text = "abcdefghijklmnopq";

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
│abcdefg│ 
◄       └┐
│hi2     │
│        │
└────────┘", output);
		}

		[Fact, AutoInitShutdown]
		public void ShowTopLine_True_TabsOnBottom_False_TestTabView_Width4 ()
		{
			var tv = GetTabView (out _, out _, false);
			tv.Width = 4;
			tv.Height = 5;
			tv.LayoutSubviews ();

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌─┐ 
│T│ 
│ └►
│hi│
└──┘", output);
		}

		[Fact, AutoInitShutdown]
		public void ShowTopLine_False_TabsOnBottom_False_TestTabView_Width4 ()
		{
			var tv = GetTabView (out _, out _, false);
			tv.Width = 4;
			tv.Height = 5;
			tv.Style = new TabStyle { ShowTopLine = false };
			tv.ApplyStyleChanges ();
			tv.LayoutSubviews ();

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
│T│ 
│ └►
│hi│
│  │
└──┘", output);
		}

		[Fact, AutoInitShutdown]
		public void ShowTopLine_True_TabsOnBottom_False_TestTabView_Width3 ()
		{
			var tv = GetTabView (out _, out _, false);
			tv.Width = 3;
			tv.Height = 5;
			tv.LayoutSubviews ();

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌┐ 
││ 
│└►
│h│
└─┘", output);
		}

		[Fact, AutoInitShutdown]
		public void ShowTopLine_False_TabsOnBottom_False_TestTabView_Width3 ()
		{
			var tv = GetTabView (out _, out _, false);
			tv.Width = 3;
			tv.Height = 5;
			tv.Style = new TabStyle { ShowTopLine = false };
			tv.ApplyStyleChanges ();
			tv.LayoutSubviews ();

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
││ 
│└►
│h│
│ │
└─┘", output);
		}

		[Fact, AutoInitShutdown]
		public void ShowTopLine_True_TabsOnBottom_True_TestThinTabView_WithLongNames ()
		{
			var tv = GetTabView (out var tab1, out var tab2, false);
			tv.Width = 10;
			tv.Height = 5;
			tv.Style = new TabStyle { TabsOnBottom = true };
			tv.ApplyStyleChanges ();

			// Ensures that the tab bar subview gets the bounds of the parent TabView
			tv.LayoutSubviews ();

			// Test two tab names that fit 
			tab1.Text = "12";
			tab2.Text = "13";

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌────────┐
│hi      │
│  ┌─────┘
│12│13    
└──┘      ", output);

			// Test first tab name too long
			tab1.Text = "12345678910";
			tab2.Text = "13";

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌────────┐
│hi      │
│       ┌►
│1234567│ 
└───────┘ ", output);

			//switch to tab2
			tv.SelectedTab = tab2;
			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌────────┐
│hi2     │
◄  ┌─────┘
│13│      
└──┘      ", output);

			// now make both tabs too long
			tab1.Text = "12345678910";
			tab2.Text = "abcdefghijklmnopq";

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌────────┐
│hi2     │
◄       ┌┘
│abcdefg│ 
└───────┘ ", output);
		}

		[Fact, AutoInitShutdown]
		public void ShowTopLine_False_TabsOnBottom_True_TestThinTabView_WithLongNames ()
		{
			var tv = GetTabView (out var tab1, out var tab2, false);
			tv.Width = 10;
			tv.Height = 5;
			tv.Style = new TabStyle { ShowTopLine = false, TabsOnBottom = true };
			tv.ApplyStyleChanges ();

			// Ensures that the tab bar subview gets the bounds of the parent TabView
			tv.LayoutSubviews ();

			// Test two tab names that fit 
			tab1.Text = "12";
			tab2.Text = "13";

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌────────┐
│hi      │
│        │
│  ┌─────┘
│12│13    ", output);

			tv.SelectedTab = tab2;

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌────────┐
│hi2     │
│        │
└──┐  ┌──┘
 12│13│   ", output);

			tv.SelectedTab = tab1;

			// Test first tab name too long
			tab1.Text = "12345678910";
			tab2.Text = "13";

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌────────┐
│hi      │
│        │
│       ┌►
│1234567│ ", output);

			//switch to tab2
			tv.SelectedTab = tab2;
			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌────────┐
│hi2     │
│        │
◄  ┌─────┘
│13│      ", output);

			// now make both tabs too long
			tab1.Text = "12345678910";
			tab2.Text = "abcdefghijklmnopq";

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌────────┐
│hi2     │
│        │
◄       ┌┘
│abcdefg│ ", output);
		}

		[Fact, AutoInitShutdown]
		public void ShowTopLine_True_TabsOnBottom_True_TestTabView_Width4 ()
		{
			var tv = GetTabView (out _, out _, false);
			tv.Width = 4;
			tv.Height = 5;
			tv.Style = new TabStyle { TabsOnBottom = true };
			tv.ApplyStyleChanges ();
			tv.LayoutSubviews ();

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──┐
│hi│
│ ┌►
│T│ 
└─┘ ", output);
		}

		[Fact, AutoInitShutdown]
		public void ShowTopLine_False_TabsOnBottom_True_TestTabView_Width4 ()
		{
			var tv = GetTabView (out _, out _, false);
			tv.Width = 4;
			tv.Height = 5;
			tv.Style = new TabStyle { ShowTopLine = false, TabsOnBottom = true };
			tv.ApplyStyleChanges ();
			tv.LayoutSubviews ();

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──┐
│hi│
│  │
│ ┌►
│T│ ", output);
		}

		[Fact, AutoInitShutdown]
		public void ShowTopLine_True_TabsOnBottom_True_TestTabView_Width3 ()
		{
			var tv = GetTabView (out _, out _, false);
			tv.Width = 3;
			tv.Height = 5;
			tv.Style = new TabStyle { TabsOnBottom = true };
			tv.ApplyStyleChanges ();
			tv.LayoutSubviews ();

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌─┐
│h│
│┌►
││ 
└┘ ", output);
		}

		[Fact, AutoInitShutdown]
		public void ShowTopLine_False_TabsOnBottom_True_TestTabView_Width3 ()
		{
			var tv = GetTabView (out _, out _, false);
			tv.Width = 3;
			tv.Height = 5;
			tv.Style = new TabStyle { ShowTopLine = false, TabsOnBottom = true };
			tv.ApplyStyleChanges ();
			tv.LayoutSubviews ();

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌─┐
│h│
│ │
│┌►
││ ", output);
		}

		[Fact, AutoInitShutdown]
		public void ShowTopLine_True_TabsOnBottom_False_With_Unicode ()
		{
			var tv = GetTabView (out var tab1, out var tab2, false);
			tv.Width = 20;
			tv.Height = 5;

			tv.LayoutSubviews ();

			tab1.Text = "Tab0";
			tab2.Text = "Les Mise" + Char.ConvertFromUtf32 (Int32.Parse ("0301", NumberStyles.HexNumber)) + "rables";

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌────┐              
│Tab0│              
│    └─────────────►
│hi                │
└──────────────────┘", output);

			tv.SelectedTab = tab2;

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────┐    
│Les Misérables│    
◄              └───┐
│hi2               │
└──────────────────┘", output);
		}

		[Fact, AutoInitShutdown]
		public void ShowTopLine_True_TabsOnBottom_True_With_Unicode ()
		{
			var tv = GetTabView (out var tab1, out var tab2, false);
			tv.Width = 20;
			tv.Height = 5;
			tv.Style = new TabStyle { TabsOnBottom = true };
			tv.ApplyStyleChanges ();

			tv.LayoutSubviews ();

			tab1.Text = "Tab0";
			tab2.Text = "Les Mise" + Char.ConvertFromUtf32 (Int32.Parse ("0301", NumberStyles.HexNumber)) + "rables";

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────┐
│hi                │
│    ┌─────────────►
│Tab0│              
└────┘              ", output);

			tv.SelectedTab = tab2;

			tv.Draw ();

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────┐
│hi2               │
◄              ┌───┘
│Les Misérables│    
└──────────────┘    ", output);
		}

		[Fact, AutoInitShutdown]
		public void MouseClick_ChangesTab ()
		{
			var tv = GetTabView (out var tab1, out var tab2, false);

			tv.Width = 20;
			tv.Height = 5;

			tv.LayoutSubviews ();

			tv.Draw ();

			var tabRow = tv.Subviews[0];
			Assert.Equal("TabRowView",tabRow.GetType().Name);

			TestHelpers.AssertDriverContentsAre (@"
┌────┐              
│Tab1│Tab2          
│    └─────────────┐
│hi                │
└──────────────────┘
", output);

			Tab clicked = null;
			

			tv.TabClicked += (s,e)=>{
				clicked = e.Tab;
			};

			// Waving mouse around does not trigger click
			for(int i=0;i<100;i++)
			{
				tabRow.MouseEvent(new MouseEvent{
						X = i,
						Y = 1,
						Flags = MouseFlags.ReportMousePosition
				});

				Assert.Null(clicked);
				Assert.Equal(tab1, tv.SelectedTab);
			}

			tabRow.MouseEvent(new MouseEvent{
					X = 3,
					Y = 1,
					Flags = MouseFlags.Button1Clicked
			});

			Assert.Equal(tab1, clicked);
			Assert.Equal(tab1, tv.SelectedTab);

			// Click to tab2
			tabRow.MouseEvent(new MouseEvent{
					X = 7,
					Y = 1,
					Flags = MouseFlags.Button1Clicked
			});

			Assert.Equal(tab2, clicked);
			Assert.Equal(tab2, tv.SelectedTab);

			// cancel navigation
			tv.TabClicked += (s,e)=>{
				clicked = e.Tab;
				e.MouseEvent.Handled = true;
			};

			tabRow.MouseEvent(new MouseEvent{
					X = 3,
					Y = 1,
					Flags = MouseFlags.Button1Clicked
			});

			// Tab 1 was clicked but event handler blocked navigation
			Assert.Equal(tab1, clicked);
			Assert.Equal(tab2, tv.SelectedTab);

			tabRow.MouseEvent (new MouseEvent {
				X = 10,
				Y = 1,
				Flags = MouseFlags.Button1Clicked
			});

			// Clicking beyond last tab should raise event with null Tab
			Assert.Null (clicked);
			Assert.Equal (tab2, tv.SelectedTab);

		}

		private void InitFakeDriver ()
		{
			var driver = new FakeDriver ();
			Application.Init (driver);
			driver.Init (() => { });
		}
	}
}