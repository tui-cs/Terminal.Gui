using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui.Graphs;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.CoreTests {
	public class LineCanvasTests {

		readonly ITestOutputHelper output;

		public LineCanvasTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[InlineData (BorderStyle.Single)]
		[InlineData (BorderStyle.Rounded)]
		[Theory, AutoInitShutdown]
		public void TestLineCanvas_Horizontal (BorderStyle style)
		{
			var v = GetCanvas (out var canvas);
			canvas.AddLine (new Point (0, 0), 1, Orientation.Horizontal, style);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
──";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void TestLineCanvas_Horizontal_Double ()
		{
			var v = GetCanvas (out var canvas);
			canvas.AddLine (new Point (0, 0), 1, Orientation.Horizontal, BorderStyle.Double);

			v.Redraw (v.Bounds);

			string looksLike =
@" 
══";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[InlineData (BorderStyle.Single)]
		[InlineData (BorderStyle.Rounded)]
		[Theory, AutoInitShutdown]
		public void TestLineCanvas_Vertical (BorderStyle style)
		{
			var v = GetCanvas (out var canvas);
			canvas.AddLine (new Point (0, 0), 1, Orientation.Vertical, style);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
│
│";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void TestLineCanvas_Vertical_Double ()
		{
			var v = GetCanvas (out var canvas);
			canvas.AddLine (new Point (0, 0), 1, Orientation.Vertical, BorderStyle.Double);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
║
║";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		/// <summary>
		/// This test demonstrates that corners are only drawn when lines overlap.
		/// Not when they terminate adjacent to one another.
		/// </summary>
		[Fact, AutoInitShutdown]
		public void TestLineCanvas_Corner_NoOverlap ()
		{
			var v = GetCanvas (out var canvas);
			canvas.AddLine (new Point (0, 0), 1, Orientation.Horizontal, BorderStyle.Single);
			canvas.AddLine (new Point (0, 1), 1, Orientation.Vertical, BorderStyle.Single);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
──
│
│";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}
		/// <summary>
		/// This test demonstrates how to correctly trigger a corner.  By
		/// overlapping the lines in the same cell
		/// </summary>
		[Fact, AutoInitShutdown]
		public void TestLineCanvas_Corner_Correct ()
		{
			var v = GetCanvas (out var canvas);
			canvas.AddLine (new Point (0, 0), 1, Orientation.Horizontal, BorderStyle.Single);
			canvas.AddLine (new Point (0, 0), 2, Orientation.Vertical, BorderStyle.Single);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
┌─
│
│";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

		}

		[Fact, AutoInitShutdown]
		public void TestLineCanvas_Window ()
		{
			var v = GetCanvas (out var canvas);

			// outer box
			canvas.AddLine (new Point (0, 0), 9, Orientation.Horizontal, BorderStyle.Single);
			canvas.AddLine (new Point (9, 0), 4, Orientation.Vertical, BorderStyle.Single);
			canvas.AddLine (new Point (9, 4), -9, Orientation.Horizontal, BorderStyle.Single);
			canvas.AddLine (new Point (0, 4), -4, Orientation.Vertical, BorderStyle.Single);


			canvas.AddLine (new Point (5, 0), 4, Orientation.Vertical, BorderStyle.Single);
			canvas.AddLine (new Point (0, 2), 9, Orientation.Horizontal, BorderStyle.Single);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
┌────┬───┐
│    │   │
├────┼───┤
│    │   │
└────┴───┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		/// <summary>
		/// Demonstrates when <see cref="BorderStyle.Rounded"/> corners are used. Notice how
		/// not all lines declare rounded.  If there are 1+ lines intersecting and a corner is
		/// to be used then if any of them are rounded a rounded corner is used.
		/// </summary>
		[Fact, AutoInitShutdown]
		public void TestLineCanvas_Window_Rounded ()
		{
			var v = GetCanvas (out var canvas);

			// outer box
			canvas.AddLine (new Point (0, 0), 9, Orientation.Horizontal, BorderStyle.Rounded);

			// BorderStyle.Single is ignored because corner overlaps with the above line which is Rounded
			// this results in a rounded corner being used.
			canvas.AddLine (new Point (9, 0), 4, Orientation.Vertical, BorderStyle.Single);
			canvas.AddLine (new Point (9, 4), -9, Orientation.Horizontal, BorderStyle.Rounded);
			canvas.AddLine (new Point (0, 4), -4, Orientation.Vertical, BorderStyle.Single);

			// These lines say rounded but they will result in the T sections which are never rounded.
			canvas.AddLine (new Point (5, 0), 4, Orientation.Vertical, BorderStyle.Rounded);
			canvas.AddLine (new Point (0, 2), 9, Orientation.Horizontal, BorderStyle.Rounded);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
╭────┬───╮
│    │   │
├────┼───┤
│    │   │
╰────┴───╯";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void TestLineCanvas_Window_Double ()
		{
			var v = GetCanvas (out var canvas);

			// outer box
			canvas.AddLine (new Point (0, 0), 9, Orientation.Horizontal, BorderStyle.Double);
			canvas.AddLine (new Point (9, 0), 4, Orientation.Vertical, BorderStyle.Double);
			canvas.AddLine (new Point (9, 4), -9, Orientation.Horizontal, BorderStyle.Double);
			canvas.AddLine (new Point (0, 4), -4, Orientation.Vertical, BorderStyle.Double);


			canvas.AddLine (new Point (5, 0), 4, Orientation.Vertical, BorderStyle.Double);
			canvas.AddLine (new Point (0, 2), 9, Orientation.Horizontal, BorderStyle.Double);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
╔════╦═══╗
║    ║   ║
╠════╬═══╣
║    ║   ║
╚════╩═══╝";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}


		[Theory, AutoInitShutdown]
		[InlineData (BorderStyle.Single)]
		[InlineData (BorderStyle.Rounded)]
		public void TestLineCanvas_Window_DoubleTop_SingleSides (BorderStyle thinStyle)
		{
			var v = GetCanvas (out var canvas);

			// outer box
			canvas.AddLine (new Point (0, 0), 9, Orientation.Horizontal, BorderStyle.Double);
			canvas.AddLine (new Point (9, 0), 4, Orientation.Vertical, thinStyle);
			canvas.AddLine (new Point (9, 4), -9, Orientation.Horizontal, BorderStyle.Double);
			canvas.AddLine (new Point (0, 4), -4, Orientation.Vertical, thinStyle);


			canvas.AddLine (new Point (5, 0), 4, Orientation.Vertical, thinStyle);
			canvas.AddLine (new Point (0, 2), 9, Orientation.Horizontal, BorderStyle.Double);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
╒════╤═══╕
│    │   │
╞════╪═══╡
│    │   │
╘════╧═══╛
";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Theory, AutoInitShutdown]
		[InlineData (BorderStyle.Single)]
		[InlineData (BorderStyle.Rounded)]
		public void TestLineCanvas_Window_SingleTop_DoubleSides (BorderStyle thinStyle)
		{
			var v = GetCanvas (out var canvas);

			// outer box
			canvas.AddLine (new Point (0, 0), 9, Orientation.Horizontal, thinStyle);
			canvas.AddLine (new Point (9, 0), 4, Orientation.Vertical, BorderStyle.Double);
			canvas.AddLine (new Point (9, 4), -9, Orientation.Horizontal, thinStyle);
			canvas.AddLine (new Point (0, 4), -4, Orientation.Vertical, BorderStyle.Double);


			canvas.AddLine (new Point (5, 0), 4, Orientation.Vertical, BorderStyle.Double);
			canvas.AddLine (new Point (0, 2), 9, Orientation.Horizontal, thinStyle);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
╓────╥───╖
║    ║   ║
╟────╫───╢
║    ║   ║
╙────╨───╜

";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void TestLineCanvas_LeaveMargin_Top1_Left1 ()
		{
			// Draw at 1,1 within client area of View (i.e. leave a top and left margin of 1)
			var v = GetCanvas (out var canvas, 1, 1);

			// outer box
			canvas.AddLine (new Point (0, 0), 8, Orientation.Horizontal, BorderStyle.Single);
			canvas.AddLine (new Point (8, 0), 3, Orientation.Vertical, BorderStyle.Single);
			canvas.AddLine (new Point (8, 3), -8, Orientation.Horizontal, BorderStyle.Single);
			canvas.AddLine (new Point (0, 3), -3, Orientation.Vertical, BorderStyle.Single);


			canvas.AddLine (new Point (5, 0), 3, Orientation.Vertical, BorderStyle.Single);
			canvas.AddLine (new Point (0, 2), 8, Orientation.Horizontal, BorderStyle.Single);

			v.Redraw (v.Bounds);

			string looksLike =
@"
 ┌────┬──┐
 │    │  │
 ├────┼──┤
 └────┴──┘
";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}
		[Fact, AutoInitShutdown]
		public void TestLineCanvas_ClipArea_Intersections ()
		{
			// Draw at 1,1 within client area of View (i.e. leave a top and left margin of 1)
			var v = GetCanvas (out var lc);
			v.Width = 10;
			v.Height = 1;
			v.Bounds = new Rect (0, 0, 10, 1);

			// ╔╡ Title ╞═════╗
			// Add a short horiz line for ╔╡
			lc.AddLine (new Point (0, 0), 1, Orientation.Horizontal, BorderStyle.Double);
			//LHS line down
			lc.AddLine (new Point (0, 0), 5, Orientation.Vertical, BorderStyle.Double);

			//Vertical line before Title, results in a ╡
			lc.AddLine (new Point (1, 0), 0, Orientation.Vertical, BorderStyle.Single);
			//Vertical line after Title, results in a ╞
			lc.AddLine (new Point (6, 0), 0, Orientation.Vertical, BorderStyle.Single);

			// remainder of title
			lc.AddLine (new Point (6, 0), 3, Orientation.Horizontal, BorderStyle.Double);
			//RHS line down
			lc.AddLine (new Point (9, 0), 5, Orientation.Vertical, BorderStyle.Double);

			v.Redraw (v.Bounds);

			string looksLike =
		@"
╔╡    ╞══╗
";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[InlineData(0,0,0, Orientation.Horizontal,BorderStyle.Double,"═")]
		[InlineData(0,0,0, Orientation.Vertical,BorderStyle.Double,"║")]
		[InlineData(0,0,0, Orientation.Horizontal,BorderStyle.Single,"─")]
		[InlineData(0,0,0, Orientation.Vertical,BorderStyle.Single,"│")]
		[AutoInitShutdown, Theory]
		public void TestLineCanvas_1LineTests(
			int x1, int y1,int l1, Orientation o1, BorderStyle s1,
			string expected
		)
		{
			var v = GetCanvas (out var lc);
			v.Width = 10;
			v.Height = 10;
			v.Bounds = new Rect (0, 0, 10, 10);

			lc.AddLine (new Point (x1, y1), l1, o1, s1);

			v.Redraw (v.Bounds);
		
			TestHelpers.AssertDriverContentsAre (expected, output);
		}


		[Theory, AutoInitShutdown]
		[InlineData(
			0,0,1,Orientation.Horizontal,BorderStyle.Double,
			1,0,0, Orientation.Vertical,BorderStyle.Single, "═╡"
		)]
		[InlineData(
			0,0,0, Orientation.Vertical,BorderStyle.Single,
			0,0,1,Orientation.Horizontal,BorderStyle.Double,
			 "╞═"
		)]
		[InlineData(
			0,0,1, Orientation.Vertical,BorderStyle.Single,
			0,0,0,Orientation.Horizontal,BorderStyle.Double,
@"
╤
│"
		)]
		[InlineData(
			0,0,1, Orientation.Vertical,BorderStyle.Single,
			0,1,0,Orientation.Horizontal,BorderStyle.Double,
			@"
│
╧
"
		)]
		[InlineData(
			0,0,0, Orientation.Vertical,BorderStyle.Single,
			0,0,0,Orientation.Horizontal,BorderStyle.Single,
			@"┼
"
		)]
		[InlineData(
			0,0,0, Orientation.Vertical,BorderStyle.Double,
			0,0,0,Orientation.Horizontal,BorderStyle.Double,
			@"╬
"
		)]
		public void TestLineCanvas_2LineTests(
			int x1, int y1,int l1, Orientation o1, BorderStyle s1,
			int x2, int y2, int l2, Orientation o2, BorderStyle s2,
			string expected
		)
		{
			var v = GetCanvas (out var lc);
			v.Width = 10;
			v.Height = 10;
			v.Bounds = new Rect (0, 0, 10, 10);

			lc.AddLine (new Point (x1, y1), l1, o1, s1);
			lc.AddLine (new Point (x2, y2), l2, o2, s2);

			v.Redraw (v.Bounds);
		
			TestHelpers.AssertDriverContentsAre (expected, output);
		}
		

		/// <summary>
		/// Creates a new <see cref="View"/> into which a <see cref="LineCanvas"/> is rendered
		/// at <see cref="View.DrawContentComplete"/> time.
		/// </summary>
		/// <param name="canvas">The <see cref="LineCanvas"/> you can draw into.</param>
		/// <param name="offsetX">How far to offset drawing in X</param>
		/// <param name="offsetY">How far to offset drawing in Y</param>
		/// <returns></returns>
		private View GetCanvas (out LineCanvas canvas, int offsetX = 0, int offsetY = 0)
		{
			var v = new View {
				Width = 10,
				Height = 5,
				Bounds = new Rect (0, 0, 10, 5)
			};

			var canvasCopy = canvas = new LineCanvas ();
			v.DrawContentComplete += (s, e) => {
					foreach(var p in canvasCopy.GenerateImage(v.Bounds))
					{
						v.AddRune(
							offsetX + p.Key.X,
							offsetY + p.Key.Y,
							p.Value);
					}
				};

			return v;
		}
	}
}
