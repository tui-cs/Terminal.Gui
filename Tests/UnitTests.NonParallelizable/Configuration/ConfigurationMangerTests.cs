using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Terminal.Gui.Configuration.ConfigurationManager;
using File = System.IO.File;
using SourcesManager = Terminal.Gui.Configuration.SourcesManager;

#pragma warning disable IDE1006

namespace UnitTests.NonParallelizable.ConfigurationTests;

public class ConfigurationMangerTests (ITestOutputHelper output)
{
    [ConfigurationProperty (Scope = typeof (AppSettingsScope))]
    [JsonConverter (typeof (YesNoBooleanJsonConverter))]
    public static bool? CustomConvertedBoolean { get; set; }

    [Fact]
    public void ModuleInitializer_Was_Called ()
    {
        Assert.False (IsEnabled);
        Assert.True (IsInitialized ());
    }

    [Fact]
    public void Initialize_Throws_If_Called_Explicitly ()
    {
        Assert.False (IsEnabled);

        Assert.Throws<InvalidOperationException> (Initialize);
    }

    [Fact]
    public void HardCodedDefaultCache_Properties_Are_Copies ()
    {
        Assert.False (IsEnabled);
        Enable (ConfigLocations.HardCoded);

        Assert.Equal (Key.Esc, Application.GetDefaultKey (Command.Quit));

        ConfigProperty fromSettings = Settings! ["Application.DefaultKeyBindings"];

        FrozenDictionary<string, ConfigProperty> initialCache = GetHardCodedConfigPropertyCache ();
        Assert.NotNull (initialCache);

        ConfigProperty fromCache = initialCache ["Application.DefaultKeyBindings"];

        // Assert
        Assert.NotEqual (fromCache, fromSettings);
        Disable (true);
    }

    [Fact]
    public void HardCodedDefaultCache_KeyBindingDictionaries_Are_Typed_Deep_Copies ()
    {
        // Copilot
        Assert.False (IsEnabled);
        Application.ResetState (true);

        try
        {
            AssertKeyBindingDictionaryIsDeepCopy ("Application.DefaultKeyBindings", Application.DefaultKeyBindings!);
            AssertKeyBindingDictionaryIsDeepCopy ("View.DefaultKeyBindings", View.DefaultKeyBindings!);
        }
        finally
        {
            Disable (true);
            Application.ResetState (true);
        }

        static void AssertKeyBindingDictionaryIsDeepCopy (string propertyName, Dictionary<Command, PlatformKeyBinding> currentBindings)
        {
            FrozenDictionary<string, ConfigProperty> cache = GetHardCodedConfigPropertyCache ();
            Dictionary<Command, PlatformKeyBinding> cachedBindings = Assert.IsType<Dictionary<Command, PlatformKeyBinding>> (cache [propertyName].PropertyValue);

            Assert.NotSame (currentBindings, cachedBindings);
            Assert.NotEmpty (cachedBindings);

            KeyValuePair<Command, PlatformKeyBinding> cachedEntry = cachedBindings.First (kvp => HasAnyKeys (kvp.Value));
            PlatformKeyBinding currentBinding = currentBindings [cachedEntry.Key];

            Assert.NotSame (currentBinding, cachedEntry.Value);
            AssertKeyArraysAreDeepCopies (currentBinding.All, cachedEntry.Value.All);
            AssertKeyArraysAreDeepCopies (currentBinding.Windows, cachedEntry.Value.Windows);
            AssertKeyArraysAreDeepCopies (currentBinding.Linux, cachedEntry.Value.Linux);
            AssertKeyArraysAreDeepCopies (currentBinding.Macos, cachedEntry.Value.Macos);
        }

        static bool HasAnyKeys (PlatformKeyBinding binding)
        {
            return binding.All is { Length: > 0 }
                   || binding.Windows is { Length: > 0 }
                   || binding.Linux is { Length: > 0 }
                   || binding.Macos is { Length: > 0 };
        }

#pragma warning disable CS8632 // Nullable annotations document the nullable PlatformKeyBinding properties in this non-nullable test file.
        static void AssertKeyArraysAreDeepCopies (Key []? currentKeys, Key []? cachedKeys)
#pragma warning restore CS8632
        {
            if (currentKeys is null)
            {
                Assert.Null (cachedKeys);

                return;
            }

            Assert.NotNull (cachedKeys);
            Assert.NotSame (currentKeys, cachedKeys);
            Assert.Equal (currentKeys.Length, cachedKeys.Length);

            for (var i = 0; i < currentKeys.Length; i++)
            {
                Assert.NotSame (currentKeys [i], cachedKeys [i]);
                Assert.Equal (currentKeys [i], cachedKeys [i]);
            }
        }
    }

    [Fact]
    public void HardCoded_Default_Theme_Uses_Fully_Populated_Cache_Values ()
    {
        // Copilot
        Assert.False (IsEnabled);

        try
        {
            Enable (ConfigLocations.HardCoded);

            ThemeScope defaultTheme = ThemeManager.Themes! [ThemeManager.DEFAULT_THEME_NAME];

            Assert.Equal (
                          MouseState.PressedOutside | MouseState.Pressed | MouseState.In,
                          (MouseState)defaultTheme ["CheckBox.DefaultMouseHighlightStates"].PropertyValue!
                         );
            Assert.Equal (
                          CursorStyle.BlinkingBlock,
                          (CursorStyle)defaultTheme ["LinearRangeDefaults.DefaultCursorStyle"].PropertyValue!
                         );
        }
        finally
        {
            Disable (true);
        }
    }

    [Fact]
    public void GetHardCodedDefaultCache_Always_Returns_Same_Ref ()
    {
        // It's important it always returns the same cache ref, so no copies are made
        // Otherwise it's a big performance hit
        Assert.False (IsEnabled);

        try
        {
            FrozenDictionary<string, ConfigProperty> initialCache = GetHardCodedConfigPropertyCache ();
            FrozenDictionary<string, ConfigProperty> cache = GetHardCodedConfigPropertyCache ();
            Assert.Equal (initialCache, cache);
        }
        finally
        {
            Disable (true);
        }
    }

    [Fact]
    public void HardCodedDefaultCache_Properties_Are_Immutable ()
    {
        Assert.False (IsEnabled);

        try
        {
            Enable (ConfigLocations.HardCoded);
            Assert.Equal (10000, FileDialog.MaxSearchResults);

            FrozenDictionary<string, ConfigProperty> initialCache = GetHardCodedConfigPropertyCache ();
            Assert.NotNull (initialCache);
            Assert.Equal (10000, (int)initialCache ["FileDialog.MaxSearchResults"].PropertyValue!);

            // Act
            Settings! ["FileDialog.MaxSearchResults"].PropertyValue = 42;
            Assert.Equal (42, (int)Settings ["FileDialog.MaxSearchResults"].PropertyValue!);

            Settings ["FileDialog.MaxSearchResults"].Apply ();
            Assert.Equal (42, FileDialog.MaxSearchResults);

            FileDialog.MaxSearchResults = 99;

            // Assert
            FrozenDictionary<string, ConfigProperty> cache = GetHardCodedConfigPropertyCache ();
            Assert.True (initialCache ["FileDialog.MaxSearchResults"].Immutable);
            Assert.Equal (10000, (int)initialCache ["FileDialog.MaxSearchResults"].PropertyValue!);
        }
        finally
        {
            Disable (true);
            Application.ResetState (true);
        }
    }

