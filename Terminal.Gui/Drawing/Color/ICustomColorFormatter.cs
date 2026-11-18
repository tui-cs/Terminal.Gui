namespace Terminal.Gui.Drawing;

/// <summary>An interface to support custom formatting and parsing of <see cref="Color"/> values.</summary>
public interface ICustomColorFormatter : IFormatProvider, ICustomFormatter
{
    /// <summary>
    ///     A method that returns a <see langword="string"/> based on the <paramref name="formatString"/> specified and
    ///     the byte parameters <paramref name="r"/>, <paramref name="g"/>, <paramref name="b"/>, and <paramref name="a"/>,
    ///     which are provided by <see cref="Color"/>
    /// </summary>
    /// <param name="formatString"></param>
    /// <param name="r"></param>
    /// <param name="g"></param>
    /// <param name="b"></param>
    /// <param name="a"></param>
    /// <returns></returns>
    string Format (string? formatString, byte r, byte g, byte b, byte a);

    /// <summary>A method that returns a <see cref="Color"/> value based on the <paramref name="text"/> specified.</summary>
    /// <param name="text">
    ///     A string or other <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> to parse as a
    ///     <see cref="Color"/>.
    /// </param>
    /// <returns>A <see cref="Color"/> value equivalent to <paramref name="text"/>.</returns>
    Color Parse (ReadOnlySpan<char> text);
}
