# Known Fragile Areas

Areas of the codebase where seemingly-safe refactors cause cascading failures. Do not "fix" these in passing — file a separate issue instead.

## TextView Initialization Ordering

Do not change `TextView`'s `EndInit` ordering or initialization flow. Moving `base.EndInit ()` before `UpdateContentSize ()`/`UpdateScrollBars ()` fixes some tests but breaks others in the non-parallel test project.

**Why:** `TextView` has complex initialization dependencies, and the non-parallel tests rely on specific ordering.

**How to apply:** If `TextView` ContentSize tests fail, note the root cause (`UpdateContentSize` runs before `IsInitialized` is set) but do NOT fix it by reordering `EndInit`. File a separate issue or let a maintainer decide when to address it.
