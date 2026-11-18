# Dim.Auto Deep Dive

## See Also

* [Lexicon & Taxonomy](lexicon.md)
* [Layout](layout.md)

## Overview

The <xref:Terminal.Gui.ViewBase.DimAuto> type is a specialized <xref:Terminal.Gui.ViewBase.Dim> class in Terminal.Gui v2 that enables automatic sizing of a <xref:Terminal.Gui.ViewBase.View> based on its content. This is particularly useful for dynamically sizing views to accommodate varying content such as text, subviews, or explicitly set content areas. Unlike other <xref:Terminal.Gui.ViewBase.Dim> types like `Dim.Absolute` or `Dim.Fill`, <xref:Terminal.Gui.ViewBase.DimAuto> calculates dimensions at runtime based on specified criteria, making it ideal for responsive UI design in terminal applications.

Like all <xref:Terminal.Gui.ViewBase.Dim> types, <xref:Terminal.Gui.ViewBase.DimAuto> is used to set the `Width` or `Height` of a view and can be combined with other <xref:Terminal.Gui.ViewBase.Dim> types using addition or subtraction (see `DimCombine`).

The <xref:Terminal.Gui.ViewBase.DimAutoStyle> enum defines the different strategies that <xref:Terminal.Gui.ViewBase.DimAuto> can employ to size a view. The <xref:Terminal.Gui.ViewBase.DimAutoStyle> enum has the following values:

- **Text**: The view is sized based on the `Text` property and `TextFormatter` settings. This considers the formatted text dimensions, constrained by any specified maximum dimensions.
- **Content**: The view is sized based on either the value returned by `View.GetContentSize()` or the `Subviews` property. If the content size is explicitly set (via `View.SetContentSize()`), the view is sized based on that value. Otherwise, it considers the subview with the largest relevant dimension plus its position.
- **Auto**: The view is sized based on both the text and content, whichever results in the larger dimension.

## Using Dim.Auto

<xref:Terminal.Gui.ViewBase.DimAuto> is defined as:

```cs
public static Dim Auto (DimAutoStyle style = DimAutoStyle.Auto, Dim minimumContentDim = null, Dim maximumContentDim = null)
```

To use <xref:Terminal.Gui.ViewBase.DimAuto>, set the `Width` or `Height` property of a view to `Dim.Auto(DimAutoStyle.Text)`, `Dim.Auto(DimAutoStyle.Content)`, or `Dim.Auto(DimAutoStyle.Auto)`.

For example, to create a <xref:Terminal.Gui.ViewBase.View> that is sized based on the `Text` property, you can do this:

```cs
View view = new ()
{
    Text = "Hello, World!",
    Width = Dim.Auto(DimAutoStyle.Text),
    Height = Dim.Auto(DimAutoStyle.Text),
};
```

Note, the built-in <xref:Terminal.Gui.Views.Label> view class does precisely this in its constructor.

To create a <xref:Terminal.Gui.ViewBase.View> that is sized based on its `Subviews`, you can do this:

```cs
View view = new ()
{
    Width = Dim.Auto(DimAutoStyle.Content),
    Height = Dim.Auto(DimAutoStyle.Content),
};
view.Add(new Label() { Text = "Hello, World!" });
```

In this example, the <xref:Terminal.Gui.ViewBase.View> will be sized based on the size of the <xref:Terminal.Gui.Views.Label> that is added to it.

### Specifying a Minimum Size

You can specify a minimum size by passing a <xref:Terminal.Gui.ViewBase.Dim> object to the `minimumContentDim` parameter. For example, to create a <xref:Terminal.Gui.ViewBase.View> that is sized based on the `Text` property, but has a minimum width of 10 columns, you can do this:

```cs
View view = new ()
{
    Text = "Hello, World!",
    Width = Dim.Auto(DimAutoStyle.Text, minimumContentDim: Dim.Absolute(10)), // Same as `minimumContentDim: 10`
    Height = Dim.Auto(DimAutoStyle.Text),
};
```

Sometimes it's useful to have the minimum size be dynamic. Use `Dim.Func` as follows:

