# Logging and Tracing

**Never use `Console.WriteLine` or `Console.Error.WriteLine` for debug output.** Console output interferes with the terminal UI framework. Use the project's logging infrastructure instead:

| Need | Use |
|------|-----|
| Library logging | `Terminal.Gui.App.Logging` |
| Test output | `Terminal.Gui.Tests.TestLogging` |
| Debug tracing | `Terminal.Gui.Tracing.Trace` |

## Test Pattern

```csharp
using Terminal.Gui.Tests;
using Terminal.Gui.Tracing;

[Fact]
public void MyTest ()
{
    // Enable logging and tracing in one call
    using (TestLogging.Verbose (_output, TraceCategory.Command))
    {
        // Logs and traces appear in xUnit test output
        CheckBox checkbox = new () { Id = "test" };
        checkbox.InvokeCommand (Command.Activate);
    }
}
```

## Rules

- `Tracing.Trace` is only available in DEBUG builds; do not use it to validate test results — all tests must pass in RELEASE builds.
- Remove temporary debug tracing before committing.

See `docfx/docs/logging.md` for full details.