    [Fact]
    public void Disable_Settings_Is_NotNull ()
    {
        Assert.False (IsEnabled);

        Disable ();
        Assert.NotNull (Settings);
    }

    [Fact]
    public void Disable_With_ResetToHardCodedDefaults_True_Works_When_Disabled ()
    {
        Assert.False (IsEnabled);
        Disable (true);
    }

    [Fact]
    public void Enable_Settings_Is_Valid ()
    {
        Assert.False (IsEnabled);

        Enable (ConfigLocations.HardCoded);

        Assert.NotNull (Settings);

        Disable (true);
    }

    [Fact]
    public void Enable_HardCoded_Resets_Schemes_After_Runtime_Config ()
    {
        Assert.False (IsEnabled);

        try
        {
            // Arrange: Start from hard-coded defaults and capture baseline scheme values.
            Enable (ConfigLocations.HardCoded);
            Dictionary<string, Scheme> schemes = SchemeManager.GetSchemes ();
            Assert.NotNull (schemes);
            Assert.NotEmpty (schemes);
            Color baselineFg = schemes ["Base"].Normal.Foreground;
            Color baselineBg = schemes ["Base"].Normal.Background;

            // Sanity: defaults should be stable
            Assert.NotEqual (default (Color), baselineFg);
            Assert.NotEqual (default (Color), baselineBg);

            // Act: Override the Base scheme via runtime JSON and apply
            ThrowOnJsonErrors = true;

            RuntimeConfig = """
                            {
                              "Themes": [
                                {
                                  "Default": {
                                    "Schemes": [
                                      {
                                        "Base": {
                                          "Normal": {
                                            "Foreground": "Black",
                                            "Background": "Gray"
                                          }
                                        }
                                      }
                                    ]
                                  }
                                }
                              ]
                            }
                            """;
            Load (ConfigLocations.Runtime);
            Apply ();

            // Verify override took effect
            Dictionary<string, Scheme> overridden = SchemeManager.GetSchemes ();
            Assert.Equal (Color.Black, overridden ["Base"].Normal.Foreground);
            Assert.Equal (Color.Gray, overridden ["Base"].Normal.Background);

            // Now simulate "CM.Enable(true)" semantics: re-enable with HardCoded to reset
            Disable ();
            Enable (ConfigLocations.HardCoded);

            // Assert: schemes are reset to the original hard-coded baseline
            Dictionary<string, Scheme> reset = SchemeManager.GetSchemes ();
            Assert.Equal (baselineFg, reset ["Base"].Normal.Foreground);
            Assert.Equal (baselineBg, reset ["Base"].Normal.Background);
        }
        finally
        {
            Disable (true);
            Application.ResetState (true);
        }
    }

    [Fact]
    public void Enable_HardCoded_Resets_Theme_Dictionary_And_Selection ()
    {
        Assert.False (IsEnabled);

        try
        {
            // Arrange: Enable defaults
            Enable (ConfigLocations.HardCoded);
            Assert.Equal (ThemeManager.DEFAULT_THEME_NAME, ThemeManager.Theme);
            Assert.Single (ThemeManager.Themes!);
            Assert.True (ThemeManager.Themes.ContainsKey (ThemeManager.DEFAULT_THEME_NAME));

            // Act: Load a runtime config that introduces a custom theme and selects it
            ThrowOnJsonErrors = true;

            RuntimeConfig = """
                            {
                              "Theme": "Custom",
                              "Themes": [
                                {
                                  "Custom": {
                                    "Schemes": [
                                      {
                                        "Base": {
                                          "Normal": {
                                            "Foreground": "Yellow",
                                            "Background": "Black"
                                          }
                                        }
                                      }
                                    ]
                                  }
                                }
                              ]
                            }
                            """;

            // Capture dynamically created hardCoded hard-coded scheme colors
            ImmutableSortedDictionary<string, Scheme> hardCodedSchemes = SchemeManager.GetHardCodedSchemes ()!;

            Color hardCodedBaseNormalFg = hardCodedSchemes ["Base"].Normal.Foreground;
            Assert.Equal (Color.None.ToString (), hardCodedBaseNormalFg.ToString ());

            Load (ConfigLocations.Runtime);
            Apply ();

            // Verify the runtime selection took effect
            Assert.Equal ("Custom", ThemeManager.Theme);

            // Now simulate "CM.Enable(true)" semantics: re-enable with HardCoded to reset
            Disable ();
            Enable (ConfigLocations.HardCoded);

            // Assert: selection and dictionary have been reset to hard-coded defaults
            Assert.Equal (ThemeManager.DEFAULT_THEME_NAME, ThemeManager.Theme);
            Assert.Single (ThemeManager.Themes!);
            Assert.True (ThemeManager.Themes.ContainsKey (ThemeManager.DEFAULT_THEME_NAME));

            // Also assert the Base scheme is back to defaults (sanity check)
            Scheme baseScheme = SchemeManager.GetSchemes () ["Base"];
            Assert.Equal (hardCodedBaseNormalFg.ToString (), SchemeManager.GetSchemes () ["Base"]!.Normal.Foreground.ToString ());
        }
        finally
        {
            Disable (true);
            Application.ResetState (true);
        }
    }

    [Fact]
    public void Apply_Applies_Theme ()
    {
        Assert.False (IsEnabled);
        Enable (ConfigLocations.HardCoded);

        var theme = new ThemeScope ();
        theme.LoadHardCodedDefaults ();
        Assert.NotEmpty (theme);

        Assert.True (ThemeManager.Themes!.TryAdd ("testTheme", theme));
        Assert.Equal (2, ThemeManager.Themes.Count);

        Assert.Equal (LineStyle.Rounded, FrameView.DefaultBorderStyle);
        theme ["FrameView.DefaultBorderStyle"].PropertyValue = LineStyle.Double;

        ThemeManager.Theme = "testTheme";
        Apply ();

        Assert.Equal (LineStyle.Double, FrameView.DefaultBorderStyle);

        Disable (true);
    }

