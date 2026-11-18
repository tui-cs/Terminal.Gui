# Post-Generation Validation Checklist

**USE THIS CHECKLIST AFTER GENERATING OR MODIFYING ANY CODE.**

This is a mandatory validation step. AI agents frequently make formatting and style errors that violate Terminal.Gui conventions. Scan every line of generated code before considering the task complete.

## Part 1: Formatting Violations (MOST CRITICAL)

These are the **most commonly violated rules**. Check EVERY line:

### Space Before Parentheses ⚠️ #1 MISTAKE
```csharp
// CORRECT ✓
void MyMethod ()
int result = Calculate (x, y);
var items = GetItems ();
if (condition)
using (var obj = Create ())

// WRONG ✗ - SCAN FOR THESE
void MyMethod()           // Missing space before ()
int result = Calculate(x, y);
var items = GetItems();
if(condition)
using(var obj = Create())
```

**Scan pattern:** Look for `\w(` (word character followed immediately by `(`)

### Space Before Brackets ⚠️ #2 MISTAKE
```csharp
// CORRECT ✓
var value = array [index];
var item = list [0];
MyArray [i] = value;

// WRONG ✗ - SCAN FOR THESE
var value = array[index];  // Missing space before [
var item = list[0];
MyArray[i] = value;
```

**Scan pattern:** Look for `\w[` (word character followed immediately by `[`)

### Braces on Next Line ⚠️ #3 MISTAKE
```csharp
// CORRECT ✓
void MyMethod ()
{
    if (condition)
    {
        DoWork ();
    }
}

// WRONG ✗ - SCAN FOR THESE
void MyMethod() {          // Brace on same line
void MyMethod () {         // Brace on same line
    if (condition) {       // Brace on same line
        DoWork();
    }
}
```

**Scan pattern:** Look for `) {` or `= {` (brace on same line)

### Blank Lines ⚠️ #4 MISTAKE
```csharp
// CORRECT ✓ - blank line BEFORE control transfer
DoWork ();

return result;  // Blank line above

// CORRECT ✓ - blank line AFTER control block
if (condition)
{
    DoWork ();
}

DoNext ();  // Blank line above

// WRONG ✗ - SCAN FOR THESE
DoWork ();
return result;  // No blank line above return

if (condition) { DoWork (); }
DoNext ();  // No blank line after control block
```

**Control transfer statements:** `return`, `break`, `continue`, `throw`
**Control blocks:** `if`, `for`, `while`, `foreach`, `using`, `try`/`catch`

### Indentation
```csharp
// CORRECT ✓ - 4 spaces per level
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

// WRONG ✗ - tabs or wrong spacing
public class MyClass
{
	public void MyMethod ()  // Tab instead of spaces
	{
    if (condition)           // 2 spaces instead of 4
    {
```

**Rule:** 4 spaces per indentation level. NO tabs.

## Part 2: Control Flow Violations

### Early Return / Guard Clauses ⚠️ COMMONLY VIOLATED

```csharp
// CORRECT ✓ — guard clause, return early
if (view is null)
{
    return;
}

DoWork (view);

// WRONG ✗ — happy path wrapped in conditional
if (view is not null)
{
    DoWork (view);
}

// CORRECT ✓ — early return in lambda
button.Accepting += (_, args) =>
                    {
                        if (_target is null)
                        {
                            return;
                        }

                        _target.DoWork ();
                    };

// WRONG ✗ — lambda body wrapped in conditional
button.Accepting += (_, args) =>
                    {
                        if (_target is not null)
                        {
                            _target.DoWork ();
                        }
                    };

// CORRECT ✓ — continue in loop
foreach (View subView in SubViews)
{
    if (!subView.Visible)
    {
        continue;
    }

    subView.Draw ();
}

// WRONG ✗ — loop body wrapped in conditional
foreach (View subView in SubViews)
{
    if (subView.Visible)
    {
        subView.Draw ();
    }
}
```

**Scan pattern:** Look for `if (condition) { ... } return` — the `if` block likely wraps happy-path code and should be inverted into a guard clause.

**Rule:** ALWAYS invert conditions and return/continue early. NEVER wrap the happy path in a conditional. See `.claude/rules/early-return.md` for full guidance.

## Part 3: Code Style Violations

### No `var` for Non-Built-In Types
```csharp
// CORRECT ✓
Label label = new () { Text = "Hello" };
List<View> views = [];
Window window = new ();
var count = 0;       // OK - int is built-in
var text = "hello";  // OK - string is built-in

// WRONG ✗
var label = new Label { Text = "Hello" };
var views = new List<View>();
var window = new Window();
```

**Built-in types where var is OK:** `int`, `string`, `bool`, `double`, `float`, `decimal`, `char`, `byte`

### Target-Typed New
```csharp
// CORRECT ✓
Label label = new () { Text = "Hello" };
Window window = new ();

// WRONG ✗
Label label = new Label() { Text = "Hello" };
Window window = new Window();
```

### Collection Expressions
```csharp
// CORRECT ✓
List<string> items = ["one", "two", "three"];
AllSuggestions = ["word1", "word2", "word3"];
return [];

// WRONG ✗
List<string> items = new () { "one", "two", "three" };
AllSuggestions = new () { "word1", "word2", "word3" };
return new List<string>();
```

### Lambda Parameter Discards
```csharp
// CORRECT ✓
textField.TextChanged += (_, _) => { /* ... */ };
button.Accepting += (_, args) => ProcessArgs (args);

// WRONG ✗ - unused parameters
textField.TextChanged += (sender, e) => { /* ... */ };
textField.TextChanged += (s, prev) => { /* ... */ };
```

### Terminology
```csharp
// CORRECT ✓ - containment relationship
View superView = new ();
View subView = new ();
superView.Add (subView);  // subView.SuperView == superView

// WRONG ✗ - for containment
View parent = new ();     // Should be: superView
View child = new ();      // Should be: subView
parent.Add (child);

// Comments:
// Add the button as a SubView of the window
// The dialog's SuperView is the application

// WRONG ✗ - in comments
// Add the button as a child of the window
// The dialog's parent is the application
```

**Use SuperView/SubView for containment.** Parent/Child only for non-containment references (rare).

## Part 3: Quick Scan Commands

Use these bash commands to catch common violations:

```bash
# Missing space before ( - most common error
grep -nE '\w\(' YourFile.cs | grep -v '//'

# Missing space before [
grep -nE '\w\[' YourFile.cs | grep -v '//'

# Brace on same line
grep -n ') {' YourFile.cs
grep -n '= {' YourFile.cs

# Var usage (manually verify each is built-in type)
grep -n '\bvar\b' YourFile.cs

# Trailing whitespace
grep -n ' $' YourFile.cs
```

## Part 4: Manual Review

After automated checks, manually review:

1. **Every method call/declaration** - space before `()`?
2. **Every array access** - space before `[]`?
3. **Every opening brace** - on next line?
4. **Every return statement** - blank line before it?
5. **Every control block** - blank line after it?

## Validation Frequency

Use this checklist:
- ✅ After generating any new class or file
- ✅ After modifying existing methods (scan modified lines)
- ✅ After completing any task that involves code generation
- ✅ Before creating a commit or pull request
- ✅ When the code "looks wrong" visually

## Why This Matters

Formatting violations:
- Create noise in code reviews
- Violate project CI/CD checks
- Make code inconsistent with the rest of Terminal.Gui
- Are the #1 complaint about AI-generated code
- Are easily preventable with this checklist

**The space-before-parentheses style is unusual compared to most C# projects, making it the most commonly violated rule.**
