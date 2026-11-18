using System.Globalization;

namespace Terminal.Gui.Views;

/// <summary>
///     Date input provider for <see cref="TextValidateField"/>.
///     Provides date editing with culture-aware formatting.
/// </summary>
/// <remarks>
///     <para>
///         This provider parses the <see cref="DateTimeFormatInfo.ShortDatePattern"/> to determine:
///         <list type="bullet">
///             <item>
///                 <description>Field order (year, month, day) based on culture</description>
///             </item>
///             <item>
///                 <description>Date separator character</description>
///             </item>
///             <item>
///                 <description>Dynamic field width based on pattern</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         The cursor automatically skips over separator characters during navigation.
///         Date values are auto-corrected to valid ranges (e.g., day clamped to days-in-month).
///     </para>
/// </remarks>
public class DateTextProvider : ITextValidateProvider
{
    private DateTimeFormatInfo _format = CultureInfo.CurrentCulture.DateTimeFormat;
    private string _separator = CultureInfo.CurrentCulture.DateTimeFormat.DateSeparator;
    private string _normalizedPattern = string.Empty;
    private DateTime _dateValue = DateTime.Today;
    private int _fieldLength;
    private readonly HashSet<int> _separatorPositions = [];

    /// <summary>
    ///     Initializes a new instance of the <see cref="DateTextProvider"/> class.
    /// </summary>
    public DateTextProvider () => AnalyzePattern ();

    /// <summary>
    ///     Gets or sets the <see cref="DateTimeFormatInfo"/> used for date formatting.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The provider uses <see cref="DateTimeFormatInfo.ShortDatePattern"/> to determine the display format.
    ///         Users can customize patterns by cloning the DateTimeFormatInfo and modifying ShortDatePattern.
    ///     </para>
    ///     <para>
    ///         The width automatically adjusts when the format changes to accommodate the new pattern.
    ///     </para>
    /// </remarks>
    public DateTimeFormatInfo Format
    {
        get => _format;
        set
        {
            _format = value;
            _separator = CleanSeparator (value.DateSeparator);
            AnalyzePattern ();
            RaiseTextChanged (new EventArgs<string> (in string.Empty));
        }
    }

    /// <summary>
    ///     Gets or sets the current date value.
    /// </summary>
    public DateTime DateValue
    {
        get => _dateValue;
        set => _dateValue = value;
    }

    /// <inheritdoc/>
    public event EventHandler<EventArgs<string>>? TextChanged;

    /// <inheritdoc/>
    public string Text
    {
        get => FormatDateValue ();
        set
        {
            if (!TryParseDateValue (value, out DateTime parsedValue))
            {
                return;
            }
            string oldValue = Text;
            _dateValue = parsedValue;

            if (oldValue != Text)
            {
                RaiseTextChanged (new EventArgs<string> (in oldValue));
            }
        }
    }

    /// <inheritdoc/>
    public string DisplayText => FormatDateValue ();

    /// <inheritdoc/>
    public bool IsValid =>

        // Always valid - we autocorrect invalid values
        true;

    /// <inheritdoc/>
    public bool Fixed => true;

    /// <inheritdoc/>
    public int Cursor (int pos)
    {
        if (pos < 0)
        {
            return CursorStart ();
        }

        if (pos >= _fieldLength)
        {
            return CursorEnd ();
        }

        // Skip over separators
        if (_separatorPositions.Contains (pos))
        {
            return CursorRight (pos);
        }

        return pos;
    }

    /// <inheritdoc/>
    public int CursorStart () => 0;

    /// <inheritdoc/>
    public int CursorEnd () => _fieldLength - 1;

    /// <inheritdoc/>
    public int CursorLeft (int pos)
    {
        if (pos <= 0)
        {
            return 0;
        }

        int newPos = pos - 1;

        // Skip over separators
        while (newPos >= 0 && _separatorPositions.Contains (newPos))
        {
            newPos--;
        }

        return newPos < 0 ? 0 : newPos;
    }