```cs
View view = new ()
{
    Width = Dim.Auto(DimAutoStyle.Content, minimumContentDim: Dim.Func(GetDynamicMinSize)),
    Height = Dim.Auto(DimAutoStyle.Text),
};

int GetDynamicMinSize()
{
    return someDynamicInt;
}
```

### Specifying a Maximum Size

It is common to want to constrain how large a <xref:Terminal.Gui.ViewBase.View> can be sized. The `maximumContentDim` parameter to the `Dim.Auto()` method enables this. Like `minimumContentDim`, it is of type <xref:Terminal.Gui.ViewBase.Dim> and thus can represent a dynamic value. For example, <xref:Terminal.Gui.Views.Dialog> specifies `maximumContentDim` as `Dim.Percent(100)` minus its adornment thickness to ensure the dialog never overflows the screen.

```cs
View dialog = new ()
{
    Width = Dim.Auto(maximumContentDim: Dim.Percent(100) - Dim.Func(_ => GetAdornmentsThickness().Horizontal)),
    Height = Dim.Auto(maximumContentDim: Dim.Percent(100) - Dim.Func(_ => GetAdornmentsThickness().Vertical)),
};
```

## Technical Details

### Calculation Logic

The <xref:Terminal.Gui.ViewBase.DimAuto> class calculates dimensions dynamically during the layout process. Here's how it works under the hood, based on the codebase analysis:

- **Text-Based Sizing (`DimAutoStyle.Text`)**: When using `Text` style, the dimension is determined by the formatted text size as computed by `TextFormatter`. For width, it uses `ConstrainToWidth`, and for height, it uses `ConstrainToHeight`. These values are set based on the formatted text size, constrained by any maximum dimensions provided.
- **Content-Based Sizing (`DimAutoStyle.Content`)**: For `Content` style, if `ContentSizeTracksViewport` is `false` and there are no subviews, it uses the explicitly set content size from `GetContentSize()`. Otherwise, it iterates through subviews to calculate the maximum dimension needed based on their positions and sizes.
- **Auto Sizing (`DimAutoStyle.Auto`)**: This combines both `Text` and `Content` strategies, taking the larger of the two calculated dimensions.

The calculation in `DimAuto.Calculate` method also respects `minimumContentDim` and `maximumContentDim`:
- The final size is at least the minimum specified (if any), and at most the maximum specified (if any).
- Adornments (like <xref:Terminal.Gui.ViewBase.Margin>, <xref:Terminal.Gui.ViewBase.Border>, and <xref:Terminal.Gui.ViewBase.Padding>) are added to the calculated content size to ensure the view's frame includes these visual elements.

### Handling Subviews

When sizing based on subviews, <xref:Terminal.Gui.ViewBase.DimAuto> employs a sophisticated approach to handle dependencies:
- It categorizes subviews based on their <xref:Terminal.Gui.ViewBase.Pos> and <xref:Terminal.Gui.ViewBase.Dim> types to manage layout dependencies. For instance, it processes subviews with absolute positions and dimensions first, then handles more complex cases like `PosAnchorEnd` or `DimView`.
- This ensures that views dependent on other views' sizes or positions are calculated correctly, avoiding circular dependencies and ensuring accurate sizing.

### Adornments Consideration

The size calculation includes the thickness of adornments (<xref:Terminal.Gui.ViewBase.Margin>, <xref:Terminal.Gui.ViewBase.Border>, <xref:Terminal.Gui.ViewBase.Padding>) to ensure the view's total frame size accounts for these elements. This is evident in the code where `adornmentThickness` is added to the computed content size.

## Limitations

<xref:Terminal.Gui.ViewBase.DimAuto> is not always the best choice for sizing a view. Consider the following limitations:

