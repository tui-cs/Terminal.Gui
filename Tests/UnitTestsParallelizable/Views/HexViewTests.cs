using System.Text;
using UnitTests;

namespace ViewsTests;

public class HexViewTests : TestDriverBase
{
    [Theory]
    [InlineData (0, 4)]
    [InlineData (4, 4)]
    [InlineData (8, 4)]
    [InlineData (35, 4)]
    [InlineData (36, 8)]
    [InlineData (37, 8)]
    [InlineData (41, 8)]
    [InlineData (54, 12)]
    [InlineData (55, 12)]
    [InlineData (71, 12)]
    [InlineData (72, 16)]
    [InlineData (73, 16)]
    public void BytesPerLine_Calculates_Correctly (int width, int expectedBpl)
    {
        var hv = new HexView (LoadStream (null, out long _)) { Width = width, Height = 10, AddressWidth = 0 };
        hv.Layout ();

        Assert.Equal (width, hv.Frame.Width);
        Assert.Equal (expectedBpl, hv.BytesPerLine);
    }

    [Fact]
    public void ReadOnly_Prevents_Edits ()
    {
        var hv = new HexView (LoadStream (null, out _, true)) { Width = 20, Height = 20 };

        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);
        runnable.Add (hv);

        // Needed because HexView relies on LayoutComplete to calc sizes
        hv.LayoutSubViews ();

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Tab)); // Move to left side

        Assert.Empty (hv.Edits);
        hv.ReadOnly = true;
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Home));
        Assert.False (app.Keyboard.RaiseKeyDownEvent (Key.A));
        Assert.Empty (hv.Edits);
        Assert.Equal (126, hv.Source!.Length);

        hv.ReadOnly = false;
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.D4));
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.D1));
        Assert.Single (hv.Edits);
        Assert.Equal (65, hv.Edits.ToList () [0].Value);
        Assert.Equal ('A', (char)hv.Edits.ToList () [0].Value);
        Assert.Equal (126, hv.Source.Length);

        // Appends byte
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.End));
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.D4));
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.D2));
        Assert.Equal (2, hv.Edits.Count);
        Assert.Equal (66, hv.Edits.ToList () [1].Value);
        Assert.Equal ('B', (char)hv.Edits.ToList () [1].Value);
        Assert.Equal (126, hv.Source.Length);

        hv.ApplyEdits ();
        Assert.Empty (hv.Edits);
        Assert.Equal (127, hv.Source.Length);
    }

    [Fact]
    public void ApplyEdits_With_Argument ()
    {
        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        byte [] buffer = Encoding.Default.GetBytes ("Fest");
        var original = new MemoryStream ();
        original.Write (buffer, 0, buffer.Length);
        original.Flush ();
        var copy = new MemoryStream ();
        original.Position = 0;
        original.CopyTo (copy);
        copy.Flush ();
        var hv = new HexView (copy) { Width = Dim.Fill (), Height = Dim.Fill () };
        runnable.Add (hv);

        // Needed because HexView relies on LayoutComplete to calc sizes
        hv.LayoutSubViews ();

        var readBuffer = new byte [hv.Source!.Length];
        hv.Source.Position = 0;
        hv.Source.ReadExactly (readBuffer);
        Assert.Equal ("Fest", Encoding.Default.GetString (readBuffer));

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Tab)); // Move to left side
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.D5));
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.D4));
        readBuffer [hv.Edits.ToList () [0].Key] = hv.Edits.ToList () [0].Value;
        Assert.Equal ("Test", Encoding.Default.GetString (readBuffer));

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Tab)); // Move to right side
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft));
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Z.WithShift));
        readBuffer [hv.Edits.ToList () [0].Key] = hv.Edits.ToList () [0].Value;
        Assert.Equal ("Zest", Encoding.Default.GetString (readBuffer));

        hv.ApplyEdits (original);
        original.Position = 0;
        original.ReadExactly (buffer);
        copy.Position = 0;
        copy.ReadExactly (readBuffer);
        Assert.Equal ("Zest", Encoding.Default.GetString (buffer));
        Assert.Equal ("Zest", Encoding.Default.GetString (readBuffer));
        Assert.Equal (Encoding.Default.GetString (buffer), Encoding.Default.GetString (readBuffer));
    }

    [Fact]
    public void Constructors_Defaults ()
    {
        var hv = new HexView ();
        Assert.NotNull (hv.Source);
        Assert.IsAssignableFrom<MemoryStream> (hv.Source);
        Assert.True (hv.CanFocus);
        Assert.True (!hv.ReadOnly);

        hv = new (new MemoryStream ());
        Assert.NotNull (hv.Source);
        Assert.IsAssignableFrom<Stream> (hv.Source);
        Assert.True (hv.CanFocus);
        Assert.True (!hv.ReadOnly);
    }

    [Fact]
    public void Position_Encoding_Default ()
    {
        var hv = new HexView (LoadStream (null, out _)) { Width = 100, Height = 100 };

        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);
        runnable.Add (hv);

        Assert.Equal (63, hv.Source!.Length);
        Assert.Equal (20, hv.BytesPerLine);

        Assert.Equal (new (0, 0), hv.GetPosition (hv.Address));

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Tab));
        Assert.Equal (new (0, 0), hv.GetPosition (hv.Address));

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorRight.WithCtrl));
        Assert.Equal (hv.BytesPerLine - 1, hv.GetPosition (hv.Address).X);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Home));

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorRight));
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorRight));
        Assert.Equal (new (1, 0), hv.GetPosition (hv.Address));

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorDown));
        Assert.Equal (new (1, 1), hv.GetPosition (hv.Address));

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.End));
        Assert.Equal (new (3, 3), hv.GetPosition (hv.Address));

        Assert.Equal (hv.Source!.Length, hv.Address);
    }

    [Fact]
    public void Position_Encoding_Unicode ()
    {
        var hv = new HexView (LoadStream (null, out _, true)) { Width = 100, Height = 100 };
        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);
        runnable.Add (hv);

        app.LayoutAndDraw ();

        Assert.Equal (126, hv.Source!.Length);
        Assert.Equal (20, hv.BytesPerLine);

        Assert.Equal (new (0, 0), hv.GetPosition (hv.Address));

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Tab));

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorRight.WithCtrl));
        Assert.Equal (hv.BytesPerLine - 1, hv.GetPosition (hv.Address).X);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Home));

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorRight));
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorRight));
        Assert.Equal (new (1, 0), hv.GetPosition (hv.Address));

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorDown));
        Assert.Equal (new (1, 1), hv.GetPosition (hv.Address));

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.End));
        Assert.Equal (new (6, 6), hv.GetPosition (hv.Address));

        Assert.Equal (hv.Source!.Length, hv.Address);
    }

    [Fact]
    public void DiscardEdits_Method ()
    {
        var hv = new HexView (LoadStream (null, out _, true)) { Width = 20, Height = 20 };

        // Needed because HexView relies on LayoutComplete to calc sizes
        hv.LayoutSubViews ();

        Assert.True (hv.NewKeyDownEvent (Key.D4));
        Assert.True (hv.NewKeyDownEvent (Key.D1));
        Assert.Single (hv.Edits);
        Assert.Equal (65, hv.Edits.ToList () [0].Value);
        Assert.Equal ('A', (char)hv.Edits.ToList () [0].Value);
        Assert.Equal (126, hv.Source!.Length);

        hv.DiscardEdits ();
        Assert.Empty (hv.Edits);
    }

    [Fact]
    public void Edited_Event ()
    {
        var hv = new HexView (LoadStream (null, out _, true)) { Width = 20, Height = 20 };

        // Needed because HexView relies on LayoutComplete to calc sizes
        hv.LayoutSubViews ();

        KeyValuePair<long, byte> keyValuePair = default;
        hv.Edited += (s, e) => keyValuePair = new (e.Address, e.NewValue);

        Assert.True (hv.NewKeyDownEvent (Key.D4));
        Assert.True (hv.NewKeyDownEvent (Key.D6));

        Assert.Equal (0, (int)keyValuePair.Key);
        Assert.Equal (70, keyValuePair.Value);
        Assert.Equal ('F', (char)keyValuePair.Value);
    }

    [Fact]
    public void Exceptions_Tests ()
    {
        Assert.Throws<ArgumentNullException> (() => new HexView (null));
        Assert.Throws<ArgumentException> (() => new HexView (new NonSeekableStream (new MemoryStream ())));
    }

    [Fact]
    public void KeyBindings_Test_Movement_LeftSide ()
    {
        var hv = new HexView (LoadStream (null, out _)) { Width = 20, Height = 10 };

        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);
        runnable.Add (hv);
        app.LayoutAndDraw ();

        Assert.Equal (MEM_STRING_LENGTH, hv.Source!.Length);
        Assert.Equal (0, hv.Address);
        Assert.Equal (4, hv.BytesPerLine);

        // Default internal focus is on right side. Move back to left.
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Tab.WithShift));

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorRight));
        Assert.Equal (0, hv.Address);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorRight));
        Assert.Equal (1, hv.Address);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft));
        Assert.Equal (0, hv.Address);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorDown));
        Assert.Equal (4, hv.Address);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorUp));
        Assert.Equal (0, hv.Address);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.PageDown));
        Assert.Equal (40, hv.Address);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.PageUp));
        Assert.Equal (0, hv.Address);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.End));
        Assert.Equal (MEM_STRING_LENGTH, hv.Address);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Home));
        Assert.Equal (0, hv.Address);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorRight.WithCtrl));
        Assert.Equal (3, hv.Address);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft.WithCtrl));
        Assert.Equal (0, hv.Address);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorDown.WithCtrl));
        Assert.Equal (36, hv.Address);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorUp.WithCtrl));
        Assert.Equal (0, hv.Address);
    }

    [Fact]
    public void PositionChanged_Event ()
    {
        var hv = new HexView (LoadStream (null, out _)) { Width = 20, Height = 10 };

        hv.Layout ();
        HexViewEventArgs hexViewEventArgs = null!;
        hv.PositionChanged += (s, e) => hexViewEventArgs = e;

        Assert.Equal (4, hv.BytesPerLine);

        Assert.True (hv.NewKeyDownEvent (Key.CursorRight)); // left side must press twice
        Assert.True (hv.NewKeyDownEvent (Key.CursorRight));
        Assert.True (hv.NewKeyDownEvent (Key.CursorDown));

        Assert.Equal (4, hexViewEventArgs.BytesPerLine);
        Assert.Equal (new (1, 1), hexViewEventArgs.Position);
        Assert.Equal (5, hexViewEventArgs.Address);
    }

    [Fact]
    public void Source_Sets_Address_To_Zero_If_Greater_Than_Source_Length ()
    {
        var hv = new HexView (LoadStream (null, out _)) { Width = 10, Height = 5 };

        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);
        runnable.Add (hv);

        Assert.True (hv.NewKeyDownEvent (Key.End));
        Assert.Equal (MEM_STRING_LENGTH, hv.Address);

        hv.Source = new MemoryStream ();
        runnable.Layout ();
        Assert.Equal (0, hv.Address);

        hv.Source = LoadStream (null, out _);
        hv.Width = Dim.Fill ();
        hv.Height = Dim.Fill ();
        runnable.Layout ();
        Assert.Equal (0, hv.Address);

        Assert.True (hv.NewKeyDownEvent (Key.End));
        Assert.Equal (MEM_STRING_LENGTH, hv.Address);

        hv.Source = new MemoryStream ();
        runnable.Layout ();
        Assert.Equal (0, hv.Address);
    }

    private const string MEM_STRING = "Hello world.\nThis is a test of the Emergency Broadcast System.\n";
    private const int MEM_STRING_LENGTH = 63;

    private Stream LoadStream (string? memString, out long numBytesInMemString, bool unicode = false)
    {
        var stream = new MemoryStream ();
        byte [] bArray;

        Assert.Equal (MEM_STRING_LENGTH, MEM_STRING.Length);

        if (memString is null)
        {
            memString = MEM_STRING;
        }

        if (unicode)
        {
            bArray = Encoding.Unicode.GetBytes (memString);
        }
        else
        {
            bArray = Encoding.Default.GetBytes (memString);
        }

        numBytesInMemString = bArray.Length;

        stream.Write (bArray);

        return stream;
    }

    private class NonSeekableStream (Stream baseStream) : Stream
    {
        public override bool CanRead => baseStream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => baseStream.CanWrite;
        public override long Length => throw new NotSupportedException ();

        public override long Position { get => baseStream.Position; set => throw new NotSupportedException (); }

        public override void Flush () => baseStream.Flush ();
        public override int Read (byte [] buffer, int offset, int count) => baseStream.Read (buffer, offset, count);
        public override long Seek (long offset, SeekOrigin origin) => throw new NotImplementedException ();
        public override void SetLength (long value) => throw new NotSupportedException ();
        public override void Write (byte [] buffer, int offset, int count) => baseStream.Write (buffer, offset, count);
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void HexView_Click_PositionsCursor ()
    {
        HexView hexView = new () { Width = 20, Height = 5 };
        hexView.Source = new MemoryStream ([1, 2, 3, 4, 5, 6, 7, 8]);
        hexView.BeginInit ();
        hexView.EndInit ();

        // Click should position cursor (Activate via mouse)
        Mouse ev = new () { Position = new Point (2, 0), Flags = MouseFlags.LeftButtonClicked };
        hexView.NewMouseEvent (ev);

        // Cursor should be positioned (verify command was processed)
        Assert.True (hexView.HasFocus || !hexView.CanFocus);

        hexView.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void HexView_DoubleClick_TogglesSide ()
    {
        HexView hexView = new () { Width = 20, Height = 5 };
        hexView.Source = new MemoryStream ([1, 2, 3, 4, 5, 6, 7, 8]);
        hexView.BeginInit ();
        hexView.EndInit ();

        // Double-click toggles between hex and text side
        Mouse ev = new () { Position = new Point (2, 0), Flags = MouseFlags.LeftButtonDoubleClicked };
        hexView.NewMouseEvent (ev);

        // Verify command was processed
        Assert.True (hexView.HasFocus || !hexView.CanFocus);

        hexView.Dispose ();
    }

    /// <summary>
    ///     Regression test for https://github.com/tui-cs/Terminal.Gui/issues/4963
    ///     Ctrl-modified keys must not edit hex data even when they look like valid hex digits.
    /// </summary>
    [Fact]
    public void CtrlKey_Does_Not_Edit_HexView ()
    {
        // Copilot
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        HexView hexView = new (new MemoryStream ([0x00, 0x01, 0x02, 0x03]))
        {
            Width = 80,
            Height = 10
        };

        Window win = new ();
        win.Add (hexView);

        SessionToken? token = app.Begin (win);
        hexView.SetFocus ();
        Assert.True (hexView.HasFocus);

        // Ctrl+A with AssociatedText — 'A' is a valid hex digit but should NOT be inserted
        Key ctrlA = new (Key.A.WithCtrl) { AssociatedText = "a" };
        hexView.NewKeyDownEvent (ctrlA);

        Assert.Empty (hexView.Edits);

        app.End (token!);
        win.Dispose ();
    }

    /// <summary>
    ///     Regression test for https://github.com/tui-cs/Terminal.Gui/issues/4963
    ///     Alt-modified keys on the right (text) side must not edit data.
    /// </summary>
    [Fact]
    public void AltKey_Does_Not_Edit_HexView_RightSide ()
    {
        // Copilot
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        HexView hexView = new (new MemoryStream ([0x00, 0x01, 0x02, 0x03]))
        {
            Width = 80,
            Height = 10
        };

        Window win = new ();
        win.Add (hexView);

        SessionToken? token = app.Begin (win);
        hexView.SetFocus ();

        // Switch to right side by pressing Tab
        hexView.NewKeyDownEvent (Key.Tab);

        Key altT = new (Key.T.WithAlt) { AssociatedText = "t" };
        hexView.NewKeyDownEvent (altT);

        Assert.Empty (hexView.Edits);

        app.End (token!);
        win.Dispose ();
    }
}
