# Target-Typed New

**ALWAYS use `new ()` when type is declared on the left side.**

## Examples

```csharp
// CORRECT
Label label = new () { Text = "Hello" };
Window window = new () { Title = "Test" };
List<View> views = [];

// WRONG - redundant type name
Label label = new Label() { Text = "Hello" };
Window window = new Window() { Title = "Test" };
List<View> views = new List<View>();
```

## Exception: Collection initializers needing type inference

When the compiler can't infer the element type, use explicit type:

```csharp
// MenuItem must be explicit - array type inference needs it
new PopoverMenu ([
    new MenuItem { Title = "Item 1" },  // Explicit required
    new Line (),
    new MenuItem { Title = "Item 2" }
]);
```

## Why this matters
- Reduces redundancy
- Cleaner, more modern C# style
- Consistent with project conventions
