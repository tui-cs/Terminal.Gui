namespace UICatalog.Scenarios;

internal class MedianCutPaletteBuilder : IPaletteBuilder
{
    public List<Color> BuildPalette (List<Color> colors, int maxColors)
    {
        if (colors.Count == 0 || maxColors <= 0)
        {
            return [];
        }

        return MedianCut (colors, maxColors);
    }

    private Color AverageColor (List<Color> cube)
    {
        var avgR = (byte)cube.Average (c => c.R);
        var avgG = (byte)cube.Average (c => c.G);
        var avgB = (byte)cube.Average (c => c.B);

        return new Color (avgR, avgG, avgB);
    }

    private (int, int) FindLargestRange (List<Color> cube)
    {
        byte minR = cube.Min (c => c.R);
        byte maxR = cube.Max (c => c.R);
        byte minG = cube.Min (c => c.G);
        byte maxG = cube.Max (c => c.G);
        byte minB = cube.Min (c => c.B);
        byte maxB = cube.Max (c => c.B);

        int rangeR = maxR - minR;
        int rangeG = maxG - minG;
        int rangeB = maxB - minB;

        if (rangeR >= rangeG && rangeR >= rangeB)
        {
            return (0, rangeR);
        }

        if (rangeG >= rangeR && rangeG >= rangeB)
        {
            return (1, rangeG);
        }

        return (2, rangeB);
    }

    private bool IsSingleColorCube (List<Color> cube)
    {
        Color firstColor = cube.First ();

        return cube.All (c => c.R == firstColor.R && c.G == firstColor.G && c.B == firstColor.B);
    }

    private List<Color> MedianCut (List<Color> colors, int maxColors)
    {
        List<List<Color>> cubes = [colors];

        while (cubes.Count < maxColors)
        {
            bool added = false;
            cubes.Sort ((a, b) => Volume (a).CompareTo (Volume (b)));

            List<Color> largestCube = cubes.Last ();
            cubes.RemoveAt (cubes.Count - 1);

            if (IsSingleColorCube (largestCube))
            {
                cubes.Add (largestCube);

                break;
            }

            (List<Color> cube1, List<Color> cube2) = SplitCube (largestCube);

            if (cube1.Any ())
            {
                cubes.Add (cube1);
                added = true;
            }

            if (cube2.Any ())
            {
                cubes.Add (cube2);
                added = true;
            }

            if (!added)
            {
                break;
            }
        }

        return cubes.Select (AverageColor).Distinct ().ToList ();
    }

    private (List<Color>, List<Color>) SplitCube (List<Color> cube)
    {
        (int component, int _) = FindLargestRange (cube);

        cube.Sort ((c1, c2) => component switch
                               {
                                   0 => c1.R.CompareTo (c2.R),
                                   1 => c1.G.CompareTo (c2.G),
                                   2 => c1.B.CompareTo (c2.B),
                                   _ => 0
                               });

        int medianIndex = cube.Count / 2;
        List<Color> cube1 = cube.Take (medianIndex).ToList ();
        List<Color> cube2 = cube.Skip (medianIndex).ToList ();

        return (cube1, cube2);
    }

    private int Volume (List<Color> cube)
    {
        if (cube == null || cube.Count == 0)
        {
            return 0;
        }

        byte minR = cube.Min (c => c.R);
        byte maxR = cube.Max (c => c.R);
        byte minG = cube.Min (c => c.G);
        byte maxG = cube.Max (c => c.G);
        byte minB = cube.Min (c => c.B);
        byte maxB = cube.Max (c => c.B);

        return (maxR - minR) * (maxG - minG) * (maxB - minB);
    }
}
