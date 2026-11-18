# Collection Expressions

**Use `[...]` syntax instead of `new () { ... }`:**

## Examples

```csharp
// CORRECT
List<string> items = ["one", "two", "three"];
AllSuggestions = ["word1", "word2", "word3"];
MenuItem[] menuItems = [new MenuItem { Title = "File" }, new MenuItem { Title = "Edit" }];

// WRONG
List<string> items = new () { "one", "two", "three" };
AllSuggestions = new () { "word1", "word2", "word3" };
MenuItem[] menuItems = new MenuItem[] { new MenuItem { Title = "File" }, new MenuItem { Title = "Edit" } };
```

## Empty collections

```csharp
// CORRECT
List<View> views = [];
return [];

// WRONG
List<View> views = new ();
return new List<View>();
```

## Spread operator

```csharp
// CORRECT
return [.. menuItems];

// WRONG
return menuItems.ToArray();
```

## Why this matters
- Modern C# 12 syntax
- More concise and readable
- Consistent with project style
