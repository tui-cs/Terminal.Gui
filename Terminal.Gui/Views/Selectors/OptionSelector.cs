using System.Diagnostics;

namespace Terminal.Gui.Views;

// DoubleClick - Focus, Activate, and Accept the item under the mouse (CanFocus or not)
// Click - Focus, Activate, and do NOT Accept the item under the mouse (CanFocus or not).
// CanFocus - Not Focused:
//  HotKey - Restore Focus. Advance Active. Do NOT Accept.
//  Item HotKey - Focus item. If item is not active, make Active. Do NOT Accept.
// !CanFocus - Not Focused:
//  HotKey - Do NOT Restore Focus. Advance Active. Do NOT Accept.
//  Item HotKey - Do NOT Focus item. If item is not active, make Active. Do NOT Accept.
// Focused:
//  Space key - Activate the focused item. Do NOT Accept.
//  Enter key - Activate and Accept the focused item.
//  HotKey - Restore Focus. Advance Active. Do NOT Accept.
//  Item HotKey - If item is not active, make Active. Do NOT Accept. If item is active, do nothing.

/// <summary>
///     Provides a user interface for displaying and selecting a single item from a list of options.
///     Each option is represented by a checkbox, but only one can be selected at a time.
///     <see cref="OptionSelector{TEnum}"/> provides a type-safe version where a <see langword="enum"/> can be
///     provided.
/// </summary>
/// <remarks>
/// <img src="../images/views/OptionSelector.gif" alt="OptionSelector demo"/>
/// </remarks>
public class OptionSelector : SelectorBase, IDesignable
{
    // By default, for OptionSelector, Value is set to 0. It can be set to null if a developer
    // really wants that.
    /// <inheritdoc/>
    public OptionSelector () => base.Value = 0;

    /// <inheritdoc/>
    protected override View? GetDispatchTarget (ICommandContext? ctx)
    {
        // Only dispatch Activate, not Accept. Accept should bubble naturally.
        if (ctx?.Command != Command.Activate)
        {
            return null;
        }

        if (ctx.Source?.TryGetTarget (out View? source) != true || source is not CheckBox cb)
        {
            return Focused;
        }

        if (ctx.Binding is { } && ctx.Binding.Commands.Contains (Command.Accept) && cb.Value == CheckState.Checked)
        {
            return null;
        }

        // When a CheckBox's activation bubbles up, the source IS the CheckBox
        return source;
    }