- **Performance Overhead**: <xref:Terminal.Gui.ViewBase.DimAuto> can introduce performance overhead due to the dynamic calculation of sizes, especially with many subviews or complex text formatting. If the size is known and static, `Dim.Absolute(n)` might be more efficient.
- **Not Suitable for Full-Screen Layouts**: If you want a view to fill the entire width or height of the superview, `Dim.Fill()` is more appropriate than `Dim.Auto(DimAutoStyle.Content)` as it directly uses the superview's dimensions without content-based calculations.
- **Dependency Complexity**: When subviews themselves use <xref:Terminal.Gui.ViewBase.DimAuto> or other dependent <xref:Terminal.Gui.ViewBase.Dim> types, the layout process can become complex and may require multiple iterations to stabilize, potentially leading to unexpected results if not carefully managed.

## Behavior of Other Pos/Dim Types When Used Within a Dim.Auto-Sized View

The table below describes the behavior of various <xref:Terminal.Gui.ViewBase.Pos> and <xref:Terminal.Gui.ViewBase.Dim> types when used by subviews of a view that uses <xref:Terminal.Gui.ViewBase.DimAuto> for its `Width` or `Height`. This reflects how these types influence the automatic sizing:

| Type          | Impacts Dimension | Notes                                                                                             |
|---------------|-------------------|---------------------------------------------------------------------------------------------------|
| **PosAlign**  | Yes               | The subviews with the same `GroupId` will be aligned at the maximum dimension to enable them to not be clipped. This dimension plus the group's position will determine the minimum `Dim.Auto` dimension. |
| **PosView**   | Yes               | The position plus the dimension of `subview.Target` will determine the minimum `Dim.Auto` dimension. |
| **PosCombine**| Yes               | Impacts dimension if it includes a `Pos` type that affects dimension (like `PosView` or `PosAnchorEnd`). |
| **PosAnchorEnd**| Yes            | The `Dim.Auto` dimension will be increased by the dimension of the subview to accommodate its anchored position. |
| **PosCenter** | No                | Does not impact the dimension as it centers based on superview size, not content.                |
| **PosPercent**| No                | Does not impact dimension unless combined with other impacting types; based on superview size.   |
| **PosAbsolute**| Yes              | Impacts dimension if the absolute position plus subview dimension exceeds current content size.  |
| **PosFunc**   | Yes               | Impacts dimension if the function returns a value that, combined with subview dimension, exceeds content size. |
| **DimView**   | Yes               | The dimension of `subview.Target` will contribute to the minimum `Dim.Auto` dimension.          |
| **DimCombine**| Yes               | Impacts dimension if it includes a `Dim` type that affects dimension (like `DimView` or `DimAuto`). |
| **DimFill**   | Only with `MinimumContentDim` | By default, does **not** impact dimension because it fills remaining space. If `Dim.Fill(margin, minimumContentDim)` is used, the `minimumContentDim` contributes a floor to the auto-sizing calculation. Without it, the SubView will be sized to 0. |
| **DimPercent**| No                | Does not impact dimension as it is based on superview size, not content.                       |
| **DimAuto**   | Yes               | Contributes to dimension based on its own content or text sizing, potentially increasing the superview's size. |
| **DimAbsolute**| Yes              | Impacts dimension if the absolute size plus position exceeds current content size.              |
| **DimFunc**   | Yes               | Impacts dimension if the function returns a size that, combined with position, exceeds content size. |

## Building Dim.Auto Friendly Views

It is common to build view classes that have a natural size based on their content. For example, the <xref:Terminal.Gui.Views.Label> class is sized based on the `Text` property.

`LinearRange` is a good example of a sophisticated <xref:Terminal.Gui.ViewBase.DimAuto>-friendly view. Developers using these views shouldn't need to know the details of how the view is sized; they should just be able to use the view and have it size itself correctly.

For example, a vertical `LinearRange` with 3 options may be created like this, sized based on the number of options, its orientation, etc.:

```cs
List<object> options = new() { "Option 1", "Option 2", "Option 3" };
LinearRange slider = new(options)
{
    Orientation = Orientation.Vertical,
    Type = SliderType.Multiple,
};
view.Add(slider);
```

Note the developer does not need to specify the size of the `LinearRange`; it will size itself based on the number of options and the orientation.

