using System.Text;
using Terminal.Gui.Input;

namespace Terminal.Gui.KeySequences;

internal static class KeySequenceMatcher
{
    public static CandidateMatch Match (KeySequencePattern pattern, IReadOnlyList<Key> keys)
    {
        CandidateMatch match = MatchFrom (pattern.Tokens, keys, 0, 0, null, []);
        return match;
    }

    public static bool IsDigit (Key key, out char digit)
    {
        Rune rune = key.AsRune;
        int value = rune.Value;

        if (value is >= '0' and <= '9')
        {
            digit = (char)value;
            return true;
        }

        digit = '\0';
        return false;
    }

    private static CandidateMatch MatchFrom (
        IReadOnlyList<KeySequenceToken> tokens,
        IReadOnlyList<Key> keys,
        int tokenIndex,
        int keyIndex,
        string? countText,
        Dictionary<string, object?> values)
    {
        if (tokenIndex == tokens.Count)
        {
            return keyIndex == keys.Count
                       ? CandidateMatch.Complete (ParseCount (countText), values)
                       : CandidateMatch.NoMatch;
        }

        KeySequenceToken token = tokens [tokenIndex];

        return token.Kind switch
        {
            KeySequenceTokenKind.Literal => MatchLiteral (tokens, keys, tokenIndex, keyIndex, countText, values),
            KeySequenceTokenKind.Count => MatchCount (tokens, keys, tokenIndex, keyIndex, values, token.Name ?? "count"),
            KeySequenceTokenKind.Char => MatchChar (tokens, keys, tokenIndex, keyIndex, countText, values, token.Name ?? "char"),
            KeySequenceTokenKind.AnyKey => MatchAnyKey (tokens, keys, tokenIndex, keyIndex, countText, values, token.Name ?? "key"),
            _ => CandidateMatch.NoMatch
        };
    }

    private static CandidateMatch MatchLiteral (
        IReadOnlyList<KeySequenceToken> tokens,
        IReadOnlyList<Key> keys,
        int tokenIndex,
        int keyIndex,
        string? countText,
        Dictionary<string, object?> values)
    {
        if (keyIndex >= keys.Count)
        {
            return CandidateMatch.Prefix (ParseCount (countText), values);
        }

        KeySequenceToken token = tokens [tokenIndex];

        if (token.Key is null || token.Key != keys [keyIndex])
        {
            return CandidateMatch.NoMatch;
        }

        return MatchFrom (tokens, keys, tokenIndex + 1, keyIndex + 1, countText, values);
    }

    private static CandidateMatch MatchCount (
        IReadOnlyList<KeySequenceToken> tokens,
        IReadOnlyList<Key> keys,
        int tokenIndex,
        int keyIndex,
        Dictionary<string, object?> values,
        string name)
    {
        CandidateMatch zeroDigitMatch = MatchFrom (tokens, keys, tokenIndex + 1, keyIndex, null, new Dictionary<string, object?> (values));

        if (zeroDigitMatch.Kind == CandidateMatchKind.Complete)
        {
            return zeroDigitMatch;
        }

        StringBuilder countBuilder = new ();
        CandidateMatch bestPrefix = zeroDigitMatch.Kind == CandidateMatchKind.Prefix ? zeroDigitMatch : CandidateMatch.NoMatch;

        for (int i = keyIndex; i < keys.Count; i++)
        {
            if (!IsDigit (keys [i], out char digit))
            {
                break;
            }

            countBuilder.Append (digit);
            string countText = countBuilder.ToString ();
            Dictionary<string, object?> nextValues = new (values)
            {
                [name] = ParseCount (countText)
            };

            CandidateMatch match = MatchFrom (tokens, keys, tokenIndex + 1, i + 1, countText, nextValues);

            if (match.Kind == CandidateMatchKind.Complete)
            {
                return match;
            }

            if (match.Kind == CandidateMatchKind.Prefix)
            {
                bestPrefix = match;
            }

            if (i == keys.Count - 1)
            {
                Dictionary<string, object?> prefixValues = new (values)
                {
                    [name] = ParseCount (countText)
                };

                bestPrefix = CandidateMatch.Prefix (ParseCount (countText), prefixValues);
            }
        }

        return bestPrefix;
    }

    private static CandidateMatch MatchChar (
        IReadOnlyList<KeySequenceToken> tokens,
        IReadOnlyList<Key> keys,
        int tokenIndex,
        int keyIndex,
        string? countText,
        Dictionary<string, object?> values,
        string name)
    {
        if (keyIndex >= keys.Count)
        {
            return CandidateMatch.Prefix (ParseCount (countText), values);
        }

        Rune rune = keys [keyIndex].AsRune;

        if (rune == default || Rune.IsControl (rune))
        {
            return CandidateMatch.NoMatch;
        }

        Dictionary<string, object?> nextValues = new (values)
        {
            [name] = keys [keyIndex]
        };

        return MatchFrom (tokens, keys, tokenIndex + 1, keyIndex + 1, countText, nextValues);
    }

    private static CandidateMatch MatchAnyKey (
        IReadOnlyList<KeySequenceToken> tokens,
        IReadOnlyList<Key> keys,
        int tokenIndex,
        int keyIndex,
        string? countText,
        Dictionary<string, object?> values,
        string name)
    {
        if (keyIndex >= keys.Count)
        {
            return CandidateMatch.Prefix (ParseCount (countText), values);
        }

        if (!keys [keyIndex].IsValid)
        {
            return CandidateMatch.NoMatch;
        }

        Dictionary<string, object?> nextValues = new (values)
        {
            [name] = keys [keyIndex]
        };

        return MatchFrom (tokens, keys, tokenIndex + 1, keyIndex + 1, countText, nextValues);
    }

    private static int ParseCount (string? countText)
    {
        if (string.IsNullOrEmpty (countText))
        {
            return 1;
        }

        if (!int.TryParse (countText, out int count))
        {
            return int.MaxValue;
        }

        return count;
    }
}
