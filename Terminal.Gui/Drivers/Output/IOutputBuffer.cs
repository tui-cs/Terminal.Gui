namespace Terminal.Gui.Drivers;

/// <summary>
///     Represents the desired screen state for console rendering. This interface provides methods for building up
///     visual content (text, attributes, fills) in a buffer that can be efficiently written to the terminal
///     in a single operation at the end of each iteration. Final output is handled by <see cref="IOutput"/>.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="IOutputBuffer"/> acts as an intermediary between Terminal.Gui's high-level drawing operations
///         and the low-level console output. Rather than writing directly to the console for each operation, views
///         draw to this buffer during layout and rendering. The buffer is then flushed to the terminal by
///         <see cref="IOutput"/> after all drawing is complete, minimizing flicker and improving performance.
///     </para>
///     <para>
///         The buffer maintains a 2D array of <see cref="Cell"/> objects in <see cref="Contents"/>, where each cell
///         represents a single character position on screen with its associated character, attributes, and dirty state.
///         Drawing operations like <see cref="AddRune(Rune)"/> and <see cref="AddStr(string)"/> modify cells at the
///         current cursor position (tracked by <see cref="Col"/> and <see cref="Row"/>), respecting any active
///         <see cref="Clip"/> region.
///     </para>
/// </remarks>
public interface IOutputBuffer
{
    /// <summary>Adds the specified rune to the display at the current cursor position.</summary>
    /// <param name="rune">Rune to add.</param>
    void AddRune (Rune rune);

    /// <summary>
    ///     Adds the specified character to the display at the current cursor position. This is a convenience method for
    ///     AddRune.
    /// </summary>
    /// <param name="c">Character to add.</param>
    void AddRune (char c);

    /// <summary>Adds the string to the display at the current cursor position.</summary>
    /// <param name="str">String to add.</param>
    void AddStr (string str);

    /// <summary>Clears the contents of the buffer.</summary>
    void ClearContents ();

    /// <summary>
    ///     Gets a value for each row indicating whether that row contains any dirty cells and should be rendered.
    ///     Used by <see cref="IOutput"/> to skip rows with no pending changes, avoiding unnecessary cursor moves.
    /// </summary>
    /// <remarks>
    ///     Output implementations clear each row's flag after emitting its dirty cells. Buffer implementations must set
    ///     the corresponding row flag whenever a cell is marked dirty. Code that modifies <see cref="Contents"/> directly
    ///     is responsible for updating the corresponding row flag.
    /// </remarks>
    bool [] DirtyLines { get; }

    /// <summary>
    ///     Gets or sets the clip rectangle that <see cref="AddRune(Rune)"/> and <see cref="AddStr(string)"/> are subject
    ///     to.
    /// </summary>
    /// <value>The rectangle describing the of <see cref="Clip"/> region.</value>
    public Region? Clip { get; set; }

    /// <summary>
    ///     Gets the column last set by <see cref="Move"/>. <see cref="Col"/> and <see cref="Row"/> are used by
    ///     <see cref="AddRune(Rune)"/> and <see cref="AddStr"/> to determine where to add content.
    /// </summary>
    public int Col { get; }

    /// <summary>The number of columns visible in the terminal.</summary>
    int Cols { get; set; }

    /// <summary>
    ///     The contents of the application output. The driver outputs this buffer to the terminal when UpdateScreen is called.
    /// </summary>
    Cell [,]? Contents { get; set; }

    /// <summary>
    ///     The <see cref="Attribute"/> that will be used for the next AddRune or AddStr call.
    /// </summary>
    Attribute CurrentAttribute { get; set; }

    /// <summary>
    ///     Fills the given <paramref name="rect"/> with the given
    ///     symbol using the currently selected attribute.
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="rune"></param>
    void FillRect (Rectangle rect, Rune rune);

    /// <summary>
    ///     Fills the given <paramref name="rect"/> with the given
    ///     symbol using the currently selected attribute.
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="rune"></param>
    void FillRect (Rectangle rect, char rune);

