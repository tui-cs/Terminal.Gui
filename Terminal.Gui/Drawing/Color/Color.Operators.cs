using System.Diagnostics.Contracts;
using System.Numerics;

namespace Terminal.Gui.Drawing;

public readonly partial record struct Color
{
    /// <inheritdoc/>
    /// <returns>
    ///     A <see cref="Color"/> <see langword="struct"/> with all values set to <see cref="byte.MaxValue"/>, meaning
    ///     white.
    /// </returns>
    public static Color MaxValue => new (uint.MaxValue);

    /// <inheritdoc/>
    /// <returns>A <see cref="Color"/> <see langword="struct"/> with all values set to zero.</returns>
    /// <remarks>
    ///     Though this returns a <see cref="Color"/> with <see cref="A"/>, <see cref="R"/>, <see cref="G"/>, and
    ///     <see cref="B"/> all set to zero, Terminal.Gui will treat it as black, because the alpha channel is not supported.
    /// </remarks>
    public static Color MinValue => new (uint.MinValue);

    /// <inheritdoc/>
    [Pure]
    public override int GetHashCode () { return Rgba.GetHashCode (); }

    /// <summary>
    ///     Implicit conversion from <see cref="Color"/> to <see cref="Vector3"/> via
    ///     <see cref="Vector3(float,float,float)"/> where ( <see cref="Vector3.X"/>, <see cref="Vector3.Y"/>,
    ///     <see cref="Vector3.Z"/>) is (R,G,B).
    /// </summary>
    /// <remarks>
    ///     This cast is narrowing and drops the alpha channel.
    ///     <para/>
    ///     Use <see cref="implicit operator Vector4(Color)"/> to maintain full value.
    /// </remarks>
    [Pure]
    public static explicit operator Vector3 (Color color) { return new Vector3 (color.R, color.G, color.B); }

    /// <summary>
    ///     Implicit conversion from <see langword="int"/> to <see cref="Color"/>, via the <see cref="Color(int)"/>
    ///     costructor.
    /// </summary>
    [Pure]
    public static implicit operator Color (int rgba) { return new Color (rgba); }

    /// <summary>
    ///     Implicit conversion from <see langword="uint"/> to <see cref="Color"/>, via the <see cref="Color(uint)"/>
    ///     costructor.
    /// </summary>
    [Pure]
    public static implicit operator Color (uint u) { return new Color (u); }

    /// <summary>
    ///     Implicit conversion from <see cref="GetClosestNamedColor16(Color)"/> to <see cref="Color"/> via lookup from
    ///     <see cref="ColorExtensions.ColorName16ToColorMap"/>.
    /// </summary>
    [Pure]
    public static implicit operator Color (ColorName16 colorName) { return ColorExtensions.ColorName16ToColorMap! [colorName]; }

    /// <summary>
    ///     Implicit conversion from <see cref="Vector4"/> to <see cref="Color"/>, where (<see cref="Vector4.X"/>,
    ///     <see cref="Vector4.Y"/>, <see cref="Vector4.Z"/>, <see cref="Vector4.W"/>) is (<see cref="A"/>,<see cref="R"/>,
    ///     <see cref="G"/>,<see cref="B"/>), via <see cref="Color(int,int,int,int)"/>.
    /// </summary>
    [Pure]
    public static implicit operator Color (Vector4 v) { return new Color ((byte)v.X, (byte)v.Y, (byte)v.Z, (byte)v.W); }

    /// <summary>
    ///     Implicit conversion from <see cref="Vector3"/>, where <see cref="Vector3.X"/> = <see cref="R"/>,
    ///     <see cref="Vector3.Y"/> = <see cref="G"/>, and <see cref="Vector3.Z"/> = <see cref="B"/>.
    /// </summary>
    [Pure]
    public static implicit operator Color (Vector3 v) { return new Color ((byte)v.X, (byte)v.Y, (byte)v.Z); }

    /// <summary>
    ///     Implicit conversion from <see cref="Color"/> to <see langword="int"/> by returning the value of the
    ///     <see cref="Rgba"/> field.
    /// </summary>
    [Pure]
    public static implicit operator int (Color color) { return color.Rgba; }

    /// <summary>
    ///     Implicit conversion from <see cref="Color"/> to <see langword="uint"/> by returning the value of the
    ///     <see cref="Argb"/> field.
    /// </summary>
    [Pure]
    public static implicit operator uint (Color color) { return color.Argb; }

    /// <summary>
    ///     Implicit conversion to <see cref="Vector3"/>, where <see cref="Vector3.X"/> = <see cref="R"/>,
    ///     <see cref="Vector3.Y"/> = <see cref="G"/>, and <see cref="Vector3.Z"/> = <see cref="B"/>.
    /// </summary>
    [Pure]
    public static implicit operator Vector4 (Color color) { return new Vector4 (color.R, color.G, color.B, color.A); }
}
