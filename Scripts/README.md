## Development and Design-Time PowerShell Modules
This directory contains PowerShell modules for use when working with Terminal.slnx

### Purpose
These modules will be modifed and extended as time goes on, whenever someone decides to add something to make life easier.

### Requirements
These modules are designed for **PowerShell Core, version 7.4 or higher**, on any platform, and must be run directly within a pwsh process.\
If you want to use them from within another application, such as PowerShell hosted inside VSCode, you must first run `pwsh` in that terminal.

As the primary development environment for Terminal.Gui is Visual Studio 2022+, some functionality may be limited, unavailable, or not work on platforms other than Windows.\
Most should still work on Linux, however.\
Functions which are platform-specific will be documented as such in their Get-Help documentation.

Specific requirements for each module can be found in the module manifests and will be automatically imported or, if unavailable, PowerShell will tell you what's missing.

### Usage
From a PowerShell 7.4 or higher prompt, navigate to your Terminal.Gui repository directory, and then into the Scripts directory (the same directory as this document).

#### Import Module and Configure Environment
Run the following command to import all Terminal.Gui.PowerShell.* modules:
```powershell
Import-Module ./Terminal.Gui.PowerShell.psd1
```
If the environment meets the requirements, the modules will now be loaded into the current powershell session and exported commands will be immediately available for use.

#### Getting Help
All exported functions and commandlets are provided with full PowerShell help annotations compatible with `Get-Help`.

See [The Get-Help documentation at Microsoft Learn]([https://](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/get-help?view=powershell-7.4)) for Get-Help information.

#### Cleaning Up/Resetting Environment
No environment changes made by the modules on import are persistent.

When you are finished using the modules, you can optionally unload the modules, which will also reset the configuration changes made on import, by simply exiting the PowerShell session (`exit`) or by running the following command:\
**NOTE DIFFERENT TEXT FROM IMPORT COMMAND!**
```powershell
Remove-Module Terminal.Gui.PowerShell
```

## Standalone Scripts

### delist-nuget.ps1

PowerShell script to delist old NuGet packages, keeping only the most recent versions.

**Purpose**: Automatically clean up old pre-release packages from NuGet.org to keep the package list manageable.

**Usage**:
```powershell
./delist-nuget.ps1 -ApiKey "your-nuget-api-key" [-JustPublishedVersion "2.0.1-rc.1"]
```

**Parameters**:
- `-ApiKey` (required): NuGet API key with package management permissions
- `-JustPublishedVersion` (optional): Version that was just published (will be kept while others are delisted)

**Behavior**:
- **Develop packages** (`*-develop.*`): Keeps only the most recent, delists all others
- **Pre-release packages** (`*-beta.*`, `*-rc.*`): Keeps only the just-published version (or most recent if not specified)

This script is currently intended to be run manually when old NuGet packages need to be delisted; it is not currently run automatically by `publish.yml`.

### LICENSE
MIT License

Original Author: Brandon Thetford (@dodexahedron)

See COPYRIGHT in this directory for license text.
