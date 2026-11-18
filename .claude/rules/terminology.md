# Terminology

**Use precise terms. See `docfx/docs/View.md` for full definitions.**

## View Relationships

| Term | Meaning | When to Use |
|------|---------|-------------|
| **SubView** | A View contained in another View via `Add()` | Most common - containment |
| **SuperView** | The View that contains SubViews | Most common - the container |
| **Child View** | A view with a parent/child reference (NOT containment) | Rare - non-containment relationships |
| **Parent View** | A view referenced by a child (NOT a SuperView) | Rare - non-containment relationships |

## Key Distinction

- **SuperView/SubView** = Containment via `View.Add()` - this is the common case
- **Parent/Child** = Other reference relationships that are NOT containment - use sparingly

## Examples

```csharp
// CORRECT - containment relationship
View superView = new ();
View subView = new () { Title = "SubView" };
superView.Add (subView);  // subView.SuperView == superView

// WRONG - using "parent" for containment
View parent = new ();      // Should be: superView (if using Add)
View child = new ();       // Should be: subView (if using Add)
```

## Methods

| Correct | Context |
|---------|---------|
| `Add` / `Remove` | Adding/removing SubViews |
| NOT "append", "insert", "attach" | |

## In Code Comments

```csharp
// CORRECT - for containment
// Add the button as a SubView of the window
window.Add (button);

// CORRECT - for non-containment references (rare)
// The dialog holds a reference to its parent window
// (when NOT using Add/SuperView relationship)
```