Views like `LinearRange` achieve this by setting `Width` and `Height` to `Dim.Auto(DimAutoStyle.Content)` in the constructor and calling `SetContentSize()` whenever the desired content size changes. The <xref:Terminal.Gui.ViewBase.View> will then be sized to be big enough to fit the content.

Views that use `Text` for their content can set `Width` and `Height` to `Dim.Auto(DimAutoStyle.Text)`. It is recommended to use `Height = Dim.Auto(DimAutoStyle.Text, minimumContentDim: 1)` to ensure the view can show at least one line of text.

### Best Practices for Custom Views

- **Set Appropriate DimAutoStyle**: Choose `Text`, `Content`, or `Auto` based on what drives the view's size. Use `Text` for text-driven views like labels, `Content` for container-like views with subviews or explicit content sizes, and `Auto` for mixed content.
- **Update Content Size Dynamically**: If your view's content changes (e.g., text updates or subviews are added/removed), call `SetContentSize()` or ensure properties like `Text` are updated to trigger re-layout.
- **Consider Minimum and Maximum Constraints**: Use `minimumContentDim` to prevent views from becoming too small to be usable, and `maximumContentDim` to prevent them from growing excessively large, especially in constrained terminal environments.
- **Handle Adornments**: Be aware that <xref:Terminal.Gui.ViewBase.DimAuto> accounts for adornments in its sizing. If your view has custom adornments, ensure they are properly factored into the layout by the base <xref:Terminal.Gui.ViewBase.View> class.

## How To: Non-Trivial Dim.Auto Patterns

The following recipes address common but non-trivial scenarios drawn from real Terminal.Gui views.

### Pin a View to Its Natural Size

Set `minimumContentDim` and `maximumContentDim` to the **same value** to lock the dimension to a specific size while still using `Dim.Auto` for the calculation. The view auto-sizes once, then the result is clamped to that value.

<xref:Terminal.Gui.Views.Shortcut> uses this to pin its width to the natural width of its content:

```cs
// Both min and max reference the same Dim.Func, so the view can't grow or shrink beyond it
Width = Dim.Auto (DimAutoStyle.Content,
                  minimumContentDim: Dim.Func (_ => naturalWidth),
                  maximumContentDim: Dim.Func (_ => naturalWidth));
```

### Dynamic Minimum That Tracks a Formatted Value

Use `Dim.Func` as `minimumContentDim` when the minimum size depends on runtime state. The function is re-evaluated on every layout pass.

`NumericUpDown` uses this so its number display is always wide enough for the formatted value:

```cs
_number = new ()
{
    X = Pos.Right (_down),
    Width = Dim.Auto (minimumContentDim: Dim.Func (_ => string.Format (Format, Value).GetColumns ())),
    Height = 1,
};
```

As `Value` changes (e.g., from `9` to `100`), the `Dim.Func` returns a larger column count and the view grows automatically.

### Clamp Auto-Sizing to the Container

Pass a percentage-based `maximumContentDim` to prevent a view from exceeding the available Screen or SuperView size. Subtract adornment thickness so the frame (including borders) fits.

<xref:Terminal.Gui.Views.Dialog> uses this pattern:

```cs
Width = Dim.Auto (
    minimumContentDim: Dim.Func (_ => GetMinimumDialogWidth ()),
    maximumContentDim: Dim.Percent (100) - Dim.Func (_ => GetAdornmentsThickness ().Horizontal));

Height = Dim.Auto (
    minimumContentDim: Dim.Func (_ => GetMinimumDialogHeight ()),
    maximumContentDim: Dim.Percent (100) - Dim.Func (_ => GetAdornmentsThickness ().Vertical));
```

The <xref:Terminal.Gui.Views.Dialog> auto-sizes to its content but never overflows the container when <xref:Terminal.Gui.ViewBase.Margin>, <xref:Terminal.Gui.ViewBase.Border>, and <xref:Terminal.Gui.ViewBase.Padding> are present.

### Uniform-Width Items in a Vertical List

When a SuperView holds multiple auto-sized items that should all share the same width (e.g., a vertical menu), measure them all first, find the widest, then pin every item to that width.

