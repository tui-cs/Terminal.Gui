using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Terminal.Gui;
using Xunit;
using System.Globalization;
using Xunit.Abstractions;
using System.Text;
using static Terminal.Gui.Application;

namespace Terminal.Gui.DialogTests {

	public class DialogTests {
		readonly ITestOutputHelper output;

		public DialogTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		//[Fact]
		//[AutoInitShutdown]
		//public void Default_Has_Border ()
		//{
		//	var d = (FakeDriver)Application.Driver;
		//	d.SetBufferSize (20, 5);
		//	Application.RunState runstate = null;

		//	var title = "Title";
		//	var btnText = "ok";
		//	var buttonRow = $"{CM.Glyphs.VLine}{CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}{CM.Glyphs.VLine}";
		//	var width = buttonRow.Length;
		//	var topRow = $"┌┤{title} {new string (d.HLine.ToString () [0], width - title.Length - 2)}├┐";
		//	var bottomRow = $"└{new string (d.HLine.ToString () [0], width - 2)}┘";

		//	var dlg = new Dialog (title, new Button (btnText));
		//	Application.Begin (dlg);

		//	TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
		//	Application.End (runstate);
		//}

		private (RunState, Dialog) RunButtonTestDialog (string title, int width, Dialog.ButtonAlignments align, params Button [] btns)
		{
			var dlg = new Dialog (btns) {
				Title = title,
				X = 0,
				Y = 0,
				Width = width,
				Height = 1,
				ButtonAlignment = align,
			};
			// Create with no top or bottom border to simplify testing button layout (no need to account for title etc..)
			dlg.Border.Thickness = new Thickness (1, 0, 1, 0);
			return (Application.Begin (dlg), dlg);
		}

		[Fact]
		[AutoInitShutdown]
		public void Size_Default ()
		{
			var d = new Dialog () {
			};
			Application.Begin (d);
			((FakeDriver)Application.Driver).SetBufferSize (100, 100);

			// Default size is Percent(85) 
			Assert.Equal (new Size ((int)(100 * .85), (int)(100 * .85)), d.Frame.Size);
		}

		[Fact]
		[AutoInitShutdown]
		public void Location_Default ()
		{
			var d = new Dialog () {
			};
			Application.Begin (d);
			((FakeDriver)Application.Driver).SetBufferSize (100, 100);

			// Default location is centered, so 100 / 2 - 85 / 2 = 7
			var expected = 7;
			Assert.Equal (new Point (expected, expected), d.Frame.Location);
		}

		[Fact]
		[AutoInitShutdown]
		public void Size_Not_Default ()
		{
			var d = new Dialog () {
				Width = 50,
				Height = 50,
			};

			Application.Begin (d);
			((FakeDriver)Application.Driver).SetBufferSize (100, 100);

			// Default size is Percent(85) 
			Assert.Equal (new Size (50, 50), d.Frame.Size);
		}

		[Fact]
		[AutoInitShutdown]
		public void Location_Not_Default ()
		{
			var d = new Dialog () {
				X = 1,
				Y = 1,
			};
			Application.Begin (d);
			((FakeDriver)Application.Driver).SetBufferSize (100, 100);

			// Default location is centered, so 100 / 2 - 85 / 2 = 7
			var expected = 1;
			Assert.Equal (new Point (expected, expected), d.Frame.Location);
		}

