namespace Terminal.Gui.Drivers;

/// <summary>
///     The <see cref="KeyCode"/> enumeration encodes key information from <see cref="IDriver"/>s and provides a
///     consistent way for application code to specify keys and receive key events.
///     <para>
///         The <see cref="Key"/> class provides a higher-level abstraction, with helper methods and properties for
///         common operations. For example, <see cref="Key.IsAlt"/> and <see cref="Key.IsCtrl"/> provide a convenient way
///         to check whether the Alt or Ctrl modifier keys were pressed when a key was pressed.
///     </para>
/// </summary>
/// <remarks>
///     <para>
///         Lowercase alpha keys are encoded as values between 65 and 90 corresponding to the un-shifted A to Z keys on a
///         keyboard. Enum values are provided for these (e.g. <see cref="KeyCode.A"/>, <see cref="KeyCode.B"/>, etc.).
///         Even though the values are the same as the ASCII values for uppercase characters, these enum values represent
///         *lowercase*, un-shifted characters.
///     </para>
///     <para>
///         Numeric keys are the values between 48 and 57 corresponding to 0 to 9 (e.g. <see cref="KeyCode.D0"/>,
///         <see cref="KeyCode.D1"/>, etc.).
///     </para>
///     <para>
///         The shift modifiers (<see cref="KeyCode.ShiftMask"/>, <see cref="KeyCode.CtrlMask"/>, and
///         <see cref="KeyCode.AltMask"/>) can be combined (with logical or) with the other key codes to represent shifted
///         keys. For example, the <see cref="KeyCode.A"/> enum value represents the un-shifted 'a' key, while
///         <see cref="KeyCode.ShiftMask"/> | <see cref="KeyCode.A"/> represents the 'A' key (shifted 'a' key). Likewise,
///         <see cref="KeyCode.AltMask"/> | <see cref="KeyCode.A"/> represents the 'Alt+A' key combination.
///     </para>
///     <para>
///         All other keys that produce a printable character are encoded as the Unicode value of the character. For
///         example, the <see cref="KeyCode"/> for the '!' character is 33, which is the Unicode value for '!'. Likewise,
///         `â` is 226, `Â` is 194, etc.
///     </para>
///     <para>
///         If the <see cref="SpecialMask"/> is set, then the value is that of the special mask, otherwise, the value is
///         the one of the lower bits (as extracted by <see cref="CharMask"/>).
///     </para>
/// </remarks>
[Flags]
public enum KeyCode : uint
{
    /// <summary>
    ///     Mask that indicates that the key is a unicode codepoint. Values outside this range indicate the key has shift
    ///     modifiers or is a special key like function keys, arrows keys and so on.
    /// </summary>
    CharMask = 0x_f_ffff,

    /// <summary>
    ///     If the <see cref="SpecialMask"/> is set, then the value is that of the special mask, otherwise, the value is
    ///     in the lower bits (as extracted by <see cref="CharMask"/>).
    /// </summary>
    SpecialMask = 0x_fff0_0000,

    /// <summary>
    ///     When this value is set, the Key encodes the sequence Shift-KeyValue. The actual value must be extracted by
    ///     removing the ShiftMask.
    /// </summary>
    ShiftMask = 0x_1000_0000,

    /// <summary>
    ///     When this value is set, the Key encodes the sequence Alt-KeyValue. The actual value must be extracted by
    ///     removing the AltMask.
    /// </summary>
    AltMask = 0x_8000_0000,

    /// <summary>
    ///     When this value is set, the Key encodes the sequence Ctrl-KeyValue. The actual value must be extracted by
    ///     removing the CtrlMask.
    /// </summary>
    CtrlMask = 0x_4000_0000,

    /// <summary>The key code representing an invalid or empty key.</summary>
    Null = 0,

    /// <summary>Backspace key.</summary>
    Backspace = 8,

    /// <summary>The key code for the tab key (forwards tab key).</summary>
    Tab = 9,

    /// <summary>The key code for the return key.</summary>
    Enter = ConsoleKey.Enter,

    /// <summary>The key code for the clear key.</summary>
    Clear = 12,

    /// <summary>The key code for the escape key.</summary>
    Esc = 27,

    /// <summary>The key code for the space bar key.</summary>
    Space = 32,

    /// <summary>Digit 0.</summary>
    D0 = 48,

    /// <summary>Digit 1.</summary>
    D1,

    /// <summary>Digit 2.</summary>
    D2,

    /// <summary>Digit 3.</summary>
    D3,

    /// <summary>Digit 4.</summary>
    D4,

    /// <summary>Digit 5.</summary>
    D5,

    /// <summary>Digit 6.</summary>
    D6,

    /// <summary>Digit 7.</summary>
    D7,

    /// <summary>Digit 8.</summary>
    D8,

    /// <summary>Digit 9.</summary>
    D9,

    /// <summary>The key code for the A key</summary>
    A = 65,

    /// <summary>The key code for the B key</summary>
    B,

    /// <summary>The key code for the C key</summary>
    C,

    /// <summary>The key code for the D key</summary>
    D,

    /// <summary>The key code for the E key</summary>
    E,

    /// <summary>The key code for the F key</summary>
    F,

    /// <summary>The key code for the G key</summary>
    G,

    /// <summary>The key code for the H key</summary>
    H,

    /// <summary>The key code for the I key</summary>
    I,

    /// <summary>The key code for the J key</summary>
    J,

