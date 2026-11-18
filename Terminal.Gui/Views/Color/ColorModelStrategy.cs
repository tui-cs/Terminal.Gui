using ColorHelper;
using ColorConverter = ColorHelper.ColorConverter;

namespace Terminal.Gui.Views;

internal class ColorModelStrategy
{
    public IEnumerable<ColorBar> CreateBars (ColorModel model) =>
        model switch
        {
            ColorModel.RGB => CreateRgbBars (),
            ColorModel.HSV => CreateHsvBars (),
            ColorModel.HSL => CreateHslBars (),
            _ => throw new ArgumentOutOfRangeException (nameof (model), model, null)
        };

    public Color GetColorFromBars (IList<IColorBar> bars, ColorModel model) =>
        model switch
        {
            ColorModel.RGB => ToColor (new ((byte)bars [0].Value, (byte)bars [1].Value, (byte)bars [2].Value)),
            ColorModel.HSV => ToColor (ColorConverter.HsvToRgb (new (bars [0].Value, (byte)bars [1].Value, (byte)bars [2].Value))),
            ColorModel.HSL => ToColor (ColorConverter.HslToRgb (new (bars [0].Value, (byte)bars [1].Value, (byte)bars [2].Value))),
            _ => throw new ArgumentOutOfRangeException (nameof (model), model, null)
        };

    public void SetBarsToColor (IList<IColorBar> bars, Color newValue, ColorModel model)
    {
        if (bars.Count == 0)
        {
            return;
        }

        switch (model)
        {
            case ColorModel.RGB:
                bars [0].SetValueWithoutRaisingEvent (newValue.R);
                bars [1].SetValueWithoutRaisingEvent (newValue.G);
                bars [2].SetValueWithoutRaisingEvent (newValue.B);

                break;
            case ColorModel.HSV:
                HSV newHsv = ColorConverter.RgbToHsv (new (newValue.R, newValue.G, newValue.B));
                bars [0].SetValueWithoutRaisingEvent (newHsv.H);
                bars [1].SetValueWithoutRaisingEvent (newHsv.S);
                bars [2].SetValueWithoutRaisingEvent (newHsv.V);

                break;
            case ColorModel.HSL:

                HSL newHsl = ColorConverter.RgbToHsl (new (newValue.R, newValue.G, newValue.B));
                bars [0].SetValueWithoutRaisingEvent (newHsl.H);
                bars [1].SetValueWithoutRaisingEvent (newHsl.S);
                bars [2].SetValueWithoutRaisingEvent (newHsl.L);

                break;
            default:
                throw new ArgumentOutOfRangeException (nameof (model), model, null);
        }
    }

    private static IEnumerable<ColorBar> CreateHslBars ()
    {
        HueBar h = new ()
        {
            Text = "H:"
        };

        yield return h;

        SaturationBar s = new ()
        {
            Text = "S:"
        };

        LightnessBar l = new ()
        {
            Text = "L:"
        };

        s.HBar = h;
        s.LBar = l;

        l.HBar = h;
        l.SBar = s;

        yield return s;
        yield return l;
    }

    private static IEnumerable<ColorBar> CreateHsvBars ()
    {
        HueBar h = new ()
        {
            Text = "H:"
        };

        yield return h;

        SaturationBar s = new ()
        {
            Text = "S:"
        };

        ValueBar v = new ()
        {
            Text = "V:"
        };

        s.HBar = h;
        s.VBar = v;

        v.HBar = h;
        v.SBar = s;

        yield return s;
        yield return v;
    }

    private static IEnumerable<ColorBar> CreateRgbBars ()
    {
        RBar r = new ()
        {
            Text = "R:"
        };

        GBar g = new ()
        {
            Text = "G:"
        };

        BBar b = new ()
        {
            Text = "B:"
        };
        r.GBar = g;
        r.BBar = b;

        g.RBar = r;
        g.BBar = b;

        b.RBar = r;
        b.GBar = g;

        yield return r;
        yield return g;
        yield return b;
    }

    private static Color ToColor (RGB rgb) { return new (rgb.R, rgb.G, rgb.B); }
}
