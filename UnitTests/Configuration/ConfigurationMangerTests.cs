using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.Json;
using Terminal.Gui;
using Xunit;
using static Terminal.Gui.ConfigurationManager;
using Attribute = Terminal.Gui.Attribute;

namespace Terminal.Gui.ConfigurationTests {
	public class ConfigurationManagerTests {

		public static readonly JsonSerializerOptions _jsonOptions = new() {
			Converters = {
				new AttributeJsonConverter (),
				new ColorJsonConverter (),
				}
		};

		[Fact ()]
		public void DeepMemberwiseCopyTest ()
		{
			// Value types
			var stringDest = "Destination";
			var stringSrc = "Source";
			var stringCopy = DeepMemberwiseCopy (stringSrc, stringDest);
			Assert.Equal (stringSrc, stringCopy);

			stringDest = "Destination";
			stringSrc = "Destination";
			stringCopy = DeepMemberwiseCopy (stringSrc, stringDest);
			Assert.Equal (stringSrc, stringCopy);

			stringDest = "Destination";
			stringSrc = null;
			stringCopy = DeepMemberwiseCopy (stringSrc, stringDest);
			Assert.Equal (stringSrc, stringCopy);

			stringDest = "Destination";
			stringSrc = string.Empty;
			stringCopy = DeepMemberwiseCopy (stringSrc, stringDest);
			Assert.Equal (stringSrc, stringCopy);

			var boolDest = true;
			var boolSrc = false;
			var boolCopy = DeepMemberwiseCopy (boolSrc, boolDest);
			Assert.Equal (boolSrc, boolCopy);

			boolDest = false;
			boolSrc = true;
			boolCopy = DeepMemberwiseCopy (boolSrc, boolDest);
			Assert.Equal (boolSrc, boolCopy);

			boolDest = true;
			boolSrc = true;
			boolCopy = DeepMemberwiseCopy (boolSrc, boolDest);
			Assert.Equal (boolSrc, boolCopy);

			boolDest = false;
			boolSrc = false;
			boolCopy = DeepMemberwiseCopy (boolSrc, boolDest);
			Assert.Equal (boolSrc, boolCopy);

			// Structs
			var attrDest = new Attribute (1);
			var attrSrc = new Attribute (2);
			var attrCopy = DeepMemberwiseCopy (attrSrc, attrDest);
			Assert.Equal (attrSrc, attrCopy);

			// Classes
			var colorschemeDest = new ColorScheme () { Disabled = new Attribute (1) };
			var colorschemeSrc = new ColorScheme () { Disabled = new Attribute (2) };
			var colorschemeCopy = DeepMemberwiseCopy (colorschemeSrc, colorschemeDest);
			Assert.Equal (colorschemeSrc, colorschemeCopy);

			// Dictionaries
			var dictDest = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (1) } };
			var dictSrc = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (2) } };
			var dictCopy = (Dictionary<string, Attribute>)DeepMemberwiseCopy (dictSrc, dictDest);
			Assert.Equal (dictSrc, dictCopy);

			dictDest = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (1) } };
			dictSrc = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (2) }, { "Normal", new Attribute (3) } };
			dictCopy = (Dictionary<string, Attribute>)DeepMemberwiseCopy (dictSrc, dictDest);
			Assert.Equal (dictSrc, dictCopy);

			// src adds an item
			dictDest = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (1) } };
			dictSrc = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (2) }, { "Normal", new Attribute (3) } };
			dictCopy = (Dictionary<string, Attribute>)DeepMemberwiseCopy (dictSrc, dictDest);
			Assert.Equal (2, dictCopy.Count);
			Assert.Equal (dictSrc ["Disabled"], dictCopy ["Disabled"]);
			Assert.Equal (dictSrc ["Normal"], dictCopy ["Normal"]);

			// src updates only one item
			dictDest = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (1) }, { "Normal", new Attribute (2) } };
			dictSrc = new Dictionary<string, Attribute> () { { "Disabled", new Attribute (3) } };
			dictCopy = (Dictionary<string, Attribute>)DeepMemberwiseCopy (dictSrc, dictDest);
			Assert.Equal (2, dictCopy.Count);
			Assert.Equal (dictSrc ["Disabled"], dictCopy ["Disabled"]);
			Assert.Equal (dictDest ["Normal"], dictCopy ["Normal"]);

		}

		//[Fact ()]
		//public void LoadFromJsonTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		//[Fact ()]
		//public void ToJsonTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		//[Fact ()]
		//public void UpdateConfigurationTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		//[Fact ()]
		//public void UpdateConfigurationFromFileTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		//[Fact ()]
		//public void SaveHardCodedDefaultsTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		//[Fact ()]
		//public void LoadGlobalFromLibraryResourceTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		//[Fact ()]
		//public void LoadGlobalFromAppDirectoryTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		//[Fact ()]
		//public void LoadGlobalFromHomeDirectoryTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		//[Fact ()]
		//public void LoadAppFromAppResourcesTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		//[Fact ()]
		//public void LoadAppFromAppDirectoryTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		//[Fact ()]
		//public void LoadAppFromHomeDirectoryTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		//[Fact ()]
		//public void LoadTest ()
		//{
		//	Assert.True (false, "This test needs an implementation");
		//}

		/// <summary>
		/// Save the `config.json` file; this can be used to update the file in `Terminal.Gui.Resources.config.json'.
		/// </summary>
		/// <remarks>
		/// IMPORTANT: For the file generated to be valid, this must be the ONLY test run. Config Properties
		/// are all static and thus can be overwritten by other tests.</remarks>
		[Fact]
		public void SaveDefaults ()
		{
			ConfigurationManager.Initialize ();
			ConfigurationManager.Reset ();

			// Get the hard coded settings
			ConfigurationManager.GetHardCodedDefaults ();

			// Serialize to a JSON string
			string json = ConfigurationManager.ToJson ();

			// Write the JSON string to the file 
			File.WriteAllText ("config.json", json);
		}

		[Fact]
		public void UseWithoutResetAsserts ()
		{
			ConfigurationManager.Initialize ();
			Assert.Throws<InvalidOperationException> (() => _ = ConfigurationManager.Settings);
		}

		[Fact]
		public void Reset_Resets()
		{
			ConfigurationManager.Locations = ConfigLocations.DefaultOnly;
			ConfigurationManager.Reset ();
			Assert.NotEmpty (ConfigurationManager.Themes);
			Assert.Equal ("Default", ConfigurationManager.Themes.Theme);
		}

		[Fact]
		public void Reset_and_ResetLoadWithLibraryResourcesOnly_are_same ()
		{
			ConfigurationManager.Locations = ConfigLocations.DefaultOnly;
			// arrange
			ConfigurationManager.Reset ();
			ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue = Key.Q;
			ConfigurationManager.Settings ["Application.AlternateForwardKey"].PropertyValue = Key.F;
			ConfigurationManager.Settings ["Application.AlternateBackwardKey"].PropertyValue = Key.B;
			ConfigurationManager.Settings ["Application.IsMouseDisabled"].PropertyValue = true;
			ConfigurationManager.Settings ["Application.EnableConsoleScrolling"].PropertyValue = true;
			ConfigurationManager.Settings.Apply ();

			// assert apply worked
			Assert.Equal (Key.Q, Application.QuitKey);
			Assert.Equal (Key.F, Application.AlternateForwardKey);
			Assert.Equal (Key.B, Application.AlternateBackwardKey);
			Assert.True (Application.IsMouseDisabled);
			Assert.True (Application.EnableConsoleScrolling);

			//act
			ConfigurationManager.Reset ();

			// assert
			Assert.NotEmpty (ConfigurationManager.Themes);
			Assert.Equal ("Default", ConfigurationManager.Themes.Theme);
			Assert.Equal (Key.Q | Key.CtrlMask, Application.QuitKey);
			Assert.Equal (Key.PageDown | Key.CtrlMask, Application.AlternateForwardKey);
			Assert.Equal (Key.PageUp | Key.CtrlMask, Application.AlternateBackwardKey);
			Assert.False (Application.IsMouseDisabled);
			Assert.False (Application.EnableConsoleScrolling);

			// arrange
			ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue = Key.Q;
			ConfigurationManager.Settings ["Application.AlternateForwardKey"].PropertyValue = Key.F;
			ConfigurationManager.Settings ["Application.AlternateBackwardKey"].PropertyValue = Key.B;
			ConfigurationManager.Settings ["Application.IsMouseDisabled"].PropertyValue = true;
			ConfigurationManager.Settings ["Application.EnableConsoleScrolling"].PropertyValue = true;
			ConfigurationManager.Settings.Apply ();

			ConfigurationManager.Locations = ConfigLocations.DefaultOnly;

			// act
			ConfigurationManager.Reset ();
			ConfigurationManager.Load ();

			// assert
			Assert.NotEmpty (ConfigurationManager.Themes);
			Assert.Equal ("Default", ConfigurationManager.Themes.Theme);
			Assert.Equal (Key.Q | Key.CtrlMask, Application.QuitKey);
			Assert.Equal (Key.PageDown | Key.CtrlMask, Application.AlternateForwardKey);
			Assert.Equal (Key.PageUp | Key.CtrlMask, Application.AlternateBackwardKey);
			Assert.False (Application.IsMouseDisabled);
			Assert.False (Application.EnableConsoleScrolling);

		}

		[Fact]
		public void TestConfigProperties ()
		{
			ConfigurationManager.Locations = ConfigLocations.All;
			ConfigurationManager.Reset ();

			Assert.NotEmpty (ConfigurationManager.Settings);
			// test that all ConfigProperites have our attribute
			Assert.All (ConfigurationManager.Settings, item => Assert.NotEmpty (item.Value.PropertyInfo.CustomAttributes.Where (a => a.AttributeType == typeof (SerializableConfigurationProperty))));
			Assert.Empty (ConfigurationManager.Settings.Where (cp => cp.Value.PropertyInfo.GetCustomAttribute (typeof (SerializableConfigurationProperty)) == null));

			// Application is a static class
			PropertyInfo pi = typeof (Application).GetProperty ("QuitKey");
			Assert.Equal (pi, ConfigurationManager.Settings ["Application.QuitKey"].PropertyInfo);

			// FrameView is not a static class and DefaultBorderStyle is Scope.Scheme
			pi = typeof (FrameView).GetProperty ("DefaultBorderStyle");
			Assert.False (ConfigurationManager.Settings.ContainsKey ("FrameView.DefaultBorderStyle"));
			Assert.True (ConfigurationManager.Themes ["Default"].ContainsKey ("FrameView.DefaultBorderStyle"));
		}

		[Fact]
		public void TestConfigPropertyOmitClassName ()
		{
			// Color.ColorShemes is serialzied as "ColorSchemes", not "Colors.ColorSchemes"
			PropertyInfo pi = typeof (Colors).GetProperty ("ColorSchemes");
			var scp = ((SerializableConfigurationProperty)pi.GetCustomAttribute (typeof (SerializableConfigurationProperty)));
			Assert.True (scp.Scope == typeof (ThemeScope));
			Assert.True (scp.OmitClassName);

			ConfigurationManager.Reset ();
			Assert.Equal (pi, ConfigurationManager.Themes ["Default"] ["ColorSchemes"].PropertyInfo);
		}

		[Fact, AutoInitShutdown]
		public void TestConfigurationManagerToJson ()
		{
			ConfigurationManager.Reset ();
			ConfigurationManager.GetHardCodedDefaults ();
			var stream = ConfigurationManager.ToStream ();
			
			ConfigurationManager.Settings.Update (stream, "TestConfigurationManagerToJson");
		}

		[Fact, AutoInitShutdown (configLocation: ConfigLocations.None)]
		public void TestConfigurationManagerInitDriver_NoLocations ()
		{

			
		}

		[Fact, AutoInitShutdown (configLocation: ConfigLocations.DefaultOnly)]
		public void TestConfigurationManagerInitDriver ()
		{
			Assert.Equal ("Default", ConfigurationManager.Themes.Theme);
			Assert.True (ConfigurationManager.Themes.ContainsKey ("Default"));

			Assert.Equal (Key.Q | Key.CtrlMask, Application.QuitKey);

			Assert.Equal (Color.White, Colors.ColorSchemes ["Base"].Normal.Foreground);
			Assert.Equal (Color.Blue, Colors.ColorSchemes ["Base"].Normal.Background);

			// Change Base
			var json = ConfigurationManager.ToStream ();
			
			ConfigurationManager.Settings.Update (json, "TestConfigurationManagerInitDriver");

			var colorSchemes = ((Dictionary<string, ColorScheme>)ConfigurationManager.Themes [ConfigurationManager.Themes.Theme] ["ColorSchemes"].PropertyValue);
			Assert.Equal (Colors.Base, colorSchemes ["Base"]);
			Assert.Equal (Colors.TopLevel, colorSchemes ["TopLevel"]);
			Assert.Equal (Colors.Error, colorSchemes ["Error"]);
			Assert.Equal (Colors.Dialog, colorSchemes ["Dialog"]);
			Assert.Equal (Colors.Menu, colorSchemes ["Menu"]);

			Colors.Base = colorSchemes ["Base"];
			Colors.TopLevel = colorSchemes ["TopLevel"];
			Colors.Error = colorSchemes ["Error"];
			Colors.Dialog = colorSchemes ["Dialog"];
			Colors.Menu = colorSchemes ["Menu"];

			Assert.Equal (colorSchemes ["Base"], Colors.Base);
			Assert.Equal (colorSchemes ["TopLevel"], Colors.TopLevel);
			Assert.Equal (colorSchemes ["Error"], Colors.Error);
			Assert.Equal (colorSchemes ["Dialog"], Colors.Dialog);
			Assert.Equal (colorSchemes ["Menu"], Colors.Menu);
		}

		[Fact]
		public void TestConfigurationManagerUpdateFromJson ()
		{
			// Arrange
			string json = @"
{
  ""$schema"": ""https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json"",
  ""Application.QuitKey"": {
    ""Key"": ""Z"",
    ""Modifiers"": [
      ""Alt""
    ]
  },
  ""Theme"": ""Default"",
  ""Themes"": [
    {
      ""Default"": {
        ""ColorSchemes"": [
          {
            ""TopLevel"": {
              ""Normal"": {
                ""Foreground"": ""BrightGreen"",
                ""Background"": ""Black""
              },
              ""Focus"": {
                ""Foreground"": ""White"",
                ""Background"": ""Cyan""
              },
              ""HotNormal"": {
                ""Foreground"": ""Brown"",
                ""Background"": ""Black""
              },
              ""HotFocus"": {
                ""Foreground"": ""Blue"",
                ""Background"": ""Cyan""
              },
              ""Disabled"": {
                ""Foreground"": ""DarkGray"",
                ""Background"": ""Black""
              }
            }
          },
          {
            ""Base"": {
              ""Normal"": {
                ""Foreground"": ""White"",
                ""Background"": ""Blue""
              },
              ""Focus"": {
                ""Foreground"": ""Black"",
                ""Background"": ""Gray""
              },
              ""HotNormal"": {
                ""Foreground"": ""BrightCyan"",
                ""Background"": ""Blue""
              },
              ""HotFocus"": {
                ""Foreground"": ""BrightBlue"",
                ""Background"": ""Gray""
              },
              ""Disabled"": {
                ""Foreground"": ""DarkGray"",
                ""Background"": ""Blue""
              }
            }
          },
          {
            ""Dialog"": {
              ""Normal"": {
                ""Foreground"": ""Black"",
                ""Background"": ""Gray""
              },
              ""Focus"": {
                ""Foreground"": ""White"",
                ""Background"": ""DarkGray""
              },
              ""HotNormal"": {
                ""Foreground"": ""Blue"",
                ""Background"": ""Gray""
              },
              ""HotFocus"": {
                ""Foreground"": ""BrightYellow"",
                ""Background"": ""DarkGray""
              },
              ""Disabled"": {
                ""Foreground"": ""Gray"",
                ""Background"": ""DarkGray""
              }
            }
          },
          {
            ""Menu"": {
              ""Normal"": {
                ""Foreground"": ""White"",
                ""Background"": ""DarkGray""
              },
              ""Focus"": {
                ""Foreground"": ""White"",
                ""Background"": ""Black""
              },
              ""HotNormal"": {
                ""Foreground"": ""BrightYellow"",
                ""Background"": ""DarkGray""
              },
              ""HotFocus"": {
                ""Foreground"": ""BrightYellow"",
                ""Background"": ""Black""
              },
              ""Disabled"": {
                ""Foreground"": ""Gray"",
                ""Background"": ""DarkGray""
              }
            }
          },
          {
            ""Error"": {
              ""Normal"": {
                ""Foreground"": ""Red"",
                ""Background"": ""White""
              },
              ""Focus"": {
                ""Foreground"": ""Black"",
                ""Background"": ""BrightRed""
              },
              ""HotNormal"": {
                ""Foreground"": ""Black"",
                ""Background"": ""White""
              },
              ""HotFocus"": {
                ""Foreground"": ""White"",
                ""Background"": ""BrightRed""
              },
              ""Disabled"": {
                ""Foreground"": ""DarkGray"",
                ""Background"": ""White""
              }
            }
          }
        ],
        ""Dialog.DefaultButtonAlignment"": ""Center""
      }
    }
  ]
}					
			";

			ConfigurationManager.Reset ();
			ConfigurationManager.ThrowOnJsonErrors = true;
			
			ConfigurationManager.Settings.Update (json, "TestConfigurationManagerUpdateFromJson");

			Assert.Equal (Key.Q | Key.CtrlMask, Application.QuitKey);
			Assert.Equal (Key.Z | Key.AltMask, ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue);

			Assert.Equal ("Default", ConfigurationManager.Themes.Theme);

			Assert.Equal (Color.White, Colors.ColorSchemes ["Base"].Normal.Foreground);
			Assert.Equal (Color.Blue, Colors.ColorSchemes ["Base"].Normal.Background);

			var colorSchemes = (Dictionary<string, ColorScheme>)Themes.First().Value ["ColorSchemes"].PropertyValue;
			Assert.Equal (Color.White, colorSchemes ["Base"].Normal.Foreground);
			Assert.Equal (Color.Blue, colorSchemes ["Base"].Normal.Background);

			// Now re-apply
			ConfigurationManager.Apply ();

			Assert.Equal (Key.Z | Key.AltMask, Application.QuitKey);
			Assert.Equal ("Default", ConfigurationManager.Themes.Theme);

			Assert.Equal (Color.White, Colors.ColorSchemes ["Base"].Normal.Foreground);
			Assert.Equal (Color.Blue, Colors.ColorSchemes ["Base"].Normal.Background);
		}

		[Fact, AutoInitShutdown]
		public void TestConfigurationManagerInvalidJsonThrows ()
		{
			ConfigurationManager.ThrowOnJsonErrors = true;
			// "yellow" is not a color
			string json = @"
			{
				""Themes"" : [
                                        {
						""Default"" : {
							""ColorSchemes"": [
							{
								""UserDefined"": {
									""hotNormal"": {
										""foreground"": ""yellow"",
										""background"": ""1234""
									}
								}
							}
							]
						}
					}
				]
			}";

			JsonException jsonException = Assert.Throws<JsonException> (() => ConfigurationManager.Settings.Update (json, "test"));
			Assert.Equal ("Invalid Color: 'yellow'", jsonException.Message);

			// AbNormal is not a ColorScheme attribute
			json = @"
			{
				""Themes"" : [ 
                                        {
						""Default"" : {
							""ColorSchemes"": [
							{
								""UserDefined"": {
									""AbNormal"": {
										""foreground"": ""green"",
										""background"": ""1234""
									}
								}
							}
							]
						}
					}
				]
			}";

			jsonException = Assert.Throws<JsonException> (() => ConfigurationManager.Settings.Update (json, "test"));
			Assert.Equal ("Unrecognized ColorScheme Attribute name: AbNormal.", jsonException.Message);

			// Modify hotNormal background only 
			json = @"
			{
				""Themes"" : [ 
                                        {
						""Default"" : {
							""ColorSchemes"": [
							{
								""UserDefined"": {
									""hotNormal"": {
										""background"": ""cyan""
									}
								}
							}
							]
						}
					}
				]
			}";

			jsonException = Assert.Throws<JsonException> (() => ConfigurationManager.Settings.Update (json, "test"));
			Assert.Equal ("Both Foreground and Background colors must be provided.", jsonException.Message);

			// Unknown proeprty
			json = @"
			{
				""Unknown"" : ""Not known""
			}";

			jsonException = Assert.Throws<JsonException> (() => ConfigurationManager.Settings.Update (json, "test"));
			Assert.StartsWith ("Unknown property", jsonException.Message);
			
			Assert.Equal (0, ConfigurationManager.jsonErrors.Length);

			ConfigurationManager.ThrowOnJsonErrors = false;
		}

		[Fact]
		public void TestConfigurationManagerInvalidJsonLogs ()
		{
			Application.Init (new FakeDriver ());

			ConfigurationManager.ThrowOnJsonErrors = false;
			// "yellow" is not a color
			string json = @"
			{
				""Themes"" : [ 
                                        {
						""Default"" : {
							""ColorSchemes"": [
							{
								""UserDefined"": {
									""hotNormal"": {
										""foreground"": ""yellow"",
										""background"": ""1234""
									}
								}
							}
							]
						}
					}
				}
			}";

			ConfigurationManager.Settings.Update (json, "test");

			// AbNormal is not a ColorScheme attribute
			json = @"
			{
				""Themes"" : [ 
                                        {
						""Default"" : {
							""ColorSchemes"": [
							{
								""UserDefined"": {
									""AbNormal"": {
										""foreground"": ""green"",
										""background"": ""1234""
									}
								}
							}
							]
						}
					}
				}
			}";

			ConfigurationManager.Settings.Update (json, "test");

			// Modify hotNormal background only 
			json = @"
			{
				""Themes"" :  [ 
                                        {
						""Default"" : {
							""ColorSchemes"": [
							{
								""UserDefined"": {
									""hotNormal"": {
										""background"": ""cyan""
									}
								}
							}
							]
						}
					}
				}
			}";

			ConfigurationManager.Settings.Update (json, "test");

			ConfigurationManager.Settings.Update ("{}}", "test");
			
			Assert.NotEqual (0, ConfigurationManager.jsonErrors.Length);

			Application.Shutdown ();

			ConfigurationManager.ThrowOnJsonErrors = false;
		}

		[Fact, AutoInitShutdown]
		public void LoadConfigurationFromAllSources_ShouldLoadSettingsFromAllSources ()
		{
			//var _configFilename = "config.json";
			//// Arrange
			//// Create a mock of the configuration files in all sources
			//// Home directory
			//string homeDir = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), ".tui");
			//if (!Directory.Exists (homeDir)) {
			//	Directory.CreateDirectory (homeDir);
			//}
			//string globalConfigFile = Path.Combine (homeDir, _configFilename);
			//string appSpecificConfigFile = Path.Combine (homeDir, "appname.config.json");
			//File.WriteAllText (globalConfigFile, "{\"Settings\": {\"TestSetting\":\"Global\"}}");
			//File.WriteAllText (appSpecificConfigFile, "{\"Settings\": {\"TestSetting\":\"AppSpecific\"}}");

			//// App directory
			//string appDir = Directory.GetCurrentDirectory ();
			//string appDirGlobalConfigFile = Path.Combine (appDir, _configFilename);
			//string appDirAppSpecificConfigFile = Path.Combine (appDir, "appname.config.json");
			//File.WriteAllText (appDirGlobalConfigFile, "{\"Settings\": {\"TestSetting\":\"GlobalAppDir\"}}");
			//File.WriteAllText (appDirAppSpecificConfigFile, "{\"Settings\": {\"TestSetting\":\"AppSpecificAppDir\"}}");

			//// App resources
			//// ...

			//// Act
			//ConfigurationManager.Locations = ConfigurationManager.ConfigLocation.All;
			//ConfigurationManager.Load ();

			//// Assert
			//// Check that the settings from the highest precedence source are loaded
			//Assert.Equal ("AppSpecific", ConfigurationManager.Config.Settings.TestSetting);
		}

		[Fact]
		public void Load_FiresUpdated ()
		{
			ConfigurationManager.Reset ();
			
			ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue = Key.Q;
			ConfigurationManager.Settings ["Application.AlternateForwardKey"].PropertyValue = Key.F;
			ConfigurationManager.Settings ["Application.AlternateBackwardKey"].PropertyValue = Key.B;
			ConfigurationManager.Settings ["Application.IsMouseDisabled"].PropertyValue = true;
			ConfigurationManager.Settings ["Application.EnableConsoleScrolling"].PropertyValue = true;

			ConfigurationManager.Updated += ConfigurationManager_Updated;
			bool fired = false;
			void ConfigurationManager_Updated (object sender, ConfigurationManagerEventArgs obj)
			{
				fired = true;
				// assert
				Assert.Equal (Key.Q | Key.CtrlMask, ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue);
				Assert.Equal (Key.PageDown | Key.CtrlMask, ConfigurationManager.Settings ["Application.AlternateForwardKey"].PropertyValue);
				Assert.Equal (Key.PageUp | Key.CtrlMask, ConfigurationManager.Settings ["Application.AlternateBackwardKey"].PropertyValue);
				Assert.False ((bool)ConfigurationManager.Settings ["Application.IsMouseDisabled"].PropertyValue);
				Assert.False ((bool)ConfigurationManager.Settings ["Application.EnableConsoleScrolling"].PropertyValue);
			}

			ConfigurationManager.Load (true);

			// assert
			Assert.True (fired);

			ConfigurationManager.Updated -= ConfigurationManager_Updated;
		}

		[Fact]
		public void Apply_FiresApplied ()
		{
			ConfigurationManager.Reset ();
			ConfigurationManager.Applied += ConfigurationManager_Applied;
			bool fired = false;
			void ConfigurationManager_Applied (object sender, ConfigurationManagerEventArgs obj)
			{
				fired = true;
				// assert
				Assert.Equal (Key.Q, Application.QuitKey);
				Assert.Equal (Key.F, Application.AlternateForwardKey);
				Assert.Equal (Key.B, Application.AlternateBackwardKey);
				Assert.True (Application.IsMouseDisabled);
				Assert.True (Application.EnableConsoleScrolling);
			}

			// act
			ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue = Key.Q;
			ConfigurationManager.Settings ["Application.AlternateForwardKey"].PropertyValue = Key.F;
			ConfigurationManager.Settings ["Application.AlternateBackwardKey"].PropertyValue = Key.B;
			ConfigurationManager.Settings ["Application.IsMouseDisabled"].PropertyValue = true;
			ConfigurationManager.Settings ["Application.EnableConsoleScrolling"].PropertyValue = true;

			ConfigurationManager.Apply ();

			// assert
			Assert.True (fired);

			ConfigurationManager.Applied -= ConfigurationManager_Applied;
		}
	}
}

