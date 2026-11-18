namespace Terminal.Gui.Drivers;

/// <summary>
///     Expectation record supporting an optional Value token used for disambiguation.
/// </summary>
internal record AnsiResponseExpectation (string? Terminator, string? Value, Action<IHeld> Response, Action? Abandoned)
{
    public bool Matches (string? cur)
    {
        if (string.IsNullOrEmpty (cur) || string.IsNullOrEmpty (Terminator))
        {
            return false;
        }

        // Must end with the terminator
        if (!cur.EndsWith (Terminator!, StringComparison.Ordinal))
        {
            return false;
        }

        // If no specific value requested, any response ending with terminator matches
        if (string.IsNullOrEmpty (Value))
        {
            return true;
        }

        string s = cur;

        if (s.Length > 0 && s [0] == '\x1B')
        {
            s = s [1..];
        }

        string? csiToken = TryGetLeadingToken (s, '[', Terminator!);

        if (!string.IsNullOrEmpty (csiToken))
        {
            return TokenMatchesValue (csiToken, Value);
        }

        string? oscToken = TryGetLeadingToken (s, ']', Terminator!);

        if (!string.IsNullOrEmpty (oscToken))
        {
            return TokenMatchesValue (oscToken, Value);
        }

        return s.Contains ($"[{Value};", StringComparison.Ordinal)
               || s.Contains ($"]{Value};", StringComparison.Ordinal)
               || s.Contains ($"[{Value}", StringComparison.Ordinal)
               || s.Contains ($"]{Value}", StringComparison.Ordinal);
    }

    private static string? TryGetLeadingToken (string input, char prefix, string terminator)
    {
        if (string.IsNullOrEmpty (input) || input [0] != prefix)
        {
            return null;
        }

        int startIndex = 1;
        int endIndex = input.IndexOfAny ([';', terminator [0]], startIndex);

        if (endIndex < 0)
        {
            endIndex = input.Length;
        }

        if (endIndex <= startIndex)
        {
            return null;
        }

        return input [startIndex..endIndex];
    }

    private static bool TokenMatchesValue (string token, string value)
    {
        if (string.Equals (token, value, StringComparison.Ordinal))
        {
            return true;
        }

        if (!char.IsDigit (value [0]))
        {
            return token.StartsWith (value, StringComparison.Ordinal);
        }

        return false;
    }
}
