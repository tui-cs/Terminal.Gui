# UnitTests.Parallelizable

This project contains unit tests that can run in parallel without interference. Tests here must not depend on global state or static Application infrastructure.

### Important Notes
- Many tests in `UnitTests` blindly use the the legacy model they don't actually need to
- These tests CAN be rewritten to remove unnecessary dependencies and migrated here
- Many tests APPEAR to be integration tests but are just poorly written and cover multiple surface areas - these can be split into focused unit tests
- When in doubt, analyze if the test truly needs global state or can be refactored

## Example Migrations

### Simple Property Test (no changes needed)
```csharp
// Before (in UnitTests)
[Fact]
public void Constructor_Sets_Defaults ()
{
    var view = new Button ();
    Assert.Empty (view.Text);
}

// After (in UnitTests.Parallelizable) - just move it!
[Fact]
public void Constructor_Sets_Defaults ()
{
    var view = new Button ();
    Assert.Empty (view.Text);
}
```

### Remove Unnecessary [SetupFakeApplication]
```csharp
// Before (in UnitTests)
[Fact]
[SetupFakeApplication]
public void Event_Fires_When_Property_Changes ()
{
    var view = new Button ();
    var fired = false;
    view.TextChanged += (s, e) => fired = true;
    view.Text = "Hello";
    Assert.True (fired);
}

// After (in UnitTests.Parallelizable) - remove attribute!
[Fact]
public void Event_Fires_When_Property_Changes ()
{
    var view = new Button ();
    var fired = false;
    view.TextChanged += (s, e) => fired = true;
    view.Text = "Hello";
    Assert.True (fired);
}
```

### Replace Application.Begin with View Initialization
```csharp
// Before (in UnitTests)
[Fact]
[AutoInitShutdown]
public void Focus_Test ()
{
    var view = new Button ();
    var top = new Runnable ();
    top.Add (view);
    Application.Begin (top);
    view.SetFocus ();
    Assert.True (view.HasFocus);
    top.Dispose ();
}

// After (in UnitTests.Parallelizable) - use BeginInit/EndInit!
[Fact]
public void Focus_Test ()
{
    var superView = new View ();
    var view = new Button ();
    superView.Add (view);
    superView.BeginInit ();
    superView.EndInit ();
    view.SetFocus ();
    Assert.True (view.HasFocus);
}
```

## Running Tests

Tests in this project run in parallel automatically. To run them:

```bash
dotnet test --project Tests/UnitTestsParallelizable
```

## See Also

- [.NET Unit Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
