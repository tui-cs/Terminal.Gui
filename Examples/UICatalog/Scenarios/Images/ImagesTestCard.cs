namespace UICatalog.Scenarios;

internal static class ImagesTestCard
{
    public const int DEFAULT_HEIGHT = 320;
    public const int DEFAULT_WIDTH = 512;

    public static Color [,] Create (int width, int height)
    {
        Color [,] image = new Color [width, height];
        Fill (image, new Color (5, 7, 12));
        DrawGrid (image, 32, new Color (22, 31, 45));
        DrawColorBars (image, new Rectangle (0, 0, width, Math.Max (48, height / 5)));
        DrawGrayscaleRamp (image, new Rectangle (32, height - 48, width - 64, 20));
        DrawLine (image, 0, 0, width - 1, height - 1, new Color (255, 255, 255));
        DrawLine (image, width - 1, 0, 0, height - 1, new Color (255, 255, 255));
        DrawCircle (image, width / 2, height / 2, Math.Min (width, height) / 3, new Color (255, 255, 255));
        DrawCircle (image, width / 2, height / 2, Math.Min (width, height) / 5, new Color (96, 200, 255));
        DrawTerminalBadge (image, new Rectangle (width / 2 - 116, height / 2 - 52, 232, 104));

        return image;
    }

    private static void DrawCircle (Color [,] image, int centerX, int centerY, int radius, Color color)
    {
        int x = radius;
        var y = 0;
        int decision = 1 - x;

        while (y <= x)
        {
            PutCirclePoints (image, centerX, centerY, x, y, color);
            y++;

            if (decision <= 0)
            {
                decision += 2 * y + 1;

                continue;
            }

            x--;
            decision += 2 * (y - x) + 1;
        }
    }

    private static void DrawColorBars (Color [,] image, Rectangle rect)
    {
        Color [] bars =
        [
            new (255, 255, 255), new (255, 255), new (0, 255, 255), new (0, 255), new (255, 0, 255), new (255, 0), new (0, 0, 255), new (16, 16, 16)
        ];

        int barWidth = Math.Max (1, rect.Width / bars.Length);

        for (var i = 0; i < bars.Length; i++)
        {
            int left = rect.X + i * barWidth;
            int right = i == bars.Length - 1 ? rect.Right : Math.Min (rect.Right, left + barWidth);
            FillRectangle (image, rect with { X = left, Width = right - left }, bars [i]);
        }
    }

    private static void DrawGlyph (Color [,] image, int x, int y, string [] glyph, int scale, Color color)
    {
        for (var row = 0; row < glyph.Length; row++)
        {
            for (var col = 0; col < glyph [row].Length; col++)
            {
                if (glyph [row] [col] != '1')
                {
                    continue;
                }

                FillRectangle (image, new Rectangle (x + col * scale, y + row * scale, scale, scale), color);
            }
        }
    }

    private static void DrawGrayscaleRamp (Color [,] image, Rectangle rect)
    {
        for (var x = 0; x < rect.Width; x++)
        {
            var value = (byte)(x * 255 / Math.Max (1, rect.Width - 1));

            for (var y = 0; y < rect.Height; y++)
            {
                PutPixel (image, rect.X + x, rect.Y + y, new Color (value, value, value));
            }
        }

        DrawRectangle (image, rect, new Color (255, 255, 255));
    }

    private static void DrawGrid (Color [,] image, int spacing, Color color)
    {
        int width = image.GetLength (0);
        int height = image.GetLength (1);

        for (var x = 0; x < width; x += spacing)
        {
            DrawLine (image, x, 0, x, height - 1, color);
        }

        for (var y = 0; y < height; y += spacing)
        {
            DrawLine (image, 0, y, width - 1, y, color);
        }
    }

