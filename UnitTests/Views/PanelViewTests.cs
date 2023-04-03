using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests {
	public class PanelViewTests {
		readonly ITestOutputHelper output;

		public PanelViewTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void Constructor_Defaults ()
		{
			var pv = new PanelView ();

			Assert.False (pv.CanFocus);
			Assert.False (pv.Visible);
			Assert.False (pv.UsePanelFrame);
			Assert.Null (pv.Child);

			pv = new PanelView (new Label ("This is a test."));

			Assert.False (pv.CanFocus);
			Assert.True (pv.Visible);
			Assert.False (pv.UsePanelFrame);
			Assert.NotNull (pv.Child);
			Assert.NotNull (pv.Border);
			Assert.NotNull (pv.Child.Border);
		}

		[Fact]
		public void Child_Sets_To_Null_Remove_From_Subviews_PanelView ()
		{
			var pv = new PanelView (new Label ("This is a test."));
			Assert.NotNull (pv.Child);
			Assert.Equal (1, pv.Subviews [0].Subviews.Count);

			pv.Child = null;
			Assert.Null (pv.Child);
			Assert.Equal (0, pv.Subviews [0].Subviews.Count);
		}

		[Fact]
		public void Add_View_Also_Sets_Child ()
		{
			var pv = new PanelView ();
			Assert.Null (pv.Child);
			Assert.Equal (0, pv.Subviews [0].Subviews.Count);

			pv.Add (new Label ("This is a test."));
			Assert.NotNull (pv.Child);
			Assert.Equal (1, pv.Subviews [0].Subviews.Count);
		}

		[Fact]
		public void Add_More_Views_Remove_Last_Child_Before__Only_One_Is_Allowed ()
		{
			var pv = new PanelView (new Label ("This is a test."));
			Assert.NotNull (pv.Child);
			Assert.Equal (1, pv.Subviews [0].Subviews.Count);
			Assert.IsType<Label> (pv.Child);

			pv.Add (new TextField ("This is a test."));
			Assert.NotNull (pv.Child);
			Assert.Equal (1, pv.Subviews [0].Subviews.Count);
			Assert.IsNotType<Label> (pv.Child);
			Assert.IsType<TextField> (pv.Child);
		}

		[Fact]
		public void Remove_RemoveAll_View_Also_Sets_Child_To_Null ()
		{
			var pv = new PanelView (new Label ("This is a test."));
			Assert.NotNull (pv.Child);
			Assert.Equal (1, pv.Subviews [0].Subviews.Count);

			pv.Remove (pv.Child);
			Assert.Null (pv.Child);
			Assert.Equal (0, pv.Subviews [0].Subviews.Count);

			pv = new PanelView (new Label ("This is a test."));
			Assert.NotNull (pv.Child);
			Assert.Equal (1, pv.Subviews [0].Subviews.Count);

			pv.RemoveAll ();
			Assert.Null (pv.Child);
			Assert.Equal (0, pv.Subviews [0].Subviews.Count);
		}

		[Fact]
		[AutoInitShutdown]
		public void AdjustContainer_Without_Border ()
		{
			var top = Application.Top;
			var win = new Window ();
			var pv = new PanelView (new Label ("This is a test."));
			win.Add (pv);
			top.Add (win);

			Application.Begin (top);

			Assert.Equal (new Rect (0, 0, 15, 1), pv.Frame);
			Assert.Equal (new Rect (0, 0, 15, 1), pv.Child.Frame);
		}

		[Fact]
		[AutoInitShutdown]
		public void AdjustContainer_With_Border_Absolute_Values ()
		{
			var top = Application.Top;
			var win = new Window ();
			var pv = new PanelView (new Label ("This is a test.") {
				Border = new Border () {
					BorderStyle = BorderStyle.Double,
					BorderThickness = new Thickness (1, 2, 3, 4),
					Padding = new Thickness (1, 2, 3, 4)
				}
			});
			win.Add (pv);
			top.Add (win);

			Application.Begin (top);

			Assert.False (pv.Child.Border.Effect3D);
			Assert.Equal (new Rect (0, 0, 25, 15), pv.Frame);
			Assert.Equal (new Rect (0, 0, 15, 1), pv.Child.Frame);

			pv.Child.Border.Effect3D = true;

			Assert.True (pv.Child.Border.Effect3D);
			Assert.Equal (new Rect (0, 0, 25, 15), pv.Frame);
			Assert.Equal (new Rect (0, 0, 15, 1), pv.Child.Frame);

			pv.Child.Border.Effect3DOffset = new Point (-1, -1);

			Assert.Equal (new Point (-1, -1), pv.Child.Border.Effect3DOffset);
			Assert.Equal (new Rect (0, 0, 25, 15), pv.Frame);
			Assert.Equal (new Rect (0, 0, 15, 1), pv.Child.Frame);
		}

		[Fact]
		[AutoInitShutdown]
		public void AdjustContainer_With_Border_Computed_Values ()
		{
			var top = Application.Top;
			var win = new Window ();
			var pv = new PanelView (new TextView () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Border = new Border () {
					BorderStyle = BorderStyle.Double,
					BorderThickness = new Thickness (1, 2, 3, 4),
					Padding = new Thickness (1, 2, 3, 4)
				}
			});

			var pv1 = new PanelView (new TextView () {
				Width = Dim.Fill (1),
				Height = Dim.Fill (1),
				Border = new Border () {
					BorderStyle = BorderStyle.Double,
					BorderThickness = new Thickness (1, 2, 3, 4),
					Padding = new Thickness (1, 2, 3, 4)
				}
			});

			var pv2 = new PanelView (new TextView () {
				Width = Dim.Fill (2),
				Height = Dim.Fill (2),
				Border = new Border () {
					BorderStyle = BorderStyle.Double,
					BorderThickness = new Thickness (1, 2, 3, 4),
					Padding = new Thickness (1, 2, 3, 4)
				}
			});

			win.Add (pv, pv1, pv2);
			top.Add (win);

			Application.Begin (top);

			Assert.Equal (new Rect (0, 0, 78, 23), pv.Frame);
			Assert.Equal (new Rect (0, 0, 68, 9), pv.Child.Frame);
			Assert.Equal (new Rect (0, 0, 77, 22), pv1.Frame);
			Assert.Equal (new Rect (0, 0, 65, 6), pv1.Child.Frame);
			Assert.Equal (new Rect (0, 0, 76, 21), pv2.Frame);
			Assert.Equal (new Rect (0, 0, 62, 3), pv2.Child.Frame);

			pv.Child.Border.Effect3D = pv1.Child.Border.Effect3D = pv2.Child.Border.Effect3D = true;

			Assert.True (pv.Child.Border.Effect3D);
			Assert.Equal (new Rect (0, 0, 78, 23), pv.Frame);
			Assert.Equal (new Rect (0, 0, 68, 9), pv.Child.Frame);
			Assert.Equal (new Rect (0, 0, 77, 22), pv1.Frame);
			Assert.Equal (new Rect (0, 0, 65, 6), pv1.Child.Frame);
			Assert.Equal (new Rect (0, 0, 76, 21), pv2.Frame);
			Assert.Equal (new Rect (0, 0, 62, 3), pv2.Child.Frame);

			pv.Child.Border.Effect3DOffset = pv1.Child.Border.Effect3DOffset = pv2.Child.Border.Effect3DOffset = new Point (-1, -1);

			Assert.Equal (new Point (-1, -1), pv.Child.Border.Effect3DOffset);
			Assert.Equal (new Rect (0, 0, 78, 23), pv.Frame);
			Assert.Equal (new Rect (0, 0, 68, 9), pv.Child.Frame);
			Assert.Equal (new Rect (0, 0, 77, 22), pv1.Frame);
			Assert.Equal (new Rect (0, 0, 65, 6), pv1.Child.Frame);
			Assert.Equal (new Rect (0, 0, 76, 21), pv2.Frame);
			Assert.Equal (new Rect (0, 0, 62, 3), pv2.Child.Frame);
		}

		[Fact]
		[AutoInitShutdown]
		public void UsePanelFrame_False_PanelView_Always_Respect_The_PanelView_Upper_Left_Corner_Position_And_The_Child_Size ()
		{
			var top = Application.Top;
			var win = new Window ();
			var pv = new PanelView (new Label ("This is a test.")) {
				X = 2,
				Y = 4,
				Width = 20,
				Height = 10
			};
			var pv1 = new PanelView (new TextField (3, 4, 15, "This is a test.")) {
				X = 2,
				Y = 4,
				Width = 20,
				Height = 10
			};
			var pv2 = new PanelView (new TextView () {
				X = 5,
				Y = 6,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			}) {
				X = 2,
				Y = 4,
				Width = 20,
				Height = 10
			};

			win.Add (pv, pv1, pv2);
			top.Add (win);

			Application.Begin (top);

			Assert.False (pv.UsePanelFrame);
			Assert.False (pv.Border.Effect3D);
			Assert.Equal (pv.Child.Border, pv.Border);
			Assert.False (pv1.UsePanelFrame);
			Assert.False (pv1.Border.Effect3D);
			Assert.Equal (pv1.Child.Border, pv1.Border);
			Assert.False (pv2.UsePanelFrame);
			Assert.False (pv2.Border.Effect3D);
			Assert.Equal (pv2.Child.Border, pv2.Border);
			Assert.Equal (new Rect (2, 4, 15, 1), pv.Frame);
			Assert.Equal (new Rect (0, 0, 15, 1), pv.Child.Frame);
			Assert.Equal (new Rect (2, 4, 18, 5), pv1.Frame);
			Assert.Equal (new Rect (3, 4, 15, 1), pv1.Child.Frame);
			Assert.Equal (new Rect (2, 4, 76, 19), pv2.Frame);
			Assert.Equal (new Rect (5, 6, 71, 13), pv2.Child.Frame);

			pv.Border.Effect3D = pv1.Border.Effect3D = pv2.Border.Effect3D = true;

			Assert.Equal (new Rect (2, 4, 15, 1), pv.Frame);
			Assert.Equal (new Rect (0, 0, 15, 1), pv.Child.Frame);
			Assert.Equal (new Rect (2, 4, 18, 5), pv1.Frame);
			Assert.Equal (new Rect (3, 4, 15, 1), pv1.Child.Frame);
			Assert.Equal (new Rect (2, 4, 76, 19), pv2.Frame);
			Assert.Equal (new Rect (5, 6, 71, 13), pv2.Child.Frame);

			pv.Border.Effect3DOffset = pv1.Border.Effect3DOffset = pv2.Border.Effect3DOffset = new Point (-1, -1);

			Assert.Equal (new Rect (2, 4, 15, 1), pv.Frame);
			Assert.Equal (new Rect (0, 0, 15, 1), pv.Child.Frame);
			Assert.Equal (new Rect (2, 4, 18, 5), pv1.Frame);
			Assert.Equal (new Rect (3, 4, 15, 1), pv1.Child.Frame);
			Assert.Equal (new Rect (2, 4, 76, 19), pv2.Frame);
			Assert.Equal (new Rect (5, 6, 71, 13), pv2.Child.Frame);
		}

		[Fact]
		[AutoInitShutdown]
		public void UsePanelFrame_True_PanelView_Position_And_Size_Are_Used_Depending_On_Effect3DOffset ()
		{
			var top = Application.Top;
			var win = new Window ();
			var pv = new PanelView (new TextView () {
				X = 2,
				Y = 4,
				Width = 20,
				Height = 10
			}) {
				X = 5,
				Y = 6,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				UsePanelFrame = true
			};
			var pv1 = new PanelView (new TextView () {
				X = 5,
				Y = 6,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			}) {
				X = 2,
				Y = 4,
				Width = 20,
				Height = 10,
				UsePanelFrame = true
			};

			win.Add (pv, pv1);
			top.Add (win);

			Application.Begin (top);

			Assert.Equal (new Rect (5, 6, 73, 17), pv.Frame);
			Assert.Equal (new Rect (2, 4, 20, 10), pv.Child.Frame);
			Assert.Equal (new Rect (2, 4, 20, 10), pv1.Frame);
			Assert.Equal (new Rect (5, 6, 15, 4), pv1.Child.Frame);

			pv.Border.Effect3D = pv1.Border.Effect3D = true;

			Assert.Equal (new Rect (5, 6, 73, 17), pv.Frame);
			Assert.Equal (new Rect (2, 4, 20, 10), pv.Child.Frame);
			Assert.Equal (new Rect (2, 4, 20, 10), pv1.Frame);
			Assert.Equal (new Rect (5, 6, 15, 4), pv1.Child.Frame);

			pv.Border.Effect3DOffset = pv1.Border.Effect3DOffset = new Point (-1, -1);

			Assert.Equal (new Rect (6, 7, 73, 17), pv.Frame);
			Assert.Equal (new Rect (2, 4, 20, 10), pv.Child.Frame);
			Assert.Equal (new Rect (3, 5, 20, 10), pv1.Frame);
			Assert.Equal (new Rect (5, 6, 15, 4), pv1.Child.Frame);
		}

//		[Fact, AutoInitShutdown]
//		public void Setting_Child_Size_Disable_AutoSize ()
//		{
//			var top = Application.Top;
//			var win = new Window ();
//			var label = new Label () {
//				ColorScheme = Colors.TopLevel,
//				Text = "This is a test\nwith a \nPanelView",
//				TextAlignment = TextAlignment.Centered,
//				Width = 24,
//				Height = 13,
//				AutoSize = false
//			};
//			var pv = new PanelView (label) {
//				Width = 24,
//				Height = 13,
//				Border = new Border () {
//					BorderStyle = BorderStyle.Single,
//					DrawMarginFrame = true,
//					BorderThickness = new Thickness (2),
//					BorderBrush = Color.Red,
//					Padding = new Thickness (2),
//					Background = Color.BrightGreen,
//					Effect3D = true
//				},
//			};
//			win.Add (pv);
//			top.Add (win);

//			Application.Begin (top);

//			Assert.False (label.AutoSize);
//			Assert.Equal (new Rect (0, 0, 24, 13), label.Frame);
//			Assert.Equal (new Rect (0, 0, 34, 23), pv.Frame);
//			Assert.Equal (new Rect (0, 0, 80, 25), win.Frame);
//			Assert.Equal (new Rect (0, 0, 80, 25), Application.Top.Frame);

//			var expected = @"
//┌──────────────────────────────────────────────────────────────────────────────┐
//│                                                                              │
//│                                                                              │
//│                                                                              │
//│                                                                              │
//│    ┌────────────────────────┐                                                │
//│    │     This is a test     │                                                │
//│    │        with a          │                                                │
//│    │       PanelView        │                                                │
//│    │                        │                                                │
//│    │                        │                                                │
//│    │                        │                                                │
//│    │                        │                                                │
//│    │                        │                                                │
//│    │                        │                                                │
//│    │                        │                                                │
//│    │                        │                                                │
//│    │                        │                                                │
//│    │                        │                                                │
//│    └────────────────────────┘                                                │
//│                                                                              │
//│                                                                              │
//│                                                                              │
//│                                                                              │
//└──────────────────────────────────────────────────────────────────────────────┘
//";

//			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
//			Assert.Equal (new Rect (0, 0, 80, 25), pos);
//		}

//		[Fact, AutoInitShutdown]
//		public void Not_Setting_Child_Size_Default_AutoSize_True ()
//		{
//			var top = Application.Top;
//			var win = new Window ();
//			var label = new Label () {
//				ColorScheme = Colors.TopLevel,
//				Text = "This is a test\nwith a \nPanelView",
//				TextAlignment = TextAlignment.Centered
//			};
//			var pv = new PanelView (label) {
//				Width = 24,
//				Height = 13,
//				Border = new Border () {
//					BorderStyle = BorderStyle.Single,
//					DrawMarginFrame = true,
//					BorderThickness = new Thickness (2),
//					BorderBrush = Color.Red,
//					Padding = new Thickness (2),
//					Background = Color.BrightGreen,
//					Effect3D = true
//				},
//			};
//			win.Add (pv);
//			top.Add (win);

//			Application.Begin (top);

//			Assert.True (label.AutoSize);
//			Assert.False (pv.UsePanelFrame);
//			Assert.Equal (new Rect (0, 0, 14, 3), label.Frame);
//			Assert.Equal (new Rect (0, 0, 24, 13), pv.Frame);
//			Assert.Equal (new Rect (0, 0, 80, 25), win.Frame);
//			Assert.Equal (new Rect (0, 0, 80, 25), Application.Top.Frame);

//			var expected = @"
//┌──────────────────────────────────────────────────────────────────────────────┐
//│                                                                              │
//│                                                                              │
//│                                                                              │
//│                                                                              │
//│    ┌──────────────┐                                                          │
//│    │This is a test│                                                          │
//│    │   with a     │                                                          │
//│    │  PanelView   │                                                          │
//│    └──────────────┘                                                          │
//│                                                                              │
//│                                                                              │
//│                                                                              │
//│                                                                              │
//│                                                                              │
//│                                                                              │
//│                                                                              │
//│                                                                              │
//│                                                                              │
//│                                                                              │
//│                                                                              │
//│                                                                              │
//│                                                                              │
//│                                                                              │
//└──────────────────────────────────────────────────────────────────────────────┘
//";

//			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
//			Assert.Equal (new Rect (0, 0, 80, 25), pos);
//		}
	}
}
