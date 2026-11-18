# Code Formatting Rules

**CRITICAL: AI agents frequently violate these formatting rules. Check EVERY line of code you write.**

## Most Commonly Violated Rules

### 1. Spacing Before Parentheses (CRITICAL!)

```csharp
// CORRECT - space BEFORE parentheses
void MyMethod ()
int result = Calculate (x, y);
var items = GetItems ();
var value = array [index];

// WRONG - no space before parentheses
void MyMethod()
int result = Calculate(x, y);
var items = GetItems();
var value = array[index];
```

**Rule:** Space BEFORE:
- Method declaration parentheses
- Method call parentheses
- Array/indexer brackets
- `if`, `while`, `for`, `foreach`, `switch`, `using`, `catch` parentheses

### 2. Brace Placement

```csharp
// CORRECT - braces on next line
void MyMethod ()
{
    if (condition)
    {
        DoSomething ();
    }
}

// WRONG - braces on same line
void MyMethod() {
    if (condition) {
        DoSomething();
    }
}
```

**Rule:** ALL opening braces on the NEXT line (Allman style).

### 3. Blank Lines

```csharp
// CORRECT
DoWork ();

return result;  // Blank line BEFORE return

// CORRECT
if (condition)
{
    DoWork ();
}

DoSomethingElse ();  // Blank line AFTER control block

// WRONG - no blank line before return
DoWork ();
return result;
```

**Rules:**
- 1 blank line BEFORE control transfer statements (`return`, `break`, `continue`, `throw`)
- 1 blank line AFTER control blocks (`if`, `for`, `while`, `foreach`)
- 1 blank line BEFORE single-line comments
- 0 blank lines inside type declarations at start/end

### 4. Indentation

```csharp
// CORRECT - 4 spaces per level
public class MyClass
{
    public void MyMethod ()
    {
        if (condition)
        {
            DoWork ();
        }
    }
}
```

**Rule:** 4 spaces per indentation level. NEVER tabs.

### 5. Expression Bodies

```csharp
// CORRECT - use expression bodies when appropriate
public int Count => _items.Count;
public string Name => _name;
public void Clear () => _items.Clear ();

// CORRECT - use block bodies for complex logic
public int Calculate ()
{
    int result = DoComplexWork ();

    return result * 2;
}
```

**Rule:** Prefer expression bodies (`=>`) for simple single-expression members.

## Additional Spacing Rules

```csharp
// After comma, colon, semicolon
Method (a, b, c);
base : MyBase
for (int i = 0; i < 10; i++)

// Around binary operators
int sum = a + b;
bool result = x == y && z != 0;

// NO space after cast, dot, or inside parentheses
var x = (int)value;
object.Property.Method ();
Calculate (a, b);  // NO space inside parens
```

## Quick Checklist

Before submitting code, verify:

- [ ] Space BEFORE all method call/declaration parentheses
- [ ] Space BEFORE array brackets
- [ ] ALL braces on next line
- [ ] Blank line BEFORE `return`/`break`/`continue`/`throw`
- [ ] Blank line AFTER control blocks
- [ ] 4-space indentation (not tabs)
- [ ] Expression bodies for simple properties/methods
- [ ] Space after commas, around binary operators

## When Modifying Existing Code

1. **Match the surrounding style** if it differs from these rules
2. **Only reformat code you're actively changing** - don't reformat entire files
3. **Run ReSharper's "Cleanup Code"** if available (Ctrl+E, C)

## Pro Tips

- The spacing before parentheses is UNUSUAL compared to most C# codebases
- This is the #1 mistake AI agents make
- When in doubt about formatting, read a similar file in the codebase
- `.editorconfig` and `Terminal.sln.DotSettings` contain the complete rules
