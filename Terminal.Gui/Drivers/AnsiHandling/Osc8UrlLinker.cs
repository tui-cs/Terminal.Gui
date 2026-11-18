
namespace Terminal.Gui.Drivers;

internal static class Osc8UrlLinker
{
    internal readonly record struct UrlRange (int Start, int Length, string Url);

    internal readonly struct Options
    {
        internal readonly string [] _allowedSchemes;
        internal readonly bool _validateWithUri;

        private Options (string [] allowedSchemes, bool validateWithUri)
        {
            _allowedSchemes = allowedSchemes;
            _validateWithUri = validateWithUri;
        }

        public static Options CreateDefault ()
        {
            return new Options (
                allowedSchemes: ["http", "https", "ftp", "ftps"],
                validateWithUri: true
            );
        }
    }

    private static readonly Options _defaultOptions = Options.CreateDefault ();

    internal static StringBuilder WrapOsc8 (StringBuilder input)
    {
        return WrapOsc8 (input, _defaultOptions);
    }

    internal static List<UrlRange> FindUrls (string input)
    {
        return FindUrls (input, _defaultOptions);
    }

    internal static StringBuilder WrapOsc8 (StringBuilder input, Options options)
    {
        if (input.Length == 0)
        {
            return input;
        }

        string text = input.ToString ();
        int len = text.Length;

        StringBuilder? result = null;
        int copyFrom = 0;

        int i = 0;
        while (i < len)
        {
            if (text [i] == '\x1B')
            {
                int escEnd = ParseEscapeSequence (text, i, len);
                if (result != null)
                {
                    if (i > copyFrom)
                    {
                        result.Append (text, copyFrom, i - copyFrom);
                    }

                    result.Append (text, i, escEnd - i);
                    copyFrom = escEnd;
                }

                i = escEnd;
                continue;
            }

            int segStart = i;
            int nextEsc = text.IndexOf ('\x1B', segStart);
            if (nextEsc < 0)
            {
                nextEsc = len;
            }

            bool changed;
            string processed = WrapPlainText (text, segStart, nextEsc, options, out changed);

            if (changed)
            {
                result ??= new StringBuilder (text.Length + 100);

                if (segStart > copyFrom)
                {
                    result.Append (text, copyFrom, segStart - copyFrom);
                }

                result.Append (processed);
                copyFrom = nextEsc;
            }

            i = nextEsc;
        }

        if (result is null)
        {
            return input;
        }

        if (copyFrom < len)
        {
            result.Append (text, copyFrom, len - copyFrom);
        }

        return result;
    }

    private static List<UrlRange> FindUrls (string input, Options options)
    {
        List<UrlRange> ranges = [];
        ReadOnlySpan<char> span = input.AsSpan ();
        ReadOnlySpan<char> delimiter = "://".AsSpan ();
        int i = 0;

        while (i < span.Length)
        {
            int rel = span.Slice (i).IndexOf (delimiter, StringComparison.Ordinal);
            if (rel < 0)
            {
                break;
            }

            int delimAt = i + rel;
            int schemeEnd = delimAt;
            int schemeStart = schemeEnd - 1;

            while (schemeStart >= 0 && char.IsLetter (span [schemeStart]))
            {
                schemeStart--;
            }

            schemeStart++;

            if (schemeStart < 0 || schemeStart >= schemeEnd)
            {
                i = delimAt + delimiter.Length;
                continue;
            }

            ReadOnlySpan<char> scheme = span.Slice (schemeStart, schemeEnd - schemeStart);
            if (!IsAllowedScheme (scheme, options))
            {
                i = delimAt + delimiter.Length;
                continue;
            }

            int urlStart = schemeStart;
            int j = delimAt + delimiter.Length;

            while (j < span.Length && !IsUrlTerminator (span [j]))
            {
                j++;
            }

            int urlEnd = TrimTrailingPunctuation (span, urlStart, j);
            if (urlEnd <= (delimAt + delimiter.Length))
            {
                i = j;
                continue;
            }

            string candidate = span.Slice (urlStart, urlEnd - urlStart).ToString ();

            Uri? _;
            if (options._validateWithUri && !IsValidUrl (candidate, options, out _))
            {
                i = j;
                continue;
            }

            ranges.Add (new (urlStart, urlEnd - urlStart, candidate));
            i = j;
        }

        return ranges;
    }

    private static int ParseEscapeSequence (string text, int start, int len)
    {
        int i = start;
        if (i + 1 >= len)
        {
            return len;
        }

        char c1 = text [i + 1];

        if (c1 == '[')
        {
            int j = i + 2;
            while (j < len)
            {
                char ch = text [j++];
                if (ch >= '@' && ch <= '~')
                {
                    break;
                }
            }

            return j;
        }

        if (c1 == ']')
        {
            int j = i + 2;
            while (j < len)
            {
                char ch = text [j++];
                if (ch == '\x07')
                {
                    break;
                }

                if (ch == '\x1B')
                {
                    if (j < len && text [j] == '\\')
                    {
                        j++;
                        break;
                    }
                }
            }

            return j;
        }

        return Math.Min (i + 2, len);
    }

