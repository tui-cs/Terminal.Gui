# CLAUDE.md

> **Guidance for AI agents working with Terminal.Gui.**
> For humans, see [CONTRIBUTING.md](./CONTRIBUTING.md).
> For Terminal.Gui's mission, tenets, and engineering philosophy, see [specs/constitution.md](./specs/constitution.md).
> See also: [llms.txt](./llms.txt) for machine-readable context.

## CRITICAL: Discard v1 Training Data

Terminal.Gui v2 is a **complete rewrite**. Pre-2025 training data is **wrong**.

> **Read [ai-v2-primer.md](./ai-v2-primer.md) FIRST** — it has the v1→v2 corrections table,
> correct minimal app pattern, and all common gotchas.

## Quick Reference: What Are You Doing?

| Your Task | Go Here |
|-----------|---------|
| **"Build me an app that..."** | [.claude/tasks/build-app.md](.claude/tasks/build-app.md) |
| **"Add a feature to Terminal.Gui..."** | Continue below (Contributor Guide) |
| **"Fix a bug in Terminal.Gui..."** | Continue below (Contributor Guide) |
| **"Record a GIF / verify a UI change..."** | [Scripts/tuirec/README.md](Scripts/tuirec/README.md) |

### App Builder Quick Start
```bash
dotnet new install Terminal.Gui.Templates@2.*
dotnet new tui-simple -n myapp
cd myapp
dotnet run
```

See [.claude/tasks/build-app.md](.claude/tasks/build-app.md) for complete app development guide.
See [.claude/cookbook/common-patterns.md](.claude/cookbook/common-patterns.md) for UI recipes.

---

# Contributor Guide

**The rest of this file is for contributors modifying Terminal.Gui itself.**

## Before Every File Edit

**READ `.claude/REFRESH.md` first.** It contains a quick checklist to prevent common mistakes.

## After Writing/Modifying Code

**USE `.claude/POST-GENERATION-VALIDATION.md` to validate ALL code.** This catches the most common formatting violations AI agents make.

## Detailed Rules

See `.claude/rules/` for detailed guidance:
- `formatting.md` - **SPACING, BRACES, BLANK LINES** (most commonly violated!)
- `type-declarations.md` - **No var** except built-in types
- `target-typed-new.md` - Use `new ()` not `new TypeName()`
- `terminology.md` - **SubView/SuperView**, never "child/parent"
- `event-patterns.md` - Lambdas, closures, handlers
- `early-return.md` - **Guard clauses, minimal nesting** (commonly violated!)
- `collection-expressions.md` - Use `[...]` syntax
- `unicode-graphemes.md` - **Think in graphemes** - `GetColumns()`, `GraphemeHelper.GetGraphemes()`
- `cwp-pattern.md` - Cancellable Workflow Pattern
- `code-layout.md` - Backing fields, member ordering
- `api-documentation.md` - XML documentation requirements
- `testing-patterns.md` - Test patterns and requirements
- `logging-tracing.md` - **No Console.WriteLine** - use Logging/TestLogging/Trace
- `fragile-areas.md` - Code that must not be refactored in passing (TextView init)

## Task-Specific Guides

See `.claude/tasks/` for task checklists:
- `clean-code-review.md` - Creating clean git commit histories
- `build-app.md` - Building applications with Terminal.Gui

## Planning Mode

When in planning mode:
- **Create plan files in `./plans/`** (relative to the repository root)
- Plan files should be markdown format
- Include detailed implementation steps, file changes, and verification steps
- Reference existing code patterns and reuse opportunities

---

## Project Overview

**Terminal.Gui** - Cross-platform .NET console UI toolkit

- **Language**: C# 14 (net10.0)
- **Branch**: `develop`
- **Version**: v2 (stable)

## Build & Test

```bash
dotnet restore
dotnet build --no-restore

# Preferred: parallelizable tests (no static state)
dotnet test --project Tests/UnitTestsParallelizable --no-build

# Tests that require process-wide static state (Application.Init, etc.)
dotnet test --project Tests/UnitTests.NonParallelizable --no-build

# Legacy tests — do NOT add new tests here; candidates for rewrite/deletion
dotnet test --project Tests/UnitTests.Legacy --no-build

# Run a single test by method name (Microsoft Testing Platform)
dotnet test --project Tests/UnitTestsParallelizable --no-build --filter-method "*MyTestMethod"

# Run all tests in a class
dotnet test --project Tests/UnitTestsParallelizable --no-build --filter-class "*MyTestClass"
```

