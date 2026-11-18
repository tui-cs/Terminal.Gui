namespace Terminal.Gui.Drivers;

/// <summary>
///     Implements a fake clipboard for testing purposes.
/// </summary>
public class FakeClipboard : ClipboardBase
{
    /// <summary>
    ///     Gets or sets an exception to be thrown by clipboard operations.
    /// </summary>
    public Exception? FakeException { get; set; }

    private readonly bool _isSupportedAlwaysFalse;
    private string _contents = string.Empty;

    /// <summary>
    ///     Constructs a new instance of <see cref="FakeClipboard"/>.
    /// </summary>
    /// <param name="fakeClipboardThrowsNotSupportedException"></param>
    /// <param name="isSupportedAlwaysFalse"></param>
    public FakeClipboard (
        bool fakeClipboardThrowsNotSupportedException = false,
        bool isSupportedAlwaysFalse = false
    )
    {
        _isSupportedAlwaysFalse = isSupportedAlwaysFalse;

        if (fakeClipboardThrowsNotSupportedException)
        {
            FakeException = new NotSupportedException ("Fake clipboard exception");
        }
    }

    /// <inheritdoc />
    public override bool IsSupported => !_isSupportedAlwaysFalse;

    /// <inheritdoc />
    protected override string GetClipboardDataImpl ()
    {
        if (FakeException is { })
        {
            throw FakeException;
        }

        return _contents;
    }

    /// <inheritdoc />
    protected override void SetClipboardDataImpl (string? text)
    {
        if (FakeException is { })
        {
            throw FakeException;
        }

        _contents = text ?? throw new ArgumentNullException (nameof (text));
    }
}
