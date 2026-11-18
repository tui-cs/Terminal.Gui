using System.Text.RegularExpressions;

namespace Terminal.Gui.Views;

/// <summary>Regex Provider for TextValidateField.</summary>
public class TextRegexProvider : ITextValidateProvider
{
    private List<Rune> _pattern = null!;
    private Regex _regex = null!;
    private List<Rune> _text = [];

    /// <summary>Empty Constructor.</summary>
    public TextRegexProvider (string pattern) => Pattern = pattern;

    /// <summary>Regex pattern property.</summary>
    public string Pattern
    {
        get => StringExtensions.ToString (_pattern);
        set
        {
            _pattern = value.ToRuneList ();
            CompileMask ();
            SetupText ();
        }
    }

    /// <summary>When true, validates with the regex pattern on each input, preventing the input if it's not valid.</summary>
    public bool ValidateOnInput { get; set; } = true;

    /// <inheritdoc/>
    public event EventHandler<EventArgs<string>>? TextChanged;

    /// <inheritdoc/>
    public string Text
    {
        get => StringExtensions.ToString (_text);
        set
        {
            _text = value != string.Empty ? value.ToRuneList () : [];
            SetupText ();
        }
    }

    /// <inheritdoc/>
    public string DisplayText => Text;

    /// <inheritdoc/>
    public bool IsValid => Validate (_text);

    /// <inheritdoc/>
    public bool Fixed => false;

    /// <inheritdoc/>
    public int Cursor (int pos)
    {
        if (pos < 0)
        {
            return CursorStart ();
        }

        if (pos >= _text.Count)
        {
            return CursorEnd ();
        }

        return pos;
    }

    /// <inheritdoc/>
    public int CursorStart () => 0;

    /// <inheritdoc/>
    public int CursorEnd () => _text.Count;

    /// <inheritdoc/>
    public int CursorLeft (int pos)
    {
        if (pos > 0)
        {
            return pos - 1;
        }

        return pos;
    }

    /// <inheritdoc/>
    public int CursorRight (int pos)
    {
        if (pos < _text.Count)
        {
            return pos + 1;
        }

        return pos;
    }

    /// <inheritdoc/>
    public bool Delete (int pos)
    {
        if (_text.Count <= 0 || pos >= _text.Count)
        {
            return true;
        }
        string oldValue = Text;
        _text.RemoveAt (pos);
        RaiseTextChanged (new EventArgs<string> (in oldValue));

        return true;
    }

    /// <inheritdoc/>
    public bool InsertAt (char ch, int pos)
    {
        List<Rune> aux = _text.ToList ();
        aux.Insert (pos, (Rune)ch);

        if (!Validate (aux) && ValidateOnInput)
        {
            return false;
        }
        string oldValue = Text;
        _text.Insert (pos, (Rune)ch);
        RaiseTextChanged (new EventArgs<string> (in oldValue));

        return true;
    }

    /// <summary>
    ///     Called when the text has changed. Subclasses can override this to perform custom actions.
    /// </summary>
    /// <param name="args">Contains the event data representing the new text value.</param>
    public virtual void OnTextChanged (EventArgs<string> args) { }

    /// <summary>
    ///     Raises the <see cref="TextChanged"/> event.
    /// </summary>
    /// <param name="args">Contains the event data representing the new text value.</param>
    private void RaiseTextChanged (EventArgs<string> args)
    {
        OnTextChanged (args);
        TextChanged?.Invoke (this, args);
    }

    /// <summary>Compiles the regex pattern for validation./></summary>
    private void CompileMask () => _regex = new Regex (StringExtensions.ToString (_pattern), RegexOptions.Compiled);

    private void SetupText ()
    {
        if (IsValid)
        {
            return;
        }

        _text = [];
    }

    private bool Validate (List<Rune> text)
    {
        Match match = _regex.Match (StringExtensions.ToString (text));

        return match.Success;
    }
}
