namespace Terminal.Gui.Views;

public partial class ListView
{
    /// <summary>
    ///     Describes one column shown by <see cref="ListView"/> when <see cref="ColumnHeaders"/> is set.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A value stays in its own column. A list or array item places element <c>i</c> in column <c>i</c>.
    ///         A line break in that value starts another row of the same column.
    ///         When <see cref="ItemHeight"/> is greater than one, a value wider than its column wraps onto the
    ///         extra rows.
    ///     </para>
    ///     <para>
    ///         A line break in <see cref="HeaderName"/> starts another header row when <see cref="Height"/> is greater
    ///         than one. A line wider than the column wraps onto those rows. Lines that do not fit <see cref="Height"/>
    ///         are clipped.
    ///     </para>
    ///     <para>
    ///         Other item types are read through <see cref="MemberName"/>. <see cref="MemberType"/> and
    ///         <see cref="MemberFormat"/> control conversion and formatting of a property or a list element.
    ///     </para>
    ///     <para>
    ///         <see cref="MemberHorizontalAlignment"/> and <see cref="MemberVerticalAlignment"/> are used instead of
    ///         <see cref="ItemsHorizontalAlignment"/> and <see cref="ItemsVerticalAlignment"/> whenever this header is set.
    ///     </para>
    /// </remarks>
    public class ColumnHeader
    {
        /// <summary>Gets or sets the text shown in the header row. When empty, <see cref="MemberName"/> is used.</summary>
        public string? HeaderName { get; set; }

        /// <summary>Gets or sets the horizontal alignment of each <see cref="HeaderName"/> line.</summary>
        public Alignment HeaderHorizontalAlignment { get; set; } = Alignment.Center;

        /// <summary>Gets or sets how <see cref="HeaderName"/> lines are placed inside <see cref="Height"/>.</summary>
        /// <remarks>
        ///     Placement matches <see cref="ItemsVerticalAlignment"/>. A height of one shows only the first line.
        /// </remarks>
        public Alignment HeaderVerticalAlignment { get; set; } = Alignment.Start;

        /// <summary>Gets or sets how many rows this header occupies. The default is one.</summary>
        /// <remarks>
        ///     <para>
        ///         A line break in <see cref="HeaderName"/> starts another row of this header.
        ///         A line wider than the column wraps onto the extra rows. Lines that do not fit are clipped.
        ///         A height of one shows only the first line.
        ///     </para>
        ///     <para>
        ///         The pinned header band is as tall as the tallest column. A shorter column leaves the rows below its
        ///         own height blank.
        ///     </para>
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

        /// <summary>
        ///     Gets or sets the horizontal alignment of the cells in this column.
        /// </summary>
        /// <remarks>
        ///     This alignment is used instead of <see cref="ItemsHorizontalAlignment"/> whenever this header is set.
        /// </remarks>
        public Alignment MemberHorizontalAlignment { get; set; } = Alignment.Start;

        /// <summary>
        ///     Gets or sets how this column's lines are placed inside <see cref="ItemHeight"/>.
        /// </summary>
        /// <remarks>
        ///     Placement matches <see cref="ItemsVerticalAlignment"/>. This alignment is used instead of
        ///     <see cref="ItemsVerticalAlignment"/> whenever this header is set.
        /// </remarks>
        public Alignment MemberVerticalAlignment { get; set; } = Alignment.Start;

        /// <summary>
        ///     Gets or sets a composite format string applied to the cell value, for example <c>{0:N2}</c>.
        /// </summary>
        public string? MemberFormat { get; set; }
    }
}
