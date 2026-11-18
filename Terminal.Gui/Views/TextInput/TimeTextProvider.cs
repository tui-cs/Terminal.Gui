using System.Globalization;

namespace Terminal.Gui.Views;

/// <summary>
///     Time input provider for <see cref="TextValidateField"/>.
///     Provides time editing with culture-aware formatting, supporting both 12-hour and 24-hour formats.
/// </summary>
/// <remarks>
///     <para>
///         This provider parses the <see cref="DateTimeFormatInfo.LongTimePattern"/> to determine:
///         <list type="bullet">
///             <item>
///                 <description>12-hour (h/hh) vs 24-hour (H/HH) format</description>
///             </item>
///             <item>
///                 <description>Presence of AM/PM designator (tt)</description>
///             </item>
///             <item>
///                 <description>Time separator character</description>
///             </item>
///             <item>
///                 <description>Dynamic field width based on pattern</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         The cursor automatically skips over separator characters and AM/PM designators during navigation.
///         For 12-hour formats, typing 'A' or 'P' on the AM/PM position toggles between AM and PM.
///     </para>
/// </remarks>
public class TimeTextProvider : ITextValidateProvider
{
    // Constants for DateTime construction to avoid magic numbers
    private const int BASE_YEAR = 2000;
    private const int BASE_MONTH = 1;
    private const int BASE_DAY = 1;
    private const int SAMPLE_HOUR = 14;
    private const int SAMPLE_MINUTE = 30;
    private const int SAMPLE_SECOND = 45;

    private DateTimeFormatInfo _format = CultureInfo.CurrentCulture.DateTimeFormat;
    private string _separator = CultureInfo.CurrentCulture.DateTimeFormat.TimeSeparator;
    private string _normalizedPattern = string.Empty;
    private TimeSpan _timeValue = TimeSpan.Zero;
    private bool _is12Hour;
    private bool _hasAmPm;
    private int _fieldLength;
    private readonly HashSet<int> _separatorPositions = [];
    private int _amPmPosition = -1;
    private bool _isPm;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TimeTextProvider"/> class.
    /// </summary>
    public TimeTextProvider () => AnalyzePattern ();

    /// <summary>
    ///     Gets or sets the <see cref="DateTimeFormatInfo"/> used for time formatting.
    /// </summary>
    /// <remarks>
    ///     The provider uses <see cref="DateTimeFormatInfo.LongTimePattern"/> to determine the display format.
    ///     Users can customize patterns by cloning the DateTimeFormatInfo and modifying LongTimePattern.
    /// </remarks>
    public DateTimeFormatInfo Format
    {
        get => _format;
        set
        {
            _format = value;
            _separator = value.TimeSeparator;
            AnalyzePattern ();
            RaiseTextChanged (new EventArgs<string> (in string.Empty));
        }
    }

    /// <summary>
    ///     Gets or sets the current time value.
    /// </summary>
    public TimeSpan TimeValue
    {
        get => _timeValue;
        set
        {
            _timeValue = value;
            _isPm = value.Hours >= 12;
        }
    }

    /// <inheritdoc/>
    public event EventHandler<EventArgs<string>>? TextChanged;

    /// <inheritdoc/>
    public string Text
    {
        get => FormatTimeValue ();
        set
        {
            if (!TryParseTimeValue (value, out TimeSpan parsedValue))
            {
                return;
            }
            string oldValue = Text;
            _timeValue = parsedValue;
            _isPm = _timeValue.Hours >= 12;

            if (oldValue != Text)
            {
                RaiseTextChanged (new EventArgs<string> (in oldValue));
            }
        }
    }

    /// <inheritdoc/>
    public string DisplayText => FormatTimeValue ();

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

        // Skip over separators and AM/PM designator
        if (_separatorPositions.Contains (pos))
        {
            return CursorRight (pos);
        }

        if (_hasAmPm && pos >= _amPmPosition && pos < _amPmPosition + 2)
        {
            return _amPmPosition;
        }

