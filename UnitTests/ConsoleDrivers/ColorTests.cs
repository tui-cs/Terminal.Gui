using System;
using Xunit;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.DriverTests {
	public class ColorTests {

		[Theory]
		[InlineData (typeof (FakeDriver))]
		//[InlineData (typeof (NetDriver))]
		//[InlineData (typeof (CursesDriver))]
		//[InlineData (typeof (WindowsDriver))]
		public void SetColors_Changes_Colors (Type driverType)
		{
			var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);
			driver.Init (() => { });
			Assert.Equal (ConsoleColor.Gray, Console.ForegroundColor);
			Assert.Equal (ConsoleColor.Black, Console.BackgroundColor);

			Console.ForegroundColor = ConsoleColor.Red;
			Assert.Equal (ConsoleColor.Red, Console.ForegroundColor);

			Console.BackgroundColor = ConsoleColor.Green;
			Assert.Equal (ConsoleColor.Green, Console.BackgroundColor);

			Console.ResetColor ();
			Assert.Equal (ConsoleColor.Gray, Console.ForegroundColor);
			Assert.Equal (ConsoleColor.Black, Console.BackgroundColor);
			driver.End ();

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact, AutoInitShutdown]
		public void ColorScheme_New ()
		{
			var scheme = new ColorScheme ();
			var lbl = new Label ();
			lbl.ColorScheme = scheme;
			lbl.Redraw (lbl.Bounds);
		}

		[Fact]
		public void TestAllColors ()
		{
			var colors = System.Enum.GetValues (typeof (Color));
			Attribute [] attrs = new Attribute [colors.Length];

			int idx = 0;
			foreach (Color bg in colors) {
				attrs [idx] = new Attribute (bg, colors.Length - 1 - bg);
				idx++;
			}
			Assert.Equal (16, attrs.Length);
			Assert.Equal (new Attribute (Color.Black, Color.White), attrs [0]);
			Assert.Equal (new Attribute (Color.Blue, Color.BrightYellow), attrs [1]);
			Assert.Equal (new Attribute (Color.Green, Color.BrightMagenta), attrs [2]);
			Assert.Equal (new Attribute (Color.Cyan, Color.BrightRed), attrs [3]);
			Assert.Equal (new Attribute (Color.Red, Color.BrightCyan), attrs [4]);
			Assert.Equal (new Attribute (Color.Magenta, Color.BrightGreen), attrs [5]);
			Assert.Equal (new Attribute (Color.Brown, Color.BrightBlue), attrs [6]);
			Assert.Equal (new Attribute (Color.Gray, Color.DarkGray), attrs [7]);
			Assert.Equal (new Attribute (Color.DarkGray, Color.Gray), attrs [8]);
			Assert.Equal (new Attribute (Color.BrightBlue, Color.Brown), attrs [9]);
			Assert.Equal (new Attribute (Color.BrightGreen, Color.Magenta), attrs [10]);
			Assert.Equal (new Attribute (Color.BrightCyan, Color.Red), attrs [11]);
			Assert.Equal (new Attribute (Color.BrightRed, Color.Cyan), attrs [12]);
			Assert.Equal (new Attribute (Color.BrightMagenta, Color.Green), attrs [13]);
			Assert.Equal (new Attribute (Color.BrightYellow, Color.Blue), attrs [14]);
			Assert.Equal (new Attribute (Color.White, Color.Black), attrs [^1]);
		}
	}
}