    [Fact]
    public void Apply_Raises_Applied ()
    {
        Assert.False (IsEnabled);

        Enable (ConfigLocations.HardCoded);

        Applied += ConfigurationManagerApplied;
        var fired = false;

        void ConfigurationManagerApplied (object sender, ConfigurationManagerEventArgs obj)
        {
            fired = true;

            // assert
            Assert.Equal (KeyCode.Q, Application.GetDefaultKey (Command.Quit).KeyCode);
            Assert.Equal (KeyCode.F, Application.GetDefaultKey (Command.NextTabGroup).KeyCode);
            Assert.Equal (KeyCode.B, Application.GetDefaultKey (Command.PreviousTabGroup).KeyCode);
        }

        // act
        Dictionary<Command, PlatformKeyBinding> bindings = new ((Dictionary<Command, PlatformKeyBinding>)Settings! ["Application.DefaultKeyBindings"].PropertyValue!);
        bindings [Command.Quit] = Bind.All (Key.Q);
        bindings [Command.NextTabGroup] = Bind.All (Key.F);
        bindings [Command.PreviousTabGroup] = Bind.All (Key.B);
        Settings! ["Application.DefaultKeyBindings"].PropertyValue = bindings;

        Apply ();

        // assert
        Assert.True (fired);

        Applied -= ConfigurationManagerApplied;

        Disable (true);
        Application.ResetState (true);
    }

    [Fact]
    public void Load_Raises_Updated ()
    {
        Assert.False (IsEnabled);

        var fired = false;
        Enable (ConfigLocations.HardCoded);

        ThrowOnJsonErrors = true;
        Dictionary<Command, PlatformKeyBinding> loadBindings = (Dictionary<Command, PlatformKeyBinding>)Settings! ["Application.DefaultKeyBindings"].PropertyValue!;
        Assert.Equal (Key.Esc, loadBindings [Command.Quit].GetCurrentPlatformKeys ().First ());

        Updated += ConfigurationManagerUpdated;

        // Act
        // Only select locations under test control
        Load (ConfigLocations.LibraryResources | ConfigLocations.AppResources | ConfigLocations.Runtime);

        // assert
        Assert.True (fired);

        // clean up
        Updated -= ConfigurationManagerUpdated;
        Disable (true);
        Application.ResetState (true);

        return;

        void ConfigurationManagerUpdated (object sender, ConfigurationManagerEventArgs obj) { fired = true; }
    }

    [Fact]
    public void Load_And_Apply_Performance_Check ()
    {
        Assert.False (IsEnabled);

        Enable (ConfigLocations.HardCoded);

        try
        {
            // Start stopwatch
            var stopwatch = new Stopwatch ();
            stopwatch.Start ();

            // Act
            Load (ConfigLocations.All);
            Apply ();

            // Stop stopwatch
            stopwatch.Stop ();

            // Assert
            output.WriteLine ($"Load took {stopwatch.ElapsedMilliseconds} ms");

            // Ensure load time is reasonable (adjust threshold as needed)
            Assert.True (
                         stopwatch.ElapsedMilliseconds < 1000,
                         $"Loading configuration took {stopwatch.ElapsedMilliseconds}ms, which exceeds reasonable threshold");
        }
        finally
        {
            Disable (true);
            Application.ResetState (true);
        }
    }

    [Fact]
    public void Load_Loads_Custom_Json ()
    {
        Assert.False (IsEnabled);

        try
        {
            Enable (ConfigLocations.HardCoded);
            ThrowOnJsonErrors = true;

            Assert.Equal (Key.Esc, Application.GetDefaultKey (Command.Quit));

            // act
            RuntimeConfig = """

                                    {
                                          "Application.DefaultKeyBindings": { "Quit": { "All": ["Ctrl-Q"] } }
                                    }
                            """;
            Load (ConfigLocations.Runtime);

            // assert
            Dictionary<Command, PlatformKeyBinding> loadedBindings = (Dictionary<Command, PlatformKeyBinding>)Settings ["Application.DefaultKeyBindings"].PropertyValue!;
            Assert.Equal (Key.Q.WithCtrl, loadedBindings [Command.Quit].GetCurrentPlatformKeys ().First ());
        }
        finally
        {
            // clean up
            Disable (true);
            Application.ResetState (true);
        }
    }

    [Fact (Skip = "Events disabled")]
    public void ResetToCurrentValues_Raises_Updated ()
    {
        Assert.False (IsEnabled);

        var fired = false;

        try
        {
            Enable (ConfigLocations.HardCoded);

            Dictionary<Command, PlatformKeyBinding> bindings6 = new ((Dictionary<Command, PlatformKeyBinding>)Settings! ["Application.DefaultKeyBindings"].PropertyValue!);
            bindings6 [Command.Quit] = Bind.All (Key.Q);
            Settings! ["Application.DefaultKeyBindings"].PropertyValue = bindings6;

            Updated += ConfigurationManagerUpdated;

            // Act
            UpdateToCurrentValues ();

            // assert
            Assert.True (fired);
        }
        finally
        {
            Updated -= ConfigurationManagerUpdated;

            Disable (true);
            Application.ResetState (true);
        }

        return;

        void ConfigurationManagerUpdated (object sender, ConfigurationManagerEventArgs obj) { fired = true; }
    }

    [Fact]
    public void ResetToHardCodedDefaults_and_Load_LibraryResourcesOnly_are_same ()
    {
        Assert.False (IsEnabled);

        try
        {
            // arrange
            Enable (ConfigLocations.HardCoded);

            Dictionary<Command, PlatformKeyBinding> bindings7 = new ((Dictionary<Command, PlatformKeyBinding>)Settings! ["Application.DefaultKeyBindings"].PropertyValue!);
            bindings7 [Command.Quit] = Bind.All (Key.Q);
            bindings7 [Command.NextTabGroup] = Bind.All (Key.F);
            bindings7 [Command.PreviousTabGroup] = Bind.All (Key.B);
            Settings! ["Application.DefaultKeyBindings"].PropertyValue = bindings7;
            Settings.Apply ();

            // assert apply worked
            Assert.Equal (KeyCode.Q, Application.GetDefaultKey (Command.Quit).KeyCode);
            Assert.Equal (KeyCode.F, Application.GetDefaultKey (Command.NextTabGroup).KeyCode);
            Assert.Equal (KeyCode.B, Application.GetDefaultKey (Command.PreviousTabGroup).KeyCode);

            //act
            ResetToHardCodedDefaults ();

            // assert
            Assert.NotEmpty (ThemeManager.Themes!);
            Assert.Equal ("Default", ThemeManager.Theme);
            Assert.Equal (Key.Esc, Application.GetDefaultKey (Command.Quit));
            Assert.Equal (Key.F6, Application.GetDefaultKey (Command.NextTabGroup));
            Assert.Equal (Key.F6.WithShift, Application.GetDefaultKey (Command.PreviousTabGroup));

            // arrange
            Dictionary<Command, PlatformKeyBinding> bindings7b = new ((Dictionary<Command, PlatformKeyBinding>)Settings ["Application.DefaultKeyBindings"].PropertyValue!);
            bindings7b [Command.Quit] = Bind.All (Key.Q);
            bindings7b [Command.NextTabGroup] = Bind.All (Key.F);
            bindings7b [Command.PreviousTabGroup] = Bind.All (Key.B);
            Settings ["Application.DefaultKeyBindings"].PropertyValue = bindings7b;
            Settings.Apply ();

            // act - ResetToHardCodedDefaults first, then load library resources on top
            ResetToHardCodedDefaults ();
            Load (ConfigLocations.LibraryResources);
            Apply ();

            // assert
            Assert.NotEmpty (ThemeManager.Themes);
            Assert.Equal ("Default", ThemeManager.Theme);
            Assert.Equal (KeyCode.Esc, Application.GetDefaultKey (Command.Quit).KeyCode);
            Assert.Equal (Key.F6, Application.GetDefaultKey (Command.NextTabGroup));
            Assert.Equal (Key.F6.WithShift, Application.GetDefaultKey (Command.PreviousTabGroup));
        }
        finally
        {
            Disable (true);
            Application.ResetState (true);
        }
    }

