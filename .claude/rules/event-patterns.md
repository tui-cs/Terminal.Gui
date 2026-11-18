# Event Patterns

## When to Use `-ing` vs `-ed` Events

Terminal.Gui exposes paired events — `Accepting`/`Accepted`, `Activating`/`Activated`, `ValueChanging`/`ValueChanged`, etc.

**Rule:** Use `-ed` (past-tense) for side-effects. Use `-ing` (present-progressive) only when you need to inspect or cancel the in-flight operation.

```csharp
// ✅ Correct — fire-and-forget side-effect
button.Accepted += (_, _) => DoTheThing ();

// ✅ Correct — actually cancels
button.Accepting += (_, e) => { if (!CanProceed ()) e.Handled = true; };

// ❌ Wrong — handler ignores EventArgs; use Accepted instead
button.Accepting += (_, _) => DoTheThing ();
```

If the handler body doesn't reference `e` at all (or ignores `e.Handled`, `e.Cancel`, and the candidate value), it belongs on the `-ed` event.

The `-ing` event runs synchronously in the middle of the dispatch path; subscribing when you don't need to cancel adds unnecessary overhead and misleads readers.

## Lambda Parameters

**Replace unused parameters with discards `_`:**

```csharp
// CORRECT
textField.TextChanged += (_, _) => { /* ... */ };
checkbox.CheckedStateChanging += (_, args) => checkbox.Enabled = args.Result == CheckState.Checked;

// WRONG - unused parameters
textField.TextChanged += (s, prev) => { /* ... */ };
textField.TextChanged += (sender, e) => { /* ... */ };
```

## Captured Variable Closures

**When a variable is captured and later modified, use sender:**

```csharp
// WRONG - textField captured, then reassigned later
textField.TextChanged += (_, _) => { label.Text = textField.Text; };
textField = new () { ... };  // Bug: closure references wrong instance

// CORRECT - use sender to get the actual firing instance
textField.TextChanged += (sender, _) => { label.Text = ((TextField)sender!).Text; };
```

**If sender won't work, disable the warning:**

```csharp
// ReSharper disable once AccessToModifiedClosure
textField.TextChanged += (_, _) => { label.Text = textField.Text; };
```

## Event Handler Nullability

**Use `object?` for sender parameter:**

```csharp
// CORRECT
void OnTextChanged (object? sender, EventArgs e) { }

// WRONG
void OnTextChanged (object sender, EventArgs e) { }
```

## Local Function Naming

**Use PascalCase for local functions** (the `.editorconfig` `local_functions_rule` enforces `upper_camel_case_style` at `warning` severity):

```csharp
// CORRECT
void TextViewDrawContent (object? sender, DrawEventArgs e) { }

// WRONG
void TextView_DrawContent (object? sender, DrawEventArgs e) { }
void textViewDrawContent (object? sender, DrawEventArgs e) { }
```