		[Fact]
		[AutoInitShutdown]
		public void Location_When_Application_Top_Not_Default ()
		{
			var expected = 5;
			var d = new Dialog () {
				X = expected,
				Y = expected,
				Height = 5,
				Width = 5
			};
			Application.Begin (d);
			((FakeDriver)Application.Driver).SetBufferSize (20, 10);

			// Default location is centered, so 100 / 2 - 85 / 2 = 7
			Assert.Equal (new Point (expected, expected), d.Frame.Location);

			TestHelpers.AssertDriverContentsWithFrameAre (@"
     ┌───┐
     │   │
     │   │
     │   │
     └───┘", output);
		}

		[Fact]
		[AutoInitShutdown]
		public void Location_When_Not_Application_Top_Not_Default ()
		{
			Application.Top.BorderStyle = LineStyle.Double;

			var iterations = -1;
			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {
					var d = new Dialog () {
						X = 5,
						Y = 5,
						Height = 3,
						Width = 5
					};
					Application.Begin (d);

					Assert.Equal (new Point (5, 5), d.Frame.Location);
					TestHelpers.AssertDriverContentsWithFrameAre (@"
╔══════════════════╗
║                  ║
║                  ║
║                  ║
║                  ║
║    ┌───┐         ║
║    │   │         ║
║    └───┘         ║
║                  ║
╚══════════════════╝", output);

					d = new Dialog () {
						X = 5,
						Y = 5,
					};
					Application.Begin (d);

					// This is because of PostionTopLevels and EnsureVisibleBounds
					Assert.Equal (new Point (3, 2), d.Frame.Location);
					Assert.Equal (new Size (17, 8), d.Frame.Size);
					TestHelpers.AssertDriverContentsWithFrameAre (@"
╔══════════════════╗
║                  ║
║  ┌───────────────┐
║  │               │
║  │               │
║  │               │
║  │               │
║  │               │
║  │               │
╚══└───────────────┘", output);

				} else if (iterations > 0) {
					Application.RequestStop ();
				}
			};

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (20, 10);
			Application.Run ();
		}

		[Fact]
		[AutoInitShutdown]
		public void ButtonAlignment_One ()
		{
			var d = (FakeDriver)Application.Driver;
			RunState runstate = null;

			var title = "1234";
			// E.g "|[ ok ]|"
			var btnText = "ok";
			var buttonRow = $"{CM.Glyphs.VLine}  {CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}  {CM.Glyphs.VLine}";
			var width = buttonRow.Length;

			d.SetBufferSize (width, 1);

			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btnText));
			// Center
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Justify 
			buttonRow = $"{CM.Glyphs.VLine}    {CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}{CM.Glyphs.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btnText));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Right
			buttonRow = $"{CM.Glyphs.VLine}    {CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}{CM.Glyphs.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btnText));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Left
			buttonRow = $"{CM.Glyphs.VLine}{CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}    {CM.Glyphs.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btnText));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Wider
			buttonRow = $"{CM.Glyphs.VLine}   {CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}   {CM.Glyphs.VLine}";
			width = buttonRow.Length;

			d.SetBufferSize (width, 1);

			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btnText));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Justify
			buttonRow = $"{CM.Glyphs.VLine}      {CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}{CM.Glyphs.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btnText));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Right
			buttonRow = $"{CM.Glyphs.VLine}      {CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}{CM.Glyphs.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btnText));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Left
			buttonRow = $"{CM.Glyphs.VLine}{CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}      {CM.Glyphs.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btnText));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);
		}

		[Fact]
		[AutoInitShutdown]
		public void ButtonAlignment_Two ()
		{
			RunState runstate = null;

			var d = (FakeDriver)Application.Driver;

			var title = "1234";
			// E.g "|[ yes ][ no ]|"
			var btn1Text = "yes";
			var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
			var btn2Text = "no";
			var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";

			var buttonRow = $@"{CM.Glyphs.VLine} {btn1} {btn2} {CM.Glyphs.VLine}";
			var width = buttonRow.Length;

			d.SetBufferSize (buttonRow.Length, 3);

			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Justify
			buttonRow = $@"{CM.Glyphs.VLine}{btn1}   {btn2}{CM.Glyphs.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Right
			buttonRow = $@"{CM.Glyphs.VLine}  {btn1} {btn2}{CM.Glyphs.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Left
			buttonRow = $@"{CM.Glyphs.VLine}{btn1} {btn2}  {CM.Glyphs.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);
		}

		[Fact]
		[AutoInitShutdown]
		public void ButtonAlignment_Two_Hidden ()
		{
			RunState runstate = null;
			bool firstIteration = false;

			var d = (FakeDriver)Application.Driver;

			var title = "1234";
			// E.g "|[ yes ][ no ]|"
			var btn1Text = "yes";
			var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
			var btn2Text = "no";
			var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";

			var buttonRow = $@"{CM.Glyphs.VLine} {btn1} {btn2} {CM.Glyphs.VLine}";
			var width = buttonRow.Length;

			d.SetBufferSize (buttonRow.Length, 3);

			Dialog dlg = null;
			Button button1, button2;

			// Default (Center)
			button1 = new Button (btn1Text);
			button2 = new Button (btn2Text);
			(runstate, dlg) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, button1, button2);
			button1.Visible = false;
			Application.RunMainLoopIteration (ref runstate, true, ref firstIteration);
			buttonRow = $@"{CM.Glyphs.VLine}         {btn2} {CM.Glyphs.VLine}";
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Justify
			Assert.Equal (width, buttonRow.Length);
			button1 = new Button (btn1Text);
			button2 = new Button (btn2Text);
			(runstate, dlg) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, button1, button2);
			button1.Visible = false;
			Application.RunMainLoopIteration (ref runstate, true, ref firstIteration);
			buttonRow = $@"{CM.Glyphs.VLine}          {btn2}{CM.Glyphs.VLine}";
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Right
			Assert.Equal (width, buttonRow.Length);
			button1 = new Button (btn1Text);
			button2 = new Button (btn2Text);
			(runstate, dlg) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, button1, button2);
			button1.Visible = false;
			Application.RunMainLoopIteration (ref runstate, true, ref firstIteration);
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Left
			Assert.Equal (width, buttonRow.Length);
			button1 = new Button (btn1Text);
			button2 = new Button (btn2Text);
			(runstate, dlg) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, button1, button2);
			button1.Visible = false;
			Application.RunMainLoopIteration (ref runstate, true, ref firstIteration);
			buttonRow = $@"{CM.Glyphs.VLine}        {btn2}  {CM.Glyphs.VLine}";
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);
		}

		[Fact]
		[AutoInitShutdown]
		public void ButtonAlignment_Three ()
		{
			RunState runstate = null;

			var d = (FakeDriver)Application.Driver;

			var title = "1234";
			// E.g "|[ yes ][ no ][ maybe ]|"
			var btn1Text = "yes";
			var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
			var btn2Text = "no";
			var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";
			var btn3Text = "maybe";
			var btn3 = $"{CM.Glyphs.LeftBracket} {btn3Text} {CM.Glyphs.RightBracket}";

			var buttonRow = $@"{CM.Glyphs.VLine} {btn1} {btn2} {btn3} {CM.Glyphs.VLine}";
			var width = buttonRow.Length;

			d.SetBufferSize (buttonRow.Length, 3);

			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Justify
			buttonRow = $@"{CM.Glyphs.VLine}{btn1}  {btn2}  {btn3}{CM.Glyphs.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Right
			buttonRow = $@"{CM.Glyphs.VLine}  {btn1} {btn2} {btn3}{CM.Glyphs.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Left
			buttonRow = $@"{CM.Glyphs.VLine}{btn1} {btn2} {btn3}  {CM.Glyphs.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);
		}

		[Fact]
		[AutoInitShutdown]
		public void ButtonAlignment_Four ()
		{
			RunState runstate = null;

			var d = (FakeDriver)Application.Driver;

			var title = "1234";

			// E.g "|[ yes ][ no ][ maybe ]|"
			var btn1Text = "yes";
			var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
			var btn2Text = "no";
			var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";
			var btn3Text = "maybe";
			var btn3 = $"{CM.Glyphs.LeftBracket} {btn3Text} {CM.Glyphs.RightBracket}";
			var btn4Text = "never";
			var btn4 = $"{CM.Glyphs.LeftBracket} {btn4Text} {CM.Glyphs.RightBracket}";

			var buttonRow = $"{CM.Glyphs.VLine} {btn1} {btn2} {btn3} {btn4} {CM.Glyphs.VLine}";
			var width = buttonRow.Length;
			d.SetBufferSize (buttonRow.Length, 3);

			// Default - Center
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Justify
			buttonRow = $"{CM.Glyphs.VLine}{btn1} {btn2}  {btn3}  {btn4}{CM.Glyphs.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Right
			buttonRow = $"{CM.Glyphs.VLine}  {btn1} {btn2} {btn3} {btn4}{CM.Glyphs.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Left
			buttonRow = $"{CM.Glyphs.VLine}{btn1} {btn2} {btn3} {btn4}  {CM.Glyphs.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);
		}

		[Fact]
		[AutoInitShutdown]
		public void ButtonAlignment_Four_On_Too_Small_Width ()
		{
			RunState runstate = null;

			var d = (FakeDriver)Application.Driver;

			var title = "1234";

			// E.g "|[ yes ][ no ][ maybe ][ never ]|"
			var btn1Text = "yes";
			var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
			var btn2Text = "no";
			var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";
			var btn3Text = "maybe";
			var btn3 = $"{CM.Glyphs.LeftBracket} {btn3Text} {CM.Glyphs.RightBracket}";
			var btn4Text = "never";
			var btn4 = $"{CM.Glyphs.LeftBracket} {btn4Text} {CM.Glyphs.RightBracket}";
			var buttonRow = string.Empty;

			var width = 30;
			d.SetBufferSize (width, 1);

			// Default - Center
			buttonRow = $"{CM.Glyphs.VLine}es {CM.Glyphs.RightBracket} {btn2} {btn3} {CM.Glyphs.LeftBracket} neve{CM.Glyphs.VLine}";
			(runstate, var dlg) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			Assert.Equal (new Size (width, 1), dlg.Frame.Size);
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Justify
			buttonRow = $"{CM.Glyphs.VLine}{CM.Glyphs.LeftBracket} yes {CM.Glyphs.LeftBracket} no {CM.Glyphs.LeftBracket} maybe {CM.Glyphs.LeftBracket} never {CM.Glyphs.RightBracket}{CM.Glyphs.VLine}";
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output); Application.End (runstate);

			// Right
			buttonRow = $"{CM.Glyphs.VLine}{CM.Glyphs.RightBracket} {btn2} {btn3} {btn4}{CM.Glyphs.VLine}";
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output); Application.End (runstate);

			// Left
			buttonRow = $"{CM.Glyphs.VLine}{btn1} {btn2} {btn3} {CM.Glyphs.LeftBracket} n{CM.Glyphs.VLine}";
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output); Application.End (runstate);
		}

		[Fact]
		[AutoInitShutdown]
		public void ButtonAlignment_Four_Wider ()
		{
			RunState runstate = null;

			var d = (FakeDriver)Application.Driver;

			var title = "1234";

			// E.g "|[ yes ][ no ][ maybe ]|"
			var btn1Text = "yes";
			var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
			var btn2Text = "no";
			var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";
			var btn3Text = "你你你你你"; // This is a wide char
			var btn3 = $"{CM.Glyphs.LeftBracket} {btn3Text} {CM.Glyphs.RightBracket}";
			// Requires a Nerd Font
			var btn4Text = "\uE36E\uE36F\uE370\uE371\uE372\uE373";
			var btn4 = $"{CM.Glyphs.LeftBracket} {btn4Text} {CM.Glyphs.RightBracket}";

			// Note extra spaces to make dialog even wider
			//                         123456                           123456
			var buttonRow = $"{CM.Glyphs.VLine}      {btn1} {btn2} {btn3} {btn4}      {CM.Glyphs.VLine}";
			var width = buttonRow.GetColumns ();
			d.SetBufferSize (width, 3);

			// Default - Center
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Justify
			buttonRow = $"{CM.Glyphs.VLine}{btn1}     {btn2}     {btn3}     {btn4}{CM.Glyphs.VLine}";
			Assert.Equal (width, buttonRow.GetColumns ());
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Right
			buttonRow = $"{CM.Glyphs.VLine}            {btn1} {btn2} {btn3} {btn4}{CM.Glyphs.VLine}";
			Assert.Equal (width, buttonRow.GetColumns ());
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Left
			buttonRow = $"{CM.Glyphs.VLine}{btn1} {btn2} {btn3} {btn4}            {CM.Glyphs.VLine}";
			Assert.Equal (width, buttonRow.GetColumns ());
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);
		}

		[Fact]
		[AutoInitShutdown]
		public void ButtonAlignment_Four_WideOdd ()
		{
			RunState runstate = null;

			var d = (FakeDriver)Application.Driver;

			var title = "1234";

			// E.g "|[ yes ][ no ][ maybe ]|"
			var btn1Text = "really long button 1";
			var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
			var btn2Text = "really long button 2";
			var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";
			var btn3Text = "really long button 3";
			var btn3 = $"{CM.Glyphs.LeftBracket} {btn3Text} {CM.Glyphs.RightBracket}";
			var btn4Text = "really long button 44"; // 44 is intentional to make length different than rest
			var btn4 = $"{CM.Glyphs.LeftBracket} {btn4Text} {CM.Glyphs.RightBracket}";

			// Note extra spaces to make dialog even wider
			//                         123456                          1234567
			var buttonRow = $"{CM.Glyphs.VLine}      {btn1} {btn2} {btn3} {btn4}      {CM.Glyphs.VLine}";
			var width = buttonRow.Length;
			d.SetBufferSize (buttonRow.Length, 1);

			// Default - Center
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Justify
			buttonRow = $"{CM.Glyphs.VLine}{btn1}     {btn2}     {btn3}     {btn4}{CM.Glyphs.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Right
			buttonRow = $"{CM.Glyphs.VLine}            {btn1} {btn2} {btn3} {btn4}{CM.Glyphs.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Left
			buttonRow = $"{CM.Glyphs.VLine}{btn1} {btn2} {btn3} {btn4}            {CM.Glyphs.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);
		}

		[Fact]
		[AutoInitShutdown]
		public void Zero_Buttons_Works ()
		{
			RunState runstate = null;

			var d = (FakeDriver)Application.Driver;

			var title = "1234";

			var buttonRow = $"{CM.Glyphs.VLine}        {CM.Glyphs.VLine}";
			var width = buttonRow.Length;
			d.SetBufferSize (buttonRow.Length, 3);

			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, null);
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);

			Application.End (runstate);
		}

		[Fact]
		[AutoInitShutdown]
		public void One_Button_Works ()
		{
			RunState runstate = null;

			var d = (FakeDriver)Application.Driver;

			var title = "";
			var btnText = "ok";
			var buttonRow = $"{CM.Glyphs.VLine}   {CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}   {CM.Glyphs.VLine}";

			var width = buttonRow.Length;
			d.SetBufferSize (buttonRow.Length, 10);

			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btnText));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);
		}

		[Fact]
		[AutoInitShutdown]
		public void Add_Button_Works ()
		{
			RunState runstate = null;

			var d = (FakeDriver)Application.Driver;

			var title = "1234";
			var btn1Text = "yes";
			var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
			var btn2Text = "no";
			var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";

			// We test with one button first, but do this to get the width right for 2
			var width = $@"{CM.Glyphs.VLine} {btn1} {btn2} {CM.Glyphs.VLine}".Length;
			d.SetBufferSize (width, 1);

			// Default (center)
			var dlg = new Dialog (new Button (btn1Text)) { Title = title, Width = width, Height = 1, ButtonAlignment = Dialog.ButtonAlignments.Center };
			// Create with no top or bottom border to simplify testing button layout (no need to account for title etc..)
			dlg.Border.Thickness = new Thickness (1, 0, 1, 0);
			runstate = Application.Begin (dlg);
			var buttonRow = $"{CM.Glyphs.VLine}     {btn1}    {CM.Glyphs.VLine}";
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);

			// Now add a second button
			buttonRow = $"{CM.Glyphs.VLine} {btn1} {btn2} {CM.Glyphs.VLine}";
			dlg.AddButton (new Button (btn2Text));
			bool first = false;
			Application.RunMainLoopIteration (ref runstate, true, ref first);
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Justify
			dlg = new Dialog (new Button (btn1Text)) { Title = title, Width = width, Height = 1, ButtonAlignment = Dialog.ButtonAlignments.Justify };
			// Create with no top or bottom border to simplify testing button layout (no need to account for title etc..)
			dlg.Border.Thickness = new Thickness (1, 0, 1, 0);
			runstate = Application.Begin (dlg);
			buttonRow = $"{CM.Glyphs.VLine}         {btn1}{CM.Glyphs.VLine}";
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);

			// Now add a second button
			buttonRow = $"{CM.Glyphs.VLine}{btn1}   {btn2}{CM.Glyphs.VLine}";
			dlg.AddButton (new Button (btn2Text));
			first = false;
			Application.RunMainLoopIteration (ref runstate, true, ref first);
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Right
			dlg = new Dialog (new Button (btn1Text)) { Title = title, Width = width, Height = 1, ButtonAlignment = Dialog.ButtonAlignments.Right };
			// Create with no top or bottom border to simplify testing button layout (no need to account for title etc..)
			dlg.Border.Thickness = new Thickness (1, 0, 1, 0);
			runstate = Application.Begin (dlg);
			buttonRow = $"{CM.Glyphs.VLine}{new string (' ', width - btn1.Length - 2)}{btn1}{CM.Glyphs.VLine}";
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);

			// Now add a second button
			buttonRow = $"{CM.Glyphs.VLine}  {btn1} {btn2}{CM.Glyphs.VLine}";
			dlg.AddButton (new Button (btn2Text));
			first = false;
			Application.RunMainLoopIteration (ref runstate, true, ref first);
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);

			// Left
			dlg = new Dialog (new Button (btn1Text)) { Title = title, Width = width, Height = 1, ButtonAlignment = Dialog.ButtonAlignments.Left };
			// Create with no top or bottom border to simplify testing button layout (no need to account for title etc..)
			dlg.Border.Thickness = new Thickness (1, 0, 1, 0);
			runstate = Application.Begin (dlg);
			buttonRow = $"{CM.Glyphs.VLine}{btn1}{new string (' ', width - btn1.Length - 2)}{CM.Glyphs.VLine}";
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);

			// Now add a second button
			buttonRow = $"{CM.Glyphs.VLine}{btn1} {btn2}  {CM.Glyphs.VLine}";
			dlg.AddButton (new Button (btn2Text));
			first = false;
			Application.RunMainLoopIteration (ref runstate, true, ref first);
			TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
			Application.End (runstate);
		}

		[Fact]
		[AutoInitShutdown]
		public void FileDialog_FileSystemWatcher ()
		{
			for (int i = 0; i < 8; i++) {
				var fd = new FileDialog ();
				fd.Ready += (s, e) => Application.RequestStop ();
				Application.Run (fd);
			}
		}

		[Fact, AutoInitShutdown]
		public void Dialog_Opened_From_Another_Dialog ()
		{
			var btn1 = new Button ("press me 1");
			Button btn2 = null;
			Button btn3 = null;
			string expected = null;
			btn1.Clicked += (s, e) => {
				btn2 = new Button ("Show Sub");
				btn3 = new Button ("Close");
				btn3.Clicked += (s, e) => Application.RequestStop ();
				btn2.Clicked += (s, e) => { MessageBox.Query (string.Empty, "ya", "Ok"); };
				var dlg = new Dialog (btn2, btn3);

				Application.Run (dlg);
			};
			var btn = $"{CM.Glyphs.LeftBracket}{CM.Glyphs.LeftDefaultIndicator} Ok {CM.Glyphs.RightDefaultIndicator}{CM.Glyphs.RightBracket}";

			var iterations = -1;
			Application.Iteration += () => {
				iterations++;
				if (iterations == 0) {
					Assert.True (btn1.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
				} else if (iterations == 1) {
					expected = @$"
      ┌──────────────────────────────────────────────────────────────────┐
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                      {CM.Glyphs.LeftBracket} Show Sub {CM.Glyphs.RightBracket} {CM.Glyphs.LeftBracket} Close {CM.Glyphs.RightBracket}                      │
      └──────────────────────────────────────────────────────────────────┘";
					TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

					Assert.True (btn2.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
				} else if (iterations == 2) {
					TestHelpers.AssertDriverContentsWithFrameAre (@$"
      ┌──────────────────────────────────────────────────────────────────┐
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │         ┌──────────────────────────────────────────────┐         │
      │         │                      ya                      │         │
      │         │                                              │         │
      │         │                   {btn}                   │         │
      │         └──────────────────────────────────────────────┘         │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                      {CM.Glyphs.LeftBracket} Show Sub {CM.Glyphs.RightBracket} {CM.Glyphs.LeftBracket} Close {CM.Glyphs.RightBracket}                      │
      └──────────────────────────────────────────────────────────────────┘", output);

					Assert.True (Application.Current.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
				} else if (iterations == 3) {
					TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

					Assert.True (btn3.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
				} else if (iterations == 4) {
					TestHelpers.AssertDriverContentsWithFrameAre ("", output);

					Application.RequestStop ();
				}
			};

			Application.Run ();
			Application.Shutdown ();

			Assert.Equal (4, iterations);
		}

		[Fact, AutoInitShutdown]
		public void Dialog_In_Window_With_Size_One_Button_Aligns ()
		{
			((FakeDriver)Application.Driver).SetBufferSize (20, 5);

			var win = new Window ();

			int iterations = 0;
			Application.Iteration += () => {
				if (++iterations > 2) {
					Application.RequestStop ();
				}
			};
			var btn = $"{CM.Glyphs.LeftBracket} Ok {CM.Glyphs.RightBracket}";

			win.Loaded += (s, a) => {
				var dlg = new Dialog (new Button ("Ok")) { Width = 18, Height = 3 };

				dlg.Loaded += (s, a) => {
					Application.Refresh ();
					var expected = @$"
┌──────────────────┐
│┌────────────────┐│
││     {btn}     ││
│└────────────────┘│
└──────────────────┘";
					_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
				};

				Application.Run (dlg);
			};
			Application.Run (win);
		}

		//		[Theory, AutoInitShutdown]
		//		[InlineData (5)]
		//		//[InlineData (6)]
		//		//[InlineData (7)]
		//		//[InlineData (8)]
		//		//[InlineData (9)]
		//		public void Dialog_In_Window_Without_Size_One_Button_Aligns (int height)
		//		{
		//			((FakeDriver)Application.Driver).SetBufferSize (20, height);
		//			var win = new Window ();

		//			Application.Iteration += () => {
		//				var dlg = new Dialog ("Test", new Button ("Ok"));

		//				dlg.LayoutComplete += (s, a) => {
		//					Application.Refresh ();
		//					// BUGBUG: This seems wrong; is it a bug in Dim.Percent(85)??
		//					var expected = @"
		//┌┌┤Test├─────────┐─┐
		//││               │ │
		//││     [ Ok ]    │ │
		//│└───────────────┘ │
		//└──────────────────┘";
		//					_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

		//					dlg.RequestStop ();
		//					win.RequestStop ();
		//				};

		//				Application.Run (dlg);
		//			};

		//			Application.Run (win);
		//			Application.Shutdown ();
		//		}

		[Fact, AutoInitShutdown]
		public void Dialog_In_Window_With_TexxtField_And_Button_AnchorEnd ()
		{
			((FakeDriver)Application.Driver).SetBufferSize (20, 5);

			var win = new Window ();

			int iterations = 0;
			Application.Iteration += () => {
				if (++iterations > 2) {
					Application.RequestStop ();
				}
			};
			var b = $"{CM.Glyphs.LeftBracket} Ok {CM.Glyphs.RightBracket}";

			win.Loaded += (s, a) => {
				var dlg = new Dialog () { Width = 18, Height = 3 };
				Button btn = null;
				btn = new Button ("Ok") {
					X = Pos.AnchorEnd () - Pos.Function (Btn_Width)
				};
				int Btn_Width ()
				{
					return (btn?.Bounds.Width) ?? 0;
				}
				var tf = new TextField ("01234567890123456789") {
					Width = Dim.Fill (1) - Dim.Function (Btn_Width)
				};

				dlg.Loaded += (s, a) => {
					Application.Refresh ();
					Assert.Equal (new Rect (10, 0, 6, 1), btn.Frame);
					Assert.Equal (new Rect (0, 0, 6, 1), btn.Bounds);
					var expected = @$"
┌──────────────────┐
│┌────────────────┐│
││23456789  {b}││
│└────────────────┘│
└──────────────────┘";
					_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

					dlg.SetNeedsLayout ();
					dlg.LayoutSubviews ();
					Application.Refresh ();
					Assert.Equal (new Rect (10, 0, 6, 1), btn.Frame);
					Assert.Equal (new Rect (0, 0, 6, 1), btn.Bounds);
					expected = @$"
┌──────────────────┐
│┌────────────────┐│
││23456789  {b}││
│└────────────────┘│
└──────────────────┘";
					_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
				};
				dlg.Add (btn, tf);

				Application.Run (dlg);
			};
			Application.Run (win);
		}
	}
}