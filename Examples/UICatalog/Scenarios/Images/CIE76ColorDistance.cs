namespace UICatalog.Scenarios;

internal class CIE76ColorDistance : LabColorDistance
{
    /// <inheritdoc/>
    public override double CalculateDistance (Color c1, Color c2)
    {
        LabColor lab1 = RgbToLab (c1);
        LabColor lab2 = RgbToLab (c2);

        return Math.Sqrt (Math.Pow (lab1.L - lab2.L, 2) + Math.Pow (lab1.A - lab2.A, 2) + Math.Pow (lab1.B - lab2.B, 2));
    }
}