See `Tests/README.md` for the full list of test projects (including `IntegrationTests`, `StressTests`, `Benchmarks`) and the static-state classification that determines where a new test belongs.

## Seeing Your Changes (Visual Verification)

Agents can observe a running Terminal.Gui app — don't ship UI changes blind. Use [`tuirec`](https://github.com/tui-cs/tuirec) to run the app in a PTY, inject keystrokes, and capture the result:

- **Full guide:** [Scripts/tuirec/README.md](Scripts/tuirec/README.md) — install, keystroke syntax, UICatalog scenario recipes, validation checklist
- The `.cast` output is asciinema v2 JSON (plain text) — **read it back** to verify what actually rendered, frame by frame
- The `.gif` output is for humans — attach it to PRs that change visuals
- For deterministic in-process assertions, use `InputInjector`/`VirtualTimeProvider` (see `docfx/docs/input-injection.md`) and driver `ToString ()` screen captures

## Key Concepts

| Concept | Documentation |
|---------|--------------|
| Application Lifecycle | `docfx/docs/application.md` |
| View Hierarchy | `docfx/docs/View.md` |
| Layout (Pos/Dim) | `docfx/docs/layout.md` |
| CWP Events | `docfx/docs/cancellable-work-pattern.md` |
| Terminology | `docfx/docs/lexicon.md` |

## Critical Rules (Summary)

1. **Space BEFORE `()` and `[]`** - `Method ()` not `Method()`, `array [i]` not `array[i]` (MOST VIOLATED!)
2. **Braces on NEXT line** - ALL opening braces use Allman style
3. **Blank lines** - before `return`/`break`/`continue`, after control blocks
4. **No `var`** except: `int`, `string`, `bool`, `double`, `float`, `decimal`, `char`, `byte`
5. **Use `new ()`** not `new TypeName()`
6. **Use `[...]`** not `new () { ... }` for collections
7. **SubView/SuperView** for containment (Parent/Child only for non-containment refs)
8. **Unused lambda params** - use `_`: `(_, _) => { }`
9. **Early return / guard clauses** - ALWAYS invert conditions and return/continue early. Never wrap the happy path in a conditional. Applies to methods, lambdas, and loops. See `.claude/rules/early-return.md`.
10. **One type per file** - Public and internal types each get their own file
11. **Docs instruction style** - In reference/how-to/API docs, write `To [goal], [imperative action].` Avoid `When/If you want/need to ...` unless describing a real condition.

## Testing

- Add new tests to `UnitTestsParallelizable`; use `UnitTests.NonParallelizable` only when static state is unavoidable. Never add to `UnitTests.Legacy`.
- Add a comment marking the test as AI-generated. Either form is acceptable: `// Claude - <model>` or `// CoPilot - <model>` — just include the agent and the model that produced the test (e.g., `// Claude - Opus 4.5` or `// CoPilot - ChatGPT v4`). Both forms are established in the codebase; which marker is used is not a style concern and reviewers should not flag inconsistency between them.
- Never decrease coverage
- Avoid `Application.Init` in tests

## Repository Structure

```
/Terminal.Gui/     - Core library
/Tests/            - Unit tests
/Examples/UICatalog/ - Demo app
/docfx/docs/       - Documentation
/.claude/          - AI agent guidance
```

## What NOT to Do

- Don't forget space before `()` and `[]` - this is the #1 mistake!
- Don't put braces on same line (use Allman style)
- Don't skip blank lines before returns or after control blocks
- Don't use `var` for non-built-in types
- Don't use redundant type names with `new`
- Don't say "child/parent" for containment (use SubView/SuperView)
- Don't wrap the happy path in a conditional — use guard clauses and return early
- Don't modify unrelated code
- Don't introduce new warnings
- Don't skip POST-GENERATION-VALIDATION.md after writing code
