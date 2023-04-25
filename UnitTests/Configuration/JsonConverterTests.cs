using Xunit;
using Terminal.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Attribute = Terminal.Gui.Attribute;

namespace Terminal.Gui.ConfigurationTests {
	public class ColorJsonConverterTests {

		[Theory]
		[InlineData ("Black", Color.Black)]
		[InlineData ("Blue", Color.Blue)]
		[InlineData ("BrightBlue", Color.BrightBlue)]
		[InlineData ("BrightCyan", Color.BrightCyan)]
		[InlineData ("BrightGreen", Color.BrightGreen)]
		[InlineData ("BrightMagenta", Color.BrightMagenta)]
		[InlineData ("BrightRed", Color.BrightRed)]
		[InlineData ("BrightYellow", Color.BrightYellow)]
		[InlineData ("Brown", Color.Brown)]
		[InlineData ("Cyan", Color.Cyan)]
		[InlineData ("DarkGray", Color.DarkGray)]
		[InlineData ("Gray", Color.Gray)]
		[InlineData ("Green", Color.Green)]
		[InlineData ("Magenta", Color.Magenta)]
		[InlineData ("Red", Color.Red)]
		[InlineData ("White", Color.White)]
		public void TestColorDeserializationFromHumanReadableColorNames (string colorName, Color expectedColor)
		{
			// Arrange
			string json = $"\"{colorName}\"";

			// Act
			Color actualColor = JsonSerializer.Deserialize<Color> (json, ConfigurationManagerTests._jsonOptions);

			// Assert
			Assert.Equal (expectedColor, actualColor);
		}

		[Theory]
		[InlineData (Color.Black, "Black")]
		[InlineData (Color.Blue, "Blue")]
		[InlineData (Color.Green, "Green")]
		[InlineData (Color.Cyan, "Cyan")]
		[InlineData (Color.Gray, "Gray")]
		[InlineData (Color.Red, "Red")]
		[InlineData (Color.Magenta, "Magenta")]
		[InlineData (Color.Brown, "Brown")]
		[InlineData (Color.DarkGray, "DarkGray")]
		[InlineData (Color.BrightBlue, "BrightBlue")]
		[InlineData (Color.BrightGreen, "BrightGreen")]
		[InlineData (Color.BrightCyan, "BrightCyan")]
		[InlineData (Color.BrightRed, "BrightRed")]
		[InlineData (Color.BrightMagenta, "BrightMagenta")]
		[InlineData (Color.BrightYellow, "BrightYellow")]
		[InlineData (Color.White, "White")]
		public void SerializesEnumValuesAsStrings (Color color, string expectedJson)
		{
			var converter = new ColorJsonConverter ();
			var options = new JsonSerializerOptions { Converters = { converter } };

			var serialized = JsonSerializer.Serialize (color, options);

			Assert.Equal ($"\"{expectedJson}\"", serialized);
		}

		[Fact]
		public void TestSerializeColor_Black ()
		{
			// Arrange
			var color = Color.Black;
			var expectedJson = "\"Black\"";

			// Act
			var json = JsonSerializer.Serialize (color, new JsonSerializerOptions {
				Converters = { new ColorJsonConverter () }
			});

			// Assert
			Assert.Equal (expectedJson, json);
		}

		[Fact]
		public void TestSerializeColor_BrightRed ()
		{
			// Arrange
			var color = Color.BrightRed;
			var expectedJson = "\"BrightRed\"";

			// Act
			var json = JsonSerializer.Serialize (color, new JsonSerializerOptions {
				Converters = { new ColorJsonConverter () }
			});

			// Assert
			Assert.Equal (expectedJson, json);
		}

		[Fact]
		public void TestDeserializeColor_Black ()
		{
			// Arrange
			var json = "\"Black\"";
			var expectedColor = Color.Black;

			// Act
			var color = JsonSerializer.Deserialize<Color> (json, new JsonSerializerOptions {
				Converters = { new ColorJsonConverter () }
			});

			// Assert
			Assert.Equal (expectedColor, color);
		}

		[Fact]
		public void TestDeserializeColor_BrightRed ()
		{
			// Arrange
			var json = "\"BrightRed\"";
			var expectedColor = Color.BrightRed;

			// Act
			var color = JsonSerializer.Deserialize<Color> (json, new JsonSerializerOptions {
				Converters = { new ColorJsonConverter () }
			});

			// Assert
			Assert.Equal (expectedColor, color);
		}
	}

