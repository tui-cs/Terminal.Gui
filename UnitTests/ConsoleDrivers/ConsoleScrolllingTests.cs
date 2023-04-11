using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.DriverTests {
	public class ConsoleScrollingTests {
		readonly ITestOutputHelper output;

		public ConsoleScrollingTests (ITestOutputHelper output)
		{
			this.output = output;
		}


		[Theory]
		[InlineData (typeof (FakeDriver))]
		public void EnableConsoleScrolling_Is_False_Left_And_Top_Is_Always_Zero (Type driverType)
		{
			var driver = (FakeDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);

			Assert.False (Application.EnableConsoleScrolling);
			Assert.Equal (0, Console.WindowLeft);
			Assert.Equal (0, Console.WindowTop);

			driver.SetWindowPosition (5, 5);
			Assert.Equal (0, Console.WindowLeft);
			Assert.Equal (0, Console.WindowTop);

			Application.Shutdown ();
		}

		[Theory]
		[InlineData (typeof (FakeDriver))]
		public void EnableConsoleScrolling_Is_True_Left_Cannot_Be_Greater_Than_WindowWidth (Type driverType)
		{
			var driver = (FakeDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);

			Application.EnableConsoleScrolling = true;
			Assert.True (Application.EnableConsoleScrolling);

			driver.SetWindowPosition (81, 25);
			Assert.Equal (0, Console.WindowLeft);
			Assert.Equal (0, Console.WindowTop);

			Application.Shutdown ();
		}

		[Theory]
		[InlineData (typeof (FakeDriver))]
		public void EnableConsoleScrolling_Is_True_Left_Cannot_Be_Greater_Than_BufferWidth_Minus_WindowWidth (Type driverType)
		{
			var driver = (FakeDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);

			Application.EnableConsoleScrolling = true;
			Assert.True (Application.EnableConsoleScrolling);

			driver.SetWindowPosition (81, 25);
			Assert.Equal (0, Console.WindowLeft);
			Assert.Equal (0, Console.WindowTop);

			// MockDriver will now be sets to 120x25
			driver.SetBufferSize (120, 25);
			Assert.Equal (120, Application.Driver.Cols);
			Assert.Equal (25, Application.Driver.Rows);
			Assert.Equal (120, Console.BufferWidth);
			Assert.Equal (25, Console.BufferHeight);
			Assert.Equal (80, Console.WindowWidth);
			Assert.Equal (25, Console.WindowHeight);
			driver.SetWindowPosition (121, 25);
			Assert.Equal (40, Console.WindowLeft);
			Assert.Equal (0, Console.WindowTop);

			driver.SetWindowSize (90, 25);
			Assert.Equal (120, Application.Driver.Cols);
			Assert.Equal (25, Application.Driver.Rows);
			Assert.Equal (120, Console.BufferWidth);
			Assert.Equal (25, Console.BufferHeight);
			Assert.Equal (90, Console.WindowWidth);
			Assert.Equal (25, Console.WindowHeight);
			driver.SetWindowPosition (121, 25);
			Assert.Equal (30, Console.WindowLeft);
			Assert.Equal (0, Console.WindowTop);

			Application.Shutdown ();
		}

		[Theory]
		[InlineData (typeof (FakeDriver))]
		public void EnableConsoleScrolling_Is_True_Top_Cannot_Be_Greater_Than_WindowHeight (Type driverType)
		{
			var driver = (FakeDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);

			Application.EnableConsoleScrolling = true;
			Assert.True (Application.EnableConsoleScrolling);

			driver.SetWindowPosition (80, 26);
			Assert.Equal (0, Console.WindowLeft);
			Assert.Equal (0, Console.WindowTop);

			Application.Shutdown ();
		}

		[Theory]
		[InlineData (typeof (FakeDriver))]
		public void EnableConsoleScrolling_Is_True_Top_Cannot_Be_Greater_Than_BufferHeight_Minus_WindowHeight (Type driverType)
		{
			var driver = (FakeDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);

			Application.EnableConsoleScrolling = true;
			Assert.True (Application.EnableConsoleScrolling);

			driver.SetWindowPosition (80, 26);
			Assert.Equal (0, Console.WindowLeft);
			Assert.Equal (0, Console.WindowTop);

			// MockDriver will now be sets to 80x40
			driver.SetBufferSize (80, 40);
			Assert.Equal (80, Application.Driver.Cols);
			Assert.Equal (40, Application.Driver.Rows);
			Assert.Equal (80, Console.BufferWidth);
			Assert.Equal (40, Console.BufferHeight);
			Assert.Equal (80, Console.WindowWidth);
			Assert.Equal (25, Console.WindowHeight);
			Assert.Equal (0, Console.WindowLeft);
			Assert.Equal (0, Console.WindowTop);
			driver.SetWindowPosition (80, 40);
			Assert.Equal (0, Console.WindowLeft);
			Assert.Equal (15, Console.WindowTop);

			driver.SetWindowSize (80, 20);
			Assert.Equal (80, Application.Driver.Cols);
			Assert.Equal (40, Application.Driver.Rows);
			Assert.Equal (80, Console.BufferWidth);
			Assert.Equal (40, Console.BufferHeight);
			Assert.Equal (80, Console.WindowWidth);
			Assert.Equal (20, Console.WindowHeight);
			Assert.Equal (0, Console.WindowLeft);
			Assert.Equal (15, Console.WindowTop);
			driver.SetWindowPosition (80, 41);
			Assert.Equal (0, Console.WindowLeft);
			Assert.Equal (20, Console.WindowTop);

			Application.Shutdown ();
		}
	}
}
