namespace Terminal.Gui.Views;

/// <summary>
/// Contains style settings for <see cref="ColorPicker"/> e.g. which <see cref="ColorModel"/>
/// to use.
/// </summary>
public class ColorPickerStyle
{
    /// <summary>
    ///     The color model for picking colors by RGB, HSV, etc.
    /// </summary>
    public ColorModel ColorModel { get; set; } = ColorModel.HSV;

    /// <summary>
    ///     True to put the numerical value of bars on the right of the color bar
    /// </summary>
    public bool ShowTextFields { get; set; } = true;

    /// <summary>
    ///     True to show an editable text field indicating the w3c/console color name of selected color.
    /// </summary>
    public bool ShowColorName { get; set; } = false;
}
