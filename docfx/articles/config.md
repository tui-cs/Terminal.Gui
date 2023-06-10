# Configuration Management

Terminal.Gui provides configuration and theme management for Terminal.Gui applications via the [`ConfigurationManager`](~/api/Terminal.Gui/Terminal.Gui.

1) **Settings**. Settings are applied to the [`Application`](~/api/Terminal.Gui/Terminal.Gui.Application.yml) class. Settings are accessed via the `Settings` property of the [`ConfigurationManager`](~/api/Terminal.Gui/Terminal.Gui.ConfigurationManager.yml) class.
2) **Themes**. Themes are a named collection of settings impacting how applications look. The default theme is named "Default". The built-in configuration stored within the Terminal.Gui library defines two additional themes: "Dark", and "Light". Additional themes can be defined in the configuration files.
3) **AppSettings**. AppSettings allow applicaitons to use the  [`ConfigurationManager`](~/api/Terminal.Gui/Terminal.Gui.ConfigurationManager.yml) to store and retrieve application-specific settings.

The The [`ConfigurationManager`](~/api/Terminal.Gui/Terminal.Gui.ConfigurationManager.yml) will look for configuration files in the `.tui` folder in the user's home directory (e.g. `C:/Users/username/.tui` or `/usr/username/.tui`), the folder where the Terminal.Gui application was launched from (e.g. `./.tui`), or as a resource within the Terminal.Gui application's main assembly.

Settings that will apply to all applications (global settings) reside in files named config.json. Settings that will apply to a specific Terminal.Gui application reside in files named appname.config.json, where appname is the assembly name of the application (e.g. `UICatalog.config.json`).

Settings are applied using the following precedence (higher precedence settings overwrite lower precedence settings):

1. App specific settings found in the users's home directory (`~/.tui/appname.config.json`). -- Highest precedence.

2. App specific settings found in the directory the app was launched from (`./.tui/appname.config.json`).

3. App settings in app resources (`Resources/config.json`).

4. Global settings found in the the user's home directory (`~/.tui/config.json`).

5. Global settings found in the directory the app was launched from (`./.tui/config.json`).

6. Default settings defined in the Terminal.Gui assembly -- Lowest precedence.

The `UI Catalog` application provides an example of how to use the [`ConfigurationManager`](~/api/Terminal.Gui/Terminal.Gui.ConfigurationManager.yml) class to load and save configuration files. The `Configuration Editor` scenario provides an editor that allows users to edit the configuration files. UI Catalog also uses a file system watcher to detect changes to the configuration files to tell [`ConfigurationManager`](~/api/Terminal.Gui/Terminal.Gui.ConfigurationManager.yml) to reaload them; allowing users to change settings without having to restart the application.

# What Can Be Configured

## Settings

(Note, this list may not be complete; search the source code for `SerializableConfigurationProperty` to find all settings that can be configured.)

    * [Application.QuitKey](~/api/Terminal.Gui/Terminal.Gui.Application.yml#Terminal_Gui_Application_QuitKey)
    * [Application.AlternateForwardKey](~/api/Terminal.Gui/Terminal.Gui.Application.yml#Terminal_Gui_Application_AlternateForwardKey)
    * [Application.AlternateBackwardKey](~/api/Terminal.Gui/Terminal.Gui.Application.yml#Terminal_Gui_Application_AlternateBackwardKey)
    * [Application.UseSystemConsole](~/api/Terminal.Gui/Terminal.Gui.Application.yml#Terminal_Gui_Application_UseSystemConsole)
    * [Application.IsMouseDisabled](~/api/Terminal.Gui/Terminal.Gui.Application.yml#Terminal_Gui_Application_IsMouseDisabled)
    * [Application.EnableConsoleScrolling](~/api/Terminal.Gui/Terminal.Gui.Application.yml#Terminal_Gui_Application_EnableConsoleScrolling)

## Glyphs

Defines the standard set of glyphs used for standard views (e.g. the default indicator for [Button](~/api/Terminal.Gui/Terminal.Gui.Button.yml)) and line drawing (e.g. [LineCanvas](~/api/Terminal.Gui/Terminal.Gui.LineCanvas.yml)).

The value can be either a decimal number or a string. The string may be:

- A unicode char (e.g. "☑")
- A hex value in U+ format (e.g. "U+2611")
- A hex value in UTF-16 format (e.g. "\\u2611")

```json
  "Glyphs": {
    "RightArrow": "►",
    "LeftArrow": "U+25C4",
    "DownArrow": "\\u25BC",
    "UpArrow": 965010
  }
```

## Themes

A Theme is a named collection of settings that impact the visual style of Terminal.Gui applications. The default theme is named "Default". The built-in configuration stored within the Terminal.Gui library defines two more themes: "Dark", and "Light". Additional themes can be defined in the configuration files. 

The Json property `Theme` defines the name of the theme that will be used. If the theme is not found, the default theme will be used.

Themes support defining ColorSchemes as well as various default settings for Views. Both the default color schemes and user defined color schemes can be configured. See [ColorSchemes](~/api/Terminal.Gui/Terminal.Gui.Colors.yml) for more information.

# Example Configuration File

```json
{
  "$schema": "https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json",
  "Application.QuitKey": {
    "Key": "Esc"
  },
  "AppSettings": {
    "UICatalog.StatusBar": false
  },
  "Theme": "UI Catalog Theme",
  "Themes": [
    {
      "UI Catalog Theme": {
        "ColorSchemes": [
          {
            "UI Catalog Scheme": {
              "Normal": {
                "Foreground": "White",
                "Background": "Green"
              },
              "Focus": {
                "Foreground": "Green",
                "Background": "White"
              },
              "HotNormal": {
                "Foreground": "Blue",
                "Background": "Green"
              },
              "HotFocus": {
                "Foreground": "BrightRed",
                "Background": "White"
              },
              "Disabled": {
                "Foreground": "BrightGreen",
                "Background": "Gray"
              }
            }
          },
          {
            "TopLevel": {
              "Normal": {
                "Foreground": "DarkGray",
                "Background": "White"
              ...
              }
            }
          }
        ],
        "Dialog.DefaultEffect3D": false
      }
    }
  ]
}
```

# Configuration File Schema

Settings are defined in JSON format, according to the schema found here: 

https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json
