using Xunit;
using Terminal.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using static Terminal.Gui.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests {
	public class ThemeScopeTests {
		public static readonly JsonSerializerOptions _jsonOptions = new() {
			Converters = {
				//new AttributeJsonConverter (),
				//new ColorJsonConverter ()
				}
		};


		[Fact]
		public void ThemeManager_ClassMethodsWork ()
		{
			ConfigurationManager.Reset ();
			Assert.Equal (ConfigurationManager.ThemeManager.Instance, ConfigurationManager.Themes);
			Assert.NotEmpty (ConfigurationManager.ThemeManager.Themes);

			ConfigurationManager.ThemeManager.SelectedTheme = "foo";
			Assert.Equal ("foo", ConfigurationManager.ThemeManager.SelectedTheme);
			ConfigurationManager.ThemeManager.Reset ();
			Assert.Equal (string.Empty, ConfigurationManager.ThemeManager.SelectedTheme);

			Assert.Empty (ConfigurationManager.ThemeManager.Themes);
		}

		[Fact]
		public void AllThemesPresent()
		{
			ConfigurationManager.Reset ();
			Assert.True (ConfigurationManager.Themes.ContainsKey ("Default"));
			Assert.True (ConfigurationManager.Themes.ContainsKey ("Dark"));
			Assert.True (ConfigurationManager.Themes.ContainsKey ("Light"));
		}

		[Fact]
		public void GetHardCodedDefaults_ShouldSetProperties ()
		{
			ConfigurationManager.Reset ();
			ConfigurationManager.GetHardCodedDefaults ();
			Assert.NotEmpty (ConfigurationManager.Themes);
			Assert.Equal ("Default", ConfigurationManager.Themes.Theme);
		}

		[Fact, AutoInitShutdown]
		public void Apply_ShouldApplyUpdatedProperties ()
		{
			ConfigurationManager.Reset ();
			Assert.NotEmpty (ConfigurationManager.Themes);
			Assert.Equal (Dialog.ButtonAlignments.Center, Dialog.DefaultButtonAlignment);

			ConfigurationManager.Themes ["Default"] ["Dialog.DefaultButtonAlignment"].PropertyValue = Dialog.ButtonAlignments.Right;

			ConfigurationManager.ThemeManager.Themes! [ThemeManager.SelectedTheme]!.Apply ();
			Assert.Equal (Dialog.ButtonAlignments.Right, Dialog.DefaultButtonAlignment);
		}
		

		[Fact]
		public void TestSerialize_RoundTrip ()
		{
			ConfigurationManager.Reset ();

			var initial = ConfigurationManager.ThemeManager.Themes;
			
			var serialized = JsonSerializer.Serialize<IDictionary<string, ThemeScope>> (ConfigurationManager.Themes, _jsonOptions);
			var deserialized = JsonSerializer.Deserialize<IDictionary<string, ThemeScope>> (serialized, _jsonOptions);

			Assert.NotEqual (initial, deserialized);
			Assert.Equal (deserialized.Count, initial.Count);
		}
	}
}