	public class AttributeJsonConverterTests {
		[Fact, AutoInitShutdown]
		public void TestDeserialize ()
		{
			// Test deserializing from human-readable color names
			var json = "{\"Foreground\":\"Blue\",\"Background\":\"Green\"}";
			var attribute = JsonSerializer.Deserialize<Attribute> (json, ConfigurationManagerTests._jsonOptions);
			Assert.Equal (Color.Blue, attribute.Foreground);
			Assert.Equal (Color.Green, attribute.Background);

			// Test deserializing from RGB values
			json = "{\"Foreground\":\"rgb(255,0,0)\",\"Background\":\"rgb(0,255,0)\"}";
			attribute = JsonSerializer.Deserialize<Attribute> (json, ConfigurationManagerTests._jsonOptions);
			Assert.Equal (Color.BrightRed, attribute.Foreground);
			Assert.Equal (Color.BrightGreen, attribute.Background);
		}

		[Fact, AutoInitShutdown]
		public void TestSerialize ()
		{
			// Test serializing to human-readable color names
			var attribute = new Attribute (Color.Blue, Color.Green);
			var json = JsonSerializer.Serialize<Attribute> (attribute, ConfigurationManagerTests._jsonOptions);
			Assert.Equal ("{\"Foreground\":\"Blue\",\"Background\":\"Green\"}", json);
		}
	}

	public class ColorSchemeJsonConverterTests {
		//string json = @"
		//	{
		//	""ColorSchemes"": {
		//		""Base"": {
		//			""normal"": {
		//				""foreground"": ""White"",
		//				""background"": ""Blue""
		//   		            },
		//			""focus"": {
		//				""foreground"": ""Black"",
		//				""background"": ""Gray""
		//			    },
		//			""hotNormal"": {
		//				""foreground"": ""BrightCyan"",
		//				""background"": ""Blue""
		//			    },
		//			""hotFocus"": {
		//				""foreground"": ""BrightBlue"",
		//				""background"": ""Gray""
		//			    },
		//			""disabled"": {
		//				""foreground"": ""DarkGray"",
		//				""background"": ""Blue""
		//			    }
		//		}
		//		}
		//	}";
		[Fact, AutoInitShutdown]
		public void TestColorSchemesSerialization ()
		{
			// Arrange
			var expectedColorScheme = new ColorScheme {
				Normal = Attribute.Make (Color.White, Color.Blue),
				Focus = Attribute.Make (Color.Black, Color.Gray),
				HotNormal = Attribute.Make (Color.BrightCyan, Color.Blue),
				HotFocus = Attribute.Make (Color.BrightBlue, Color.Gray),
				Disabled = Attribute.Make (Color.DarkGray, Color.Blue)
			};
			var serializedColorScheme = JsonSerializer.Serialize<ColorScheme> (expectedColorScheme, ConfigurationManagerTests._jsonOptions);

			// Act
			var actualColorScheme = JsonSerializer.Deserialize<ColorScheme> (serializedColorScheme, ConfigurationManagerTests._jsonOptions);

			// Assert
			Assert.Equal (expectedColorScheme, actualColorScheme);
		}
	}

	public class KeyJsonConverterTests {
		[Theory, AutoInitShutdown]
		[InlineData (Key.A, "A")]
		[InlineData (Key.a | Key.ShiftMask, "a, ShiftMask")]
		[InlineData (Key.A | Key.CtrlMask, "A, CtrlMask")]
		[InlineData (Key.a | Key.AltMask | Key.CtrlMask, "a, CtrlMask, AltMask")]
		[InlineData (Key.Delete | Key.AltMask | Key.CtrlMask, "Delete, CtrlMask, AltMask")]
		[InlineData (Key.D4, "D4")]
		[InlineData (Key.Esc, "Esc")]
		public void TestKeyRoundTripConversion (Key key, string expectedStringTo)
		{
			// Arrange
			var options = new JsonSerializerOptions ();
			options.Converters.Add (new KeyJsonConverter ());

			// Act
			var json = JsonSerializer.Serialize (key, options);
			var deserializedKey = JsonSerializer.Deserialize<Key> (json, options);

			// Assert
			Assert.Equal (expectedStringTo, deserializedKey.ToString ());
		}
	}
}