<xref:Terminal.Gui.Views.Bar> (the base class for <xref:Terminal.Gui.Views.Menu>) implements this:

```cs
// Phase 1 — measure each item at its natural width
var maxWidth = 0;
foreach (Shortcut shortcut in SubViews.OfType<Shortcut> ().Where (s => s.Visible))
{
    shortcut.Width = shortcut.GetWidthDimAuto ();
    shortcut.Layout (screenSize);
    maxWidth = Math.Max (maxWidth, shortcut.Frame.Width);
}

// Phase 2 — pin every item to the widest
foreach (View subView in SubViews)
{
    if (subView is not Line)
    {
        subView.Width = Dim.Auto (DimAutoStyle.Auto,
                                  minimumContentDim: maxWidth,
                                  maximumContentDim: maxWidth);
    }
}
```

This two-pass approach ensures the menu is exactly as wide as its widest item—no wider, no narrower.

### Cap a SubView Without Capping the Container

A SuperView can use unconstrained <xref:Terminal.Gui.ViewBase.DimAuto> while one of its SubViews uses `maximumContentDim` to limit its own growth. This lets the SuperView size to the sum of its SubViews, while one SubView is capped.

`Shortcut.HelpView` limits its text width to the remaining space after `CommandView` and `KeyView`:

```cs
HelpView.Width = Dim.Auto (DimAutoStyle.Text,
                            maximumContentDim: Dim.Func (_ => maxHelpWidth));
```

When the <xref:Terminal.Gui.Views.Shortcut> is constrained externally (e.g., `Width = Dim.Fill()`), `OnSubViewLayout` recalculates `maxHelpWidth` and the help text truncates gracefully.

### Guarantee Minimum Width for Alignment Across Siblings

Use `minimumContentDim` to ensure a SubView never shrinks below a value that keeps it visually aligned with the same SubView in sibling views.

`Shortcut.KeyView` uses this so key labels (e.g., `Ctrl+S`, `F5`) align across multiple <xref:Terminal.Gui.Views.Shortcut> instances in a <xref:Terminal.Gui.Views.Bar>:

```cs
KeyView.Width = Dim.Auto (DimAutoStyle.Text,
                          minimumContentDim: Dim.Func (_ => MinimumKeyTextSize));
```

The <xref:Terminal.Gui.Views.Bar> calculates the widest key text across all shortcuts and sets `MinimumKeyTextSize` on each, producing a clean right-aligned column.

### Nested Auto-Sizing (Container Wraps Auto-Sized Children)

A SuperView using `Dim.Auto(DimAutoStyle.Content)` automatically expands to fit SubViews that are themselves auto-sized. Chain `Pos.Right` / `Pos.Bottom` to lay out SubViews sequentially.

`NumericUpDown` is a compact example:

```cs
// Container
Width = Dim.Auto (DimAutoStyle.Content);
Height = Dim.Auto (DimAutoStyle.Content);

// Children chain left-to-right
_down   = new () { Width = 1, Height = 1 };
_number = new () { X = Pos.Right (_down), Width = Dim.Auto (...), Height = 1 };
_up     = new () { X = Pos.Right (_number), Width = 1, Height = 1 };

Add (_down, _number, _up);
```

The SuperViews's <xref:Terminal.Gui.ViewBase.DimAuto> walks the SubViews, sees that `_up.X + _up.Width` is the rightmost extent, and sizes the SuperView accordingly. When `_number` grows (because the value got wider), the container grows too.

### Use Dim.Fill Inside a Dim.Auto SuperView

A SubView using `Dim.Fill()` inside a <xref:Terminal.Gui.ViewBase.DimAuto>-sized SuperView will receive a size of **0** by default. This is because `Dim.Fill` derives its size from the SuperView's ContentSize, but <xref:Terminal.Gui.ViewBase.DimAuto> computes ContentSize from its SubViews—creating a circular dependency that resolves to 0.

**The fix:** use `Dim.Fill(margin, minimumContentDim)` to contribute a floor to the auto-sizing calculation:

