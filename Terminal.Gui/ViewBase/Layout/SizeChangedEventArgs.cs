namespace Terminal.Gui.ViewBase;

/// <summary>Args for events about Size (e.g. Resized)</summary>
public class SizeChangedEventArgs : EventArgs
{
    /// <summary>Creates a new instance of the <see cref="SizeChangedEventArgs"/> class.</summary>
    /// <param name="size"></param>
    public SizeChangedEventArgs (Size? size) => Size = size;

    /// <summary>Set to <see langword="true"/> to cause the resize to be cancelled, if appropriate.</summary>
    public bool Cancel { get; set; }

    /// <summary>Gets the size the event describes.  This should reflect the new/current size after the event resolved.</summary>
    public Size? Size { get; }
}
