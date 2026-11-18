namespace Terminal.Gui.Views;

/// <summary>A location on an axis of a <see cref="GraphView"/> that may or may not have a label associated with it</summary>
public class AxisIncrementToRender
{
    /// <summary>Describe a new section of an axis that requires an axis increment symbol and/or label</summary>
    /// <param name="orientation"></param>
    /// <param name="screen"></param>
    /// <param name="value"></param>
    public AxisIncrementToRender (Orientation orientation, int screen, float value)
    {
        Orientation = orientation;
        ScreenLocation = screen;
        Value = value;
    }

    /// <summary>Direction of the parent axis</summary>
    public Orientation Orientation { get; }

    /// <summary>The screen location (X or Y depending on <see cref="Orientation"/>) that the increment will be rendered at</summary>
    public int ScreenLocation { get; }

    /// <summary>The value at this position on the axis in graph space</summary>
    public float Value { get; }

    /// <summary>The text (if any) that should be displayed at this axis increment</summary>
    /// <value></value>
    internal string Text { get; set; } = "";
}
