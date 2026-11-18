namespace Terminal.Gui.Views;

/// <summary>
///     Theme-scoped defaults shared by all <see cref="LinearRangeViewBase{TOption,TValue}"/>
///     subclasses (<see cref="LinearSelector{T}"/>, <see cref="LinearMultiSelector{T}"/>,
///     <see cref="LinearRange{T}"/>).
/// </summary>
public static class LinearRangeDefaults
{
    /// <summary>Gets or sets the default cursor style applied to a new linear range view.</summary>
    public static CursorStyle DefaultCursorStyle => LinearRangeSettings.Current.DefaultCursorStyle;
}
