<#
  .SYNOPSIS
  Builds the Terminal.Gui library.
  .DESCRIPTION
  Builds the Terminal.Gui library.
  Optional parameter sets are available to customize the build.
  .PARAMETER versionBase
  The base version for the Terminal.Gui library.
#>
Function Build-TerminalGui {
  [CmdletBinding(SupportsShouldProcess, PositionalBinding=$false, DefaultParameterSetName="Basic", ConfirmImpact="Medium")]
  [OutputType([bool],[PSObject])]
  param(
      [Parameter(Mandatory=$true)]
      [Version]$versionBase,
      [Parameter(Mandatory=$true, ParameterSetName="Custom")]
      [switch]$Custom,
      [Parameter(Mandatory=$false, ParameterSetName="Custom")]
      [ValidateSet("Debug", "Release")]
      [string]$slnBuildConfiguration = "Release",
      [Parameter(Mandatory=$false, ParameterSetName="Custom")]
      [ValidateSet("Any CPU", "x86"<#, "x64" #>)]
      [string]$slnBuildPlatform = "Any CPU"
  )

  if(!$PSCmdlet.ShouldProcess("Building in $slnBuildConfiguration configuration for $slnBuildPlatform", "Terminal.Gui", "BUILDING")) {
    return $null
  }

  Write-Host NOT IMPLEMENTED. No Action has been taken.
  return $false
}
