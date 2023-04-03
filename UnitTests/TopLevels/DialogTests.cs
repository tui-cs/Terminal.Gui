using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Terminal.Gui;
using Xunit;
using System.Globalization;
using Xunit.Abstractions;
using NStack;

namespace Terminal.Gui.TopLevelTests {

	public class DialogTests {
		readonly ITestOutputHelper output;

		public DialogTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		private (Application.RunState, Dialog) RunButtonTestDialog (string title, int width, Dialog.ButtonAlignments align, params Button [] btns)
		{
			var dlg = new Dialog (title, width, 3, btns) { ButtonAlignment = align };
			return (Application.Begin (dlg), dlg);
		}

		[Fact]
		[AutoInitShutdown]
		public void ButtonAlignment_One ()
		{
			var d = (FakeDriver)Application.Driver;
			Application.RunState runstate = null;

			var title = "1234";
			// E.g "|[ ok ]|"
			var btnText = "ok";
			var buttonRow = $"{d.VLine}   {d.LeftBracket} {btnText} {d.RightBracket}   {d.VLine}";
			var width = buttonRow.Length;
			var topRow = $"┌ {title} {new string (d.HLine.ToString () [0], width - title.Length - 4)}┐";
			var bottomRow = $"└{new string (d.HLine.ToString () [0], width - 2)}┘";

			d.SetBufferSize (width, 3);

			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btnText));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Justify
			buttonRow = $"{d.VLine}      {d.LeftBracket} {btnText} {d.RightBracket}{d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btnText));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Right
			buttonRow = $"{d.VLine}      {d.LeftBracket} {btnText} {d.RightBracket}{d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btnText));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Left
			buttonRow = $"{d.VLine}{d.LeftBracket} {btnText} {d.RightBracket}      {d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btnText));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);
		}

		[Fact]
		[AutoInitShutdown]
		public void ButtonAlignment_Two ()
		{
			Application.RunState runstate = null;

			var d = (FakeDriver)Application.Driver;

			var title = "1234";
			// E.g "|[ yes ][ no ]|"
			var btn1Text = "yes";
			var btn1 = $"{d.LeftBracket} {btn1Text} {d.RightBracket}";
			var btn2Text = "no";
			var btn2 = $"{d.LeftBracket} {btn2Text} {d.RightBracket}";

			var buttonRow = $@"{d.VLine} {btn1} {btn2} {d.VLine}";
			var width = buttonRow.Length;
			var topRow = $"┌ {title} {new string (d.HLine.ToString () [0], width - title.Length - 4)}┐";
			var bottomRow = $"└{new string (d.HLine.ToString () [0], width - 2)}┘";

			d.SetBufferSize (buttonRow.Length, 3);

			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Justify
			buttonRow = $@"{d.VLine}{btn1}   {btn2}{d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Right
			buttonRow = $@"{d.VLine}  {btn1} {btn2}{d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Left
			buttonRow = $@"{d.VLine}{btn1} {btn2}  {d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);
		}

		[Fact]
		[AutoInitShutdown]
		public void ButtonAlignment_Two_Hidden ()
		{
			Application.RunState runstate = null;
			bool firstIteration = false;

			var d = (FakeDriver)Application.Driver;

			var title = "1234";
			// E.g "|[ yes ][ no ]|"
			var btn1Text = "yes";
			var btn1 = $"{d.LeftBracket} {btn1Text} {d.RightBracket}";
			var btn2Text = "no";
			var btn2 = $"{d.LeftBracket} {btn2Text} {d.RightBracket}";

			var buttonRow = $@"{d.VLine} {btn1} {btn2} {d.VLine}";
			var width = buttonRow.Length;
			var topRow = $"┌ {title} {new string (d.HLine.ToString () [0], width - title.Length - 4)}┐";
			var bottomRow = $"└{new string (d.HLine.ToString () [0], width - 2)}┘";

			d.SetBufferSize (buttonRow.Length, 3);

			Dialog dlg = null;
			Button button1, button2;

			// Default (Center)
			button1 = new Button (btn1Text);
			button2 = new Button (btn2Text);
			(runstate, dlg) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, button1, button2);
			button1.Visible = false;
			Application.RunMainLoopIteration (ref runstate, true, ref firstIteration);
			buttonRow = $@"{d.VLine}         {btn2} {d.VLine}";
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Justify
			Assert.Equal (width, buttonRow.Length);
			button1 = new Button (btn1Text);
			button2 = new Button (btn2Text);
			(runstate, dlg) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, button1, button2);
			button1.Visible = false;
			Application.RunMainLoopIteration (ref runstate, true, ref firstIteration);
			buttonRow = $@"{d.VLine}          {btn2}{d.VLine}";
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Right
			Assert.Equal (width, buttonRow.Length);
			button1 = new Button (btn1Text);
			button2 = new Button (btn2Text);
			(runstate, dlg) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, button1, button2);
			button1.Visible = false;
			Application.RunMainLoopIteration (ref runstate, true, ref firstIteration);
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Left
			Assert.Equal (width, buttonRow.Length);
			button1 = new Button (btn1Text);
			button2 = new Button (btn2Text);
			(runstate, dlg) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, button1, button2);
			button1.Visible = false;
			Application.RunMainLoopIteration (ref runstate, true, ref firstIteration);
			buttonRow = $@"{d.VLine}        {btn2}  {d.VLine}";
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);
		}

		[Fact]
		[AutoInitShutdown]
		public void ButtonAlignment_Three ()
		{
			Application.RunState runstate = null;

			var d = (FakeDriver)Application.Driver;

			var title = "1234";
			// E.g "|[ yes ][ no ][ maybe ]|"
			var btn1Text = "yes";
			var btn1 = $"{d.LeftBracket} {btn1Text} {d.RightBracket}";
			var btn2Text = "no";
			var btn2 = $"{d.LeftBracket} {btn2Text} {d.RightBracket}";
			var btn3Text = "maybe";
			var btn3 = $"{d.LeftBracket} {btn3Text} {d.RightBracket}";

			var buttonRow = $@"{d.VLine} {btn1} {btn2} {btn3} {d.VLine}";
			var width = buttonRow.Length;
			var topRow = $"┌ {title} {new string (d.HLine.ToString () [0], width - title.Length - 4)}┐";
			var bottomRow = $"└{new string (d.HLine.ToString () [0], width - 2)}┘";

			d.SetBufferSize (buttonRow.Length, 3);

			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Justify
			buttonRow = $@"{d.VLine}{btn1}  {btn2}  {btn3}{d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Right
			buttonRow = $@"{d.VLine}  {btn1} {btn2} {btn3}{d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Left
			buttonRow = $@"{d.VLine}{btn1} {btn2} {btn3}  {d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);
		}

		[Fact]
		[AutoInitShutdown]
		public void ButtonAlignment_Four ()
		{
			Application.RunState runstate = null;

			var d = (FakeDriver)Application.Driver;

			var title = "1234";

			// E.g "|[ yes ][ no ][ maybe ]|"
			var btn1Text = "yes";
			var btn1 = $"{d.LeftBracket} {btn1Text} {d.RightBracket}";
			var btn2Text = "no";
			var btn2 = $"{d.LeftBracket} {btn2Text} {d.RightBracket}";
			var btn3Text = "maybe";
			var btn3 = $"{d.LeftBracket} {btn3Text} {d.RightBracket}";
			var btn4Text = "never";
			var btn4 = $"{d.LeftBracket} {btn4Text} {d.RightBracket}";

			var buttonRow = $"{d.VLine} {btn1} {btn2} {btn3} {btn4} {d.VLine}";
			var width = buttonRow.Length;
			var topRow = $"┌ {title} {new string (d.HLine.ToString () [0], width - title.Length - 4)}┐";
			var bottomRow = $"└{new string (d.HLine.ToString () [0], width - 2)}┘";
			d.SetBufferSize (buttonRow.Length, 3);

			// Default - Center
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Justify
			buttonRow = $"{d.VLine}{btn1} {btn2}  {btn3}  {btn4}{d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Right
			buttonRow = $"{d.VLine}  {btn1} {btn2} {btn3} {btn4}{d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Left
			buttonRow = $"{d.VLine}{btn1} {btn2} {btn3} {btn4}  {d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);
		}

		[Fact]
		[AutoInitShutdown]
		public void ButtonAlignment_Four_On_Smaller_Width ()
		{
			Application.RunState runstate = null;

			var d = (FakeDriver)Application.Driver;

			var title = "1234";

			// E.g "|[ yes ][ no ][ maybe ][ never ]|"
			var btn1Text = "yes";
			var btn1 = $"{d.LeftBracket} {btn1Text} {d.RightBracket}";
			var btn2Text = "no";
			var btn2 = $"{d.LeftBracket} {btn2Text} {d.RightBracket}";
			var btn3Text = "maybe";
			var btn3 = $"{d.LeftBracket} {btn3Text} {d.RightBracket}";
			var btn4Text = "never";
			var btn4 = $"{d.LeftBracket} {btn4Text} {d.RightBracket}";

			var buttonRow = $"{d.VLine} {btn1} {btn2} {btn3} {btn4} {d.VLine}";
			var width = buttonRow.Length;
			var topRow = "34 ───────────────────────────";
			var bottomRow = "──────────────────────────────";
			d.SetBufferSize (30, 3);

			// Default - Center
			buttonRow = $"yes ] {btn2} {btn3} [ never";
			Assert.NotEqual (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Justify
			buttonRow = $"es ] {btn2}  {btn3}  [ neve";
			Assert.NotEqual (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Right
			buttonRow = $" yes ] {btn2} {btn3} [ neve";
			Assert.NotEqual (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Left
			buttonRow = $"es ] {btn2} {btn3} [ never ";
			Assert.NotEqual (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);
		}

		[Fact]
		[AutoInitShutdown]
		public void ButtonAlignment_Four_Wider ()
		{
			Application.RunState runstate = null;

			var d = (FakeDriver)Application.Driver;

			var title = "1234";

			// E.g "|[ yes ][ no ][ maybe ]|"
			var btn1Text = "yes";
			var btn1 = $"{d.LeftBracket} {btn1Text} {d.RightBracket}";
			var btn2Text = "no";
			var btn2 = $"{d.LeftBracket} {btn2Text} {d.RightBracket}";
			var btn3Text = "你你你你你"; // This is a wide char
			var btn3 = $"{d.LeftBracket} {btn3Text} {d.RightBracket}";
			// Requires a Nerd Font
			var btn4Text = "\uE36E\uE36F\uE370\uE371\uE372\uE373";
			var btn4 = $"{d.LeftBracket} {btn4Text} {d.RightBracket}";

			// Note extra spaces to make dialog even wider
			//                         12345                           123456
			var buttonRow = $"{d.VLine}     {btn1} {btn2} {btn3} {btn4}      {d.VLine}";
			var width = ustring.Make (buttonRow).ConsoleWidth;
			var topRow = $"┌ {title} {new string (d.HLine.ToString () [0], width - title.Length - 4)}┐";
			var bottomRow = $"└{new string (d.HLine.ToString () [0], width - 2)}┘";
			d.SetBufferSize (width, 3);

			// Default - Center
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Justify
			buttonRow = $"{d.VLine}{btn1}    {btn2}     {btn3}     {btn4}{d.VLine}";
			Assert.Equal (width, ustring.Make (buttonRow).ConsoleWidth);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Right
			buttonRow = $"{d.VLine}           {btn1} {btn2} {btn3} {btn4}{d.VLine}";
			Assert.Equal (width, ustring.Make (buttonRow).ConsoleWidth);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Left
			buttonRow = $"{d.VLine}{btn1} {btn2} {btn3} {btn4}           {d.VLine}";
			Assert.Equal (width, ustring.Make (buttonRow).ConsoleWidth);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);
		}

		[Fact]
		[AutoInitShutdown]
		public void ButtonAlignment_Four_WideOdd ()
		{
			Application.RunState runstate = null;

			var d = (FakeDriver)Application.Driver;

			var title = "1234";

			// E.g "|[ yes ][ no ][ maybe ]|"
			var btn1Text = "really long button 1";
			var btn1 = $"{d.LeftBracket} {btn1Text} {d.RightBracket}";
			var btn2Text = "really long button 2";
			var btn2 = $"{d.LeftBracket} {btn2Text} {d.RightBracket}";
			var btn3Text = "really long button 3";
			var btn3 = $"{d.LeftBracket} {btn3Text} {d.RightBracket}";
			var btn4Text = "really long button 44"; // 44 is intentional to make length different than rest
			var btn4 = $"{d.LeftBracket} {btn4Text} {d.RightBracket}";

			// Note extra spaces to make dialog even wider
			//                         12345                          123456
			var buttonRow = $"{d.VLine}     {btn1} {btn2} {btn3} {btn4}      {d.VLine}";
			var width = buttonRow.Length;
			var topRow = $"┌ {title} {new string (d.HLine.ToString () [0], width - title.Length - 4)}┐";
			var bottomRow = $"└{new string (d.HLine.ToString () [0], width - 2)}┘";
			d.SetBufferSize (buttonRow.Length, 3);

			// Default - Center
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Justify
			buttonRow = $"{d.VLine}{btn1}    {btn2}     {btn3}     {btn4}{d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Right
			buttonRow = $"{d.VLine}           {btn1} {btn2} {btn3} {btn4}{d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Left
			buttonRow = $"{d.VLine}{btn1} {btn2} {btn3} {btn4}           {d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);
		}

		[Fact]
		[AutoInitShutdown]
		public void Zero_Buttons_Works ()
		{
			Application.RunState runstate = null;

			var d = (FakeDriver)Application.Driver;

			var title = "1234";

			var buttonRow = $"{d.VLine}        {d.VLine}";
			var width = buttonRow.Length;
			var topRow = $"┌ {title} {new string (d.HLine.ToString () [0], width - title.Length - 4)}┐";
			var bottomRow = $"└{new string (d.HLine.ToString () [0], width - 2)}┘";
			d.SetBufferSize (buttonRow.Length, 3);

			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, null);
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			Application.End (runstate);
		}

		[Fact]
		[AutoInitShutdown]
		public void One_Button_Works ()
		{
			Application.RunState runstate = null;

			var d = (FakeDriver)Application.Driver;

			var title = "1234";
			var btnText = "ok";
			var buttonRow = $"{d.VLine}   {d.LeftBracket} {btnText} {d.RightBracket}   {d.VLine}";

			var width = buttonRow.Length;
			var topRow = $"┌ {title} {new string (d.HLine.ToString () [0], width - title.Length - 4)}┐";
			var bottomRow = $"└{new string (d.HLine.ToString () [0], width - 2)}┘";
			d.SetBufferSize (buttonRow.Length, 3);

			(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btnText));
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);
		}

		[Fact]
		[AutoInitShutdown]
		public void Add_Button_Works ()
		{
			Application.RunState runstate = null;

			var d = (FakeDriver)Application.Driver;

			var title = "1234";
			var btn1Text = "yes";
			var btn1 = $"{d.LeftBracket} {btn1Text} {d.RightBracket}";
			var btn2Text = "no";
			var btn2 = $"{d.LeftBracket} {btn2Text} {d.RightBracket}";

			// We test with one button first, but do this to get the width right for 2
			var width = $@"{d.VLine} {btn1} {btn2} {d.VLine}".Length;
			d.SetBufferSize (width, 3);

			var topRow = $"{d.ULCorner} {title} {new string (d.HLine.ToString () [0], width - title.Length - 4)}{d.URCorner}";
			var bottomRow = $"{d.LLCorner}{new string (d.HLine.ToString () [0], width - 2)}{d.LRCorner}";

			// Default (center)
			var dlg = new Dialog (title, width, 3, new Button (btn1Text)) { ButtonAlignment = Dialog.ButtonAlignments.Center };
			runstate = Application.Begin (dlg);
			var buttonRow = $"{d.VLine}    {btn1}     {d.VLine}";
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			// Now add a second button
			buttonRow = $"{d.VLine} {btn1} {btn2} {d.VLine}";
			dlg.AddButton (new Button (btn2Text));
			bool first = false;
			Application.RunMainLoopIteration (ref runstate, true, ref first);
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Justify
			dlg = new Dialog (title, width, 3, new Button (btn1Text)) { ButtonAlignment = Dialog.ButtonAlignments.Justify };
			runstate = Application.Begin (dlg);
			buttonRow = $"{d.VLine}         {btn1}{d.VLine}";
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			// Now add a second button
			buttonRow = $"{d.VLine}{btn1}   {btn2}{d.VLine}";
			dlg.AddButton (new Button (btn2Text));
			first = false;
			Application.RunMainLoopIteration (ref runstate, true, ref first);
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Right
			dlg = new Dialog (title, width, 3, new Button (btn1Text)) { ButtonAlignment = Dialog.ButtonAlignments.Right };
			runstate = Application.Begin (dlg);
			buttonRow = $"{d.VLine}{new string (' ', width - btn1.Length - 2)}{btn1}{d.VLine}";
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			// Now add a second button
			buttonRow = $"{d.VLine}  {btn1} {btn2}{d.VLine}";
			dlg.AddButton (new Button (btn2Text));
			first = false;
			Application.RunMainLoopIteration (ref runstate, true, ref first);
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);

			// Left
			dlg = new Dialog (title, width, 3, new Button (btn1Text)) { ButtonAlignment = Dialog.ButtonAlignments.Left };
			runstate = Application.Begin (dlg);
			buttonRow = $"{d.VLine}{btn1}{new string (' ', width - btn1.Length - 2)}{d.VLine}";
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			// Now add a second button
			buttonRow = $"{d.VLine}{btn1} {btn2}  {d.VLine}";
			dlg.AddButton (new Button (btn2Text));
			first = false;
			Application.RunMainLoopIteration (ref runstate, true, ref first);
			TestHelpers.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
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
				btn2.Clicked += (s, e) => { MessageBox.Query ("hey", "ya", "ok"); };
				var dlg = new Dialog ("Hey", btn2, btn3);

				Application.Run (dlg);
			};

			var iterations = -1;
			Application.Iteration += () => {
				iterations++;
				if (iterations == 0) {
					Assert.True (btn1.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
				} else if (iterations == 1) {
					expected = @"
      ┌ Hey ─────────────────────────────────────────────────────────────┐
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
      │                      [ Show Sub ] [ Close ]                      │
      └──────────────────────────────────────────────────────────────────┘";
					TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

					Assert.True (btn2.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
				} else if (iterations == 2) {
					TestHelpers.AssertDriverContentsWithFrameAre (@"
      ┌ Hey ─────────────────────────────────────────────────────────────┐
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │         ┌ hey ─────────────────────────────────────────┐         │
      │         │                      ya                      │         │
      │         │                                              │         │
      │         │                   [◦ ok ◦]                   │         │
      │         └──────────────────────────────────────────────┘         │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                                                                  │
      │                      [ Show Sub ] [ Close ]                      │
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
	}
}