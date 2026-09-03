using System.Text;

namespace Terminal.Gui.Drivers;

/// <summary>
///     PERF: UTF-8 byte buffer — replaces StringBuilder in the output hot path.
///     ANSI sequences (CSI/SGR/OSC) are pure ASCII and appended via fast path (no encoding).
///     Unicode graphemes are UTF-8 encoded on append.
///     Zero allocation in stable state (internal buffer grows as needed).
/// </summary>
public sealed class Utf8Buffer
{
    private byte [] _buffer = new byte [256];
    private int _length;

    /// <summary>Gets the number of bytes written.</summary>
    public int Length => _length;

    /// <summary>Resets the buffer for reuse without releasing the backing array.</summary>
    public void Clear () => _length = 0;

    /// <summary>Gets a read-only span over the written bytes.</summary>
    public ReadOnlySpan<byte> AsSpan () => _buffer.AsSpan (0, _length);

    /// <summary>
    ///     Appends an ASCII string as raw bytes. Caller must ensure <paramref name="text"/> is pure ASCII.
    ///     No UTF-8 encoding is performed — each char is truncated to byte directly.
    /// </summary>
    public void AppendAscii (string text)
    {
        int len = text.Length;
        if (len == 0)
        {
            return;
        }

        EnsureCapacity (len);

        for (int i = 0; i < len; i++)
        {
            _buffer [_length + i] = (byte)text [i];
        }

        _length += len;
    }

    /// <summary>
    ///     Appends a string, auto-detecting ASCII vs Unicode.
    ///     ASCII strings use a fast truncation path; Unicode strings are UTF-8 encoded.
    /// </summary>
    public void Append (string text)
    {
        int len = text.Length;
        if (len == 0)
        {
            return;
        }

        // Fast check: if all chars < 0x80, use ASCII path (no encoding)
        bool allAscii = true;

        for (int i = 0; i < len; i++)
        {
            if (text [i] >= 0x80)
            {
                allAscii = false;

                break;
            }
        }

        if (allAscii)
        {
            AppendAscii (text);
            return;
        }

        Append (text.AsSpan ());
    }

    /// <summary>
    ///     Appends a char span as UTF-8 encoded bytes.
    /// </summary>
    public void Append (ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return;
        }

        int byteCount = Encoding.UTF8.GetByteCount (text);
        EnsureCapacity (byteCount);
        Encoding.UTF8.GetBytes (text, _buffer.AsSpan (_length));
        _length += byteCount;
    }

    /// <summary>
    ///     Appends a single ASCII byte directly.
    /// </summary>
    public void AppendByte (byte b)
    {
        EnsureCapacity (1);
        _buffer [_length++] = b;
    }

    /// <summary>
    ///     Appends an integer as ASCII digits directly (no string allocation).
    /// </summary>
    public void AppendInt (int value)
    {
        if (value == 0)
        {
            AppendByte ((byte)'0');
            return;
        }

        if (value < 0)
        {
            AppendByte ((byte)'-');
            value = -value;
        }

        int temp = value;
        int digitCount = 0;

        while (temp > 0)
        {
            digitCount++;
            temp /= 10;
        }

        EnsureCapacity (digitCount);
        int start = _length + digitCount;

        while (value > 0)
        {
            _buffer [--start] = (byte)('0' + value % 10);
            value /= 10;
        }

        _length += digitCount;
    }

    /// <summary>
    ///     Appends the contents of another <see cref="Utf8Buffer"/>.
    /// </summary>
    public void Append (Utf8Buffer other)
    {
        if (other._length == 0)
        {
            return;
        }

        EnsureCapacity (other._length);
        other._buffer.AsSpan (0, other._length).CopyTo (_buffer.AsSpan (_length));
        _length += other._length;
    }

    /// <summary>
    ///     Appends raw UTF-8 bytes directly (e.g. from another buffer or pre-encoded span).
    /// </summary>
    public void AppendBytes (ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return;
        }

        EnsureCapacity (bytes.Length);
        bytes.CopyTo (_buffer.AsSpan (_length));
        _length += bytes.Length;
    }

    private void EnsureCapacity (int additional)
    {
        if (_length + additional <= _buffer.Length)
        {
            return;
        }

        // Guard against int overflow: if required size exceeds Array.MaxLength, throw instead of hanging.
        long required = (long)_length + additional;

        if (required > Array.MaxLength)
        {
            throw new OutOfMemoryException ($"Utf8Buffer capacity {required} exceeds Array.MaxLength.");
        }

        // Perform doubling in long to avoid int overflow, then cast back.
        long newSize = _buffer.Length;

        while (newSize < required)
        {
            newSize *= 2;
        }

        if (newSize > Array.MaxLength)
        {
            newSize = Array.MaxLength;
        }

        Array.Resize (ref _buffer, (int)newSize);
    }
}
