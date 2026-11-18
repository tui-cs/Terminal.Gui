namespace UICatalog.Scenarios;

internal class DoomFire
{
    private static Color [] _palette;

    public DoomFire (int width, int height)
    {
        _width = width;
        _height = height;
        _firePixels = new Color [width, height];
        InitializePalette ();
        InitializeFire ();
    }

    private readonly int _width;
    private readonly int _height;
    private readonly Color [,] _firePixels;
    private readonly Random _random = new ();

    public Color [] Palette => _palette;

    public void AdvanceFrame ()
    {
        for (var x = 0; x < _width; x++)
        {
            for (var y = 1; y < _height; y++)
            {
                int dstY = y - 1;
                int decay = _random.Next (0, 2);
                int dstX = x + _random.Next (-1, 2);

                if (dstX < 0 || dstX >= _width)
                {
                    dstX = x;
                }

                Color srcColor = _firePixels [x, y];
                int intensity = Array.IndexOf (_palette, srcColor) - decay;

                if (intensity < 0)
                {
                    intensity = 0;
                }

                _firePixels [dstX, dstY] = _palette [intensity];
            }
        }
    }

    public Color [,] GetFirePixels () => _firePixels;

    public void InitializeFire ()
    {
        for (var x = 0; x < _width; x++)
        {
            _firePixels [x, _height - 1] = _palette [36];
        }

        for (var y = 0; y < _height - 1; y++)
        {
            for (var x = 0; x < _width; x++)
            {
                _firePixels [x, y] = _palette [0];
            }
        }
    }

    private void InitializePalette ()
    {
        _palette = new Color [37];
        _palette [0] = new Color (0, 0, 0, 0);

        for (var i = 1; i < 37; i++)
        {
            var r = (byte)Math.Min (255, i * 7);
            var g = (byte)Math.Min (255, i * 5);
            var b = (byte)Math.Min (255, i * 2);
            _palette [i] = new Color (r, g, b);
        }
    }
}
