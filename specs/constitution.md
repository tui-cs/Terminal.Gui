# Terminal.Gui — Constitution

> The tenets in each section are listed in **precedence order**. When two tenets conflict, the one higher in this document wins.

This document is the single authoritative source for Terminal.Gui's product mission, non-goals, engineering philosophy, and design tenets. All other documents (`CONTRIBUTING.md`, `CLAUDE.md`, `.claude/rules/`, and the deep-dive docs in `docfx/docs/`) elaborate on these tenets; they do not supersede them.

## Table of Contents

- [I. Mission](#i-mission)
- [II. Non-Goals](#ii-non-goals)
- [III. Tenets](#iii-tenets)
- [IV. Engineering Philosophy](#iv-engineering-philosophy)
- [V. Code Style Tenets](#v-code-style-tenets)
- [Relationship to Sub-Projects](#relationship-to-sub-projects)

---

## I. Mission

Terminal.Gui is a **cross-platform UI toolkit for building sophisticated terminal UI (TUI) applications** on .NET. It is the standard by which TUI applications on .NET are measured.

---

## II. Non-Goals

These were considered and rejected — do not accidentally pursue them:

- **Terminal.Gui is not a web framework.** We do not pursue HTML/CSS layout models.
- **Terminal.Gui is not a replacement for ncurses.** We target .NET developers, not C developers.
- **Terminal.Gui is not a pixel renderer.** Width is measured in terminal cells, not pixels.
- **Terminal.Gui is not opinionated about application architecture.** We provide building blocks; we do not mandate MVVM, MVC, or any other application pattern.
- **Terminal.Gui does not own the terminal.** We share it with the host shell and must be good citizens (clean up on exit, respect terminal state).

---

## III. Tenets

### Users Have Final Control

Users choose the platform, the terminal, and the key bindings. Our defaults are consistent and sensible, but everything configurable must be configurable. We never hardcode behavior that the user or developer cannot override. See the [Keyboard deep dive](../docfx/docs/keyboard.md) and [Mouse deep dive](../docfx/docs/mouse.md).

### Keyboard First; Mouse Optional

Terminal users expect full functionality without a mouse. Anything that can be done with the mouse must also be doable with the keyboard. We avoid mouse-only features. See the [Mouse deep dive](../docfx/docs/mouse.md).

### More Editor Than Command Line

Once a Terminal.Gui app starts, the user is no longer using the command line. Users expect keyboard idioms consistent with GUI apps (VS Code, Vim, Emacs, etc.), not shell idioms. See the [Keyboard deep dive](../docfx/docs/keyboard.md).

### Be Consistent With the User's Platform

Users choose their platform. Terminal.Gui apps must respond to keyboard and mouse input in a way consistent with platform conventions. The source of truth for default key bindings is [Wikipedia's keyboard shortcuts table](https://en.wikipedia.org/wiki/Table_of_keyboard_shortcuts). See the [Keyboard deep dive](../docfx/docs/keyboard.md).

### If It's Hot, It Works

If a `View` with a `HotKey` is visible and the HotKey is shown, pressing that HotKey must invoke the defined behavior. We strive to ensure that modal contexts do not leave HotKeys appearing active when they are not. See the [Keyboard deep dive](../docfx/docs/keyboard.md).

### Separation of Concerns

Layout, focus, input, and drawing are cleanly decoupled. We resist the urge to merge them for short-term convenience. See the [v2 Architecture overview](../docfx/docs/newinv2.md) and [Layout deep dive](../docfx/docs/layout.md).

### Testability First

Views must be testable in isolation without global state. `Application.Init` is required only for integration tests. We maintain ≥80% test coverage and we never decrease it. See [Testing patterns](../.claude/rules/testing-patterns.md).

### Performance Is a Feature

We measure rendering and event-handling overhead. We never accept regressions in the hot path without a documented justification. See the [Drawing deep dive](../docfx/docs/drawing.md).

### Documentation Is the Spec

API documentation is the contract. When docs and code conflict, the code is wrong. See [api-documentation rules](../.claude/rules/api-documentation.md) and Code Style Tenet 5 in [CONTRIBUTING.md](../CONTRIBUTING.md).

### Think in Graphemes, Not Runes

Text measurement and rendering always operate on grapheme clusters, not `char` or `Rune` values. Always use `string.GetColumns()` for width; always iterate with `GraphemeHelper.GetGraphemes()` for rendering. See [Unicode/Grapheme rules](../.claude/rules/unicode-graphemes.md).

---

## IV. Engineering Philosophy

Developers — AI agents and humans — working on Terminal.Gui strive to raise the bar as Principal Engineers. Principal Engineers are measured by how they live the [Amazon PE Community Tenets](https://www.amazon.jobs/content/en/teams/principal-engineering/tenets):

1. **Exemplary practitioner** — set the standard through your own work.
2. **Technically fearless** — tackle the hardest, most ambiguous problems.
3. **Lead with empathy** — foster inclusion; be mindful of your impact.
4. **Balanced and pragmatic** — neither dogmatic nor reckless.
5. **Illuminate and clarify** — bring clarity to complexity; drive crisp decisions.
6. **Flexible in approach** — adapt style and methods to the problem at hand.
7. **Respect what came before** — appreciate existing systems; learn from the past.
8. **Learn, educate, and advocate** — pursue continuous learning and teach others.
9. **Have resounding impact** — results are the minimum; lasting impact is the bar.

---

## V. Code Style Tenets

*(Source of truth: [CONTRIBUTING.md](../CONTRIBUTING.md))*

1. **Six-Year-Old Reading Level** — Readability over terseness.
2. **Consistency, Consistency, Consistency** — Follow existing patterns ruthlessly.
3. **Don't Be Weird** — Follow Microsoft/.NET conventions.
4. **Set and Forget** — Rely on automated tooling; don't fight the formatter.
5. **Documentation Is the Spec** — API docs define the contract; implementation must match.

---

## Relationship to Sub-Projects

Sub-projects (e.g., `Terminal.Gui.Text`) may extend this constitution. When a sub-project tenet conflicts with a tenet in this document, this document wins unless the sub-project explicitly documents the exception and the reason.
