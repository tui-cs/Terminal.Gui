# UnitTests.NonParallelizable

This project contains tests that must not run concurrently with other tests because they read or write
process-wide static state.

## When to add tests here

Add a test here if it does **any** of the following:

- Calls `Application.Init()` or `Application.Shutdown()` directly (not through `SetupFakeApplication`).
- Mutates `Application.DefaultKeyBindings` (a process-wide static dictionary).
- Calls `ApplicationImpl.ResetModelUsageTracking()`.
- Calls `Application.Create()` and tests the static-vs-instance model fencing.
- Sets `SynchronizationContext.Current` or depends on it being in a particular state.
- Otherwise cannot be run concurrently with other tests due to shared mutable static state.

## What does NOT belong here

- Tests that use `[SetupFakeApplication]` or `ApplicationImpl.SetInstance()` — those use a fake driver
  instance and do not touch global static state in a way that prevents parallelism. They may belong in
  `UnitTestsParallelizable` after review.
- New feature tests — those belong in `UnitTestsParallelizable`.

See the [Testing wiki](https://github.com/tui-cs/Terminal.Gui/wiki/Testing) for details.
