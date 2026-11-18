using System.ComponentModel;

namespace Terminal.Gui.Views;

/// <summary>
///     .Net MaskedTextProvider Provider for TextValidateField.
///     <para></para>
///     <para>
///         <a
///             href="https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.maskedtextprovider?view=net-5.0">
///             Wrapper around MaskedTextProvider
///         </a>
///     </para>
///     <para>
///         <a
///             href="https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.maskedtextbox.mask?view=net-5.0">
///             Masking elements
///         </a>
///     </para>
/// </summary>
public class NetMaskedTextProvider : ITextValidateProvider
{
    private MaskedTextProvider? _provider;

    /// <summary>Empty Constructor</summary>
    public NetMaskedTextProvider (string mask) => Mask = mask;

    /// <summary>Mask property</summary>
    public string Mask
    {
        get => _provider!.Mask;
        set
        {
            string current = _provider != null ? _provider.ToString (false, false) : string.Empty;
            _provider = new MaskedTextProvider (value == string.Empty ? "&&&&&&" : value);

            if (!string.IsNullOrEmpty (current))
            {
                _provider.Set (current);
            }
        }
    }

    /// <inheritdoc/>
    public event EventHandler<EventArgs<string>>? TextChanged;

    /// <inheritdoc/>
    public string Text { get => _provider!.ToString (); set => _provider!.Set (value); }

    /// <inheritdoc/>
    public bool IsValid => _provider!.MaskCompleted;

    /// <inheritdoc/>
    public bool Fixed => true;

    /// <inheritdoc/>
    public string DisplayText => _provider!.ToDisplayString ();

    /// <inheritdoc/>
    public int Cursor (int pos)
    {
        if (pos < 0)
        {
            return CursorStart ();
        }

        if (pos > _provider!.Length)
        {
            return CursorEnd ();
        }

        int p = _provider.FindEditPositionFrom (pos, false);

        if (p == -1)
        {
            p = _provider.FindEditPositionFrom (pos, true);
        }

        return p;
    }

    /// <inheritdoc/>
    public int CursorStart () => _provider!.IsEditPosition (0) ? 0 : _provider.FindEditPositionFrom (0, true);

    /// <inheritdoc/>
    public int CursorEnd () =>
        _provider!.IsEditPosition (_provider.Length - 1) ? _provider.Length - 1 : _provider.FindEditPositionFrom (_provider.Length, false);

    /// <inheritdoc/>
    public int CursorLeft (int pos)
    {
        int c = _provider!.FindEditPositionFrom (pos - 1, false);

        return c == -1 ? pos : c;
    }

    /// <inheritdoc/>
    public int CursorRight (int pos)
    {
        int c = _provider!.FindEditPositionFrom (pos + 1, true);

        return c == -1 ? pos : c;
    }

    /// <inheritdoc/>
    public bool Delete (int pos)
    {
        string oldValue = Text;
        bool result = _provider!.Replace (' ', pos); // .RemoveAt (pos);

        if (result)
        {
            RaiseTextChanged (new EventArgs<string> (in oldValue));
        }

        return result;
    }

    /// <inheritdoc/>
    public bool InsertAt (char ch, int pos)
    {
        string oldValue = Text;
        bool result = _provider!.Replace (ch, pos);

        if (result)
        {
            RaiseTextChanged (new EventArgs<string> (in oldValue));
        }

        return result;
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
}
