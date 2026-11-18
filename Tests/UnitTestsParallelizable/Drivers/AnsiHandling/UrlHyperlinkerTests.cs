#nullable enable
using System.Text;

namespace DriverTests.AnsiHandling;

[Collection ("Driver Tests")]
public class Osc8UrlLinkerTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Theory]
    [InlineData ("<https://example.com>", "<", "https://example.com", ">")]
    [InlineData ("\"https://example.com\"", "\"", "https://example.com", "\"")]
    public void WrapOsc8_Does_Not_Cross_Delimiters (string input, string prefix, string url, string suffix)
    {
        string actual = Wrap (input);
        string expected = prefix + LinkWrappedWithOSCs (url) + suffix;
        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData ("No url here")]
    [InlineData ("http://")]
    [InlineData ("://missing-scheme.com")]
    public void WrapOsc8_Leaves_Text_Unchanged_Without_Urls (string input)
    {
        StringBuilder sb = new (input);
        StringBuilder result = Osc8UrlLinker.WrapOsc8 (sb);

        Assert.Same (sb, result);
        Assert.Equal (input, result.ToString ());
    }

    [Fact]
    public void WrapOsc8_Stops_At_Ansi_Escape_Sequence ()
    {
        var esc = "\x1B[38;2;173m";
        string input = "https://example.com" + esc + "/more";

        string wrapped = Wrap (input);
        string expected = LinkWrappedWithOSCs ("https://example.com") + esc + "/more";
        Assert.Equal (expected, wrapped);

        // Verify that the hyperlinked visible content contains no ESC
        string start = EscSeqUtils.OSC_StartHyperlink ("https://example.com");
        string end = EscSeqUtils.OSC_EndHyperlink ();
        int s = wrapped.IndexOf (start, StringComparison.Ordinal);
        Assert.True (s >= 0);

        int e = wrapped.IndexOf (end, s + start.Length, StringComparison.Ordinal);
        Assert.True (e > s);

        int contentStart = s + start.Length;
        int contentLength = e - contentStart;
        string visible = wrapped.Substring (contentStart, contentLength);

        Assert.DoesNotContain ('\x1B', visible);
        Assert.Equal ("https://example.com", visible);
    }

    [Theory]
    [InlineData ("See https://example.com/path.", "See ", "https://example.com/path", ".")]
    [InlineData ("See https://example.com/file.tar.gz,", "See ", "https://example.com/file.tar.gz", ",")]
    [InlineData ("See https://example.com/path_(with)_parens)", "See ", "https://example.com/path_(with)_parens", ")")]
    public void WrapOsc8_Trims_Trailing_Punctuation (string input, string prefix, string url, string suffix)
    {
        string actual = Wrap (input);
        string expected = prefix + LinkWrappedWithOSCs (url) + suffix;
        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData (
                    "Multiple: https://a.com, https://b.com!",
                    "Multiple: ",
                    "https://a.com",
                    ", ",
                    "https://b.com",
                    "!")]
    [InlineData (
                    "List https://one.com https://two.com https://three.com",
                    "List ",
                    "https://one.com",
                    " ",
                    "https://two.com",
                    " ",
                    "https://three.com",
                    "")]
    public void WrapOsc8_Wraps_Multiple_Urls (string input, params string [] segments)
    {
        // segments: text0, url1, text1, [url2, text2]...
        string expected = BuildExpected (segments);

        string actual = Wrap (input);
        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData ("https://github.com", "https://github.com")]
    [InlineData ("http://example.com", "http://example.com")]
    [InlineData ("ftp://ftp.example.com/file.zip", "ftp://ftp.example.com/file.zip")]
    [InlineData ("https://example.com:8080/path", "https://example.com:8080/path")]
    [InlineData ("https://example.com/path#anchor", "https://example.com/path#anchor")]
    [InlineData ("https://example.com/path?query=value&other=123", "https://example.com/path?query=value&other=123")]
    [InlineData ("https://commons.wikimedia.org/wiki/File:Spinning_globe.gif", "https://commons.wikimedia.org/wiki/File:Spinning_globe.gif")]
    public void WrapOsc8_Wraps_Standalone_Url (string input, string expectedUrl)
    {
        string actual = Wrap (input);
        string expected = LinkWrappedWithOSCs (expectedUrl);
        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData (
                    "Visit https://github.com for more info",
                    "Visit ",
                    "https://github.com",
                    " for more info")]
    [InlineData (
                    "Check out https://commons.wikimedia.org/wiki/File:Spinning_globe.gif!",
                    "Check out ",
                    "https://commons.wikimedia.org/wiki/File:Spinning_globe.gif",
                    "!")]
    [InlineData (
                    "URLs: https://example.com and http://other.com",
                    "URLs: ",
                    "https://example.com",
                    " and ",
                    "http://other.com",
                    "")]
    public void WrapOsc8_Wraps_Urls_In_Text (string input, params string [] segments)
    {
        // segments: text0, url1, text1, [url2, text2]...
        string expected = BuildExpected (segments);

        string actual = Wrap (input);
        Assert.Equal (expected, actual);
    }

    private static string BuildExpected (string [] segments)
    {
        // segments must be: text0, url1, text1, url2, text2, ...
        string expected = segments.Length > 0 ? segments [0] : string.Empty;

        for (var i = 1; i + 1 < segments.Length; i += 2)
        {
            string url = segments [i];
            string textAfter = segments [i + 1];

            expected += LinkWrappedWithOSCs (url);
            expected += textAfter;
        }

        return expected;
    }

    private static string LinkWrappedWithOSCs (string url, string? display = null)
    {
        display ??= url;

        return EscSeqUtils.OSC_StartHyperlink (url) + display + EscSeqUtils.OSC_EndHyperlink ();
    }

    private static string Wrap (string input)
    {
        StringBuilder sb = new (input);

        return Osc8UrlLinker.WrapOsc8 (sb).ToString ();
    }
}
