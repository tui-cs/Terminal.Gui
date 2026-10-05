using System.Text;

namespace Terminal.Gui.Drivers;

/// <summary>
///     PERF: UTF-8 byte buffer — replaces StringBuilder in the output hot path.
///     ANSI sequences (CSI/SGR/OSC) are pure ASCII and appended via fast path (no encoding).
///     Unicode graphemes are UTF-8 encoded on append.
///     Reuses its backing array at stable sizes; other rendering work can still allocate.
/// </summary>
internal sealed class Utf8Buffer
{
    internal const int RetainedCapacityLimit = 1024 * 1024;

    private byte [] _buffer = new byte [256];
    private int _length;

    /// <summary>Gets the number of bytes written.</summary>
    public int Length => _length;

    internal int Capacity => _buffer.Length;

    /// <summary>Resets the buffer for reuse without releasing the backing array.</summary>
    public void Clear () => _length = 0;

    /// <summary>Releases an unusually large backing array after a frame is sent.</summary>
    public void TrimExcess ()
    {
        if (_length == 0 && _buffer.Length > RetainedCapacityLimit)
        {
            _buffer = new byte [256];
        }
    }

    /// <summary>Gets a read-only span over the written bytes.</summary>
    public ReadOnlySpan<byte> AsSpan () => _buffer.AsSpan (0, _length);

    /// <summary>
    ///     Appends ASCII as raw bytes. Non-ASCII input falls back to UTF-8 encoding
    ///     instead of truncating characters.
    /// </summary>
    public void AppendAscii (string text)
    {
        int len = text.Length;
        if (len == 0)
        {
            return;
        }

        if (len <= 32)
        {
            EnsureCapacity (len);

            for (int index = 0; index < len; index++)
            {
                char current = text [index];

                if (current >= 0x80)
                {
                    Append (text.AsSpan ());
                    return;
                }

                _buffer [_length + index] = (byte)current;
            }

            _length += len;
            return;
        }

        if (!Ascii.IsValid (text))
        {
            Append (text.AsSpan ());
            return;
        }

        AppendKnownAscii (text);
    }

    /// <summary>
    ///     Appends a string, auto-detecting ASCII vs Unicode.
    ///     ASCII strings use a direct byte path; Unicode strings are UTF-8 encoded.
    /// </summary>
    public void Append (string text)
    {
        int len = text.Length;
        if (len == 0)
        {
            return;
        }

        if (len == 1 && text [0] < 0x80)
        {
            AppendByte ((byte)text [0]);
            return;
        }

        AppendAscii (text);
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
        uint magnitude = value < 0 ? (uint)-(long)value : (uint)value;
        uint remaining = magnitude;
        int digits = 1;

        while (remaining >= 10)
        {
            remaining /= 10;
            digits++;
        }

        int width = digits + (value < 0 ? 1 : 0);
        EnsureCapacity (width);
        int position = _length + width;

        do
        {
            _buffer [--position] = (byte)('0' + magnitude % 10);
            magnitude /= 10;
        } while (magnitude > 0);

        if (value < 0)
        {
            _buffer [--position] = (byte)'-';
        }

        _length += width;
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
        long required = (long)_length + additional;

        if (required <= _buffer.Length)
        {
            return;
        }

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

    private void AppendKnownAscii (ReadOnlySpan<char> text)
    {
        EnsureCapacity (text.Length);
        _length += Encoding.ASCII.GetBytes (text, _buffer.AsSpan (_length));
    }
}