```cs
// WRONG — SubView will be 0 wide because SuperView has no content-based size to fill against
View superView = new () { Width = Dim.Auto (DimAutoStyle.Content) };
View subView = new () { Width = Dim.Fill () }; // Gets 0!
superView.Add (subView);

// CORRECT — minimumContentDim breaks the circular dependency
View superView = new () { Width = Dim.Auto (DimAutoStyle.Content) };
View subView = new () { Width = Dim.Fill (margin: 0, minimumContentDim: 20) }; // At least 20 wide
superView.Add (subView);
```

The `minimumContentDim` ensures the SuperView's <xref:Terminal.Gui.ViewBase.DimAuto> calculation sees a content size of at least 20, so the SubView's `Dim.Fill` can then fill that space. If the SuperView has other SubViews that push it wider, the `Dim.Fill` SubView will grow to match. `Dim.Func` can be used for a dynamic minimum.

### Ensure a Minimum Height of One Line

For text-based views, pass `1` as `minimumContentDim` to guarantee the view is visible even when `Text` is empty:

```cs
Height = Dim.Auto (DimAutoStyle.Text, minimumContentDim: 1);
```

<xref:Terminal.Gui.Views.CheckBox>, <xref:Terminal.Gui.Views.TextField>, <xref:Terminal.Gui.Views.Shortcut>, and <xref:Terminal.Gui.Views.Menu> all use this pattern.

## Debugging Dim.Auto Issues

If you encounter unexpected sizing with <xref:Terminal.Gui.ViewBase.DimAuto>, consider the following debugging steps based on the codebase's diagnostic capabilities:

- **Enable Validation**: Set `ValidatePosDim` to `true` on the view to enable runtime validation of `Pos` and `Dim` settings. This will throw exceptions if invalid configurations are detected, helping identify issues like circular dependencies or negative sizes.
- **Check Content Size**: Verify if `ContentSizeTracksViewport` is behaving as expected. If set to `false`, ensure `SetContentSize()` is called with the correct dimensions. Use logging to track `GetContentSize()` outputs.
- **Review Subview Dependencies**: Look for subviews with `Pos` or `Dim` types that impact dimension (like `PosAnchorEnd` or `DimView`). Ensure their target views are laid out before the current view to avoid incorrect sizing.
- **Inspect Text Formatting**: For `Text` style, check `TextFormatter` settings and constraints (`ConstrainToWidth`, `ConstrainToHeight`). Ensure text is formatted correctly before sizing calculations.

By understanding the intricacies of <xref:Terminal.Gui.ViewBase.DimAuto> as implemented in Terminal.Gui v2, developers can create responsive and adaptive terminal UIs that automatically adjust to content changes, enhancing user experience and maintainability.

## Internal Architecture

<xref:Terminal.Gui.ViewBase.DimAuto> uses a polymorphic design to minimize coupling with specific <xref:Terminal.Gui.ViewBase.Pos> and <xref:Terminal.Gui.ViewBase.Dim> types. The layout system uses virtual properties and methods to categorize and process layout elements:

- **`DependsOnSuperViewContentSize`**: Identifies types that actively contribute to content size determination (e.g., `DimPercent`, `DimFill`, `PosAnchorEnd`, `PosAlign`)
- **`CanContributeToAutoSizing`**: Indicates whether a `Dim` can meaningfully contribute to auto-sizing (returns `false` for `DimPercent` and `DimFill` without `MinimumContentDim`/`To`)
- **`GetMinimumContribution()`**: Calculates the minimum size contribution during auto-sizing (overridden by `DimFill` to return its `MinimumContentDim`)
- **`IsFixed`**: Identifies fixed-value types that don't depend on layout calculations (`DimAbsolute`, `PosAbsolute`, `DimFunc`, `PosFunc`, `DimAuto`)
- **`RequiresTargetLayout`**: Indicates types requiring target view layout first (`DimView`, `PosView`)

This design allows new <xref:Terminal.Gui.ViewBase.Pos>/<xref:Terminal.Gui.ViewBase.Dim> types to be added without modifying `DimAuto.Calculate()`.
