# Cancellable Workflow Pattern (CWP)

## Overview

CWP is the standard pattern for implementing events in Terminal.Gui.

## Key Principles

1. **Virtual `OnXXX` methods** - Should be empty in base class (for subclass override)
2. **Work happens BEFORE** the notification, not after
3. **Events** are raised after the virtual method call

## Implementation Pattern

```csharp
// CORRECT CWP pattern
internal void RaiseSubViewAdded (View view)
{
    // 1. Do work BEFORE notifications
    if (AssignHotKeys)
    {
        AssignHotKeyToView (view);
    }

    // 2. Call virtual method (empty in base class)
    OnSubViewAdded (view);

    // 3. Raise event
    SubViewAdded?.Invoke (this, new (this, view));
}

// Virtual method - empty in base, subclasses override
protected virtual void OnSubViewAdded (View view) { }
```

## WRONG Pattern

```csharp
// WRONG - work after notification
internal void RaiseSubViewAdded (View view)
{
    OnSubViewAdded (view);
    SubViewAdded?.Invoke (this, new (this, view));

    // WRONG: work should be before notifications
    if (AssignHotKeys)
    {
        AssignHotKeyToView (view);
    }
}
```

## See Also

- `docfx/docs/cancellable-work-pattern.md` for full documentation