    [Fact]
    public void ResetToHardCodedDefaults_Resets ()
    {
        Assert.False (IsEnabled);

        try
        {
            Enable (ConfigLocations.HardCoded);

            // Capture dynamically created hardCoded hard-coded scheme colors
            ImmutableSortedDictionary<string, Scheme> hardCodedSchemesViaSchemeManager = SchemeManager.GetHardCodedSchemes ()!;

            Dictionary<string, Scheme> hardCodedSchemes =
                GetHardCodedConfigPropertiesByScope ("ThemeScope")!.ToFrozenDictionary () ["Schemes"].PropertyValue as Dictionary<string, Scheme>;

            Color hardCodedBaseNormalFg = hardCodedSchemesViaSchemeManager ["Base"].Normal.Foreground;

            Assert.Equal (Color.None.ToString (), hardCodedBaseNormalFg.ToString ());

            // Capture current scheme colors
            Dictionary<string, Scheme> currentSchemes = SchemeManager.GetSchemes ()!;

            Color currentBaseNormalFg = currentSchemes ["Base"].Normal.Foreground;

            Assert.Equal (hardCodedBaseNormalFg.ToString (), currentBaseNormalFg.ToString ());

            // Arrange
            var json = @"
{
  ""$schema"": ""https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json"",
  ""Application.DefaultKeyBindings"": { ""Quit"": { ""All"": [""Alt-Z""] } },
  ""Theme"": ""Default"",
  ""Themes"": [
    {
      ""Default"": {
        ""MessageBox.DefaultButtonAlignment"": ""End"",
        ""Schemes"": [
          {
            ""Accent"": {
              ""Normal"": {
                ""Foreground"": ""BrightGreen"",
                ""Background"": ""Black""
              },
              ""Focus"": {
                ""Foreground"": ""White"",
                ""Background"": ""Cyan""
              },
              ""HotNormal"": {
                ""Foreground"": ""Yellow"",
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
        ]
      }
    }
  ]
}
			";

            // ResetToCurrentValues ();

            ThrowOnJsonErrors = true;
            ConfigurationManager.SourcesManager?.Load (Settings, json, "UpdateFromJson", ConfigLocations.Runtime);

            Assert.Equal ("Default", ThemeManager.Theme);
            Assert.Equal (KeyCode.Esc, Application.GetDefaultKey (Command.Quit).KeyCode);
            Dictionary<Command, PlatformKeyBinding> settingsBindings8 = (Dictionary<Command, PlatformKeyBinding>)Settings! ["Application.DefaultKeyBindings"].PropertyValue!;
            Assert.Equal (KeyCode.Z | KeyCode.AltMask, settingsBindings8 [Command.Quit].GetCurrentPlatformKeys ().First ().KeyCode);
            Assert.Equal (Alignment.Center, MessageBox.DefaultButtonAlignment);

            // Get current scheme colors again
            currentSchemes = SchemeManager.GetSchemes ()!;

            currentBaseNormalFg = currentSchemes ["Base"].Normal.Foreground;

            Assert.Equal (Color.White.ToString (), currentBaseNormalFg.ToString ());

            // Now Apply
            Apply ();

            Assert.Equal ("Default", ThemeManager.Theme);
            Assert.Equal (KeyCode.Z | KeyCode.AltMask, Application.GetDefaultKey (Command.Quit).KeyCode);
            Assert.Equal (Alignment.End, MessageBox.DefaultButtonAlignment);

            Assert.Equal (Color.White.ToString (), currentBaseNormalFg.ToString ());

            // Reset
            ResetToHardCodedDefaults ();

            hardCodedSchemes =
                GetHardCodedConfigPropertiesByScope ("ThemeScope")!.ToFrozenDictionary () ["Schemes"].PropertyValue as Dictionary<string, Scheme>;
            hardCodedBaseNormalFg = hardCodedSchemes! ["Base"].Normal.Foreground;
            Assert.Equal (Color.None.ToString (), hardCodedBaseNormalFg.ToString ());

            FrozenDictionary<string, ConfigProperty> hardCodedCache = GetHardCodedConfigPropertyCache ()!;

            Assert.Equal (hardCodedCache ["Theme"].PropertyValue, ThemeManager.Theme);
            Dictionary<Command, PlatformKeyBinding> hardCodedBindings = (Dictionary<Command, PlatformKeyBinding>)hardCodedCache ["Application.DefaultKeyBindings"].PropertyValue!;
            Assert.Equal (hardCodedBindings [Command.Quit].GetCurrentPlatformKeys ().First (), Application.GetDefaultKey (Command.Quit));

            // Themes
            Assert.Equal (hardCodedCache ["MessageBox.DefaultButtonAlignment"].PropertyValue, MessageBox.DefaultButtonAlignment);

            Assert.Equal (GetHardCodedConfigPropertyCache ()! ["MessageBox.DefaultButtonAlignment"].PropertyValue, MessageBox.DefaultButtonAlignment);

            // Schemes
            currentSchemes = SchemeManager.GetSchemes ()!;
            currentBaseNormalFg = currentSchemes ["Base"].Normal.Foreground;
            Assert.Equal (hardCodedBaseNormalFg.ToString (), currentBaseNormalFg.ToString ());

            Scheme baseScheme = SchemeManager.GetScheme ("Base");

            Attribute attr = baseScheme.Normal;

            // Use ToString so Assert.Equal shows the actual vs expected values on failure
            Assert.Equal (hardCodedBaseNormalFg.ToString (), attr.Foreground.ToString ());
        }
        finally
        {
            output.WriteLine ("Disabling CM to clean up.");

            Disable (true);
        }
    }

    [Fact (Skip = "ResetToCurrentValues corrupts hard coded cache")]
    public void ResetToCurrentValues_Enabled_Resets ()
    {
        Assert.False (IsEnabled);

        try
        {
            // Act
            Enable (ConfigLocations.HardCoded);

            Application.DefaultKeyBindings! [Command.Quit] = Bind.All (Key.A);

            UpdateToCurrentValues ();

            Dictionary<Command, PlatformKeyBinding> bindings9 = (Dictionary<Command, PlatformKeyBinding>)Settings! ["Application.DefaultKeyBindings"].PropertyValue!;
            Assert.Equal (Key.A, bindings9 [Command.Quit].GetCurrentPlatformKeys ().First ());
            Assert.NotNull (Settings);
            Assert.NotNull (AppSettings);
            Assert.NotNull (ThemeManager.Themes);

            // Default Theme should be "Default"
            Assert.Single (ThemeManager.Themes);
            Assert.Equal (ThemeManager.DEFAULT_THEME_NAME, ThemeManager.Theme);
        }
        finally
        {
            Disable (true);
        }
    }

    [Fact]
    public void ConfigurationManager_DefaultPrecedence_IsRespected ()
    {
        Assert.False (IsEnabled);

        try
        {
            // arrange
            Enable (ConfigLocations.HardCoded);

            ThrowOnJsonErrors = true;

            // Setup multiple configurations with the same setting
            // with different precedence levels
            RuntimeConfig = """
                            {
                                 "Application.DefaultKeyBindings": { "Quit": { "All": ["Alt+Q"] } }
                            }
                            """;

            var defaultConfig = """
                                {
                                     "Application.DefaultKeyBindings": { "Quit": { "All": ["Ctrl+X"] } }
                                }
                                """;

            // Update default config first (lower precedence)
            ConfigurationManager.SourcesManager?.Load (Settings, defaultConfig, "default-test", ConfigLocations.LibraryResources);

            // Then load runtime config, which should override default
            Load (ConfigLocations.Runtime);

            // Assert - the runtime config should win due to precedence
            Dictionary<Command, PlatformKeyBinding> precedenceBindings = (Dictionary<Command, PlatformKeyBinding>)Settings! ["Application.DefaultKeyBindings"].PropertyValue!;
            Assert.Equal (Key.Q.WithAlt, precedenceBindings [Command.Quit].GetCurrentPlatformKeys ().First ());
        }
        finally
        {
            Disable (true);
        }
    }

    /// <summary>Save the `config.json` file; this can be used to update the file in `Terminal.Gui.Resources.config.json'.</summary>
    /// <remarks>
    ///     IMPORTANT: For the file generated to be valid, this must be the ONLY test run. Config Properties are all
    ///     static and thus can be overwritten by other tests.
    /// </remarks>
    [Fact]
    public void Save_HardCodedDefaults_To_config_json ()
    {
        Assert.False (IsEnabled);

        try
        {
            Enable (ConfigLocations.HardCoded);

            // Get the hard coded settings
            ResetToHardCodedDefaults ();

            // Serialize to a JSON string
            string json = ConfigurationManager.SourcesManager?.ToJson (Settings);

            // Write the JSON string to the file
            File.WriteAllText ("hard_coded_defaults_config.json", json);

            // Verify the file was created
            Assert.True (File.Exists ("hard_coded_defaults_config.json"), "Failed to create config.json file");
        }
        finally
        {
            Disable (true);
        }
    }

    /// <summary>Save the `config.json` file; this can be used to update the file in `Terminal.Gui.Resources.config.json'.</summary>
    /// <remarks>
    ///     IMPORTANT: For the file generated to be valid, this must be the ONLY test run. Config Properties are all
    ///     static and thus can be overwritten by other tests.
    /// </remarks>
    [Fact]
    public void Save_Library_Defaults_To_config_json ()
    {
        Assert.False (IsEnabled);

        try
        {
            Enable (ConfigLocations.LibraryResources);

            // Serialize to a JSON string
            string json = ConfigurationManager.SourcesManager?.ToJson (Settings);

            // Write the JSON string to the file
            File.WriteAllText ("library_defaults_config.json", json);

            // Verify the file was created
            Assert.True (File.Exists ("library_defaults_config.json"), "Failed to create config.json file");
        }
        finally
        {
            Disable (true);
        }
    }

    [Fact]
    public void Save_HardCodedDefaults_ToJson_Omits_Invalid_Null_ValueType_Entries_And_RoundTrips ()
    {
        Assert.False (IsEnabled);

        try
        {
            Enable (ConfigLocations.HardCoded);
            ResetToHardCodedDefaults ();

            string json = ConfigurationManager.SourcesManager?.ToJson (Settings);

            Assert.NotNull (json);
            Assert.DoesNotContain ("\"MessageBox.DefaultBorderStyle\": null", json);
            Assert.DoesNotContain ("\"Button.DefaultShadow\": null", json);

            SettingsScope roundTripped = JsonSerializer.Deserialize (json, SerializerContext.SettingsScope);

            Assert.NotNull (roundTripped);
            Assert.NotNull (roundTripped! ["Themes"].PropertyValue);
        }
        finally
        {
            Disable (true);
        }
    }

    [Fact]
    public void Load_And_Save_AppSettings_With_Custom_Property_Converter_Works_In_Jit ()
    {
        Assert.False (IsEnabled);

        try
        {
            CustomConvertedBoolean = false;
            ThrowOnJsonErrors = true;
            Enable (ConfigLocations.HardCoded);

            RuntimeConfig = """
                            {
                              "AppSettings": {
                                "ConfigurationMangerTests.CustomConvertedBoolean": "yes"
                              }
                            }
                            """;

            Load (ConfigLocations.Runtime);
            Apply ();

            Assert.True (CustomConvertedBoolean);

            string json = ConfigurationManager.SourcesManager?.ToJson (Settings);

            Assert.NotNull (json);
            Assert.Contains ("\"ConfigurationMangerTests.CustomConvertedBoolean\": \"yes\"", json);
        }
        finally
        {
            Disable (true);
            CustomConvertedBoolean = false;
        }
    }

    [Fact]
    public void TestConfigProperties ()
    {
        Assert.False (IsEnabled);

        try
        {
            Enable (ConfigLocations.HardCoded);

            Assert.NotEmpty (Settings!);

            // test that all ConfigProperties have our attribute
            Assert.All (
                        Settings,
                        item => Assert.Contains (
                                                 item.Value.PropertyInfo!.CustomAttributes,
                                                 a => a.AttributeType
                                                      == typeof (ConfigurationPropertyAttribute)
                                                ));

#pragma warning disable xUnit2030
            Assert.DoesNotContain (
                                   Settings,
                                   cp => cp.Value.PropertyInfo!.GetCustomAttribute (
                                                                                    typeof (ConfigurationPropertyAttribute)
                                                                                   )
                                         == null
                                  );
#pragma warning restore xUnit2030

            // Application is a static class
            PropertyInfo pi = typeof (Application).GetProperty ("DefaultKeyBindings");
            Assert.Equal (pi, Settings ["Application.DefaultKeyBindings"].PropertyInfo);

            // FrameView is not a static class and DefaultBorderStyle is Scope.Scheme
            pi = typeof (FrameView).GetProperty ("DefaultBorderStyle");
            Assert.False (Settings.ContainsKey ("FrameView.DefaultBorderStyle"));
            Assert.True (ThemeManager.GetCurrentTheme ().ContainsKey ("FrameView.DefaultBorderStyle"));
            Assert.Equal (pi, ThemeManager.GetCurrentTheme () ["FrameView.DefaultBorderStyle"].PropertyInfo);
        }
        finally
        {
            Disable (true);
        }
    }

    [Fact]
    public void Load_And_Apply_HardCoded ()
    {
        Assert.False (IsEnabled);

        try
        {
            // Spot check by setting some of the config properties
            Application.DefaultKeyBindings! [Command.Quit] = Bind.All (Key.X.WithCtrl);
            FileDialog.MaxSearchResults = 1;

            Enable (ConfigLocations.HardCoded);
            Load (ConfigLocations.HardCoded);

            // Spot check
            Dictionary<Command, PlatformKeyBinding> hcBindings = (Dictionary<Command, PlatformKeyBinding>)Settings ["Application.DefaultKeyBindings"].PropertyValue!;
            Assert.Equal (Key.Esc, hcBindings [Command.Quit].GetCurrentPlatformKeys ().First ());
            Assert.Equal (10000, (int)Settings ["FileDialog.MaxSearchResults"].PropertyValue!);

            Assert.Single (ThemeManager.Themes!);
            Assert.Equal ("Default", ThemeManager.Theme);
            Assert.NotEmpty (ThemeManager.Themes [ThemeManager.Theme]);

            // Verify schemes are properly initialized
            Assert.NotNull (SchemeManager.GetSchemes ());
            Assert.NotEmpty (SchemeManager.GetSchemes ());

            // Verify "Base" has correct values
            //Assert.Equal (Color.White, SchemeManager.GetSchemes () ["Base"]!.Normal.Foreground);
            //Assert.Equal (Color.Blue, SchemeManager.GetSchemes () ["Base"].Normal.Background);

            Apply ();
            Assert.Equal (Key.Esc, Application.GetDefaultKey (Command.Quit));
            Assert.Equal (10000, FileDialog.MaxSearchResults);
        }
        finally
        {
            Disable (true);
        }
    }

    [Fact]
    public void Load_And_Apply_LibraryResources ()
    {
        Assert.False (IsEnabled);

        try
        {
            // Spot check by setting some of the config properties
            Application.DefaultKeyBindings! [Command.Quit] = Bind.All (Key.X.WithCtrl);
            FileDialog.MaxSearchResults = 1;

            Enable (ConfigLocations.HardCoded);
            Load (ConfigLocations.LibraryResources);

            // Spot check
            Dictionary<Command, PlatformKeyBinding> lrBindings = (Dictionary<Command, PlatformKeyBinding>)Settings! ["Application.DefaultKeyBindings"].PropertyValue!;
            Assert.Equal (Key.Esc, lrBindings [Command.Quit].GetCurrentPlatformKeys ().First ());
            Assert.Equal (10000, (int)Settings ["FileDialog.MaxSearchResults"].PropertyValue!);

            Assert.NotEmpty (ThemeManager.Themes!);
            Assert.Equal ("Default", ThemeManager.Theme);
            Assert.NotEmpty (ThemeManager.Themes [ThemeManager.Theme]);

            // Verify schemes are properly initialized
            Assert.NotNull (SchemeManager.GetSchemes ());
            Assert.NotEmpty (SchemeManager.GetSchemes ());

            // This is too fragile as the default scheme may change
            // Verify "Base" has correct values
            //Assert.Equal (Color.White, SchemeManager.Schemes ["Base"]!.Normal.Foreground);
            //Assert.Equal (Color.Blue, SchemeManager.Schemes ["Base"].Normal.Background);

            Apply ();
            Assert.Equal (Key.Esc, Application.GetDefaultKey (Command.Quit));
            Assert.Equal (10000, FileDialog.MaxSearchResults);
        }
        finally
        {
            Disable (true);
        }
    }

    [Fact]
    public void Load_And_Apply_RuntimeConfig ()
    {
        Assert.False (IsEnabled);

        try
        {
            // Spot check by setting some of the config properties
            Application.DefaultKeyBindings! [Command.Quit] = Bind.All (Key.X.WithCtrl);
            FileDialog.MaxSearchResults = 1;

            ThrowOnJsonErrors = true;
            Enable (ConfigLocations.HardCoded);

            RuntimeConfig = """
                            {
                                "Application.DefaultKeyBindings": { "Quit": { "All": ["Alt-Q"] } },
                                "FileDialog.MaxSearchResults":9
                            }
                            """;
            Load (ConfigLocations.Runtime);

            Dictionary<Command, PlatformKeyBinding> rtBindings = (Dictionary<Command, PlatformKeyBinding>)Settings! ["Application.DefaultKeyBindings"].PropertyValue!;
            Assert.Equal (Key.Q.WithAlt, rtBindings [Command.Quit].GetCurrentPlatformKeys ().First ());
            Assert.Equal (9, (int)Settings ["FileDialog.MaxSearchResults"].PropertyValue!);

            Apply ();
            Assert.Equal (Key.Q.WithAlt, Application.GetDefaultKey (Command.Quit));
            Assert.Equal (9, FileDialog.MaxSearchResults);
        }
        finally
        {
            Disable (true);
        }
    }

    [Fact]
    public void InvalidJsonLogs ()
    {
        Assert.False (IsEnabled);

        Enable (ConfigLocations.HardCoded);

        ThrowOnJsonErrors = false;

        // "brown" is not a color
        var json = @"
			{
				""Themes"" : [
                                        {
						""Default"" : {
							""Schemes"": [
							{
								""UserDefined"": {
									""hotNormal"": {
										""foreground"": ""brown"",
										""background"": ""1234""
									}
								}
							}
							]
						}
					}
				}
			}";

        ConfigurationManager.SourcesManager?.Load (Settings, json, "test", ConfigLocations.Runtime);

        // AbNormal is not a Scheme attribute
        json = @"
			{
				""Themes"" : [
                                        {
						""Default"" : {
							""Schemes"": [
							{
								""UserDefined"": {
									""AbNormal"": {
										""foreground"": ""green"",
										""background"": ""black""
									}
								}
							}
							]
						}
					}
				}
			}";

        ConfigurationManager.SourcesManager?.Load (Settings, json, "test", ConfigLocations.Runtime);

        // Modify hotNormal background only
        json = @"
			{
				""Themes"" :  [
                                        {
						""Default"" : {
							""Schemes"": [
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

        ConfigurationManager.SourcesManager?.Load (Settings, json, "test", ConfigLocations.Runtime);

        ConfigurationManager.SourcesManager?.Load (Settings, "{}}", "test", ConfigLocations.Runtime);

        Assert.NotEqual (0, _jsonErrors.Length);

        ThrowOnJsonErrors = false;

        Disable (true);
    }

    [Fact]
    public void InvalidJsonThrows ()
    {
        Assert.False (IsEnabled);
        Enable (ConfigLocations.HardCoded);

        ThrowOnJsonErrors = true;

        // "yellow" is not a color
        var json = @"
			{
				""Themes"" : [
                                        {
						""Default"" : {
							""Schemes"": [
							{
								""UserDefined"": {
									""hotNormal"": {
										""foreground"": ""brownish"",
										""background"": ""1234""
									}
								}
							}
							]
						}
					}
				]
			}";

        var jsonException = Assert.Throws<JsonException> (() => ConfigurationManager.SourcesManager?.Load (Settings, json, "test", ConfigLocations.Runtime));
        Assert.StartsWith ("foreground: \"\"brownish\"\"", jsonException.Message);

        // AbNormal is not a Scheme attribute
        json = @"
			{
				""Themes"" : [
                                        {
						""Default"" : {
							""Schemes"": [
							{
								""UserDefined"": {
									""AbNormal"": {
										""foreground"": ""green"",
										""background"": ""black""
									}
								}
							}
							]
						}
					}
				]
			}";

        jsonException = Assert.Throws<JsonException> (() => ConfigurationManager.SourcesManager?.Load (Settings, json, "test", ConfigLocations.Runtime));
        Assert.StartsWith ("AbNormal:", jsonException.Message);

        // Modify hotNormal background only
        json = @"
			{
				""Themes"" : [
                                        {
						""Default"" : {
							""Schemes"": [
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

        jsonException = Assert.Throws<JsonException> (() => ConfigurationManager.SourcesManager?.Load (Settings, json, "test", ConfigLocations.Runtime));
        Assert.StartsWith ("background:", jsonException.Message);

        // Unknown property
        json = @"
			{
				""Unknown"" : ""Not known""
			}";

        jsonException = Assert.Throws<JsonException> (() => ConfigurationManager.SourcesManager?.Load (Settings, json, "test", ConfigLocations.Runtime));
        Assert.StartsWith ("Unknown:", jsonException.Message);

        Assert.Equal (0, _jsonErrors.Length);

        ThrowOnJsonErrors = false;

        Disable (true);
    }

    [Fact]
    public void SourcesManager_Load_FromJson_Loads ()
    {
        Assert.False (IsEnabled);

        try
        {
            Enable (ConfigLocations.HardCoded);

            // Arrange
            var json = @"
{
  ""$schema"": ""https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json"",
  ""Application.DefaultKeyBindings"": { ""Quit"": { ""All"": [""Alt-Z""] } },
  ""Theme"": ""Default"",
  ""Themes"": [
    {
      ""Default"": {
        ""MessageBox.DefaultButtonAlignment"": ""End"",
        ""Schemes"": [
          {
            ""Accent"": {
              ""Normal"": {
                ""Foreground"": ""BrightGreen"",
                ""Background"": ""Black""
              },
              ""Focus"": {
                ""Foreground"": ""White"",
                ""Background"": ""Cyan""
              },
              ""HotNormal"": {
                ""Foreground"": ""Yellow"",
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
        ]
      }
    }
  ]
}
			";

            //ResetToCurrentValues ();
            ThrowOnJsonErrors = true;

            ConfigurationManager.SourcesManager?.Load (Settings, json, "UpdateFromJson", ConfigLocations.Runtime);

            Assert.Equal ("Default", ThemeManager.Theme);

            Assert.Equal (KeyCode.Esc, Application.GetDefaultKey (Command.Quit).KeyCode);
            Dictionary<Command, PlatformKeyBinding> smBindings = (Dictionary<Command, PlatformKeyBinding>)Settings! ["Application.DefaultKeyBindings"].PropertyValue!;
            Assert.Equal (KeyCode.Z | KeyCode.AltMask, smBindings [Command.Quit].GetCurrentPlatformKeys ().First ().KeyCode);
            Assert.Equal (Alignment.Center, MessageBox.DefaultButtonAlignment);

            // Now Apply
            Apply ();

            Assert.Equal ("Default", ThemeManager.Theme);
            Assert.Equal (KeyCode.Z | KeyCode.AltMask, Application.GetDefaultKey (Command.Quit).KeyCode);
            Assert.Equal (Alignment.End, MessageBox.DefaultButtonAlignment);

            Assert.Equal (Color.White, SchemeManager.GetSchemes ()! ["Base"].Normal.Foreground);
            Assert.Equal (Color.Blue, SchemeManager.GetSchemes ()! ["Base"].Normal.Background);
        }
        finally
        {
            output.WriteLine ("Disabling CM to clean up.");

            Disable (true);
        }
    }

    [ConfigurationProperty (Scope = typeof (CMTestsScope))]
    public static bool? TestProperty { get; set; }

    private class CMTestsScope : Scope<CMTestsScope>
    { }

    [Fact]
    public void GetConfigPropertiesByScope_Gets ()
    {
        IEnumerable<KeyValuePair<string, ConfigProperty>> props = GetUninitializedConfigPropertiesByScope ("CMTestsScope");

        Assert.NotNull (props);
        Assert.NotEmpty (props);
    }

    [Fact]
    public void ConfigLocations_LoadOrder_IsCorrect ()
    {
        Assert.False (IsEnabled);

        try
        {
            // Arrange
            Enable (ConfigLocations.HardCoded);
            ThrowOnJsonErrors = true;

            // Test that each location overrides the previous ones
            // Priority order (lowest to highest): HardCoded → LibraryResources → AppResources → GlobalHome → GlobalCurrent → AppHome → AppCurrent → Env → Runtime

            // Start with HardCoded
            Assert.Equal (10000, (int)Settings! ["FileDialog.MaxSearchResults"].PropertyValue!);

            // Load LibraryResources (should override HardCoded)
            var libraryConfig = """
                                {
                                     "FileDialog.MaxSearchResults": 1
                                }
                                """;
            ConfigurationManager.SourcesManager?.Load (Settings, libraryConfig, "library-test", ConfigLocations.LibraryResources);
            Assert.Equal (1, (int)Settings! ["FileDialog.MaxSearchResults"].PropertyValue!);

            // Load AppResources (should override LibraryResources)
            var appResourcesConfig = """
                                     {
                                          "FileDialog.MaxSearchResults": 2
                                     }
                                     """;
            ConfigurationManager.SourcesManager?.Load (Settings, appResourcesConfig, "appresources-test", ConfigLocations.AppResources);
            Assert.Equal (2, (int)Settings! ["FileDialog.MaxSearchResults"].PropertyValue!);

            // Load GlobalHome (should override AppResources)
            var globalHomeConfig = """
                                   {
                                        "FileDialog.MaxSearchResults": 3
                                   }
                                   """;
            ConfigurationManager.SourcesManager?.Load (Settings, globalHomeConfig, "globalhome-test", ConfigLocations.GlobalHome);
            Assert.Equal (3, (int)Settings! ["FileDialog.MaxSearchResults"].PropertyValue!);

            // Load GlobalCurrent (should override GlobalHome)
            var globalCurrentConfig = """
                                      {
                                           "FileDialog.MaxSearchResults": 4
                                      }
                                      """;
            ConfigurationManager.SourcesManager?.Load (Settings, globalCurrentConfig, "globalcurrent-test", ConfigLocations.GlobalCurrent);
            Assert.Equal (4, (int)Settings! ["FileDialog.MaxSearchResults"].PropertyValue!);

            // Load AppHome (should override GlobalCurrent)
            var appHomeConfig = """
                                {
                                     "FileDialog.MaxSearchResults": 5
                                }
                                """;
            ConfigurationManager.SourcesManager?.Load (Settings, appHomeConfig, "apphome-test", ConfigLocations.AppHome);
            Assert.Equal (5, (int)Settings! ["FileDialog.MaxSearchResults"].PropertyValue!);

            // Load AppCurrent (should override AppHome)
            var appCurrentConfig = """
                                   {
                                        "FileDialog.MaxSearchResults": 6
                                   }
                                   """;
            ConfigurationManager.SourcesManager?.Load (Settings, appCurrentConfig, "appcurrent-test", ConfigLocations.AppCurrent);
            Assert.Equal (6, (int)Settings! ["FileDialog.MaxSearchResults"].PropertyValue!);

            // Load Env (should override AppCurrent)
            var envConfig = """
                            {
                                 "FileDialog.MaxSearchResults": 7
                            }
                            """;
            ConfigurationManager.SourcesManager?.Load (Settings, envConfig, "env-test", ConfigLocations.Env);
            Assert.Equal (7, (int)Settings! ["FileDialog.MaxSearchResults"].PropertyValue!);

            // Load Runtime (should override Env - highest priority)
            RuntimeConfig = """
                            {
                                 "FileDialog.MaxSearchResults": 8
                            }
                            """;
            Load (ConfigLocations.Runtime);
            Assert.Equal (8, (int)Settings! ["FileDialog.MaxSearchResults"].PropertyValue!);
        }
        finally
        {
            Disable (true);
        }
    }

    [Fact]
    public void ConfigLocations_Env_LoadsFromEnvironmentVariable ()
    {
        Assert.False (IsEnabled);

        try
        {
            // Arrange
            Enable (ConfigLocations.HardCoded);
            ThrowOnJsonErrors = true;

            // Set environment variable
            Environment.SetEnvironmentVariable (
                                                SourcesManager.TUI_CONFIG_ENV_S,
                                                """
                                                {
                                                     "FileDialog.MaxSearchResults": 42
                                                }
                                                """);

            // Act
            Load (ConfigLocations.Env);

            // Assert
            Assert.Equal (42, (int)Settings! ["FileDialog.MaxSearchResults"].PropertyValue!);
        }
        finally
        {
            Environment.SetEnvironmentVariable (SourcesManager.TUI_CONFIG_ENV_S, null);
            Disable (true);
        }
    }

    [Fact]
    public void ConfigLocations_Runtime_HasHighestPriority ()
    {
        Assert.False (IsEnabled);

        try
        {
            // Arrange
            Enable (ConfigLocations.HardCoded);
            ThrowOnJsonErrors = true;

            // Set Env config
            Environment.SetEnvironmentVariable (
                                                SourcesManager.TUI_CONFIG_ENV_S,
                                                """
                                                {
                                                     "FileDialog.MaxSearchResults": 7
                                                }
                                                """);

            // Set Runtime config
            RuntimeConfig = """
                            {
                                 "FileDialog.MaxSearchResults": 8
                            }
                            """;

            // Act - Load both Env and Runtime
            Load (ConfigLocations.Env | ConfigLocations.Runtime);

            // Assert - Runtime should win (highest priority)
            Assert.Equal (8, (int)Settings! ["FileDialog.MaxSearchResults"].PropertyValue!);
        }
        finally
        {
            Environment.SetEnvironmentVariable (SourcesManager.TUI_CONFIG_ENV_S, null);
            Disable (true);
        }
    }

    [Fact]
    public void ConfigLocations_All_LoadsInCorrectOrder ()
    {
        Assert.False (IsEnabled);

        try
        {
            // Arrange - Set up all possible configuration sources
            Enable (ConfigLocations.HardCoded);
            ThrowOnJsonErrors = true;

            // Set environment variable (second-highest priority)
            Environment.SetEnvironmentVariable (
                                                SourcesManager.TUI_CONFIG_ENV_S,
                                                """
                                                {
                                                     "FileDialog.MaxSearchResults": 7
                                                }
                                                """);

            // Set runtime config (highest priority)
            RuntimeConfig = """
                            {
                                 "FileDialog.MaxSearchResults": 8
                            }
                            """;

            // Act - Load all locations
            Load (ConfigLocations.All);

            // Assert - Runtime should win (highest priority)
            Assert.Equal (8, (int)Settings! ["FileDialog.MaxSearchResults"].PropertyValue!);

            // Now test without Runtime
            RuntimeConfig = null;
            LoadHardCodedDefaults ();

            Environment.SetEnvironmentVariable (
                                                SourcesManager.TUI_CONFIG_ENV_S,
                                                """
                                                {
                                                     "FileDialog.MaxSearchResults": 7
                                                }
                                                """);
            Load (ConfigLocations.Env);

            // Assert - Env should be used when Runtime is not set
            Assert.Equal (7, (int)Settings! ["FileDialog.MaxSearchResults"].PropertyValue!);
        }
        finally
        {
            Environment.SetEnvironmentVariable (SourcesManager.TUI_CONFIG_ENV_S, null);
            Disable (true);
        }
    }

    private sealed class YesNoBooleanJsonConverter : JsonConverter<bool?>
    {
        public override bool? Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string value = reader.GetString ();

            return value?.ToLowerInvariant () switch
            {
                "yes" => true,
                "no" => false,
                _ => throw new JsonException ($"Unexpected boolean token '{value}'.")
            };
        }

        public override void Write (Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
        {
            writer.WriteStringValue (value == true ? "yes" : "no");
        }
    }
}
