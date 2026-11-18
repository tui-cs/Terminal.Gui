# Pre-Edit Checklist

**READ THIS BEFORE MODIFYING ANY FILE.**

## Quick Rules (memorize these)

1. **No `var`** except for: `int`, `string`, `bool`, `double`, `float`, `decimal`, `char`, `byte`
2. **Use `new ()`** not `new TypeName()` when type is on left side
3. **Use `[...]`** not `new () { ... }` for collections
4. **SubView/SuperView** - never say "child", "parent", or "container"
5. **Unused lambda params** - use `_` discard: `(_, _) => { }`
6. **Local functions** - use PascalCase: `void MyLocalFunc ()`
7. **Backing fields** - place immediately before their property (ReSharper bug, must do manually)
8. **SPACE BEFORE PARENTHESES** - `Method ()` not `Method()`, `array [i]` not `array[i]` (see `formatting.md`)
9. **Braces on next line** - ALL opening braces on next line (Allman style)
10. **Blank lines** - before `return`/`break`/`continue`/`throw`, after control blocks
11. **Early return / guard clauses** - ALWAYS invert conditions and return/continue early. NEVER wrap the happy path in a conditional. This applies in methods, lambdas, loops — everywhere. (see `early-return.md`)

## Before Each File Edit

Ask yourself:
- [ ] Am I using explicit types (not var)?
- [ ] Am I using target-typed new ()?
- [ ] Am I using collection expressions []?
- [ ] Are my lambda parameters discards if unused?
- [ ] Am I using correct terminology (SubView, not child)?
- [ ] **Did I add space BEFORE parentheses and brackets?**
- [ ] **Are ALL braces on the next line?**
- [ ] **Did I add blank lines before returns and after control blocks?**
- [ ] **Am I using guard clauses / early return instead of nesting?**

## If Unsure

Re-read the relevant rule file in `.claude/rules/`:
- `formatting.md` - **SPACING, BRACES, BLANK LINES** (most commonly violated!)
- `type-declarations.md` - var vs explicit types
- `target-typed-new.md` - new() syntax
- `terminology.md` - SubView/SuperView terms
- `event-patterns.md` - lambdas, closures, handlers
- `collection-expressions.md` - [...] syntax
- `cwp-pattern.md` - Cancellable Workflow Pattern
- `code-layout.md` - backing fields, member ordering

## Task-Specific Guides

Check `.claude/tasks/` for task-specific checklists:
- `build-app.md` - Building applications with Terminal.Gui

Check `.claude/cookbook/` for common UI patterns.