    private static void DrawLine (Color [,] image, int x0, int y0, int x1, int y1, Color color)
    {
        int dx = Math.Abs (x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs (y1 - y0);
        int sy = y0 < y1 ? 1 : -1;
        int error = dx + dy;

        while (true)
        {
            PutPixel (image, x0, y0, color);

            if (x0 == x1 && y0 == y1)
            {
                return;
            }

            int e2 = 2 * error;

            if (e2 >= dy)
            {
                error += dy;
                x0 += sx;
            }

            if (e2 > dx)
            {
                continue;
            }
            error += dx;
            y0 += sy;
        }
    }

    private static void DrawRectangle (Color [,] image, Rectangle rect, Color color)
    {
        DrawLine (image, rect.X, rect.Y, rect.Right - 1, rect.Y, color);
        DrawLine (image, rect.X, rect.Bottom - 1, rect.Right - 1, rect.Bottom - 1, color);
        DrawLine (image, rect.X, rect.Y, rect.X, rect.Bottom - 1, color);
        DrawLine (image, rect.Right - 1, rect.Y, rect.Right - 1, rect.Bottom - 1, color);
    }

    private static void DrawTerminalBadge (Color [,] image, Rectangle rect)
    {
        FillRectangle (image, rect, new Color (9, 14, 20));
        DrawRectangle (image, rect, new Color (96, 200, 255));
        DrawRectangle (image, new Rectangle (rect.X + 3, rect.Y + 3, rect.Width - 6, rect.Height - 6), new Color (31, 65, 74));
        FillRectangle (image, new Rectangle (rect.X + 12, rect.Y + 12, rect.Width - 24, rect.Height - 24), new Color (4, 18, 13));

        var tuiScale = 6;
        var promptScale = 4;
        int tuiWidth = GetTextWidth ("TUI", tuiScale);
        int promptWidth = GetTextWidth (">_", promptScale);
        DrawText (image, rect.X + (rect.Width - tuiWidth) / 2, rect.Y + 22, "TUI", tuiScale, new Color (72, 255, 142));
        DrawText (image, rect.X + (rect.Width - promptWidth) / 2, rect.Y + 68, ">_", promptScale, new Color (72, 255, 142));
    }

    private static void DrawText (Color [,] image, int x, int y, string text, int scale, Color color)
    {
        int cursor = x;

        foreach (char ch in text)
        {
            string [] glyph = GetGlyph (ch);
            DrawGlyph (image, cursor, y, glyph, scale, color);
            cursor += (glyph [0].Length + 1) * scale;
        }
    }

    private static void Fill (Color [,] image, Color color)
    {
        int width = image.GetLength (0);
        int height = image.GetLength (1);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image [x, y] = color;
            }
        }
    }

    private static void FillRectangle (Color [,] image, Rectangle rect, Color color)
    {
        Rectangle bounds = new (0, 0, image.GetLength (0), image.GetLength (1));
        Rectangle clipped = Rectangle.Intersect (rect, bounds);

        for (int y = clipped.Y; y < clipped.Bottom; y++)
        {
            for (int x = clipped.X; x < clipped.Right; x++)
            {
                image [x, y] = color;
            }
        }
    }

    private static string [] GetGlyph (char ch) =>
        ch switch
        {
            'T' => ["11111", "00100", "00100", "00100", "00100", "00100", "00100"],
            'U' => ["10001", "10001", "10001", "10001", "10001", "10001", "01110"],
            'I' => ["111", "010", "010", "010", "010", "010", "111"],
            '>' => ["10000", "01000", "00100", "00010", "00100", "01000", "10000"],
            '_' => ["00000", "00000", "00000", "00000", "00000", "00000", "11111"],
            _ => ["0"]
        };

    private static int GetTextWidth (string text, int scale)
    {
        var width = 0;

        foreach (char ch in text)
        {
            width += (GetGlyph (ch) [0].Length + 1) * scale;
        }

        return Math.Max (0, width - scale);
    }

    private static void PutCirclePoints (Color [,] image, int centerX, int centerY, int x, int y, Color color)
    {
        PutPixel (image, centerX + x, centerY + y, color);
        PutPixel (image, centerX + y, centerY + x, color);
        PutPixel (image, centerX - y, centerY + x, color);
        PutPixel (image, centerX - x, centerY + y, color);
        PutPixel (image, centerX - x, centerY - y, color);
        PutPixel (image, centerX - y, centerY - x, color);
        PutPixel (image, centerX + y, centerY - x, color);
        PutPixel (image, centerX + x, centerY - y, color);
    }

    private static void PutPixel (Color [,] image, int x, int y, Color color)
    {
        if (x < 0 || y < 0 || x >= image.GetLength (0) || y >= image.GetLength (1))
        {
            return;
        }

        image [x, y] = color;
    }
}