        return pos;
    }

    /// <inheritdoc/>
    public int CursorStart () => 0;

    /// <inheritdoc/>
    public int CursorEnd ()
    {
        if (_hasAmPm)
        {
            return _amPmPosition;
        }

        return _fieldLength - 1;
    }

    /// <inheritdoc/>
    public int CursorLeft (int pos)
    {
        if (pos <= 0)
        {
            return 0;
        }

        int newPos = pos - 1;

        // Skip over AM/PM designator
        if (_hasAmPm && newPos >= _amPmPosition && newPos < _amPmPosition + 2)
        {
            newPos = _amPmPosition - 1;
        }

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
        if (_hasAmPm && pos >= _amPmPosition)
        {
            return _amPmPosition;
        }

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

        // Stop at AM/PM position
        if (_hasAmPm && newPos >= _amPmPosition)
        {
            return _amPmPosition;
        }

        return newPos >= _fieldLength ? CursorEnd () : newPos;
    }

    /// <inheritdoc/>
    public bool Delete (int pos)
    {
        string oldValue = Text;

        if (_hasAmPm && pos == _amPmPosition)
        {
            // Can't delete AM/PM, just ignore
            return false;
        }

        // Replace digit with '0'
        string currentText = FormatTimeValue ();

        if (pos < 0 || pos >= currentText.Length || !char.IsDigit (currentText [pos]))
        {
            return false;
        }
        StringBuilder sb = new (currentText) { [pos] = '0' };

        if (!TryParseTimeValue (sb.ToString (), out TimeSpan newValue))
        {
            return false;
        }
        _timeValue = newValue;
        RaiseTextChanged (new EventArgs<string> (in oldValue));

        return true;
    }

    /// <inheritdoc/>
    public bool InsertAt (char ch, int pos)
    {
        string oldValue = Text;

        // Handle AM/PM toggle
        if (_hasAmPm && pos == _amPmPosition)
        {
            if (char.ToUpperInvariant (ch) != 'A' && char.ToUpperInvariant (ch) != 'P')
            {
                return false;
            }
            _isPm = char.ToUpperInvariant (ch) == 'P';

            // Update the time value hours to reflect AM/PM change
            int hours = _timeValue.Hours % 12;

            if (_isPm && hours < 12)
            {
                hours += 12;
            }

            _timeValue = new TimeSpan (hours, _timeValue.Minutes, _timeValue.Seconds);
            RaiseTextChanged (new EventArgs<string> (in oldValue));

            return true;
        }

        // Only accept digits for time positions
        if (!char.IsDigit (ch))
        {
            return false;
        }

        // Replace digit at position
        string currentText = FormatTimeValue ();

        if (pos < 0 || pos >= currentText.Length)
        {
            return false;
        }
        StringBuilder sb = new (currentText) { [pos] = ch };

        if (!TryParseTimeValue (sb.ToString (), out TimeSpan newValue))
        {
            return false;
        }
        _timeValue = newValue;
        RaiseTextChanged (new EventArgs<string> (in oldValue));

        return true;
    }

    /// <summary>
    ///     Called when the text has changed. Subclasses can override this to perform custom actions.
    /// </summary>
    /// <param name="args">Contains the event data for the text change.</param>
    public virtual void OnTextChanged (EventArgs<string> args) { }

    /// <summary>
    ///     Raises the <see cref="TextChanged"/> event.
    /// </summary>
    /// <param name="args">Contains the event data for the text change.</param>
    private void RaiseTextChanged (EventArgs<string> args)
    {
        OnTextChanged (args);
        TextChanged?.Invoke (this, args);
    }

    /// <summary>
    ///     Analyzes the LongTimePattern to detect format characteristics.
    /// </summary>
    private void AnalyzePattern ()
    {
        string pattern = _format.LongTimePattern;
        _separatorPositions.Clear ();
        _amPmPosition = -1;
        _is12Hour = pattern.Contains ('h');
        _hasAmPm = pattern.Contains ("tt");

        // Normalize to 2-digit specifiers for consistent fixed-width fields
        _normalizedPattern = NormalizePattern (pattern);

        // Build a sample time to determine field positions
        DateTime sampleTime = new (BASE_YEAR, BASE_MONTH, BASE_DAY, SAMPLE_HOUR, SAMPLE_MINUTE, SAMPLE_SECOND);
        var formatted = sampleTime.ToString (_normalizedPattern, _format);

        _fieldLength = formatted.Length;

        // Find separator positions
        for (var i = 0; i < formatted.Length; i++)
        {
            if (formatted [i].ToString () == _separator)
            {
                _separatorPositions.Add (i);
            }
        }

        // Find AM/PM position
        if (!_hasAmPm)
        {
            return;
        }
        string amDesignator = _format.AMDesignator;
        string pmDesignator = _format.PMDesignator;

        int amIndex = formatted.IndexOf (amDesignator, StringComparison.Ordinal);
        int pmIndex = formatted.IndexOf (pmDesignator, StringComparison.Ordinal);

        _amPmPosition = Math.Max (amIndex, pmIndex);
    }

    /// <summary>
    ///     Normalizes the time pattern to always use 2-digit specifiers (e.g. h → hh, m → mm)
    ///     so that field positions are consistent regardless of the current time value.
    /// </summary>
    private static string NormalizePattern (string pattern)
    {
        if (!pattern.Contains ("hh") && pattern.Contains ('h'))
        {
            pattern = pattern.Replace ("h", "hh");
        }

        if (!pattern.Contains ("HH") && pattern.Contains ('H'))
        {
            pattern = pattern.Replace ("H", "HH");
        }

        if (!pattern.Contains ("mm") && pattern.Contains ('m'))
        {
            pattern = pattern.Replace ("m", "mm");
        }

        if (!pattern.Contains ("ss") && pattern.Contains ('s'))
        {
            pattern = pattern.Replace ("s", "ss");
        }

        return pattern;
    }

    /// <summary>
    ///     Formats the current time value according to the pattern.
    /// </summary>
    private string FormatTimeValue ()
    {
        DateTime dt = DateTime.Today.Add (_timeValue);

        // For 12-hour format, adjust the hours if needed
        if (!_is12Hour || !_hasAmPm)
        {
            return dt.ToString (_normalizedPattern, _format);
        }
        int hours = _timeValue.Hours % 12;

        if (hours == 0)
        {
            hours = 12;
        }

        // Convert to 24-hour format for DateTime construction
        int hours24;

        if (_isPm)
        {
            hours24 = hours == 12 ? 12 : hours + 12;
        }
        else
        {
            hours24 = hours == 12 ? 0 : hours;
        }

        dt = new DateTime (BASE_YEAR, BASE_MONTH, BASE_DAY, hours24, _timeValue.Minutes, _timeValue.Seconds);

        return dt.ToString (_normalizedPattern, _format);
    }

    /// <summary>
    ///     Attempts to parse a time string according to the pattern.
    /// </summary>
    private bool TryParseTimeValue (string text, out TimeSpan result)
    {
        result = TimeSpan.Zero;

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
        result = dt.TimeOfDay;

        return true;

        // Fallback: try manual parsing for partial/invalid input
    }

    /// <summary>
    ///     Manual parsing for partially entered or invalid time values.
    /// </summary>
    private bool TryManualParse (string text, out TimeSpan result)
    {
        result = TimeSpan.Zero;

        try
        {
            string [] parts = text.Split (_separator [0]);

            if (parts.Length < 2)
            {
                return false;
            }

            // Extract AM/PM if present
            var isPm = false;
            string lastPart = parts [^1].Trim ();

            if (_hasAmPm)
            {
                if (lastPart.EndsWith (_format.PMDesignator, StringComparison.OrdinalIgnoreCase))
                {
                    isPm = true;
                    lastPart = lastPart [..^_format.PMDesignator.Length].Trim ();
                }
                else if (lastPart.EndsWith (_format.AMDesignator, StringComparison.OrdinalIgnoreCase))
                {
                    isPm = false;
                    lastPart = lastPart [..^_format.AMDesignator.Length].Trim ();
                }

                parts [^1] = lastPart;
            }

            // Parse hours
            if (!int.TryParse (parts [0], out int hours))
            {
                return false;
            }

            // Parse minutes
            if (!int.TryParse (parts [1], out int minutes))
            {
                return false;
            }

            // Parse seconds (if present)
            var seconds = 0;

            if (parts.Length > 2 && !string.IsNullOrWhiteSpace (parts [2]))
            {
                if (!int.TryParse (parts [2], out seconds))
                {
                    return false;
                }
            }

            // Validate and adjust ranges
            if (_is12Hour)
            {
                // 12-hour format: 1-12
                hours = Math.Max (1, Math.Min (12, hours));

                if (isPm && hours != 12)
                {
                    hours += 12;
                }
                else if (!isPm && hours == 12)
                {
                    hours = 0;
                }
            }
            else
            {
                // 24-hour format: 0-23
                hours = Math.Max (0, Math.Min (23, hours));
            }

            minutes = Math.Max (0, Math.Min (59, minutes));
            seconds = Math.Max (0, Math.Min (59, seconds));

            result = new TimeSpan (hours, minutes, seconds);
            _isPm = hours >= 12;

            return true;
        }
        catch (ArgumentException)
        {
            // TimeSpan constructor can throw ArgumentOutOfRangeException
            return false;
        }
        catch (FormatException)
        {
            // String parsing operations can throw FormatException
            return false;
        }
        catch (OverflowException)
        {
            // Arithmetic operations can throw OverflowException
            return false;
        }
    }
}
