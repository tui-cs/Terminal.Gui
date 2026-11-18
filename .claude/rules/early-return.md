# Early Return / Guard Clauses

**Invert conditions, return/continue early, keep happy path at lowest indentation. Applies everywhere: methods, lambdas, loops.**

## ✅ Do / ❌ Don't

```csharp
// ✅ Guard clause
if (view is null) { return; }
DoWork (view);

// ❌ Wrapped happy path
if (view is not null) { DoWork (view); }
```

```csharp
// ✅ Sequential guards in lambda
button.Accepting += (_, args) =>
                    {
                        if (_target is null) { return; }
                        if (args.Cancel) { return; }
                        _target.DoWork ();
                    };

// ❌ Compound condition wrapping lambda body
button.Accepting += (_, args) =>
                    {
                        if (_target is not null && !args.Cancel)
                        {
                            _target.DoWork ();
                        }
                    };
```

```csharp
// ✅ Continue in loops
foreach (View subView in SubViews)
{
    if (!subView.Visible) { continue; }
    subView.Draw ();
}

// ❌ Loop body inside conditional
foreach (View subView in SubViews)
{
    if (subView.Visible) { subView.Draw (); }
}
```

```csharp
// ✅ Guard early before tail work
int total = widths.Sum ();
if (total <= available) { return widths; }
int excess = total - available;
ReduceEvenly (widths, minWidths, excess);
return widths;

// ❌ Tail work wrapped in conditional
int total = widths.Sum ();
if (total > available)
{
    int excess = total - available;
    ReduceEvenly (widths, minWidths, excess);
}
return widths;
```

## Key Principle

When adding a null check or validation, **add a guard at the top** — don't indent existing code into a new `if` block. Compound conditions are fine in guard clauses (`if (x <= 0 || y <= 0) { return; }`) but never for wrapping the happy path.
