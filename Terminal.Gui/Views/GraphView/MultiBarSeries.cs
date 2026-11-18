using System.Collections.ObjectModel;

namespace Terminal.Gui.Views;

/// <summary>Collection of <see cref="BarSeries"/> in which bars are clustered by category</summary>
public class MultiBarSeries : ISeries
{
    private readonly BarSeries [] _subSeries;

    /// <summary>Creates a new series of clustered bars.</summary>
    /// <param name="numberOfBarsPerCategory">Each category has this many bars</param>
    /// <param name="barsEvery">How far apart to put each category (in graph space)</param>
    /// <param name="spacing">
    ///     How much spacing between bars in a category (should be less than <paramref name="barsEvery"/>/
    ///     <paramref name="numberOfBarsPerCategory"/>)
    /// </param>
    /// <param name="colors">
    ///     Array of colors that define bar color in each category. Length must match
    ///     <paramref name="numberOfBarsPerCategory"/>
    /// </param>
    public MultiBarSeries (int numberOfBarsPerCategory, float barsEvery, float spacing, Attribute []? colors = null)
    {
        _subSeries = new BarSeries [numberOfBarsPerCategory];

        if (colors is { } && colors.Length != numberOfBarsPerCategory)
        {
            throw new ArgumentException (@"Number of colors must match the number of bars", nameof (numberOfBarsPerCategory));
        }

        for (var i = 0; i < numberOfBarsPerCategory; i++)
        {
            _subSeries [i] = new ()
            {
                BarEvery = barsEvery,
                Offset = i * spacing,

                // Only draw labels for the first bar in each category
                DrawLabels = i == 0
            };

            if (colors is { })
            {
                _subSeries [i].OverrideBarColor = colors [i];
            }
        }

        Spacing = spacing;
    }

    /// <summary>
    ///     The number of units of graph space between bars. Should be less than <see cref="BarSeries.BarEvery"/>
    /// </summary>
    public float Spacing { get; }

    /// <summary>
    ///     Sub collections. Each series contains the bars for a different category. Thus, SubSeries[0].Bars[0] is the
    ///     first bar on the axis and SubSeries[1].Bars[0] is the second etc.
    /// </summary>
    public IReadOnlyCollection<BarSeries> SubSeries => new ReadOnlyCollection<BarSeries> (_subSeries);

    /// <summary>Draws all <see cref="SubSeries"/></summary>
    /// <param name="graph"></param>
    /// <param name="drawBounds"></param>
    /// <param name="graphBounds"></param>
    public void DrawSeries (GraphView graph, Rectangle drawBounds, RectangleF graphBounds)
    {
        foreach (BarSeries bar in _subSeries)
        {
            bar.DrawSeries (graph, drawBounds, graphBounds);
        }
    }

    /// <summary>Adds a new cluster of bars</summary>
    /// <param name="label"></param>
    /// <param name="fill"></param>
    /// <param name="values">Values for each bar in category, must match the number of bars per category</param>
    public void AddBars (string label, Rune fill, params float [] values)
    {
        if (values.Length != _subSeries.Length)
        {
            throw new ArgumentException (@"Number of values must match the number of bars per category", nameof (values));
        }

        for (var i = 0; i < values.Length; i++)
        {
            _subSeries [i]
                .Bars.Add (
                           new (
                                label,
                                new (fill),
                                values [i]
                               )
                          );
        }
    }
}
