namespace Terminal.Gui.Drawing;

public readonly partial record struct Color
{
    /// <summary>
    ///     The WCAG 2.1 contrast ratio for normal text (success criterion 1.4.3).
    /// </summary>
    public const double MinimumReadableContrastRatio = 4.5;

    /// <summary>
    ///     Gets the WCAG 2.1 relative luminance of this color in the range 0 to 1.
    /// </summary>
    /// <returns>The relative luminance of the RGB channels. Alpha is ignored.</returns>
    public double GetRelativeLuminance ()
    {
        return 0.2126 * Linearize (R) + 0.7152 * Linearize (G) + 0.0722 * Linearize (B);

        static double Linearize (byte channel)
        {
            double srgb = channel / 255.0;

            if (srgb <= 0.03928)
            {
                return srgb / 12.92;
            }

            return Math.Pow ((srgb + 0.055) / 1.055, 2.4);
        }
    }

    /// <summary>
    ///     Gets the WCAG 2.1 contrast ratio between this color and <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The color to compare against.</param>
    /// <returns>
    ///     A ratio of at least 1. White against black is 21. Alpha is ignored.
    /// </returns>
    public double GetContrastRatio (Color other)
    {
        double l1 = GetRelativeLuminance ();
        double l2 = other.GetRelativeLuminance ();
        double lighter = Math.Max (l1, l2);
        double darker = Math.Min (l1, l2);

        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>
    ///     Returns this color, or a lightness-adjusted copy, so the WCAG contrast against
    ///     <paramref name="background"/> is at least <paramref name="minimumRatio"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Hue and saturation are preserved. Lightness moves away from the background.
    ///         If this color is already on the dark side of the background and cannot reach the
    ///         ratio by going darker, lightness increases instead (and the reverse toward white).
    ///         Named grays are not substituted.
    ///     </para>
    /// </remarks>
    /// <param name="background">The background to contrast against.</param>
    /// <param name="minimumRatio">
    ///     The minimum WCAG contrast ratio. Values of 1 or less return this color unchanged.
    ///     The default is <see cref="MinimumReadableContrastRatio"/>.
    /// </param>
    /// <returns>This color, or a copy with lightness moved far enough to meet the ratio.</returns>
    public Color EnsureContrastAgainst (Color background, double minimumRatio = MinimumReadableContrastRatio)
    {
        if (minimumRatio <= 1.0)
        {
            return this;
        }

        if (GetContrastRatio (background) >= minimumRatio)
        {
            return this;
        }

        GetHsl (out double hue, out double saturation, out double lightness);
        double backgroundLuminance = background.GetRelativeLuminance ();
        double foregroundLuminance = GetRelativeLuminance ();
        bool preferLighter = foregroundLuminance > backgroundLuminance;

        if (Math.Abs (foregroundLuminance - backgroundLuminance) < 1e-9)
        {
            preferLighter = backgroundLuminance < 0.5;
        }

        Color adjusted = SearchLightness (hue, saturation, lightness, background, minimumRatio, preferLighter);

        if (adjusted.GetContrastRatio (background) >= minimumRatio)
        {
            return adjusted;
        }

        return SearchLightness (hue, saturation, lightness, background, minimumRatio, !preferLighter);
    }

    private static Color SearchLightness (
        double hue,
        double saturation,
        double lightness,
        Color background,
        double minimumRatio,
        bool lighter)
    {
        double lo = lighter ? lightness : 0.0;
        double hi = lighter ? 1.0 : lightness;
        Color best = FromHsl (hue, saturation, lighter ? 1.0 : 0.0);

        if (best.GetContrastRatio (background) < minimumRatio)
        {
            return best;
        }

        for (int i = 0; i < 24; i++)
        {
            double mid = (lo + hi) / 2.0;
            Color candidate = FromHsl (hue, saturation, mid);

            if (candidate.GetContrastRatio (background) >= minimumRatio)
            {
                best = candidate;

                if (lighter)
                {
                    hi = mid;
                }
                else
                {
                    lo = mid;
                }

                continue;
            }

            if (lighter)
            {
                lo = mid;
            }
            else
            {
                hi = mid;
            }
        }

        return best;
    }
}
