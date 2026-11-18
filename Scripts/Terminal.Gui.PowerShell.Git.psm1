<#
  .SYNOPSIS
  Creates a new branch with the specified name.
  .DESCRIPTION
  Creates a new branch with the specified name.
  .PARAMETER Name
  The name of the new branch.
  Always required.
  Must match the .net regex pattern "v2_\d{4}_[a-zA-Z0-9()_-]+".
  Must also otherwise be a valid identifier for a git branch and follow any other project guidelines.
  .PARAMETER NoSwitch
  If specified, does not automatically switch to your new branch after creating it.
  Default is to switch to the new branch after creating it.
  .PARAMETER Push
  If specified, automatically pushes the new branch to your remote after creating it.
  .PARAMETER Remote
  The name of the git remote, as configured.
  If you never explicitly set this yourself, it is typically "origin".
  If you only have one remote defined or have not explicitly set a remote yourself, do not provide this parameter; It will be detected automatically.
  .INPUTS
  None
  .OUTPUTS
  The name of the current branch after the operation, as a String.
  If NoSwitch was specified and the operation succeeded, this should be the source branch.
  If NoSwith was not specified or was explicitly set to $false and the operation succeeded, this should be the new branch.
  If an exception occurs, does not return. Exceptions are unhandled and are the responsibility of the caller.
  .NOTES
  Errors thrown by git commands are not explicitly handled.
#>
Function New-GitBranch {
  [CmdletBinding(PositionalBinding=$false, SupportsShouldProcess=$true, ConfirmImpact="Low", DefaultParameterSetName="Basic")]
  param(
    [Parameter(Mandatory=$true, ParameterSetName="Basic")]
    [Parameter(Mandatory=$true, ParameterSetName="NoSwitch")]
    [Parameter(Mandatory=$true, ParameterSetName="Push")]
    [ValidatePattern("v2_\d{4}_[a-zA-Z0-9()_-]+")]
    [string]$Name,
    [Parameter(Mandatory=$true,ParameterSetName="NoSwitch",DontShow)]
    [switch]$NoSwitch,
    [Parameter(Mandatory=$false, ParameterSetName="Basic")]
    [Parameter(Mandatory=$true, ParameterSetName="Push")]
    [switch]$Push,
    [Parameter(Mandatory=$false, ParameterSetName="Push")]
    [string]$Remote = $null
  )
  $currentBranch = (& git branch --show-current)

  if(!$PSCmdlet.ShouldProcess("Creating new branch named $Name from $currentBranch", $Name, "Creating branch")) {
    return $null
  }

  git branch $Name

  if(!$NoSwitch) {
    git switch $Name

    if($Push) {
      if([String]::IsNullOrWhiteSpace($Remote)) {
        $tempRemotes = (git remote show)
        if($tempRemotes -is [array]){
          # If we've gotten here, Push was specified, a remote was not specified or was blank, and there are multiple remotes defined locally.
          # Not going to support that. Just error out.
          Remove-Variable tempRemotes
          throw "No Remote specified and multiple remotes are defined. Cannot continue."
        } else {
          # Push is set, Remote wasn't, but there's only one defined. Safe to continue. Use the only remote.
          $Remote = $tempRemotes
          Remove-Variable tempRemotes
        }
      }

      # Push is set, and either Remote was specified or there's only one remote defined and we will use that.
      # Perform the push. 
      git push --set-upstream $Remote $Name
    }
  } else{
    # NoSwitch was specified.
    # Return the current branch name.
    return $currentBranch
  }

  # If we made it to this point, return the Name that was specified.
  return $Name
}

<#
  .SYNOPSIS
  Checks if the command 'git' is available in the current session.
  .DESCRIPTION
  Checks if the command 'git' is available in the current session.
  Throws an error if not.
  Returns $true if git is available.
  Only intended for use in scripts and module manifests.
  .INPUTS
  None
  .OUTPUTS
  If git exists, $true.
  Otherwise, $false.
#>
Function Test-GitAvailable {
  [OutputType([Boolean])]
  [CmdletBinding()]
  param()
  if($null -eq (Get-Command git -ErrorAction Ignore)) {
    Write-Error -Message "git was not found. Git functionality will not work." -Category ObjectNotFound -TargetObject "git"
    return $false
  }
  return $true
}

Test-GitAvailable -ErrorAction Continue