    /// <inheritdoc/>
    public int CursorRight (int pos)
    {
        if (pos >= _fieldLength - 1)
        {
            return CursorEnd ();
        }

        int newPos = pos + 1;

        // Skip over separators
        while (newPos < _fieldLength && _separatorPositions.Contains (newPos))
        {
            newPos++;
        }

        return newPos >= _fieldLength ? CursorEnd () : newPos;
    }

    /// <inheritdoc/>
    public bool Delete (int pos)
    {
        string oldValue = Text;

        // Replace digit with '0'
        string currentText = FormatDateValue ();

        if (pos < 0 || pos >= currentText.Length || !char.IsDigit (currentText [pos]))
        {
            return false;
        }
        StringBuilder sb = new (currentText) { [pos] = '0' };

        if (!TryParseDateValue (sb.ToString (), out DateTime newValue))
        {
            return false;
        }
        _dateValue = newValue;
        RaiseTextChanged (new EventArgs<string> (in oldValue));

        return true;
    }

    /// <inheritdoc/>
    public bool InsertAt (char ch, int pos)
    {
        string oldValue = Text;

        // Only accept digits for date positions
        if (!char.IsDigit (ch))
        {
            return false;
        }

        // Replace digit at position
        string currentText = FormatDateValue ();

        if (pos < 0 || pos >= currentText.Length)
        {
            return false;
        }
        StringBuilder sb = new (currentText) { [pos] = ch };

        if (!TryParseDateValue (sb.ToString (), out DateTime newValue))
        {
            return false;
        }
        _dateValue = newValue;
        RaiseTextChanged (new EventArgs<string> (in oldValue));

        return true;
    }

    /// <summary>
    ///     Called when the text has changed. Subclasses can override this to perform custom actions.
    /// </summary>
    /// <param name="args">An <see cref="EventArgs{T}"/> object that contains the event data for the text change.</param>
    public virtual void OnTextChanged (EventArgs<string> args) { }

    /// <summary>
    ///     Raises the <see cref="TextChanged"/> event.
    /// </summary>
    /// <param name="args">An <see cref="EventArgs{T}"/> object that contains the event data for the text change.</param>
    private void RaiseTextChanged (EventArgs<string> args)
    {
        OnTextChanged (args);
        TextChanged?.Invoke (this, args);
    }

    /// <summary>
    ///     Analyzes the ShortDatePattern to detect format characteristics.
    /// </summary>
    private void AnalyzePattern ()
    {
        string pattern = _format.ShortDatePattern;
        _separatorPositions.Clear ();

        // Clean separator (strip RTL marks etc.)
        _separator = CleanSeparator (_format.DateSeparator);

        // Normalize to 2-digit day/month and 4-digit year for consistent fixed-width fields
        _normalizedPattern = NormalizePattern (pattern);

        // Build a sample date to determine field positions
        DateTime sampleDate = new (2024, 11, 25);
        var formatted = sampleDate.ToString (_normalizedPattern, _format);

        _fieldLength = formatted.Length;

        // Find separator positions
        for (var i = 0; i < formatted.Length; i++)
        {
            if (formatted [i].ToString () == _separator)
            {
                _separatorPositions.Add (i);
            }
        }
    }

