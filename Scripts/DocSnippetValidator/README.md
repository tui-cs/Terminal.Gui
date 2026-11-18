# DocSnippetValidator

Compiles every fenced ` ```csharp ` block in the AI agent docs against the built `Terminal.Gui.dll`, so example rot (v1 APIs, renamed members, invalid constructors) fails CI instead of misleading agents and users.

## Usage

```bash
dotnet build Terminal.Gui/Terminal.Gui.csproj -c Debug
dotnet run --project Scripts/DocSnippetValidator -- \
    Terminal.Gui/bin/Debug/net10.0/Terminal.Gui.dll \
    ai-v2-primer.md .claude/tasks/build-app.md .claude/cookbook/common-patterns.md
```

## How blocks are compiled

- **Complete units** (blocks containing type declarations or `using` directives) compile as-is, with a standard set of `using` directives prepended. Blocks that also contain top-level statements compile as programs.
- **Statement fragments** (e.g. `X = Pos.Center ();`) are wrapped in a method inside a harness class deriving from `Runnable<string?>`, with common fields (`app`, `view`, `button`, `textField`, ...) in scope. If that fails, the block is retried as class members (for fragments that declare methods).

## Opting a block out

- Blocks containing `// WRONG`, `❌`, or `✗` are skipped automatically (anti-pattern examples are intentionally wrong).
- Precede a fence with `<!-- snippet: ignore -->` to skip it explicitly.

## Obsolete APIs fail

Obsolete-API use (`CS0618`/`CS0612`) is treated as a **failure**, not a warning, so v1 rot like the legacy static `Application.Init` cannot pass as "compiled" just because the member still exists as an `[Obsolete]` shim. (For a deprecated-but-intentional usage, mark the block as opted out above.)

## Tests

`testdata/obsolete-api.md` is a negative-test fixture: it uses an obsolete API and is **not** in the validated doc set. CI (`validate-doc-snippets.yml`) runs the validator against it and asserts a non-zero exit, so a regression that stops failing on obsolete APIs is itself caught.

Run via `.github/workflows/validate-doc-snippets.yml`.