    private static string WrapPlainText (string full, int start, int endExclusive, Options options, out bool changed)
    {
        ReadOnlySpan<char> span = full.AsSpan (start, endExclusive - start);
        ReadOnlySpan<char> delimiter = "://".AsSpan ();
        int i = 0;
        int copyFrom = 0;
        StringBuilder? sb = null;

        while (i < span.Length)
        {
            int rel = span.Slice (i).IndexOf (delimiter, StringComparison.Ordinal);
            if (rel < 0)
            {
                break;
            }

            int delimAt = i + rel;
            int schemeEnd = delimAt;
            int schemeStart = schemeEnd - 1;

            while (schemeStart >= 0 && char.IsLetter (span [schemeStart]))
            {
                schemeStart--;
            }

            schemeStart++;

            if (schemeStart < 0 || schemeStart >= schemeEnd)
            {
                i = delimAt + delimiter.Length;
                continue;
            }

            ReadOnlySpan<char> scheme = span.Slice (schemeStart, schemeEnd - schemeStart);
            if (!IsAllowedScheme (scheme, options))
            {
                i = delimAt + delimiter.Length;
                continue;
            }

            int urlStart = schemeStart;
            int j = delimAt + delimiter.Length;

            while (j < span.Length && !IsUrlTerminator (span [j]))
            {
                j++;
            }

            // Trim punctuation from the end of the URL, but remember it so we can re-append it after the hyperlink
            int urlEnd = TrimTrailingPunctuation (span, urlStart, j);
            ReadOnlySpan<char> trailing = span.Slice (urlEnd, j - urlEnd);

            if (urlEnd <= (delimAt + delimiter.Length))
            {
                i = j;
                continue;
            }

            string candidate = span.Slice (urlStart, urlEnd - urlStart).ToString ();

            Uri? _;
            if (options._validateWithUri && !IsValidUrl (candidate, options, out _))
            {
                i = j;
                continue;
            }

            sb ??= new StringBuilder (span.Length + 64);

            if (urlStart > copyFrom)
            {
                sb.Append (span.Slice (copyFrom, urlStart - copyFrom));
            }

            // Preserve original candidate for link target and display
            string linkTarget = candidate;

            sb.Append (EscSeqUtils.OSC_StartHyperlink (linkTarget));
            sb.Append (candidate);
            sb.Append (EscSeqUtils.OSC_EndHyperlink ());

            // Re-append the trimmed punctuation/suffix (e.g., "!", ",", ")")
            if (!trailing.IsEmpty)
            {
                sb.Append (trailing);
            }

            copyFrom = j;
            i = j;
        }

        if (sb is null)
        {
            changed = false;
            return span.ToString ();
        }

        if (copyFrom < span.Length)
        {
            sb.Append (span.Slice (copyFrom));
        }

        changed = true;
        return sb.ToString ();
    }

    private static bool IsAllowedScheme (ReadOnlySpan<char> scheme, Options options)
    {
        for (int i = 0; i < options._allowedSchemes.Length; i++)
        {
            string s = options._allowedSchemes [i];
            if (scheme.Equals (s, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsUrlTerminator (char c)
    {
        return char.IsWhiteSpace (c) || c == '<' || c == '>' || c == '"' || c == '\'' || c == '\x1B';
    }

    private static int TrimTrailingPunctuation (ReadOnlySpan<char> span, int start, int end)
    {
        int e = end;

        while (e > start)
        {
            char c = span [e - 1];
            if (c is '.' or ',' or '!' or '?' or ';' or ':' or ']' or '}' or '"')
            {
                e--;
            }
            else
            {
                break;
            }
        }

        if (e > start && span [e - 1] == ')')
        {
            int opens = 0;
            int closes = 0;

            for (int k = start; k < e; k++)
            {
                if (span [k] == '(')
                {
                    opens++;
                }
                else if (span [k] == ')')
                {
                    closes++;
                }
            }

            while (e > start && closes > opens && span [e - 1] == ')')
            {
                e--;
                closes--;
            }
        }

        return e;
    }

    private static bool IsValidUrl (string candidate, Options options, out Uri? uri)
    {
        if (Uri.TryCreate (candidate, UriKind.Absolute, out uri))
        {
            for (int i = 0; i < options._allowedSchemes.Length; i++)
            {
                string s = options._allowedSchemes [i];
                if (uri.Scheme.Equals (s, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        uri = null;
        return false;
    }
}
