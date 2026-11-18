namespace Terminal.Gui.Views;

/// <summary>All <see cref="LinearRangeViewBase{TOption,TValue}"/> configuration is grouped in this class.</summary>
internal class LinearRangeConfiguration
{
    internal bool _allowEmpty;
    internal int _endSpacing;
    internal int _minInnerSpacing = 1;
    internal int _cachedInnerSpacing; // Currently calculated
    internal Orientation _legendsOrientation = Orientation.Horizontal;
    internal bool _rangeAllowSingle;
    internal bool _showEndSpacing;
    internal bool _showLegends;
    internal bool _showLegendsAbbr;
    internal Orientation _linearRangeOrientation = Orientation.Horizontal;
    internal int _startSpacing;
    internal LinearRangeRenderMode _renderMode = LinearRangeRenderMode.Single;
    internal bool _useMinimumSize;
}