    /// <summary>
    ///     Tests whether the specified coordinate is valid for drawing the specified Text.
    /// </summary>
    /// <param name="text">Used to determine if one or two columns are required.</param>
    /// <param name="col">The column.</param>
    /// <param name="row">The row.</param>
    /// <returns>
    ///     True if the coordinate is valid for the Text; false otherwise.
    /// </returns>
    bool IsValidLocation (string text, int col, int row);

    /// <summary>
    ///     The first cell index on left of screen - basically always 0.
    ///     Changing this may have unexpected consequences.
    /// </summary>
    int Left { get; set; }

    /// <summary>
    ///     Updates the column and row to the specified location in the buffer.
    /// </summary>
    /// <param name="col">The column to move to.</param>
    /// <param name="row">The row to move to.</param>
    void Move (int col, int row);

    /// <summary>
    ///     Adds or replaces a raster image command, capturing the current <see cref="Clip"/> for later composition.
    /// </summary>
    /// <param name="command">The raster image command to add.</param>
    void AddRasterImage (RasterImageCommand command);

    /// <summary>
    ///     Removes a raster image command from the output buffer.
    /// </summary>
    /// <param name="id">The raster image command identifier.</param>
    void RemoveRasterImage (string id);

    /// <summary>
    ///     Removes every raster image command whose <see cref="RasterImageCommand.Id"/> is not in
    ///     <paramref name="activeIds"/>, marking the cells they covered dirty so they are repainted.
    /// </summary>
    /// <remarks>
    ///     Raster graphics (Sixel/Kitty) are emitted out-of-band and persist on screen until explicitly
    ///     deleted, so they linger after the view that transmitted them is no longer rendered (hidden,
    ///     removed, or on a runnable that has ended). The framework calls this after each draw with the ids
    ///     of the images still owned by rendered views, releasing the rest. See <see cref="RemoveRasterImage"/>.
    /// </remarks>
    /// <param name="activeIds">The ids of raster images still owned by a rendered view.</param>
    void RetainRasterImages (IReadOnlyCollection<string> activeIds);

    /// <summary>
    ///     Gets the raster image commands currently held by the output buffer.
    /// </summary>
    /// <returns>The raster image commands.</returns>
    IReadOnlyList<RasterImageCommand> GetRasterImages ();

    /// <summary>
    ///     Gets the row last set by <see cref="Move"/>. <see cref="Col"/> and <see cref="Row"/> are used by
    ///     <see cref="AddRune(Rune)"/> and <see cref="AddStr"/> to determine where to add content.
    /// </summary>
    public int Row { get; }

    /// <summary>The number of rows visible in the terminal.</summary>
    int Rows { get; set; }

    /// <summary>
    ///     Gets or sets the URL that will be associated with cells added via <see cref="AddRune(Rune)"/> or <see cref="AddStr(string)"/>.
    ///     When set, subsequent cells will include this URL for OSC 8 hyperlink rendering.
    /// </summary>
    string? CurrentUrl { get; set; }

    /// <summary>
    ///     Gets the URL associated with the cell at the specified position.
    ///     Returns <see langword="null"/> if no URL is associated with the cell.
    /// </summary>
    /// <param name="col">The column position.</param>
    /// <param name="row">The row position.</param>
    /// <returns>The URL associated with the cell, or <see langword="null"/> if none exists.</returns>
    string? GetCellUrl (int col, int row);

    /// <summary>
    ///     Changes the size of the buffer to the given size
    /// </summary>
    /// <param name="cols"></param>
    /// <param name="rows"></param>
    void SetSize (int cols, int rows);

    /// <summary>
    ///     The first cell index on top of screen - basically always 0.
    ///     Changing this may have unexpected consequences.
    /// </summary>
    int Top { get; set; }

    /// <summary>
    ///     Sets the replacement character that will be used when a wide glyph (double-width character) cannot fit in the
    ///     available space.
    ///     If not set, the default will be <see cref="Glyphs.WideGlyphReplacement"/>.
    /// </summary>
    /// <param name="column1ReplacementChar">
    ///     The character used when the first column of a wide character is invalid (for example, when it is overlapped by the
    ///     trailing half of a previous wide character).
    /// </param>
    void SetWideGlyphReplacement (Rune column1ReplacementChar);
}