    /// <summary>
    ///     Consumes: OptionSelector owns selection state, not the individual CheckBoxes.
    /// </summary>
    protected override bool ConsumeDispatch => true;

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? ctx)
    {
        base.OnActivated (ctx);

        // Apply the value change. Runs for ALL activation paths uniformly.
        // No routing-direction check needed — the framework handled dispatch/consumption.
        ApplyActivation (ctx);
    }

    /// <summary>
    ///     Applies the value change based on the activation source.
    /// </summary>
    private void ApplyActivation (ICommandContext? ctx)
    {
        CheckBox? checkBox = null;

        if (ctx?.Source?.TryGetTarget (out View? sourceView) == true && sourceView is CheckBox cb)
        {
            checkBox = cb;
        }
        else if (ctx?.Routing == CommandRouting.DispatchingDown && Focused is CheckBox focusedCb)
        {
            // External dispatch (e.g. Menu → MenuItem → OptionSelector): the DispatchingDown guard
            // blocked dispatch to inner CheckBoxes. Use the currently focused CheckBox as the
            // selection target — SetFocus() was called before OnActivated, so Focused is reliable.
            checkBox = focusedCb;
        }

        if (checkBox is null)
        {
            Cycle ();

            return;
        }

        if (GetCheckBoxValue (checkBox) == Value)
        {
            // Already selected — no-op regardless of activation source (Space, click, etc.).
            return;
        }

        Value = GetCheckBoxValue (checkBox);
    }

    /// <inheritdoc/>
    protected override void OnSubViewAdded (View view)
    {
        base.OnSubViewAdded (view);

        if (view is not CheckBox checkbox)
        {
            return;
        }

        checkbox.RadioStyle = true;
    }

    private void Cycle ()
    {
        int valueIndex = Values.IndexOf (v => v == Value);

        Value = valueIndex == Values?.Count - 1 ? Values! [0] : Values! [valueIndex + 1];

        if (HasFocus)
        {
            valueIndex = Values.IndexOf (v => v == Value);
            SubViews.OfType<CheckBox> ().ToArray () [valueIndex].SetFocus ();
        }

        // Verify at most one is checked
        Debug.Assert (SubViews.OfType<CheckBox> ().Count (cb => cb.Value == CheckState.Checked) <= 1);
    }

    /// <summary>
    ///     Updates the checked state of all checkbox subviews so that only the checkbox corresponding
    ///     to the current <see cref="SelectorBase.Value"/> is checked.
    /// </summary>
    /// <remarks>
    ///     If <see cref="SelectorBase.Value"/> doesn't exist in the list of checkbox values, then the first checkbox will be checked by default
    ///     and will raise the <see cref="SelectorBase.ValueChanging"/>/<see cref="SelectorBase.ValueChanged"/> events.
    /// </remarks>
    public override void UpdateChecked ()
    {
        Dictionary<CheckBox, int> checkBoxValueMap = SubViews.OfType<CheckBox> ().ToDictionary (cb => cb, GetCheckBoxValue);

        foreach (CheckBox cb in SubViews.OfType<CheckBox> ())
        {
            int value = GetCheckBoxValue (cb);

            cb.Value = value == Value ? CheckState.Checked : CheckState.UnChecked;
        }

        // If Value doesn't exist in any checkbox, use the first checkbox's value
        if (Value is not null && checkBoxValueMap.Count > 0 && Values!.All (v => v != Value) && checkBoxValueMap.Values.All (v => v != Value))
        {
            Value = checkBoxValueMap.Values.First ();

            foreach (KeyValuePair<CheckBox, int> kvp in checkBoxValueMap)
            {
                if (kvp.Value != Value)
                {
                    continue;
                }
                kvp.Key.Value = CheckState.Checked;

                break;
            }

            // Sanity checks to verify the assumptions above
            Debug.Assert (checkBoxValueMap.Values.First () != (int)SubViews.OfType<CheckBox> ().First ().Value);
            Debug.Assert (checkBoxValueMap.Values.First () == Values! [0]);
        }

        // Verify at most one is checked
        Debug.Assert (SubViews.OfType<CheckBox> ().Count (cb => cb.Value == CheckState.Checked) <= 1);
    }

    /// <inheritdoc/>
    protected override void OnValueChanged (int? value, int? previousValue)
    {
        if (value is null || Values is null)
        {
            return;
        }

        int index = Values.IndexOf (v => v == value);

        if (index >= 0 && index < SubViews.OfType<CheckBox> ().Count ())
        {
            FocusedItem = index;
        }
    }

    /// <summary>
    ///     Gets or sets the <see cref="SelectorBase.Labels"/> index for the focused item. The active item may or may not be
    ///     the selected
    ///     RadioItem.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Maps to either the X or Y position within <see cref="View.Viewport"/> depending on <see cref="Orientation"/>.
    ///     </para>
    /// </remarks>
    public int FocusedItem
    {
        get
        {
            if (!CanFocus)
            {
                return 0;
            }

            return HasFocus ? SubViews.OfType<CheckBox> ().ToArray ().IndexOf (Focused) : field;
        }
        set
        {
            if (!CanFocus)
            {
                return;
            }

            field = value;

            CheckBox [] checkBoxes = SubViews.OfType<CheckBox> ().ToArray ();

            if (value < 0 || value >= checkBoxes.Length)
            {
                throw new ArgumentOutOfRangeException (nameof (value), @"FocusedItem index is out of range");
            }

            if (HasFocus)
            {
                checkBoxes [value].SetFocus ();
            }
        }
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        AssignHotKeys = true;
        Labels = ["Option 1", "Option 2", "Third Option", "Option Quattro"];

        return true;
    }

    /// <inheritdoc/>
    public string? GetDemoKeyStrokes () => "wait:500,CursorDown,wait:600,CursorDown,wait:600,CursorUp,wait:400,Space,wait:800";
}
