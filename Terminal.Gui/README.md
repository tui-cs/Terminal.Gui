# Terminal.Gui Project

All files required to build the **Terminal.Gui** library (and NuGet package).

## Project Folder Structure

- `\` - The root folder contains the source code for the library.
	- `Terminal.Gui.sln` - The Visual Studio solution
	- `Application.cs` - A `static` class that provides the base 'application driver'. Given it defines a **Terminal.Gui** application it is both logically and literally (because `static`) a singleton. It has direct dependencies on `MainLoop`, `Events.cs` `NetDriver`, `CursesDriver`, `WindowsDriver`, `Responder`, `View`, and `TopLevel` (and nothing else).
	- `MainLoop.cs` - Defines `IMainLoopDriver` and implements the `MainLoop` class.
	- A few supporting class files

- `ConsoleDrivers\`
	- `ConsoleDriver.cs` - Definition for the Console Driver API.
	- Source files for the three `ConsoleDriver`-based drivers: .NET: `NetDriver`, Unix & Mac: `UnixDriver`, and Windows: `WindowsDriver`.

- `Configuration\` - Classes related the `ConfigurationManager`.

- `Clipboard\` - Classes related to clipboard access.

- `Input\` - Classes relating to keyboard and mouse input. Includes `Responder`, which is the base class for `View`
	- `Events.cs` - Defines keyboard and mouse-related structs & classes. 
	- `Responder` - Base class for the windowing class hierarchy. Implements support for keyboard & mouse input.
	- etc...

- `Text\` - Classes related to text processing

- `Drawing\` - Classes related to drawing 

- `View\` - The `View` class heirarchy, not including any sub-classes
	- `View.cs` - Derived from `Responder`, the base class for non-modal visual elements such as controls.
	- `Layout\`	
		- `PosDim.cs` - Implements *Computed Layout* system. These classes have deep dependencies on `View`.

- `Views\` - Sub-classes of `View` 
	- `Toplevel` - Derived from `View`, the base class for modal visual elements such as top-level windows and dialogs. Supports the concept of `MenuBar` and `StatusBar`.
	- `Window` - Derived from `TopLevel`; implements toplevel views with a visible frame and Title.
	- `Dialog` -
	- etc...

- `Types/` - A folder (not namespace) containing implementations of `Point`, `Rect`, and `Size` which are ancient versions of the modern `System.Drawing.Point`, `System.Drawing.Size`, and `System.Drawning.Rectangle`.


## Version numbers

Version info for Terminal.Gui is managed by [gitversion](https://gitversion.net).

Install `gitversion`:

```powershell
dotnet tool install --global GitVersion.Tool
dotnet-gitversion
```

The project version (the nuget package and in `Terminal.Gui.dll`) is determined from the latest `git tag`. 

The format of version numbers is `vmajor.minor.patch.build.height` and follows the [Semantic Versioning](https://semver.org/) rules.

To define a new version (e.g. with a higher `major`, `minor`, `patch`, or `build` value) tag a commit using `git tag`:

```powershell
git tag v1.3.4-beta.5 -a -m "Release v1.3.4 Beta 5"
dotnet-gitversion /updateprojectfiles
dotnet build -c Release
```

**DO NOT COMMIT AFTER USING `/updateprojectfiles`!**

Doing so will update the `.csproj` files in your branch with version info, which we do not want.

## Publishing a Release of Terminal.Gui

First, use the [Semantic Versioning](https://semver.org/) rules.to determine the new verison number. 

Given a version number MAJOR.MINOR.PATCH, increment the:

* MAJOR version when you make incompatible API changes
* MINOR version when you add functionality in a backwards compatible manner
* PATCH version when you make backwards compatible bug fixes

Additional labels for pre-release and build metadata are available as extensions to the MAJOR.MINOR.PATCH format.

To release a new version (e.g. with a higher `major`, `minor`, or `patch` value) tag a commit using `git tag` and then push that tag directly to the `main` branch on `github.com/gui-cs/Terminal.Gui` (`upstream`).

The `tag` must be of the form `v<major>.<minor>.<patch>`, e.g. `v2.3.4`.

`patch` can indicate pre-release or not (e.g. `pre`, `beta`, `rc`, etc...). 

### 1) Verify the `develop` branch is ready for release

* Ensure everything is committed and pushed to the `develop` branch
* Ensure your local `develop` branch is up-to-date with `upstream/develop`

### 2) Create a pull request for the release in the `develop` branch

The PR title should be of the form "Release v2.3.4"

```powershell
git checkout develop
git pull upstream develop
git checkout -b v2_3_4
git add .
git commit -m "Release v2.3.4"
git push
```

Go to the link printed by `git push` and fill out the Pull Request.

### 3) On github.com, verify the build action worked on your fork, then merge the PR

### 4) Pull the merged `develop` from `upstream`

```powershell
git checkout develop
git pull upstream develop
```

### 5) Merge `develop` into `main`

```powershell
git checkout main
git pull upstream main
git merge develop
```

Fix any merge errors.

### 6) Create a new annotated tag for the release on `main`

```powershell
git tag v2.3.4 -a -m "Release v2.3.4"
```       

### 7) Push the new tag to `main` on `upstream`

```powershell
git push --atomic upstream main v2.3.4
```       

*See https://stackoverflow.com/a/3745250/297526*

### 8) Monitor Github Actions to ensure the Nuget publishing worked.

https://github.com/gui-cs/Terminal.Gui/actions

### 9) Check Nuget to see the new package version (wait a few minutes) 
https://www.nuget.org/packages/Terminal.Gui

### 10) Add a new Release in Github: https://github.com/gui-cs/Terminal.Gui/releases

Generate release notes with the list of PRs since the last release.

### 11) Update the `develop` branch with the new version

```powershell
git checkout develop
git pull upstream develop
git merge main
git push upstream develop
```

## Nuget

https://www.nuget.org/packages/Terminal.Gui

When a new version tag is defined and merged into `main`, a Nuget package will be generated by a Github Action.

If the version is pre-release (includes a hyphen, e.g. `1.3.4-beta.5`) the Nuget package will be tagged as pre-release.

Miguel & Tig can hide defunct/old Nuget packages.

## Contributing

See [CONTRIBUTING.md](https://github.com/gui-cs/Terminal.Gui/blob/master/CONTRIBUTING.md).
