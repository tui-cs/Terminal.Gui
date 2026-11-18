# Terminal.Gui Examples

This repository now keeps only:

- `UICatalog` - the main demo app
- `ScenarioRunner` - scenario automation tool

All other examples were moved to the [tui-cs/Examples](https://github.com/tui-cs/Examples) repository.

## Building Examples Against Local Source

The [tui-cs/Examples](https://github.com/tui-cs/Examples) repo supports building against your local Terminal.Gui source (instead of the NuGet package) to catch breaking changes immediately:

```bash
cd ../Examples
dotnet build -p:TerminalGuiRoot=../Terminal.Gui
```
