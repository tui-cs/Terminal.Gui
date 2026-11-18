@{

# No root module because this is a manifest module.
RootModule = ''

# Version number of this module.
ModuleVersion = '1.0.0'

# Supported PSEditions
CompatiblePSEditions = @('Core')

# ID used to uniquely identify this module
GUID = 'c4a1de77-83fb-45a3-b1b5-18d275ef3601'

# Author of this module
Author = 'Brandon Thetford (GitHub @dodexahedron)'

# Company or vendor of this module
CompanyName = 'The Terminal.Gui Project'

# Copyright statement for this module
Copyright = 'Brandon Thetford (GitHub @dodexahedron), provided to the Terminal.Gui project and you under the MIT license'

# Description of the functionality provided by this module
Description = 'Build helper functions for Terminal.Gui.'

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
    },
    "./Terminal.Gui.PowerShell.Core.psd1"
)

# Assemblies that must be loaded prior to importing this module
# RequiredAssemblies = @()

# Script files (.ps1) that are run in the caller's environment prior to importing this module.
# ScriptsToProcess = @()

# Type files (.ps1xml) to be loaded when importing this module
# TypesToProcess = @()

# Format files (.ps1xml) to be loaded when importing this module
# FormatsToProcess = @()

# Modules to import as nested modules.
NestedModules = @('./Terminal.Gui.PowerShell.Build.psm1')

# Functions to export from this module.
FunctionsToExport = @('Build-TerminalGui')

# Cmdlets to export from this module.
CmdletsToExport = @()

# Variables to export from this module
VariablesToExport = @()

# Aliases to export from this module.
AliasesToExport = @()

# List of all modules packaged with this module
# ModuleList = @()

# List of all files packaged with this module
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

