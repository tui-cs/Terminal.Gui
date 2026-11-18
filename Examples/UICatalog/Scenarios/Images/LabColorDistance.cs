using ColorHelper;

namespace UICatalog.Scenarios;

internal abstract class LabColorDistance : IColorDistance
{
    private const double REF_X = 95.047;
    private const double REF_Y = 100.000;
    private const double REF_Z = 108.883;

    /// <inheritdoc/>
    public abstract double CalculateDistance (Color c1, Color c2);

    protected LabColor RgbToLab (Color c)
    {
        XYZ xyz = ColorConverter.RgbToXyz (new RGB (c.R, c.G, c.B));

        double x = xyz.X / REF_X;
        double y = xyz.Y / REF_Y;
        double z = xyz.Z / REF_Z;

        x = x > 0.008856 ? Math.Pow (x, 1.0 / 3.0) : 7.787 * x + 16.0 / 116.0;
        y = y > 0.008856 ? Math.Pow (y, 1.0 / 3.0) : 7.787 * y + 16.0 / 116.0;
        z = z > 0.008856 ? Math.Pow (z, 1.0 / 3.0) : 7.787 * z + 16.0 / 116.0;

        double l = 116.0 * y - 16.0;
        double a = 500.0 * (x - y);
        double b = 200.0 * (y - z);

        return new LabColor (l, a, b);
    }

    protected class LabColor (double l, double a, double b)
    {
        public double A { get; } = a;
        public double B { get; } = b;
        public double L { get; } = l;
    }
}
