using Xunit;
using Terminal.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Terminal.Gui.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests {
	public class SettingsScopeTests {

		[Fact]
		public void GetHardCodedDefaults_ShouldSetProperties ()
		{
			ConfigurationManager.Reset ();

			Assert.Equal (3, ((Dictionary<string, ConfigurationManager.ThemeScope>)ConfigurationManager.Settings ["Themes"].PropertyValue).Count);

			ConfigurationManager.GetHardCodedDefaults ();
			Assert.NotEmpty (ConfigurationManager.Themes);
			Assert.Equal ("Default", ConfigurationManager.Themes.Theme);

			Assert.True (ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue is Key);
			Assert.True (ConfigurationManager.Settings ["Application.AlternateForwardKey"].PropertyValue is Key);
			Assert.True (ConfigurationManager.Settings ["Application.AlternateBackwardKey"].PropertyValue is Key);
			Assert.True (ConfigurationManager.Settings ["Application.UseSystemConsole"].PropertyValue is bool);
			Assert.True (ConfigurationManager.Settings ["Application.IsMouseDisabled"].PropertyValue is bool);
			Assert.True (ConfigurationManager.Settings ["Application.EnableConsoleScrolling"].PropertyValue is bool);

			Assert.True (ConfigurationManager.Settings ["Theme"].PropertyValue is string);
			Assert.Equal ("Default", ConfigurationManager.Settings ["Theme"].PropertyValue as string);

			Assert.True (ConfigurationManager.Settings ["Themes"].PropertyValue is Dictionary<string, ConfigurationManager.ThemeScope>);
			Assert.Single (((Dictionary<string, ConfigurationManager.ThemeScope>)ConfigurationManager.Settings ["Themes"].PropertyValue));

		}

		[Fact, AutoInitShutdown]
		public void Apply_ShouldApplyProperties ()
		{
			// arrange
			Assert.Equal (Key.Q | Key.CtrlMask, (Key)ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue);
			Assert.Equal (Key.PageDown | Key.CtrlMask, (Key)ConfigurationManager.Settings ["Application.AlternateForwardKey"].PropertyValue);
			Assert.Equal (Key.PageUp | Key.CtrlMask, (Key)ConfigurationManager.Settings ["Application.AlternateBackwardKey"].PropertyValue);
			Assert.False ((bool)ConfigurationManager.Settings ["Application.UseSystemConsole"].PropertyValue);
			Assert.False ((bool)ConfigurationManager.Settings ["Application.IsMouseDisabled"].PropertyValue);
			Assert.False ((bool)ConfigurationManager.Settings ["Application.EnableConsoleScrolling"].PropertyValue);

			// act
			ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue = Key.Q;
			ConfigurationManager.Settings ["Application.AlternateForwardKey"].PropertyValue = Key.F;
			ConfigurationManager.Settings ["Application.AlternateBackwardKey"].PropertyValue = Key.B;
			ConfigurationManager.Settings ["Application.UseSystemConsole"].PropertyValue = true;
			ConfigurationManager.Settings ["Application.IsMouseDisabled"].PropertyValue = true;
			ConfigurationManager.Settings ["Application.EnableConsoleScrolling"].PropertyValue = true;

			ConfigurationManager.Settings.Apply ();

			// assert
			Assert.Equal (Key.Q, Application.QuitKey);
			Assert.Equal (Key.F, Application.AlternateForwardKey);
			Assert.Equal (Key.B, Application.AlternateBackwardKey);
			Assert.True (Application.UseSystemConsole);
			Assert.True (Application.IsMouseDisabled);
			Assert.True (Application.EnableConsoleScrolling);
		}

		[Fact, AutoInitShutdown]
		public void CopyUpdatedProperitesFrom_ShouldCopyChangedPropertiesOnly ()
		{
			ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue = Key.End;

			var updatedSettings = new SettingsScope ();

			///Don't set Quitkey
			updatedSettings["Application.AlternateForwardKey"].PropertyValue = Key.F;
			updatedSettings["Application.AlternateBackwardKey"].PropertyValue = Key.B;
			updatedSettings["Application.UseSystemConsole"].PropertyValue = true;
			updatedSettings["Application.IsMouseDisabled"].PropertyValue = true;
			updatedSettings["Application.EnableConsoleScrolling"].PropertyValue = true;

			ConfigurationManager.Settings.Update (updatedSettings);
			Assert.Equal (Key.End, ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue);
			Assert.Equal (Key.F, updatedSettings ["Application.AlternateForwardKey"].PropertyValue);
			Assert.Equal (Key.B, updatedSettings ["Application.AlternateBackwardKey"].PropertyValue);
			Assert.True ((bool)updatedSettings ["Application.UseSystemConsole"].PropertyValue);
			Assert.True ((bool)updatedSettings ["Application.IsMouseDisabled"].PropertyValue);
			Assert.True ((bool)updatedSettings ["Application.EnableConsoleScrolling"].PropertyValue);
		}
	}
}