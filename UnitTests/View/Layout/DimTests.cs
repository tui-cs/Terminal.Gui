using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Terminal.Gui;
using Xunit;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.ViewTests {
	public class LayoutTests_DimTests {
		readonly ITestOutputHelper output;

		public LayoutTests_DimTests (ITestOutputHelper output)
		{
			this.output = output;
			Console.OutputEncoding = System.Text.Encoding.Default;
			// Change current culture
			var culture = CultureInfo.CreateSpecificCulture ("en-US");
			Thread.CurrentThread.CurrentCulture = culture;
			Thread.CurrentThread.CurrentUICulture = culture;
		}

		[Fact]
		public void New_Works ()
		{
			var dim = new Dim ();
			Assert.Equal ("Terminal.Gui.Dim", dim.ToString ());
		}

		[Fact]
		public void Sized_SetsValue ()
		{
			var dim = Dim.Sized (0);
			Assert.Equal ("Absolute(0)", dim.ToString ());

			int testVal = 5;
			dim = Dim.Sized (testVal);
			Assert.Equal ($"Absolute({testVal})", dim.ToString ());

			testVal = -1;
			dim = Dim.Sized (testVal);
			Assert.Equal ($"Absolute({testVal})", dim.ToString ());
		}

		[Fact]
		public void Sized_Equals ()
		{
			int n1 = 0;
			int n2 = 0;
			var dim1 = Dim.Sized (n1);
			var dim2 = Dim.Sized (n2);
			Assert.Equal (dim1, dim2);

			n1 = n2 = 1;
			dim1 = Dim.Sized (n1);
			dim2 = Dim.Sized (n2);
			Assert.Equal (dim1, dim2);

			n1 = n2 = -1;
			dim1 = Dim.Sized (n1);
			dim2 = Dim.Sized (n2);
			Assert.Equal (dim1, dim2);

			n1 = 0;
			n2 = 1;
			dim1 = Dim.Sized (n1);
			dim2 = Dim.Sized (n2);
			Assert.NotEqual (dim1, dim2);
		}

		[Fact]
		public void Width_Set_To_Null_Throws ()
		{
			var dim = Dim.Width (null);
			Assert.Throws<NullReferenceException> (() => dim.ToString ());
		}

		[Fact]
		public void SetsValue ()
		{
			var testVal = Rect.Empty;
			var dim = Dim.Width (new View (testVal));
			Assert.Equal ($"View(Width,View()({testVal}))", dim.ToString ());

			testVal = new Rect (1, 2, 3, 4);
			dim = Dim.Width (new View (testVal));
			Assert.Equal ($"View(Width,View()({testVal}))", dim.ToString ());
		}

		[Fact]
		public void Width_Equals ()
		{
			var testRect1 = Rect.Empty;
			var view1 = new View (testRect1);
			var testRect2 = Rect.Empty;
			var view2 = new View (testRect2);

			var dim1 = Dim.Width (view1);
			var dim2 = Dim.Width (view1);
			// FIXED: Dim.Width should support Equals() and this should change to Equal.
			Assert.Equal (dim1, dim2);

			dim2 = Dim.Width (view2);
			Assert.NotEqual (dim1, dim2);

			testRect1 = new Rect (0, 1, 2, 3);
			view1 = new View (testRect1);
			testRect2 = new Rect (0, 1, 2, 3);
			dim1 = Dim.Width (view1);
			dim2 = Dim.Width (view1);
			// FIXED: Dim.Width should support Equals() and this should change to Equal.
			Assert.Equal (dim1, dim2);

			testRect1 = new Rect (0, -1, 2, 3);
			view1 = new View (testRect1);
			testRect2 = new Rect (0, -1, 2, 3);
			dim1 = Dim.Width (view1);
			dim2 = Dim.Width (view1);
			// FIXED: Dim.Width should support Equals() and this should change to Equal.
			Assert.Equal (dim1, dim2);

			testRect1 = new Rect (0, -1, 2, 3);
			view1 = new View (testRect1);
			testRect2 = Rect.Empty;
			view2 = new View (testRect2);
			dim1 = Dim.Width (view1);
			dim2 = Dim.Width (view2);
			Assert.NotEqual (dim1, dim2);
		}

		[Fact]
		public void Height_Set_To_Null_Throws ()
		{
			var dim = Dim.Height (null);
			Assert.Throws<NullReferenceException> (() => dim.ToString ());
		}

		[Fact]
		public void Height_SetsValue ()
		{
			var testVal = Rect.Empty;
			var dim = Dim.Height (new View (testVal));
			Assert.Equal ($"View(Height,View()({testVal}))", dim.ToString ());

			testVal = new Rect (1, 2, 3, 4);
			dim = Dim.Height (new View (testVal));
			Assert.Equal ($"View(Height,View()({testVal}))", dim.ToString ());
		}

		// TODO: Other Dim.Height tests (e.g. Equal?)

		[Fact]
		public void Fill_SetsValue ()
		{
			var testMargin = 0;
			var dim = Dim.Fill ();
			Assert.Equal ($"Fill({testMargin})", dim.ToString ());

			testMargin = 0;
			dim = Dim.Fill (testMargin);
			Assert.Equal ($"Fill({testMargin})", dim.ToString ());

			testMargin = 5;
			dim = Dim.Fill (testMargin);
			Assert.Equal ($"Fill({testMargin})", dim.ToString ());
		}

		[Fact]
		public void Fill_Equal ()
		{
			var margin1 = 0;
			var margin2 = 0;
			var dim1 = Dim.Fill (margin1);
			var dim2 = Dim.Fill (margin2);
			Assert.Equal (dim1, dim2);
		}

		[Fact]
		public void Percent_SetsValue ()
		{
			float f = 0;
			var dim = Dim.Percent (f);
			Assert.Equal ($"Factor({f / 100:0.###},{false})", dim.ToString ());
			f = 0.5F;
			dim = Dim.Percent (f);
			Assert.Equal ($"Factor({f / 100:0.###},{false})", dim.ToString ());
			f = 100;
			dim = Dim.Percent (f);
			Assert.Equal ($"Factor({f / 100:0.###},{false})", dim.ToString ());
		}

		[Fact]
		public void Percent_Equals ()
		{
			float n1 = 0;
			float n2 = 0;
			var dim1 = Dim.Percent (n1);
			var dim2 = Dim.Percent (n2);
			Assert.Equal (dim1, dim2);

			n1 = n2 = 1;
			dim1 = Dim.Percent (n1);
			dim2 = Dim.Percent (n2);
			Assert.Equal (dim1, dim2);

			n1 = n2 = 0.5f;
			dim1 = Dim.Percent (n1);
			dim2 = Dim.Percent (n2);
			Assert.Equal (dim1, dim2);

			n1 = n2 = 100f;
			dim1 = Dim.Percent (n1);
			dim2 = Dim.Percent (n2);
			Assert.Equal (dim1, dim2);

			n1 = n2 = 0.3f;
			dim1 = Dim.Percent (n1, true);
			dim2 = Dim.Percent (n2, true);
			Assert.Equal (dim1, dim2);

			n1 = n2 = 0.3f;
			dim1 = Dim.Percent (n1);
			dim2 = Dim.Percent (n2, true);
			Assert.NotEqual (dim1, dim2);

			n1 = 0;
			n2 = 1;
			dim1 = Dim.Percent (n1);
			dim2 = Dim.Percent (n2);
			Assert.NotEqual (dim1, dim2);

			n1 = 0.5f;
			n2 = 1.5f;
			dim1 = Dim.Percent (n1);
			dim2 = Dim.Percent (n2);
			Assert.NotEqual (dim1, dim2);
		}

		[Fact]
		public void Percent_Invalid_Throws ()
		{
			var dim = Dim.Percent (0);
			Assert.Throws<ArgumentException> (() => dim = Dim.Percent (-1));
			Assert.Throws<ArgumentException> (() => dim = Dim.Percent (101));
			Assert.Throws<ArgumentException> (() => dim = Dim.Percent (100.0001F));
			Assert.Throws<ArgumentException> (() => dim = Dim.Percent (1000001));
		}

		[Fact, AutoInitShutdown]
		public void ForceValidatePosDim_True_Dim_Validation_If_NewValue_Is_DimAbsolute_And_OldValue_Is_Another_Type_Throws ()
		{
			var t = Application.Top;

			var w = new Window () {
				Width = Dim.Fill (0),
				Height = Dim.Sized (10)
			};
			var v = new View ("v") {
				Width = Dim.Width (w) - 2,
				Height = Dim.Percent (10),
				ForceValidatePosDim = true
			};

			w.Add (v);
			t.Add (w);

			t.Ready += (s, e) => {
				Assert.Equal (2, w.Width = 2);
				Assert.Equal (2, w.Height = 2);
				Assert.Throws<ArgumentException> (() => v.Width = 2);
				Assert.Throws<ArgumentException> (() => v.Height = 2);
				v.ForceValidatePosDim = false;
				var exception = Record.Exception (() => v.Width = 2);
				Assert.Null (exception);
				Assert.Equal (2, v.Width);
				exception = Record.Exception (() => v.Height = 2);
				Assert.Null (exception);
				Assert.Equal (2, v.Height);
			};

			Application.Iteration += () => Application.RequestStop ();

			Application.Run ();
		}

		[Fact]
		public void Dim_Validation_Do_Not_Throws_If_NewValue_Is_DimAbsolute_And_OldValue_Is_Null ()
		{
			var t = new View ("top") { Width = 80, Height = 25 };

			var w = new Window (new Rect (1, 2, 4, 5)) { Title = "w" };
			t.Add (w);
			t.LayoutSubviews ();

			Assert.Equal (3, w.Width = 3);
			Assert.Equal (4, w.Height = 4);

		}

		[Fact]
		public void Dim_Validation_Do_Not_Throws_If_NewValue_Is_DimAbsolute_And_OldValue_Is_Another_Type_After_Sets_To_LayoutStyle_Absolute ()
		{
			var t = new View ("top") { Width = 80, Height = 25 };

			var w = new Window () {
				Width = Dim.Fill (0),
				Height = Dim.Sized (10)
			};
			var v = new View ("v") {
				Width = Dim.Width (w) - 2,
				Height = Dim.Percent (10)
			};

			w.Add (v);
			t.Add (w);

			t.LayoutSubviews ();
			Assert.Equal (2, v.Width = 2);
			Assert.Equal (2, v.Height = 2);

			v.LayoutStyle = LayoutStyle.Absolute;
			t.LayoutSubviews ();

			Assert.Equal (2, v.Width = 2);
			Assert.Equal (2, v.Height = 2);
		}

		[Fact, AutoInitShutdown]
		public void Only_DimAbsolute_And_DimFactor_As_A_Different_Procedure_For_Assigning_Value_To_Width_Or_Height ()
		{
			// Testing with the Button because it properly handles the Dim class.
			var t = Application.Top;

			var w = new Window () {
				Width = 100,
				Height = 100
			};

			var f1 = new FrameView ("f1") {
				X = 0,
				Y = 0,
				Width = Dim.Percent (50),
				Height = 5
			};

			var f2 = new FrameView ("f2") {
				X = Pos.Right (f1),
				Y = 0,
				Width = Dim.Fill (),
				Height = 5
			};

			var v1 = new Button ("v1") {
				AutoSize = false,
				X = Pos.X (f1) + 2,
				Y = Pos.Bottom (f1) + 2,
				Width = Dim.Width (f1) - 2,
				Height = Dim.Fill () - 2,
				ForceValidatePosDim = true
			};

			var v2 = new Button ("v2") {
				AutoSize = false,
				X = Pos.X (f2) + 2,
				Y = Pos.Bottom (f2) + 2,
				Width = Dim.Width (f2) - 2,
				Height = Dim.Fill () - 2,
				ForceValidatePosDim = true
			};

			var v3 = new Button ("v3") {
				AutoSize = false,
				Width = Dim.Percent (10),
				Height = Dim.Percent (10),
				ForceValidatePosDim = true
			};

			var v4 = new Button ("v4") {
				AutoSize = false,
				Width = Dim.Sized (50),
				Height = Dim.Sized (50),
				ForceValidatePosDim = true
			};

			var v5 = new Button ("v5") {
				AutoSize = false,
				Width = Dim.Width (v1) - Dim.Width (v3),
				Height = Dim.Height (v1) - Dim.Height (v3),
				ForceValidatePosDim = true
			};

			var v6 = new Button ("v6") {
				AutoSize = false,
				X = Pos.X (f2),
				Y = Pos.Bottom (f2) + 2,
				Width = Dim.Percent (20, true),
				Height = Dim.Percent (20, true),
				ForceValidatePosDim = true
			};

			w.Add (f1, f2, v1, v2, v3, v4, v5, v6);
			t.Add (w);

			t.Ready += (s, e) => {
				Assert.Equal ("Absolute(100)", w.Width.ToString ());
				Assert.Equal ("Absolute(100)", w.Height.ToString ());
				Assert.Equal (100, w.Frame.Width);
				Assert.Equal (100, w.Frame.Height);

				Assert.Equal ("Factor(0.5,False)", f1.Width.ToString ());
				Assert.Equal ("Absolute(5)", f1.Height.ToString ());
				Assert.Equal (49, f1.Frame.Width); // 50-1=49
				Assert.Equal (5, f1.Frame.Height);

				Assert.Equal ("Fill(0)", f2.Width.ToString ());
				Assert.Equal ("Absolute(5)", f2.Height.ToString ());
				Assert.Equal (49, f2.Frame.Width); // 50-1=49
				Assert.Equal (5, f2.Frame.Height);

				Assert.Equal ("Combine(View(Width,FrameView(f1)((0,0,49,5)))-Absolute(2))", v1.Width.ToString ());
				Assert.Equal ("Combine(Fill(0)-Absolute(2))", v1.Height.ToString ());
				Assert.Equal (47, v1.Frame.Width); // 49-2=47
				Assert.Equal (89, v1.Frame.Height); // 98-5-2-2=89

				Assert.Equal ("Combine(View(Width,FrameView(f2)((49,0,49,5)))-Absolute(2))", v2.Width.ToString ());
				Assert.Equal ("Combine(Fill(0)-Absolute(2))", v2.Height.ToString ());
				Assert.Equal (47, v2.Frame.Width); // 49-2=47
				Assert.Equal (89, v2.Frame.Height); // 98-5-2-2=89

				Assert.Equal ("Factor(0.1,False)", v3.Width.ToString ());
				Assert.Equal ("Factor(0.1,False)", v3.Height.ToString ());
				Assert.Equal (9, v3.Frame.Width); // 98*10%=9
				Assert.Equal (9, v3.Frame.Height); // 98*10%=9

				Assert.Equal ("Absolute(50)", v4.Width.ToString ());
				Assert.Equal ("Absolute(50)", v4.Height.ToString ());
				Assert.Equal (50, v4.Frame.Width);
				Assert.Equal (50, v4.Frame.Height);

				Assert.Equal ("Combine(View(Width,Button(v1)((2,7,47,89)))-View(Width,Button(v3)((0,0,9,9))))", v5.Width.ToString ());
				Assert.Equal ("Combine(View(Height,Button(v1)((2,7,47,89)))-View(Height,Button(v3)((0,0,9,9))))", v5.Height.ToString ());
				Assert.Equal (38, v5.Frame.Width); // 47-9=38
				Assert.Equal (80, v5.Frame.Height); // 89-9=80

				Assert.Equal ("Factor(0.2,True)", v6.Width.ToString ());
				Assert.Equal ("Factor(0.2,True)", v6.Height.ToString ());
				Assert.Equal (9, v6.Frame.Width); // 47*20%=9
				Assert.Equal (18, v6.Frame.Height); // 89*20%=18

				w.Width = 200;
				Assert.True (t.LayoutNeeded);
				w.Height = 200;
				t.LayoutSubviews ();

				Assert.Equal ("Absolute(200)", w.Width.ToString ());
				Assert.Equal ("Absolute(200)", w.Height.ToString ());
				Assert.Equal (200, w.Frame.Width);
				Assert.Equal (200, w.Frame.Height);

				f1.Text = "Frame1";
				Assert.Equal ("Factor(0.5,False)", f1.Width.ToString ());
				Assert.Equal ("Absolute(5)", f1.Height.ToString ());
				Assert.Equal (99, f1.Frame.Width); // 100-1=99
				Assert.Equal (5, f1.Frame.Height);

				f2.Text = "Frame2";
				Assert.Equal ("Fill(0)", f2.Width.ToString ());
				Assert.Equal ("Absolute(5)", f2.Height.ToString ());
				Assert.Equal (99, f2.Frame.Width); // 100-1=99
				Assert.Equal (5, f2.Frame.Height);

				v1.Text = "Button1";
				Assert.Equal ("Combine(View(Width,FrameView(f1)((0,0,99,5)))-Absolute(2))", v1.Width.ToString ());
				Assert.Equal ("Combine(Fill(0)-Absolute(2))", v1.Height.ToString ());
				Assert.Equal (97, v1.Frame.Width); // 99-2=97
				Assert.Equal (189, v1.Frame.Height); // 198-2-7=189

				v2.Text = "Button2";
				Assert.Equal ("Combine(View(Width,FrameView(f2)((99,0,99,5)))-Absolute(2))", v2.Width.ToString ());
				Assert.Equal ("Combine(Fill(0)-Absolute(2))", v2.Height.ToString ());
				Assert.Equal (97, v2.Frame.Width); // 99-2=97
				Assert.Equal (189, v2.Frame.Height); // 198-2-7=189

				v3.Text = "Button3";
				Assert.Equal ("Factor(0.1,False)", v3.Width.ToString ());
				Assert.Equal ("Factor(0.1,False)", v3.Height.ToString ());
				Assert.Equal (19, v3.Frame.Width); // 198*10%=19 * Percent is related to the super-view if it isn't null otherwise the view width
				Assert.Equal (19, v3.Frame.Height); // 199*10%=19

				v4.Text = "Button4";
				v4.AutoSize = false;
				Assert.Equal ("Absolute(50)", v4.Width.ToString ());
				Assert.Equal ("Absolute(50)", v4.Height.ToString ());
				Assert.Equal (50, v4.Frame.Width);
				Assert.Equal (50, v4.Frame.Height);
				v4.AutoSize = true;
				Assert.Equal ("Absolute(11)", v4.Width.ToString ());
				Assert.Equal ("Absolute(1)", v4.Height.ToString ());
				Assert.Equal (11, v4.Frame.Width); // 11 is the text length and because is Dim.DimAbsolute
				Assert.Equal (1, v4.Frame.Height); // 1 because is Dim.DimAbsolute

				v5.Text = "Button5";
				Assert.Equal ("Combine(View(Width,Button(v1)((2,7,97,189)))-View(Width,Button(v3)((0,0,19,19))))", v5.Width.ToString ());
				Assert.Equal ("Combine(View(Height,Button(v1)((2,7,97,189)))-View(Height,Button(v3)((0,0,19,19))))", v5.Height.ToString ());
				Assert.Equal (78, v5.Frame.Width); // 97-9=78
				Assert.Equal (170, v5.Frame.Height); // 189-19=170

				v6.Text = "Button6";
				Assert.Equal ("Factor(0.2,True)", v6.Width.ToString ());
				Assert.Equal ("Factor(0.2,True)", v6.Height.ToString ());
				Assert.Equal (19, v6.Frame.Width); // 99*20%=19
				Assert.Equal (38, v6.Frame.Height); // 198-7*20=18
			};

			Application.Iteration += () => Application.RequestStop ();

			Application.Run ();
		}

		// See #2461
		//[Fact]
		//public void Dim_Referencing_SuperView_Throws ()
		//{
		//	var super = new View ("super") {
		//		Width = 10,
		//		Height = 10
		//	};
		//	var view = new View ("view") {
		//		Width = Dim.Width (super),	// this is not allowed
		//		Height = Dim.Height (super),    // this is not allowed
		//	};

		//	super.Add (view);
		//	super.BeginInit ();
		//	super.EndInit ();
		//	Assert.Throws<InvalidOperationException> (() => super.LayoutSubviews ());
		//}

		/// <summary>
		/// This is an intentionally obtuse test. See https://github.com/gui-cs/Terminal.Gui/issues/2461
		/// </summary>
		[Fact]
		public void DimCombine_ObtuseScenario_Throw_If_SuperView_Refs_SubView ()
		{
			var t = new View () { Width = 80, Height = 25 };

			var w = new Window () {
				Width = Dim.Width (t) - 2,    // 78
				Height = Dim.Height (t) - 2   // 23
			};
			var f = new FrameView ();
			var v1 = new View () {
				Width = Dim.Width (w) - 2,    // 76
				Height = Dim.Height (w) - 2   // 21
			};
			var v2 = new View () {
				Width = Dim.Width (v1) - 2,   // 74
				Height = Dim.Height (v1) - 2  // 19
			};

			f.Add (v1, v2);
			w.Add (f);
			t.Add (w);

			// BUGBUG: v2 - f references t here; t is f's super-superview. This is supported!
			// BUGBUG: v2 - f references v2 here; v2 is f's subview. This is not supported!
			f.Width = Dim.Width (t) - Dim.Width (v2);      // 80 - 74 = 6
			f.Height = Dim.Height (t) - Dim.Height (v2);   // 25 - 19 = 6

			Assert.Throws<InvalidOperationException> (t.LayoutSubviews);
			Assert.Equal (80, t.Frame.Width);
			Assert.Equal (25, t.Frame.Height);
			Assert.Equal (78, w.Frame.Width);
			Assert.Equal (23, w.Frame.Height);
			// BUGBUG: v2 - this no longer works - see above
			//Assert.Equal (6, f.Frame.Width);
			//Assert.Equal (6, f.Frame.Height);
			//Assert.Equal (76, v1.Frame.Width);
			//Assert.Equal (21, v1.Frame.Height);
			//Assert.Equal (74, v2.Frame.Width);
			//Assert.Equal (19, v2.Frame.Height);
		}

		[Fact]
		public void DimCombine_ObtuseScenario_Does_Not_Throw_If_Two_SubViews_Refs_The_Same_SuperView ()
		{
			var t = new View ("top") { Width = 80, Height = 25 };

			var w = new Window () {
				Width = Dim.Width (t) - 2,    // 78
				Height = Dim.Height (t) - 2   // 23
			};
			var f = new FrameView ();
			var v1 = new View () {
				Width = Dim.Width (w) - 2,    // 76
				Height = Dim.Height (w) - 2   // 21
			};
			var v2 = new View () {
				Width = Dim.Width (v1) - 2,   // 74
				Height = Dim.Height (v1) - 2  // 19
			};

			f.Add (v1, v2);
			w.Add (f);
			t.Add (w);

			f.Width = Dim.Width (t) - Dim.Width (w) + 4;      // 80 - 74 = 6
			f.Height = Dim.Height (t) - Dim.Height (w) + 4;   // 25 - 19 = 6

			// BUGBUG: v2 - f references t and w here; t is f's super-superview and w is f's superview. This is supported!
			var exception = Record.Exception (t.LayoutSubviews);
			Assert.Null (exception);
			Assert.Equal (80, t.Frame.Width);
			Assert.Equal (25, t.Frame.Height);
			Assert.Equal (78, w.Frame.Width);
			Assert.Equal (23, w.Frame.Height);
			Assert.Equal (6, f.Frame.Width);
			Assert.Equal (6, f.Frame.Height);
			Assert.Equal (76, v1.Frame.Width);
			Assert.Equal (21, v1.Frame.Height);
			Assert.Equal (74, v2.Frame.Width);
			Assert.Equal (19, v2.Frame.Height);
		}

		[Fact]
		public void PosCombine_View_Not_Added_Throws ()
		{
			var t = new View () { Width = 80, Height = 50 };

			// BUGBUG: v2 - super should not reference it's superview (t)
			var super = new View () {
				Width = Dim.Width (t) - 2,
				Height = Dim.Height (t) - 2
			};
			t.Add (super);

			var sub = new View ();
			super.Add (sub);

			var v1 = new View () {
				Width = Dim.Width (super) - 2,
				Height = Dim.Height (super) - 2
			};
			var v2 = new View () {
				Width = Dim.Width (v1) - 2,
				Height = Dim.Height (v1) - 2
			};
			sub.Add (v1);
			// v2 not added to sub; should cause exception on Layout since it's referenced by sub.
			sub.Width = Dim.Fill () - Dim.Width (v2);
			sub.Height = Dim.Fill () - Dim.Height (v2);

			Assert.Throws<InvalidOperationException> (() => t.LayoutSubviews ());
		}

		[Fact, AutoInitShutdown]
		public void Dim_Add_Operator ()
		{
			var top = Application.Top;

			var view = new View () { X = 0, Y = 0, Width = 20, Height = 0 };
			var field = new TextField () { X = 0, Y = Pos.Bottom (view), Width = 20 };
			var count = 0;

			field.KeyDown += (s, k) => {
				if (k.KeyEvent.Key == Key.Enter) {
					field.Text = $"Label {count}";
					var label = new Label (field.Text) { X = 0, Y = view.Bounds.Height, Width = 20 };
					view.Add (label);
					Assert.Equal ($"Label {count}", label.Text);
					Assert.Equal ($"Absolute({count})", label.Y.ToString ());

					Assert.Equal ($"Absolute({count})", view.Height.ToString ());
					view.Height += 1;
					count++;
					Assert.Equal ($"Absolute({count})", view.Height.ToString ());
				}
			};

			Application.Iteration += () => {
				while (count < 20) field.OnKeyDown (new KeyEvent (Key.Enter, new KeyModifiers ()));

				Application.RequestStop ();
			};

			var win = new Window ();
			win.Add (view);
			win.Add (field);

			top.Add (win);

			Application.Run (top);

			Assert.Equal (20, count);
		}

		private string [] expecteds = new string [21] {
@"
┌────────────────────┐
│View with long text │
│                    │
└────────────────────┘",
@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 0             │
└────────────────────┘",
@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 1             │
└────────────────────┘",
@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 2             │
└────────────────────┘",
@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 3             │
└────────────────────┘",
@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 4             │
└────────────────────┘",
@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 5             │
└────────────────────┘",
@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 6             │
└────────────────────┘",
@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 7             │
└────────────────────┘",
@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 8             │
└────────────────────┘",
@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 9             │
└────────────────────┘",
@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 10            │
└────────────────────┘",
@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 11            │
└────────────────────┘",
@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 12            │
└────────────────────┘",
@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 13            │
└────────────────────┘",
@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 14            │
└────────────────────┘",
@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 15            │
│Label 15            │
└────────────────────┘",
@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 15            │
│Label 16            │
│Label 16            │
└────────────────────┘",
@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 15            │
│Label 16            │
│Label 17            │
│Label 17            │
└────────────────────┘",
@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 15            │
│Label 16            │
│Label 17            │
│Label 18            │
│Label 18            │
└────────────────────┘",
@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 15            │
│Label 16            │
│Label 17            │
│Label 18            │
│Label 19            │
│Label 19            │
└────────────────────┘",
};

		[Fact, AutoInitShutdown]
		public void Dim_Add_Operator_With_Text ()
		{
			var top = Application.Top;

			// BUGBUG: v2 - If a View's height is zero, it should not be drawn.
			//// Although view height is zero the text it's draw due the SetMinWidthHeight method
			var view = new View ("View with long text") { X = 0, Y = 0, Width = 20, Height = 1 };
			var field = new TextField () { X = 0, Y = Pos.Bottom (view), Width = 20 };
			var count = 0;
			var listLabels = new List<Label> ();

			field.KeyDown += (s, k) => {
				if (k.KeyEvent.Key == Key.Enter) {
					((FakeDriver)Application.Driver).SetBufferSize (22, count + 4);
					var pos = TestHelpers.AssertDriverContentsWithFrameAre (expecteds [count], output);
					Assert.Equal (new Rect (0, 0, 22, count + 4), pos);

					if (count < 20) {
						field.Text = $"Label {count}";
						var label = new Label (field.Text) { X = 0, Y = view.Bounds.Height, Width = 10 };
						view.Add (label);
						Assert.Equal ($"Label {count}", label.Text);
						Assert.Equal ($"Absolute({count + 1})", label.Y.ToString ());
						listLabels.Add (label);
						//if (count == 0) {
						//	Assert.Equal ($"Absolute({count})", view.Height.ToString ());
						//	view.Height += 2;
						//} else {
						Assert.Equal ($"Absolute({count + 1})", view.Height.ToString ());
						view.Height += 1;
						//}
						count++;
					}
					Assert.Equal ($"Absolute({count + 1})", view.Height.ToString ());
				}
			};

			Application.Iteration += () => {
				while (count < 21) {
					field.OnKeyDown (new KeyEvent (Key.Enter, new KeyModifiers ()));
					if (count == 20) {
						field.OnKeyDown (new KeyEvent (Key.Enter, new KeyModifiers ()));
						break;
					}
				}

				Application.RequestStop ();
			};

			var win = new Window ();
			win.Add (view);
			win.Add (field);

			top.Add (win);

			Application.Run (top);

			Assert.Equal (20, count);
			Assert.Equal (count, listLabels.Count);
		}

		[Fact, AutoInitShutdown]
		public void Dim_Subtract_Operator ()
		{
			var top = Application.Top;

			var view = new View () { X = 0, Y = 0, Width = 20, Height = 0 };
			var field = new TextField () { X = 0, Y = Pos.Bottom (view), Width = 20 };
			var count = 20;
			var listLabels = new List<Label> ();

			for (int i = 0; i < count; i++) {
				field.Text = $"Label {i}";
				var label = new Label (field.Text) { X = 0, Y = view.Bounds.Height, Width = 20 };
				view.Add (label);
				Assert.Equal ($"Label {i}", label.Text);
				// BUGBUG: Bogus test; views have not been initialized yet
				//Assert.Equal ($"Absolute({i})", label.Y.ToString ());
				listLabels.Add (label);

				// BUGBUG: Bogus test; views have not been initialized yet
				//Assert.Equal ($"Absolute({i})", view.Height.ToString ());
				view.Height += 1;
				//Assert.Equal ($"Absolute({i + 1})", view.Height.ToString ());
			}

			field.KeyDown += (s, k) => {
				if (k.KeyEvent.Key == Key.Enter) {
					Assert.Equal ($"Label {count - 1}", listLabels [count - 1].Text);
					view.Remove (listLabels [count - 1]);

					Assert.Equal ($"Absolute({count})", view.Height.ToString ());
					view.Height -= 1;
					count--;
					Assert.Equal ($"Absolute({count})", view.Height.ToString ());
				}
			};

			Application.Iteration += () => {
				while (count > 0) field.OnKeyDown (new KeyEvent (Key.Enter, new KeyModifiers ()));

				Application.RequestStop ();
			};

			var win = new Window ();
			win.Add (view);
			win.Add (field);

			top.Add (win);

			Application.Run (top);

			Assert.Equal (0, count);
		}

		[Fact, AutoInitShutdown]
		public void Dim_Subtract_Operator_With_Text ()
		{
			var top = Application.Top;

			// BUGBUG: v2 - If a View's height is zero, it should not be drawn.
			//// Although view height is zero the text it's draw due the SetMinWidthHeight method
			var view = new View ("View with long text") { X = 0, Y = 0, Width = 20, Height = 1 };
			var field = new TextField () { X = 0, Y = Pos.Bottom (view), Width = 20 };
			var count = 20;
			var listLabels = new List<Label> ();

			for (int i = 0; i < count; i++) {
				field.Text = $"Label {i}";
				// BUGBUG: v2 - view has not been initialied yet; view.Bounds is indeterminate
				var label = new Label (field.Text) { X = 0, Y = i + 1, Width = 10 };
				view.Add (label);
				Assert.Equal ($"Label {i}", label.Text);
				// BUGBUG: Bogus test; views have not been initialized yet
				//Assert.Equal ($"Absolute({i + 1})", label.Y.ToString ());
				listLabels.Add (label);

				//if (i == 0) {
				// BUGBUG: Bogus test; views have not been initialized yet
				//Assert.Equal ($"Absolute({i})", view.Height.ToString ());
				//view.Height += 2;
				// BUGBUG: Bogus test; views have not been initialized yet
				//Assert.Equal ($"Absolute({i + 2})", view.Height.ToString ());
				//} else {
				// BUGBUG: Bogus test; views have not been initialized yet
				//Assert.Equal ($"Absolute({i + 1})", view.Height.ToString ());
				view.Height += 1;
				// BUGBUG: Bogus test; views have not been initialized yet
				//Assert.Equal ($"Absolute({i + 2})", view.Height.ToString ());
				//}
			}

			field.KeyDown += (s, k) => {
				if (k.KeyEvent.Key == Key.Enter) {
					((FakeDriver)Application.Driver).SetBufferSize (22, count + 4);
					var pos = TestHelpers.AssertDriverContentsWithFrameAre (expecteds [count], output);
					Assert.Equal (new Rect (0, 0, 22, count + 4), pos);

					if (count > 0) {
						Assert.Equal ($"Label {count - 1}", listLabels [count - 1].Text);
						view.Remove (listLabels [count - 1]);
						listLabels.RemoveAt (count - 1);
						Assert.Equal ($"Absolute({count + 1})", view.Height.ToString ());
						view.Height -= 1;
						count--;
						if (listLabels.Count > 0)
							field.Text = listLabels [count - 1].Text;
						else
							field.Text = string.Empty;
					}
					Assert.Equal ($"Absolute({count + 1})", view.Height.ToString ());
				}
			};

			Application.Iteration += () => {
				while (count > -1) {
					field.OnKeyDown (new KeyEvent (Key.Enter, new KeyModifiers ()));
					if (count == 0) {
						field.OnKeyDown (new KeyEvent (Key.Enter, new KeyModifiers ()));
						break;
					}
				}

				Application.RequestStop ();
			};

			var win = new Window ();
			win.Add (view);
			win.Add (field);

			top.Add (win);

			Application.Run (top);

			Assert.Equal (0, count);
			Assert.Equal (count, listLabels.Count);
		}

		[Fact]
		public void Internal_Tests ()
		{
			var dimFactor = new Dim.DimFactor (0.10F);
			Assert.Equal (10, dimFactor.Anchor (100));

			var dimAbsolute = new Dim.DimAbsolute (10);
			Assert.Equal (10, dimAbsolute.Anchor (0));

			var dimFill = new Dim.DimFill (1);
			Assert.Equal (99, dimFill.Anchor (100));

			var dimCombine = new Dim.DimCombine (true, dimFactor, dimAbsolute);
			Assert.Equal (dimCombine.left, dimFactor);
			Assert.Equal (dimCombine.right, dimAbsolute);
			Assert.Equal (20, dimCombine.Anchor (100));

			var view = new View (new Rect (20, 10, 20, 1));
			var dimViewHeight = new Dim.DimView (view, 0);
			Assert.Equal (1, dimViewHeight.Anchor (0));
			var dimViewWidth = new Dim.DimView (view, 1);
			Assert.Equal (20, dimViewWidth.Anchor (0));
		}

		[Fact]
		public void Function_SetsValue ()
		{
			var text = "Test";
			var dim = Dim.Function (() => text.Length);
			Assert.Equal ("DimFunc(4)", dim.ToString ());

			text = "New Test";
			Assert.Equal ("DimFunc(8)", dim.ToString ());

			text = "";
			Assert.Equal ("DimFunc(0)", dim.ToString ());
		}

		[Fact]
		public void Function_Equal ()
		{
			var f1 = () => 0;
			var f2 = () => 0;

			var dim1 = Dim.Function (f1);
			var dim2 = Dim.Function (f2);
			Assert.Equal (dim1, dim2);

			f2 = () => 1;
			dim2 = Dim.Function (f2);
			Assert.NotEqual (dim1, dim2);
		}

		[Theory, AutoInitShutdown]
		[InlineData (0, true)]
		[InlineData (0, false)]
		[InlineData (50, true)]
		[InlineData (50, false)]
		public void DimPercentPlusOne (int startingDistance, bool testHorizontal)
		{
			var container = new View {
				Width = 100,
				Height = 100,
			};

			var label = new Label {
				X = testHorizontal ? startingDistance : 0,
				Y = testHorizontal ? 0 : startingDistance,
				Width = testHorizontal ? Dim.Percent (50) + 1 : 1,
				Height = testHorizontal ? 1 : Dim.Percent (50) + 1,
			};

			container.Add (label);
			Application.Top.Add (container);
			Application.Top.LayoutSubviews ();

			Assert.Equal (100, container.Frame.Width);
			Assert.Equal (100, container.Frame.Height);

			if (testHorizontal) {
				Assert.Equal (51, label.Frame.Width);
				Assert.Equal (1, label.Frame.Height);
			} else {
				Assert.Equal (1, label.Frame.Width);
				Assert.Equal (51, label.Frame.Height);
			}
		}

		[Fact]
		public void Dim_Referencing_SuperView_Does_Not_Throw ()
		{
			var super = new View ("super") {
				Width = 10,
				Height = 10
			};
			var view = new View ("view") {
				Width = Dim.Width (super),      // this is allowed
				Height = Dim.Height (super),    // this is allowed
			};

			super.Add (view);
			super.BeginInit ();
			super.EndInit ();

			var exception = Record.Exception (super.LayoutSubviews);
			Assert.Null (exception);
		}

		[Fact]
		public void Dim_SyperView_Referencing_SubView_Throws ()
		{
			var super = new View ("super") {
				Width = 10,
				Height = 10
			};
			var view2 = new View ("view2") {
				Width = 10,
				Height = 10,
			};
			var view = new View ("view") {
				Width = Dim.Width (view2),      // this is not allowed
				Height = Dim.Height (view2),    // this is not allowed
			};

			view.Add (view2);
			super.Add (view);
			super.BeginInit ();
			super.EndInit ();

			Assert.Throws<InvalidOperationException> (super.LayoutSubviews);
		}
	}
}
