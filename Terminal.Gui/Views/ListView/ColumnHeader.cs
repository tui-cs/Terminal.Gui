namespace Terminal.Gui.Views;

public partial class ListView
{
    /// <summary>
    ///     Describes one column shown by <see cref="ListView"/> when <see cref="ColumnHeaders"/> is set.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         String items are split on newlines and placed into columns, or into successive lines when
    ///         <see cref="ItemHeight"/> is greater than one.
    ///     </para>
    ///     <para>
    ///         Other item types are read through <see cref="MemberName"/>. <see cref="MemberType"/> and
    ///         <see cref="MemberFormat"/> control conversion and formatting.
    ///     </para>
    /// </remarks>
    public class ColumnHeader
    {
        /// <summary>Gets or sets the text shown in the header row. When empty, <see cref="MemberName"/> is used.</summary>
        public string? HeaderName { get; set; }

        /// <summary>Gets or sets the alignment of <see cref="HeaderName"/>.</summary>
        public Alignment HeaderAlignment { get; set; } = Alignment.Center;

        /// <summary>
        ///     Gets or sets the column width in columns.
        /// </summary>
        /// <remarks>
        ///     Zero sizes the column from its content. A positive value is a fixed width. Negative one hides the column.
        /// </remarks>
        public int Width { get; set; }

        /// <summary>Gets or sets the property name read from non-string items.</summary>
        public string? MemberName { get; set; }

        /// <summary>Gets or sets the type used when formatting <see cref="MemberName"/> or a string cell.</summary>
        public Type MemberType { get; set; } = typeof (string);

        /// <summary>Gets or sets the alignment of the cells in this column.</summary>
        public Alignment MemberAlignment { get; set; } = Alignment.Start;

        /// <summary>
        ///     Gets or sets a composite format string applied to the cell value, for example <c>{0:N2}</c>.
        /// </summary>
        public string? MemberFormat { get; set; }
    }
}
