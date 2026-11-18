namespace Terminal.Gui.Views;

internal static class MarkdownImageResolver
{
    public static string GetFallbackText (string? altText) => string.IsNullOrWhiteSpace (altText) ? "[image]" : $"[{altText}]";

    public static bool TryGetSixelData (Func<string, byte []?>? imageLoader, string imageSource, out string sixelData)
    {
        sixelData = string.Empty;

        if (imageLoader is null || string.IsNullOrWhiteSpace (imageSource))
        {
            return false;
        }

        byte []? raw = imageLoader (imageSource);

        if (raw is null || raw.Length == 0)
        {
            return false;
        }

        string decoded = Encoding.UTF8.GetString (raw);

        if (!decoded.Contains ("\u001bP") || !decoded.Contains ("\u001b\\"))
        {
            return false;
        }

        sixelData = decoded;

        return true;
    }
}
