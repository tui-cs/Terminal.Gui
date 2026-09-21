# Agent Design Loop (Proposal)

This proposal adapts the WinUI plugin model ([microsoft/win-dev-skills](https://github.com/microsoft/win-dev-skills)) for the `gui-cs` ecosystem and incorporates the design-loop concept from [tig/winprint#73](https://github.com/tig/winprint/issues/73).

## Problem

AI agents can generate Terminal.Gui apps, but they need a fast, reliable way to **see** what was rendered and share it with humans in chat-first workflows.

## Proposed End State

Create a new `gui-cs` repository (working name: `gui-cs/tui-dev-skills`) that provides a Copilot/Claude/Codex plugin with a focused TUI agent and skills.

### Suggested repository layout

```text
.github/plugin/           marketplace metadata
plugins/tui/              plugin manifest
  agents/tui-dev/         orchestrator agent
  skills/                 focused skills
src/tools/                helper CLIs used by skills
```

### Suggested skill set

- `tui-setup` - install/verify prerequisites for Terminal.Gui, `dotnet`, and `tuirec`
- `tui-design` - headless render + iterative app UX/layout refinement
- `tui-dev-workflow` - scaffold/build/run/fix inner-loop workflow
- `tui-snapshot` - capture/compare golden screen snapshots
- `tui-record` - produce GIF/video evidence using `tuirec`
- `tui-code-review` - TUI-specific review pass before commit
- `tui-session-report` - summarize what happened in an agent session, including what the agent learned

## Design-loop contract across repos

The design loop should be first-class and consistent:

1. Agent edits app code (views, state, commands, and workflows)
2. Agent renders offscreen in a deterministic terminal size
3. Agent captures the **full text grid**
4. Agent optionally rasterizes to PNG/GIF for human review
5. Accepted grid becomes a golden snapshot artifact

## Scope and rollout

The proposal should target full Terminal.Gui app development as the first milestone.

1. **Phase 1 (now):** deeply optimize for Terminal.Gui app authoring and review.
2. **Phase 2:** generalize the same design-loop contracts to other .NET TUI frameworks.
3. **Phase 3:** extend the plugin model to non-.NET TUI frameworks where the same artifacts/contracts apply.

## Self-improvement feedback loop

The plugin should include a built-in path for agents to improve guidance over time:

1. `tui-session-report` emits a structured "lessons learned" block (failures, recovery steps, missing docs, tool friction).
2. A dedupe step computes a stable fingerprint so repeated sessions with the same lesson do not create duplicates.
3. Only high-signal lessons open issues in the plugin repo (for example: seen in multiple sessions or marked as blocking).
4. Issue creation is rate-limited (for example: max N auto-filed issues per day) and labels items as `agent-feedback`.
5. Each issue links evidence (session id, logs/snapshots, repro steps) and a concrete proposed guidance/tooling update.

## Required enhancements by repo

### Terminal.Gui (this repo)

- Preserve and document that `IDriver.ToString ()` is the canonical full-grid capture for snapshots/design review.
- Preserve and document that `IDriver.ToAnsi ()` is terminal-output-oriented and should not be used as the golden snapshot source.
- Keep headless rendering deterministic (fixed size + stable frame capture).

### gui-cs/tuirec

- Add a still-image/snapshot mode aligned with the same full-grid-first contract.
- Keep GIF/video record paths and snapshot paths compatible in keystroke scripting.

### gui-cs/cli

- Add commands that expose the loop as one-step workflows (build/run/render/snapshot/report).
- Emit structured outputs suitable for agent consumption.

## Why this matters

This turns TUI design review into a tight human+agent loop while simultaneously producing regression artifacts. The same render used for design feedback can be reused in CI snapshot tests.
