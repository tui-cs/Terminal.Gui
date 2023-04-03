using Xunit;
using Terminal.Gui.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using static Terminal.Gui.Configuration.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests {
	public class ThemeTests {
		public static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions () {
			Converters = {
				new AttributeJsonConverter (),
				new ColorJsonConverter ()
				}
		};

		[Fact]
		public void TestApply_UpdatesColors ()
		{
			// Arrange
			ConfigurationManager.Reset ();

			Assert.False (Colors.ColorSchemes.ContainsKey ("test"));

			var theme = new ThemeScope ();
			Assert.NotEmpty (theme);

			Themes.Add ("testTheme", theme);

			var colorScheme = new ColorScheme { Normal = new Attribute (Color.Red, Color.Green) };

			theme ["ColorSchemes"].PropertyValue = new Dictionary<string, ColorScheme> () {
				{ "test",  colorScheme }
			};

			Assert.Equal (Color.Red, ((Dictionary<string, ColorScheme>)theme ["ColorSchemes"].PropertyValue) ["test"].Normal.Foreground);
			Assert.Equal (Color.Green, ((Dictionary<string, ColorScheme>)theme ["ColorSchemes"].PropertyValue) ["test"].Normal.Background);

			// Act
			Themes.Theme = "testTheme";
			Themes! [ThemeManager.SelectedTheme]!.Apply ();

			// Assert
			var updatedScheme = Colors.ColorSchemes ["test"];
			Assert.Equal (Color.Red, updatedScheme.Normal.Foreground);
			Assert.Equal (Color.Green, updatedScheme.Normal.Background);
		}

		[Fact]
		public void TestApply ()
		{
			ConfigurationManager.Reset ();

			var theme = new ThemeScope ();
			Assert.NotEmpty (theme);

			Themes.Add ("testTheme", theme);

			Assert.True (Dialog.DefaultBorder.Effect3D);
			Assert.Equal (typeof (Border), theme ["Dialog.DefaultBorder"].PropertyInfo.PropertyType);
			theme ["Dialog.DefaultBorder"].PropertyValue = new Border () { Effect3D = false }; // default is true

			Themes.Theme = "testTheme";
			Themes! [ThemeManager.SelectedTheme]!.Apply ();

			Assert.False (Dialog.DefaultBorder.Effect3D);
		}

		[Fact]
		public void TestUpdatFrom_Change ()
		{
			// arrange
			ConfigurationManager.Reset ();

			var theme = new ThemeScope ();
			Assert.NotEmpty (theme);

			var colorScheme = new ColorScheme {
				// note: ColorScheme's can't be partial; default for each attribute
				// is always White/Black
				Normal = new Attribute (Color.Red, Color.Green),
				Focus = new Attribute (Color.Cyan, Color.BrightCyan),
				HotNormal = new Attribute (Color.Brown, Color.BrightYellow),
				HotFocus = new Attribute (Color.Green, Color.BrightGreen),
				Disabled = new Attribute (Color.Gray, Color.DarkGray),
			};
			theme ["ColorSchemes"].PropertyValue = Colors.Create ();
			((Dictionary<string, ColorScheme>)theme ["ColorSchemes"].PropertyValue) ["test"] = colorScheme;

			var colorSchemes = (Dictionary<string, ColorScheme>)theme ["ColorSchemes"].PropertyValue;
			Assert.Equal (colorScheme.Normal, colorSchemes ["Test"].Normal);
			Assert.Equal (colorScheme.Focus, colorSchemes ["Test"].Focus);

			// Change just Normal
			var newTheme = new ThemeScope ();
			var newColorScheme = new ColorScheme {
				Normal = new Attribute (Color.Blue, Color.BrightBlue),
				
				Focus = colorScheme.Focus,
				HotNormal =colorScheme.HotNormal,
				HotFocus = colorScheme.HotFocus,
				Disabled = colorScheme.Disabled,
			};
			newTheme ["ColorSchemes"].PropertyValue = Colors.Create ();
			((Dictionary<string, ColorScheme>)newTheme ["ColorSchemes"].PropertyValue) ["test"] = newColorScheme;

			// Act
			theme.Update (newTheme);

			// Assert
			colorSchemes = (Dictionary<string, ColorScheme>)theme ["ColorSchemes"].PropertyValue;
			// Normal should have changed
			Assert.Equal (Color.Blue, colorSchemes ["Test"].Normal.Foreground);
			Assert.Equal (Color.BrightBlue, colorSchemes ["Test"].Normal.Background);
			Assert.Equal (Color.Cyan, colorSchemes ["Test"].Focus.Foreground);
			Assert.Equal (Color.BrightCyan, colorSchemes ["Test"].Focus.Background);
		}

		[Fact]
		public void TestUpdatFrom_Add ()
		{
			// arrange
			ConfigurationManager.Reset ();

			var theme = new ThemeScope ();
			Assert.NotEmpty (theme);

			theme ["ColorSchemes"].PropertyValue = Colors.Create ();
			var colorSchemes = (Dictionary<string, ColorScheme>)theme ["ColorSchemes"].PropertyValue;
			Assert.Equal (Colors.ColorSchemes.Count, colorSchemes.Count);

			var newTheme = new ThemeScope ();
			var colorScheme = new ColorScheme {
				// note: ColorScheme's can't be partial; default for each attribute
				// is always White/Black
				Normal = new Attribute (Color.Red, Color.Green),
				Focus = new Attribute (Color.Cyan, Color.BrightCyan),
				HotNormal = new Attribute (Color.Brown, Color.BrightYellow),
				HotFocus = new Attribute (Color.Green, Color.BrightGreen),
				Disabled = new Attribute (Color.Gray, Color.DarkGray),
			};

			newTheme ["ColorSchemes"].PropertyValue = Colors.Create ();
			// add a new ColorScheme to the newTheme
			((Dictionary<string, ColorScheme>)theme ["ColorSchemes"].PropertyValue) ["test"] = colorScheme;

			colorSchemes = (Dictionary<string, ColorScheme>)theme ["ColorSchemes"].PropertyValue;
			Assert.Equal (Colors.ColorSchemes.Count + 1, colorSchemes.Count);

			// Act
			theme.Update (newTheme);

			// Assert
			colorSchemes = (Dictionary<string, ColorScheme>)theme ["ColorSchemes"].PropertyValue;
			Assert.Equal (colorSchemes ["Test"].Normal, colorScheme.Normal);
			Assert.Equal (colorSchemes ["Test"].Focus, colorScheme.Focus);
		}

		[Fact]
		public void TestSerialize_RoundTrip ()
		{
			var theme = new ThemeScope ();
			theme ["Dialog.DefaultButtonAlignment"].PropertyValue = Dialog.ButtonAlignments.Right;

			var json = JsonSerializer.Serialize (theme, _jsonOptions);

			var deserialized = JsonSerializer.Deserialize<ThemeScope> (json, _jsonOptions);

			Assert.Equal (Dialog.ButtonAlignments.Right, (Dialog.ButtonAlignments)deserialized ["Dialog.DefaultButtonAlignment"].PropertyValue);
		}
	}
}