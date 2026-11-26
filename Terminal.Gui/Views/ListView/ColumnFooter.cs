namespace Terminal.Gui.Views;

public partial class ListView
{
    /// <summary>
    ///     Describes the text shown for one column when <see cref="ShowColumnFooters"/> is <see langword="true"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="HorizontalAlignment"/>, <see cref="VerticalAlignment"/>, and <see cref="Format"/> are optional.
    ///         When one of them is not set, the footer uses the matching
    ///         <see cref="ColumnHeader.MemberHorizontalAlignment"/>, <see cref="ColumnHeader.MemberVerticalAlignment"/>,
    ///         or <see cref="ColumnHeader.MemberFormat"/> of the header at the same index.
    ///     </para>
    ///     <para>
    ///         A line break in <see cref="FooterName"/> starts another footer row when <see cref="Height"/> is greater
    ///         than one. A line wider than the column wraps onto those rows. A height of one shows only the first line.
    ///     </para>
    /// </remarks>
    public class ColumnFooter
    {
        /// <summary>Gets or sets the footer text. A numeric <see cref="Format"/> parses this text.</summary>
        public string? FooterName { get; set; }

        /// <summary>
        ///     Gets or sets the horizontal alignment of each footer line.
        ///     <see langword="null"/> uses the column header's <see cref="ColumnHeader.MemberHorizontalAlignment"/>.
        /// </summary>
        public Alignment? HorizontalAlignment { get; set; }

        /// <summary>
        ///     Gets or sets how footer lines are placed inside <see cref="Height"/>.
        ///     <see langword="null"/> uses the column header's <see cref="ColumnHeader.MemberVerticalAlignment"/>.
        /// </summary>
        public Alignment? VerticalAlignment { get; set; }

        /// <summary>
        ///     Gets or sets a composite format string, for example <c>{0:N2}</c>.
        ///     <see langword="null"/> uses the column header's <see cref="ColumnHeader.MemberFormat"/>.
        /// </summary>
        public string? Format { get; set; }

        /// <summary>Gets or sets how many rows this footer occupies. The default is one.</summary>
        /// <remarks>
        ///     The footer band is as tall as the tallest column. A shorter column leaves the rows below its own height blank.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">The value is less than one.</exception>
        public int Height
        {
            get;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan (value, 1);

                if (field == value)
                {
                    return;
                }

                field = value;
                HeightChanged?.Invoke (this, EventArgs.Empty);
            }
        } = 1;

        /// <summary>Raised when <see cref="Height"/> changes.</summary>
        internal event EventHandler? HeightChanged;

        /// <summary>
        ///     Gets the horizontal alignment to draw. A value set on this footer wins. Otherwise the header's
        ///     <see cref="ColumnHeader.MemberHorizontalAlignment"/> is used.
        /// </summary>
        /// <param name="header">The header at the same column index, or <see langword="null"/>.</param>
        /// <param name="fallback">Alignment used when neither the footer nor the header sets one.</param>
        /// <returns>The alignment to draw.</returns>
        public Alignment ResolveHorizontalAlignment (ColumnHeader? header, Alignment fallback)
        {
            if (HorizontalAlignment is { } alignment)
            {
                return alignment;
            }

            if (header is not null)
            {
                return header.MemberHorizontalAlignment;
            }

            return fallback;
        }

        /// <summary>
        ///     Gets the vertical alignment to draw. A value set on this footer wins. Otherwise the header's
        ///     <see cref="ColumnHeader.MemberVerticalAlignment"/> is used.
        /// </summary>
        /// <param name="header">The header at the same column index, or <see langword="null"/>.</param>
        /// <param name="fallback">Alignment used when neither the footer nor the header sets one.</param>
        /// <returns>The alignment to draw.</returns>
        public Alignment ResolveVerticalAlignment (ColumnHeader? header, Alignment fallback)
        {
            if (VerticalAlignment is { } alignment)
            {
                return alignment;
            }

            if (header is not null)
            {
                return header.MemberVerticalAlignment;
            }

            return fallback;
        }

        /// <summary>
        ///     Gets the format string to apply. A value set on this footer wins. Otherwise the header's
        ///     <see cref="ColumnHeader.MemberFormat"/> is used.
        /// </summary>
        /// <param name="header">The header at the same column index, or <see langword="null"/>.</param>
        /// <returns>The format string, or <see langword="null"/> when neither side sets one.</returns>
        public string? ResolveFormat (ColumnHeader? header)
        {
            if (Format is not null)
            {
                return Format;
            }

            return header?.MemberFormat;
        }
    }
}
