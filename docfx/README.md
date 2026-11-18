This folder generates the API docs for Terminal.Gui. 

The API documentation is generated via a GitHub Action (`.github/workflows/api-docs.yml`) using [DocFX](https://github.com/dotnet/docfx). 

## To Generate the Docs Locally

0. Install DocFX: https://dotnet.github.io/docfx/tutorial/docfx_getting_started.html
1. Run `./docfx/scripts/build.ps1`
2. Browse to http://localhost:8080 and verify everything looks good.
3. Hit Ctrl-C to stop the script.

## To Update `views.md`

0. Switch to the `./docfx` folder
1. Run `./scripts/generate-views-doc.ps1`
2. Commit the changes to `docs/views.md`

## API Documentation Overview

The API documentation for Terminal.Gui is a critical resource for developers, providing detailed information on classes, methods, properties, and events within the library. This documentation is hosted at [tui-cs.github.io/Terminal.Gui](https://tui-cs.github.io/Terminal.Gui) and includes both auto-generated API references and conceptual guides. For a broader overview of the Terminal.Gui project, including project structure and contribution guidelines, refer to the main [Terminal.Gui README](https://github.com/tui-cs/Terminal.Gui/blob/main/README.md).

### Scripts for Documentation Generation

The `scripts` folder contains PowerShell scripts to assist in generating and updating documentation:

- **`Build.ps1`**: Main build script that generates the documentation site locally. Running this script with DocFX installed will:
  1. Generate the AI agent source index (see below)
  2. Build the documentation site at `http://localhost:8080`

- **`Generate-SourceIndex.ps1`**: Generates Vercel-style file path indices for AI agent retrieval-led reasoning. This script:
  - Scans `./Terminal.Gui/` for `.cs` source files, grouped by directory
  - Scans `./docfx/docs/` for `.md` documentation files
  - Outputs to `.tg-docs/INDEX.md`
  - Injects the source index into `AGENTS.md` between markers

- **`generate-views-doc.ps1`**: Updates the `views.md` file in the `docs` directory. This script automates the process of documenting the various view classes in Terminal.Gui.

- **`OutputView/`**: Directory for storing intermediate files related to the documentation generation process.

These scripts streamline the process of maintaining up-to-date documentation, ensuring that contributors can easily generate and verify documentation changes locally before committing them.

## Adding API Xref Links to Docs

When writing or editing `.md` files in `docfx/docs/`, use `<xref:uid>` syntax to link to API types and members instead of backtick code spans. DocFX resolves these at build time to clickable API reference links.

### Finding UIDs

All valid UIDs are in `docfx/_site/xrefmap.yml` (regenerated on each build). Search for the type name:

```bash
grep "ClassName" docfx/_site/xrefmap.yml | grep "^- uid:"
```

### Namespace Prefixes

| Type | UID Prefix |
|------|-----------|
| `Command`, `CommandRouting`, `ICommandContext`, `CommandContext`, `ICommandBinding`, `CommandBridge` | `Terminal.Gui.Input.` |
| `View` | `Terminal.Gui.ViewBase.View` |
| All view classes (`Button`, `CheckBox`, `Dialog`, etc.) | `Terminal.Gui.Views.` |
| `IAcceptTarget` | `Terminal.Gui.Input.` |

### Syntax

```markdown
<!-- Type reference (renders as nameWithType, e.g. "Command.Accept") -->
<xref:Terminal.Gui.Input.Command.Accept>

<!-- Method/property overload page -->
<xref:Terminal.Gui.ViewBase.View.RaiseActivating*>

<!-- Custom display text -->
[My Link Text](xref:Terminal.Gui.Input.Command.Accept)
```

### Key UIDs for Command System

| Symbol | UID |
|--------|-----|
| `Command` enum | `Terminal.Gui.Input.Command` |
| `Command.Activate` | `Terminal.Gui.Input.Command.Activate` |
| `Command.Accept` | `Terminal.Gui.Input.Command.Accept` |
| `Command.HotKey` | `Terminal.Gui.Input.Command.HotKey` |
| `CommandRouting` | `Terminal.Gui.Input.CommandRouting` |
| `ICommandContext` | `Terminal.Gui.Input.ICommandContext` |
| `CommandContext` | `Terminal.Gui.Input.CommandContext` |
| `CommandBridge` | `Terminal.Gui.Input.CommandBridge` |
| `IAcceptTarget` | `Terminal.Gui.Input.IAcceptTarget` |
| `View` | `Terminal.Gui.ViewBase.View` |
| `View.CommandsToBubbleUp` | `Terminal.Gui.ViewBase.View.CommandsToBubbleUp` |
| `View.DefaultAcceptView` | `Terminal.Gui.ViewBase.View.DefaultAcceptView` |
| `View.ConsumeDispatch` | `Terminal.Gui.ViewBase.View.ConsumeDispatch` |
| `View.GetDispatchTarget` | `Terminal.Gui.ViewBase.View.GetDispatchTarget*` |
| `View.TryBubbleUp` | `Terminal.Gui.ViewBase.View.TryBubbleUp*` |
| `View.DispatchDown` | `Terminal.Gui.ViewBase.View.DispatchDown*` |
| `View.InvokeCommand` | `Terminal.Gui.ViewBase.View.InvokeCommand*` |
| `View.RaiseActivating` | `Terminal.Gui.ViewBase.View.RaiseActivating*` |
| `View.RaiseActivated` | `Terminal.Gui.ViewBase.View.RaiseActivated*` |
| `View.RaiseAccepting` | `Terminal.Gui.ViewBase.View.RaiseAccepting*` |
| `View.RaiseAccepted` | `Terminal.Gui.ViewBase.View.RaiseAccepted*` |
| `View.RaiseHandlingHotKey` | `Terminal.Gui.ViewBase.View.RaiseHandlingHotKey*` |
| `View.Activating` (event) | `Terminal.Gui.ViewBase.View.Activating` |
| `View.Activated` (event) | `Terminal.Gui.ViewBase.View.Activated` |
| `View.Accepting` (event) | `Terminal.Gui.ViewBase.View.Accepting` |
| `View.Accepted` (event) | `Terminal.Gui.ViewBase.View.Accepted` |

### Rules

- Use `<xref:uid>` for type/member names in prose and table cells
- Use `<xref:uid*>` (with `*`) for method overload pages
- Leave code inside ` ```csharp ``` ` and ` ```mermaid ``` ` blocks unchanged
- Do NOT link internal methods not in the xrefmap (e.g., `SetupCommands`, `TryDispatchToTarget`)
- See `docfx/docs/command-diagrams.md` for real-world examples

---

## AI Agent Source Index

The `Generate-SourceIndex.ps1` script creates a file path index using the **Vercel-style format** for AI agent "retrieval-led reasoning". This approach is inspired by Vercel's work on optimizing LLM context for large codebases.

### Background

This implementation is based on Vercel's blog post: [How we compressed our JS SDK documentation by 80%](https://vercel.com/blog/how-we-compressed-our-js-sdk-docs-by-80-percent-for-llms)

The key insight is that instead of embedding full descriptions in the context (which can become stale), we point AI agents to actual source files. The agents then read the files they need using "retrieval-led reasoning" rather than relying on "pre-training-led reasoning" from potentially outdated embedded content.

### Format

```
[Index Name]|root: ./path/to/root
|IMPORTANT: Prefer retrieval-led reasoning over pre-training-led reasoning. Read files when needed.
|directory:{file1.cs,file2.cs,...}
|subdirectory:{file3.cs,file4.cs,...}
```

### Files Generated

| File | Description |
|------|-------------|
| `.tg-docs/INDEX.md` | Complete auto-generated index with both source and docs indices |
| `AGENTS.md` | Source index injected between `<!-- BEGIN AUTO-GENERATED-SOURCE-INDEX -->` and `<!-- END AUTO-GENERATED-SOURCE-INDEX -->` markers |

### Benefits

1. **Always in sync** - Index is regenerated on every build
2. **No manual maintenance** - File paths are discovered automatically
3. **Retrieval-led** - AI reads actual files, not stale summaries
4. **~80% smaller** - File paths only, no embedded content
