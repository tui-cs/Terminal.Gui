using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui.Drawing;

/// <summary>An exception thrown when something goes wrong when trying to parse a <see cref="Color"/>.</summary>
/// <remarks>Contains additional information to help locate the problem. <br/> Not intended to be thrown by consumers.</remarks>
public sealed class ColorParseException : FormatException
{
    internal const string DefaultMessage = "Failed to parse text as Color.";

    internal ColorParseException (string colorString, string? message, Exception? innerException = null) :
        base (message ?? DefaultMessage, innerException)
    {
        ColorString = colorString;
    }

    internal ColorParseException (string colorString, string? message = DefaultMessage) : base (message) { ColorString = colorString; }

    /// <summary>Creates a new instance of a <see cref="ColorParseException"/> populated with the provided values.</summary>
    /// <param name="colorString">The text that caused this exception, as a <see langword="string"/>.</param>
    /// <param name="badValue">The specific value in <paramref name="colorString"/> that caused this exception.</param>
    /// <param name="badValueName">The name of the value (red, green, blue, alpha) that <paramref name="badValue"/> represents.</param>
    /// <param name="reason">The reason that <paramref name="badValue"/> failed to parse.</param>
    internal ColorParseException (
        in ReadOnlySpan<char> colorString,
        string reason,
        in ReadOnlySpan<char> badValue = default,
        in ReadOnlySpan<char> badValueName = default
    ) : base (DefaultMessage)
    {
        ColorString = colorString.ToString ();
        BadValue = badValue.ToString ();
        BadValueName = badValueName.ToString ();
        Reason = reason;
    }

    /// <summary>Creates a new instance of a <see cref="ColorParseException"/> populated with the provided values.</summary>
    /// <param name="colorString">The text that caused this exception, as a <see langword="string"/>.</param>
    /// <param name="badValue">The specific value in <paramref name="colorString"/> that caused this exception.</param>
    /// <param name="badValueName">The name of the value (red, green, blue, alpha) that <paramref name="badValue"/> represents.</param>
    /// <param name="reason">The reason that <paramref name="badValue"/> failed to parse.</param>
    internal ColorParseException (
        in ReadOnlySpan<char> colorString,
        string? badValue = null,
        string? badValueName = null,
        string? reason = null
    ) : base (DefaultMessage)
    {
        ColorString = colorString.ToString ();
        BadValue = badValue;
        BadValueName = badValueName;
        Reason = reason;
    }

    /// <summary>Gets the substring of <see cref="ColorString"/> caused this exception, as a <see langword="string"/></summary>
    /// <remarks>May be null or empty - only set if known.</remarks>
    public string? BadValue { get; }

    /// <summary>Gets the name of the color component corresponding to <see cref="BadValue"/>, if known.</summary>
    /// <remarks>May be null or empty - only set if known.</remarks>
    public string? BadValueName { get; }

    /// <summary>Gets the text that failed to parse, as a <see langword="string"/></summary>
    /// <remarks>Is marked <see langword="required"/>, so must be set by a constructor or initializer.</remarks>
    public string ColorString { get; }

    /// <summary>Gets the reason that <see cref="BadValue"/> failed to parse, if known.</summary>
    /// <remarks>May be null or empty - only set if known.</remarks>
    public string? Reason { get; }

    [DoesNotReturn]
    internal static void Throw (
        in ReadOnlySpan<char> colorString,
        string reason,
        in ReadOnlySpan<char> badValue = default,
        in ReadOnlySpan<char> badValueName = default
    )
    {
        throw new ColorParseException (in colorString, reason, in badValue, in badValueName);
    }

    [DoesNotReturn]
    internal static void ThrowIfNotAsciiDigits (
        in ReadOnlySpan<char> valueText,
        string reason,
        in ReadOnlySpan<char> badValue = default,
        in ReadOnlySpan<char> badValueName = default
    )
    {
        throw new ColorParseException (in valueText, reason, in badValue, in badValueName);
    }
}
