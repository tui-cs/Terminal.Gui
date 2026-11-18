# Type Declarations

**NEVER use `var` except for built-in types.**

## Built-in types (OK to use var):
`int`, `string`, `bool`, `double`, `float`, `decimal`, `char`, `byte`

## Examples

```csharp
// CORRECT
Label label = new () { Text = "Hello" };
List<View> views = [];
Window win = new () { Title = "Test" };
var count = 0;      // OK - int
var text = "hello"; // OK - string

// WRONG
var label = new Label { Text = "Hello" };
var views = new List<View>();
var win = new Window { Title = "Test" };
```

## Why this matters
- Explicit types improve code readability
- Makes code self-documenting
- Consistent with project style
