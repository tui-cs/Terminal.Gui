using System.Text;

namespace DriverTests.Output;

/// <summary>
///     Focused tests for <see cref="Utf8Buffer"/> encoding, reuse, and capacity behavior.
/// </summary>
[Collection ("Driver Tests")]
public class Utf8BufferTests
{
    [Fact]
    public void AppendAscii_EmptyString_DoesNothing ()
    {
        Utf8Buffer buffer = new ();
        buffer.AppendAscii (string.Empty);

        Assert.Equal (0, buffer.Length);
    }

    [Fact]
    public void AppendAscii_PureAscii_StoresRawBytes ()
    {
        Utf8Buffer buffer = new ();
        buffer.AppendAscii ("Hello");

        Assert.Equal (5, buffer.Length);
        Assert.Equal ("Hello", Encoding.UTF8.GetString (buffer.AsSpan ()));
    }

    [Fact]
    public void AppendAscii_CsiEscapeSequence_StoresExactBytes ()
    {
        Utf8Buffer buffer = new ();
        buffer.AppendAscii ("\x1b[38;2;1;2;3m");

        ReadOnlySpan<byte> span = buffer.AsSpan ();
        Assert.Equal ((byte)0x1b, span [0]);
        Assert.Equal ((byte)'[', span [1]);
        Assert.Equal ("38;2;1;2;3m", Encoding.UTF8.GetString (span [2..]));
    }

    [Fact]
    public void Append_AsciiString_UsesFastPath ()
    {
        Utf8Buffer buffer = new ();
        buffer.Append ("ABC");

        Assert.Equal (3, buffer.Length);
        Assert.Equal ("ABC", Encoding.UTF8.GetString (buffer.AsSpan ()));
    }

    [Fact]
    public void Append_MultibyteUtf8_EncodesCorrectly ()
    {
        Utf8Buffer buffer = new ();
        // "héllo" — 'é' is U+00E9, 2 bytes in UTF-8 (0xC3 0xA9)
        buffer.Append ("héllo");

        Assert.Equal (6, buffer.Length);
        Assert.Equal ("héllo", Encoding.UTF8.GetString (buffer.AsSpan ()));
    }

    [Fact]
    public void Append_CjkCharacters_EncodesMultibyte ()
    {
        Utf8Buffer buffer = new ();
        // Chinese "你好" — each char is 3 bytes in UTF-8
        buffer.Append ("你好");

        Assert.Equal (6, buffer.Length);
        Assert.Equal ("你好", Encoding.UTF8.GetString (buffer.AsSpan ()));
    }

    [Fact]
    public void Append_SurrogatePair_Emoji_EncodesAs4Bytes ()
    {
        Utf8Buffer buffer = new ();
        // U+1F600 (😀) is a surrogate pair in UTF-16, 4 bytes in UTF-8
        buffer.Append ("😀");

        Assert.Equal (4, buffer.Length);
        Assert.Equal ("😀", Encoding.UTF8.GetString (buffer.AsSpan ()));
    }

    [Fact]
    public void Append_MixedAsciiAndUnicode_EncodesCorrectly ()
    {
        Utf8Buffer buffer = new ();
        buffer.Append ("A你B😀C");

        Assert.Equal ("A你B😀C", Encoding.UTF8.GetString (buffer.AsSpan ()));
    }

    [Fact]
    public void Append_EmptyString_DoesNothing ()
    {
        Utf8Buffer buffer = new ();
        buffer.Append (string.Empty);

        Assert.Equal (0, buffer.Length);
    }

    [Fact]
    public void Append_CharSpan_Empty_DoesNothing ()
    {
        Utf8Buffer buffer = new ();
        buffer.Append (ReadOnlySpan<char>.Empty);

        Assert.Equal (0, buffer.Length);
    }

    [Fact]
    public void AppendByte_SingleByte_StoresAtCorrectPosition ()
    {
        Utf8Buffer buffer = new ();
        buffer.AppendByte (0x41);
        buffer.AppendByte (0x42);

        Assert.Equal (2, buffer.Length);
        Assert.Equal ("AB", Encoding.UTF8.GetString (buffer.AsSpan ()));
    }

    [Fact]
    public void AppendInt_Zero_AppendsDigitZero ()
    {
        Utf8Buffer buffer = new ();
        buffer.AppendInt (0);

        Assert.Equal ("0", Encoding.UTF8.GetString (buffer.AsSpan ()));
    }

    [Fact]
    public void AppendInt_Positive_AppendsDigits ()
    {
        Utf8Buffer buffer = new ();
        buffer.AppendInt (255);

        Assert.Equal ("255", Encoding.UTF8.GetString (buffer.AsSpan ()));
    }

    [Fact]
    public void AppendInt_Negative_AppendsMinusAndDigits ()
    {
        Utf8Buffer buffer = new ();
        buffer.AppendInt (-42);

        Assert.Equal ("-42", Encoding.UTF8.GetString (buffer.AsSpan ()));
    }

