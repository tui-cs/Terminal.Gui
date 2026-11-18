<#
  .SYNOPSIS
  All-inclusive module that includes all other Terminal.Gui.PowerShell.* modules.
  .DESCRIPTION
  All-inclusive module that includes all other Terminal.Gui.PowerShell.* modules.
  .EXAMPLE
  Import-Module ./Terminal.Gui.PowerShell.psd1
  .NOTES
  Doc comments on manifest files are not supported by Get-Help as of PowerShell 7.4.2.
  This comment block is purely informational and will not interfere with module loading.
#>


@{

# No root module because this is a manifest module.
RootModule = ''

# Version number of this module.
ModuleVersion = '1.0.0'

# Supported PSEditions
CompatiblePSEditions = @('Core')

# ID used to uniquely identify this module
GUID = 'f28198f9-cf4b-4ab0-9f94-aef5616b7989'

# Author of this module
Author = 'Brandon Thetford (GitHub @dodexahedron)'

# Company or vendor of this module
CompanyName = 'The Terminal.Gui Project'

# Copyright statement for this module
Copyright = 'Brandon Thetford (GitHub @dodexahedron), provided to the Terminal.Gui project and you under the MIT license'

# Description of the functionality provided by this module
Description = 'Utilities for development-time operations on and management of components of Terminal.Gui code and other assets.'

# Minimum version of the PowerShell engine required by this module
PowerShellVersion = '7.4.0'

# Name of the PowerShell "host" subsystem (not system host name). Helps ensure that we know what to expect from the environment.
PowerShellHostName = 'ConsoleHost'

# Minimum version of the PowerShell host required by this module
PowerShellHostVersion = '7.4.0'

# Processor architecture (None, MSIL, X86, IA64, Amd64, Arm, or an empty string) required by this module. One value only.
# Set to AMD64 here because development on Terminal.Gui isn't really supported on anything else.
# Has nothing to do with runtime use of Terminal.Gui.
ProcessorArchitecture = ''

# Modules that must be imported into the global environment prior to importing this module
RequiredModules = @(
    @{
        ModuleName='Microsoft.PowerShell.Utility'
        ModuleVersion='7.0.0'
    },
    @{
        ModuleName='Microsoft.PowerShell.Management'
        ModuleVersion='7.0.0'
    },
    @{
        ModuleName='PSReadLine'
        ModuleVersion='2.3.4'
    }
)

# Assemblies that must be loaded prior to importing this module
# RequiredAssemblies = @()

# Script files (.ps1) that are run in the caller's environment prior to importing this module.
# ScriptsToProcess = @()

# Type files (.ps1xml) to be loaded when importing this module
# TypesToProcess = @()

# Format files (.ps1xml) to be loaded when importing this module
# FormatsToProcess = @()

# Modules to import as nested modules of this module.
# This module is just a shortcut that loads all of our modules.
NestedModules = @('./Terminal.Gui.PowerShell.Core.psd1', './Terminal.Gui.PowerShell.Git.psd1', './Terminal.Gui.PowerShell.Build.psd1')

# Functions to export from this module.
# Not filtered, so exports all functions exported by all nested modules.
FunctionsToExport = '*'

# Cmdlets to export from this module.
# We don't have any, so empty array.
CmdletsToExport = @()

# Variables to export from this module.
# We explicitly control scope of variables, so empty array.
VariablesToExport = @()

# Aliases to export from this module.
# None defined at this time.
AliasesToExport = @()

# List of all modules packaged with this module
# This is informational ONLY, so it's just blank right now.
# ModuleList = @()

# List of all files packaged with this module
# This is informational ONLY, so it's just blank right now.
# FileList = @()

# Private data to pass to the module specified in RootModule/ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
PrivateData = @{

    PSData = @{

        # Tags applied to this module. These help with module discovery in online galleries.
        # Tags = @()

        # A URL to the license for this module.
        LicenseUri = 'https://github.com/tui-cs/Terminal.Gui/tree/develop/Scripts/COPYRIGHT'

        # A URL to the main website for this project.
        ProjectUri = 'https://github.com/tui-cs/Terminal.Gui'

        # A URL to an icon representing this module.
        # IconUri = ''

        # ReleaseNotes of this module
        ReleaseNotes = 'See change history and releases for Terminal.Gui on GitHub'

        # Prerelease string of this module
        # Prerelease = ''

        # Flag to indicate whether the module requires explicit user acceptance for install/update/save
        RequireLicenseAcceptance = $false

        # External dependent modules of this module
        # ExternalModuleDependencies = @()

    } # End of PSData hashtable

} # End of PrivateData hashtable

# HelpInfo URI of this module
# HelpInfoURI = ''

# Default prefix for commands exported from this module. Override the default prefix using Import-Module -Prefix.
# DefaultCommandPrefix = ''

}

