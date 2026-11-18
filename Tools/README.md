# Tools/

Standalone utilities that are **not** part of the shipping `Terminal.Gui`
library and **not** part of the `Examples/` set users learn the library
from.

## What lives here

| Tool | Purpose | Shipped? |
|------|---------|----------|
| [`MigrateConfig/`](./MigrateConfig/) | Converts a pre-MEC flat-key `config.json` to the nested MEC schema | No — standalone console app, run on demand |

## Conventions

- Each tool gets its own folder + `.csproj` + `README.md`.
- Tool csprojs are **not** added to `Terminal.slnx`. They are built
  on demand (`dotnet run --project Tools/<Name>/`).
- Tools are not user-facing examples. If you want to demonstrate
  Terminal.Gui idioms, add to `Examples/` instead.
- Tools may be deleted in any release without a deprecation cycle.

## Why a separate folder?

Without this folder, one-off utilities tend to land either inside
`Terminal.Gui.dll` (bloating AOT output) or inside `Examples/` (where
they mislead newcomers into thinking they're recommended app patterns).
The `Tools/` convention is also used by `dotnet/roslyn`,
`dotnet/runtime`, and `dotnet/aspnetcore`.
