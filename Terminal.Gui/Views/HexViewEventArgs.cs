#nullable disable
//
// HexView.cs: A hexadecimal viewer
//
// TODO:
// - Support searching and highlighting of the search result
// - Bug showing the last line
// 

namespace Terminal.Gui.Views;

/// <summary>Defines the event arguments for <see cref="HexView.PositionChanged"/> event.</summary>
public class HexViewEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of <see cref="HexViewEventArgs"/></summary>
    /// <param name="address">The byte position in the steam.</param>
    /// <param name="position">The edit position.</param>
    /// <param name="lineLength">Line bytes length.</param>
    public HexViewEventArgs (long address, Point position, int lineLength)
    {
        Address = address;
        Position = position;
        BytesPerLine = lineLength;
    }

    /// <summary>The bytes length per line.</summary>
    public int BytesPerLine { get; private set; }

    /// <summary>Gets the current edit position.</summary>
    public Point Position { get; private set; }

    /// <summary>Gets the byte position in the <see cref="Stream"/>.</summary>
    public long Address { get; private set; }
}

/// <summary>Defines the event arguments for <see cref="HexView.Edited"/> event.</summary>
public class HexViewEditEventArgs : EventArgs
{
    /// <summary>Creates a new instance of the <see cref="HexViewEditEventArgs"/> class.</summary>
    /// <param name="address"></param>
    /// <param name="newValue"></param>
    public HexViewEditEventArgs (long address, byte newValue)
    {
        Address = address;
        NewValue = newValue;
    }

    /// <summary>Gets the new value for that <see cref="Address"/>.</summary>
    public byte NewValue { get; }

    /// <summary>Gets the address of the edit in the stream.</summary>
    public long Address { get; }
}