    /// <summary>The key code for the K key</summary>
    K,

    /// <summary>The key code for the L key</summary>
    L,

    /// <summary>The key code for the M key</summary>
    M,

    /// <summary>The key code for the N key</summary>
    N,

    /// <summary>The key code for the O key</summary>
    O,

    /// <summary>The key code for the P key</summary>
    P,

    /// <summary>The key code for the Q key</summary>
    Q,

    /// <summary>The key code for the R key</summary>
    R,

    /// <summary>The key code for the S key</summary>
    S,

    /// <summary>The key code for the T key</summary>
    T,

    /// <summary>The key code for the U key</summary>
    U,

    /// <summary>The key code for the V key</summary>
    V,

    /// <summary>The key code for the W key</summary>
    W,

    /// <summary>The key code for the X key</summary>
    X,

    /// <summary>The key code for the Y key</summary>
    Y,

    /// <summary>The key code for the Z key</summary>
    Z,

    ///// <summary>
    ///// The key code for the Delete key.
    ///// </summary>
    //Delete = 127,

    // --- Special keys ---
    // The values below are common non-alphanum keys. Their values are
    // based on the .NET ConsoleKey values, which, in-turn are based on the
    // VK_ values from the Windows API.
    // We add MaxCodePoint to avoid conflicts with the Unicode values.

    /// <summary>The maximum Unicode codepoint value. Used to encode the non-alphanumeric control keys.</summary>
    MaxCodePoint = 0x10FFFF,

    /// <summary>Cursor up key</summary>
    CursorUp = MaxCodePoint + ConsoleKey.UpArrow,

    /// <summary>Cursor down key.</summary>
    CursorDown = MaxCodePoint + ConsoleKey.DownArrow,

    /// <summary>Cursor left key.</summary>
    CursorLeft = MaxCodePoint + ConsoleKey.LeftArrow,

    /// <summary>Cursor right key.</summary>
    CursorRight = MaxCodePoint + ConsoleKey.RightArrow,

    /// <summary>Page Up key.</summary>
    PageUp = MaxCodePoint + ConsoleKey.PageUp,

    /// <summary>Page Down key.</summary>
    PageDown = MaxCodePoint + ConsoleKey.PageDown,

    /// <summary>Home key.</summary>
    Home = MaxCodePoint + ConsoleKey.Home,

    /// <summary>End key.</summary>
    End = MaxCodePoint + ConsoleKey.End,

    /// <summary>Insert (INS) key.</summary>
    Insert = MaxCodePoint + ConsoleKey.Insert,

    /// <summary>Delete (DEL) key.</summary>
    Delete = MaxCodePoint + ConsoleKey.Delete,

    /// <summary>Print screen character key.</summary>
    PrintScreen = MaxCodePoint + ConsoleKey.PrintScreen,

    /// <summary>F1 key.</summary>
    F1 = MaxCodePoint + ConsoleKey.F1,

    /// <summary>F2 key.</summary>
    F2 = MaxCodePoint + ConsoleKey.F2,

    /// <summary>F3 key.</summary>
    F3 = MaxCodePoint + ConsoleKey.F3,

    /// <summary>F4 key.</summary>
    F4 = MaxCodePoint + ConsoleKey.F4,

    /// <summary>F5 key.</summary>
    F5 = MaxCodePoint + ConsoleKey.F5,

    /// <summary>F6 key.</summary>
    F6 = MaxCodePoint + ConsoleKey.F6,

    /// <summary>F7 key.</summary>
    F7 = MaxCodePoint + ConsoleKey.F7,

    /// <summary>F8 key.</summary>
    F8 = MaxCodePoint + ConsoleKey.F8,

    /// <summary>F9 key.</summary>
    F9 = MaxCodePoint + ConsoleKey.F9,

    /// <summary>F10 key.</summary>
    F10 = MaxCodePoint + ConsoleKey.F10,

    /// <summary>F11 key.</summary>
    F11 = MaxCodePoint + ConsoleKey.F11,

    /// <summary>F12 key.</summary>
    F12 = MaxCodePoint + ConsoleKey.F12,

    /// <summary>F13 key.</summary>
    F13 = MaxCodePoint + ConsoleKey.F13,

    /// <summary>F14 key.</summary>
    F14 = MaxCodePoint + ConsoleKey.F14,

    /// <summary>F15 key.</summary>
    F15 = MaxCodePoint + ConsoleKey.F15,

    /// <summary>F16 key.</summary>
    F16 = MaxCodePoint + ConsoleKey.F16,

    /// <summary>F17 key.</summary>
    F17 = MaxCodePoint + ConsoleKey.F17,

    /// <summary>F18 key.</summary>
    F18 = MaxCodePoint + ConsoleKey.F18,

    /// <summary>F19 key.</summary>
    F19 = MaxCodePoint + ConsoleKey.F19,

    /// <summary>F20 key.</summary>
    F20 = MaxCodePoint + ConsoleKey.F20,

    /// <summary>F21 key.</summary>
    F21 = MaxCodePoint + ConsoleKey.F21,

    /// <summary>F22 key.</summary>
    F22 = MaxCodePoint + ConsoleKey.F22,

    /// <summary>F23 key.</summary>
    F23 = MaxCodePoint + ConsoleKey.F23,

    /// <summary>F24 key.</summary>
    F24 = MaxCodePoint + ConsoleKey.F24
}