    /// <summary>
    ///     Normalizes the date pattern to always use 2-digit day/month and 4-digit year
    ///     so that field positions are consistent regardless of the current date value.
    /// </summary>
    internal static string NormalizePattern (string pattern)
    {
        // Strip any era designator prefix/suffix (e.g., "g yyyy/M/d" → "yyyy/M/d")
        pattern = pattern.Replace ("g ", "").Replace (" g", "").Trim ();

        // Strip any trailing literal text (e.g., "d.MM.yyyy '?'." or "d. M. yyyy.")
        int quoteIdx = pattern.IndexOf ('\'');

        if (quoteIdx >= 0)
        {
            pattern = pattern [..quoteIdx].Trim ();
        }

        // Remove trailing dots that are not separators (e.g., "d.M.yyyy." → "d.M.yyyy")
        pattern = pattern.TrimEnd ('.');

        // Normalize spacing around separators (e.g., "d. M. yyyy" → "d.M.yyyy")
        pattern = pattern.Replace (" ", "");

        // Normalize day
        if (!pattern.Contains ("dd") && pattern.Contains ('d'))
        {
            pattern = pattern.Replace ("d", "dd");
        }

        // Normalize month
        if (!pattern.Contains ("MM") && pattern.Contains ('M'))
        {
            pattern = pattern.Replace ("M", "MM");
        }

        // Normalize year to 4 digits
        if (!pattern.Contains ("yyyy") && pattern.Contains ("yy"))
        {
            pattern = pattern.Replace ("yy", "yyyy");
        }
        else if (!pattern.Contains ("yyyy") && pattern.Contains ('y'))
        {
            pattern = pattern.Replace ("y", "yyyy");
        }

        // Handle "d?" pattern used by some cultures
        pattern = pattern.Replace ("dd?", "dd").Replace ("MM?", "MM");

        return pattern;
    }

    /// <summary>
    ///     Cleans the date separator by stripping RTL marks and other non-separator characters.
    /// </summary>
    private static string CleanSeparator (string separator)
    {
        string cleaned = separator.Trim ().Replace ("\u200f", "");

        if (cleaned.Length <= 1)
        {
            return cleaned;
        }

        // Take only the first non-whitespace character
        foreach (char c in cleaned)
        {
            if (!char.IsWhiteSpace (c))
            {
                return c.ToString ();
            }
        }

        return cleaned;
    }

    /// <summary>
    ///     Formats the current date value according to the pattern.
    /// </summary>
    private string FormatDateValue () => _dateValue.ToString (_normalizedPattern, _format);

    /// <summary>
    ///     Attempts to parse a date string according to the pattern.
    /// </summary>
    private bool TryParseDateValue (string text, out DateTime result)
    {
        result = DateTime.MinValue;

        if (string.IsNullOrWhiteSpace (text))
        {
            return false;
        }

        text = text.Trim ();

        // Try to parse using the current pattern
        if (!DateTime.TryParseExact (text, _normalizedPattern, _format, DateTimeStyles.None, out DateTime dt))
        {
            return TryManualParse (text, out result);
        }
        result = dt;

        return true;

        // Fallback: try manual parsing for partial/invalid input
    }

    /// <summary>
    ///     Manual parsing for partially entered or invalid date values.
    /// </summary>
    private bool TryManualParse (string text, out DateTime result)
    {
        result = DateTime.MinValue;

        try
        {
            string [] parts = text.Split (_separator [0]);

            if (parts.Length < 3)
            {
                return false;
            }

            // Determine field order from normalized pattern
            string [] patternParts = _normalizedPattern.Split (_separator [0]);

            if (patternParts.Length < 3)
            {
                return false;
            }

            var year = 1;
            var month = 1;
            var day = 1;

            for (var i = 0; i < patternParts.Length && i < parts.Length; i++)
            {
                if (!int.TryParse (parts [i], out int val))
                {
                    continue;
                }

                if (patternParts [i].Contains ('y'))
                {
                    year = Math.Max (1, Math.Min (9999, val));
                }
                else if (patternParts [i].Contains ('M'))
                {
                    month = Math.Max (1, Math.Min (12, val));
                }
                else if (patternParts [i].Contains ('d'))
                {
                    day = Math.Max (1, val);
                }
            }

            // Clamp day to valid range for the given month/year
            int maxDay = DateTime.DaysInMonth (Math.Max (1, year), month);
            day = Math.Min (day, maxDay);

            result = new DateTime (year, month, day);

            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}