    [Theory]
    [InlineData (1)]
    [InlineData (9)]
    [InlineData (10)]
    [InlineData (99)]
    [InlineData (100)]
    [InlineData (999)]
    [InlineData (1000)]
    [InlineData (int.MaxValue)]
    public void AppendInt_VariousValues_MatchesToString (int value)
    {
        Utf8Buffer buffer = new ();
        buffer.AppendInt (value);

        Assert.Equal (value.ToString (), Encoding.UTF8.GetString (buffer.AsSpan ()));
    }

    [Fact]
    public void AppendBytes_RawBytes_StoresExactContent ()
    {
        Utf8Buffer buffer = new ();
        byte [] data = [0x1b, (byte)'[', (byte)'m'];

        buffer.AppendBytes (data);

        Assert.Equal (3, buffer.Length);
        Assert.Equal ("\x1b[m", Encoding.UTF8.GetString (buffer.AsSpan ()));
    }

    [Fact]
    public void AppendBytes_EmptySpan_DoesNothing ()
    {
        Utf8Buffer buffer = new ();
        buffer.AppendBytes (ReadOnlySpan<byte>.Empty);

        Assert.Equal (0, buffer.Length);
    }

    [Fact]
    public void Append_OtherBuffer_CopiesContent ()
    {
        Utf8Buffer source = new ();
        source.AppendAscii ("Hello");
        source.Append ("世界");

        Utf8Buffer dest = new ();
        dest.Append (source);

        Assert.Equal (source.Length, dest.Length);
        Assert.Equal (Encoding.UTF8.GetString (source.AsSpan ()), Encoding.UTF8.GetString (dest.AsSpan ()));
    }

    [Fact]
    public void Append_OtherEmptyBuffer_DoesNothing ()
    {
        Utf8Buffer source = new ();
        Utf8Buffer dest = new ();
        dest.Append (source);

        Assert.Equal (0, dest.Length);
    }

    [Fact]
    public void Clear_ResetsLength_KeepsCapacity ()
    {
        Utf8Buffer buffer = new ();
        buffer.AppendAscii ("Hello");

        buffer.Clear ();

        Assert.Equal (0, buffer.Length);
        Assert.True (buffer.AsSpan ().IsEmpty);
    }

    [Fact]
    public void Clear_ThenAppend_WorksCorrectly ()
    {
        Utf8Buffer buffer = new ();
        buffer.AppendAscii ("First");
        buffer.Clear ();
        buffer.AppendAscii ("Second");

        Assert.Equal (6, buffer.Length);
        Assert.Equal ("Second", Encoding.UTF8.GetString (buffer.AsSpan ()));
    }

    [Fact]
    public void AsSpan_ReturnsWrittenBytesOnly ()
    {
        Utf8Buffer buffer = new ();
        buffer.AppendAscii ("AB");

        ReadOnlySpan<byte> span = buffer.AsSpan ();

        Assert.Equal (2, span.Length);
        Assert.Equal ((byte)'A', span [0]);
        Assert.Equal ((byte)'B', span [1]);
    }

    [Fact]
    public void Capacity_GrowsAutomatically_WhenExceeded ()
    {
        Utf8Buffer buffer = new ();

        // Append a large string that exceeds the initial 256-byte capacity.
        string large = new ('x', 1000);
        buffer.Append (large);

        Assert.Equal (1000, buffer.Length);
        Assert.Equal (large, Encoding.UTF8.GetString (buffer.AsSpan ()));
    }

    [Fact]
    public void Capacity_GrowsForMultibyteContent ()
    {
        Utf8Buffer buffer = new ();

        // Append 500 CJK chars (3 bytes each = 1500 bytes, exceeds initial 256).
        string large = new ('你', 500);
        buffer.Append (large);

        Assert.Equal (1500, buffer.Length);
        Assert.Equal (large, Encoding.UTF8.GetString (buffer.AsSpan ()));
    }

    [Fact]
    public void MultipleAppends_AccumulateCorrectly ()
    {
        Utf8Buffer buffer = new ();
        buffer.AppendAscii ("\x1b[");
        buffer.AppendAscii ("38;2;");
        buffer.AppendInt (255);
        buffer.AppendByte ((byte)';');
        buffer.AppendInt (128);
        buffer.AppendByte ((byte)';');
        buffer.AppendInt (0);
        buffer.AppendByte ((byte)'m');
        buffer.Append ("X");

        Assert.Equal ("\x1b[38;2;255;128;0mX", Encoding.UTF8.GetString (buffer.AsSpan ()));
    }

    [Fact]
    public void ReuseAfterClear_DoesNotCorruptData ()
    {
        Utf8Buffer buffer = new ();

        for (int i = 0; i < 10; i++)
        {
            buffer.Clear ();
            buffer.AppendAscii ($"Iter{i}");
            Assert.Equal ($"Iter{i}", Encoding.UTF8.GetString (buffer.AsSpan ()));
        }
    }

    [Fact]
    public void Append_CharSpan_Unicode_EncodesCorrectly ()
    {
        Utf8Buffer buffer = new ();
        buffer.Append ("héllo".AsSpan ());

        Assert.Equal (6, buffer.Length);
        Assert.Equal ("héllo", Encoding.UTF8.GetString (buffer.AsSpan ()));
    }
}
