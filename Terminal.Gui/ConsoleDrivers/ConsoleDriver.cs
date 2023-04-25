//
// ConsoleDriver.cs: Base class for Terminal.Gui ConsoleDriver implementations.
//
using NStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Terminal.Gui;
using static Terminal.Gui.ConfigurationManager;

namespace Terminal.Gui {
	/// <summary>
	/// Colors that can be used to set the foreground and background colors in console applications.
	/// </summary>
	/// <remarks>
	/// The <see cref="Attribute.HasValidColors"/> value indicates either no-color has been set or the color is invalid.
	/// </remarks>
	[JsonConverter (typeof (ColorJsonConverter))]
	public enum Color {
		/// <summary>
		/// The black color.
		/// </summary>
		Black,
		/// <summary>
		/// The blue color.
		/// </summary>
		Blue,
		/// <summary>
		/// The green color.
		/// </summary>
		Green,
		/// <summary>
		/// The cyan color.
		/// </summary>
		Cyan,
		/// <summary>
		/// The red color.
		/// </summary>
		Red,
		/// <summary>
		/// The magenta color.
		/// </summary>
		Magenta,
		/// <summary>
		/// The brown color.
		/// </summary>
		Brown,
		/// <summary>
		/// The gray color.
		/// </summary>
		Gray,
		/// <summary>
		/// The dark gray color.
		/// </summary>
		DarkGray,
		/// <summary>
		/// The bright bBlue color.
		/// </summary>
		BrightBlue,
		/// <summary>
		/// The bright green color.
		/// </summary>
		BrightGreen,
		/// <summary>
		/// The bright cyan color.
		/// </summary>
		BrightCyan,
		/// <summary>
		/// The bright red color.
		/// </summary>
		BrightRed,
		/// <summary>
		/// The bright magenta color.
		/// </summary>
		BrightMagenta,
		/// <summary>
		/// The bright yellow color.
		/// </summary>
		BrightYellow,
		/// <summary>
		/// The White color.
		/// </summary>
		White
	}

	/// <summary>
	/// Indicates the RGB for true colors.
	/// </summary>
	public class TrueColor {
		/// <summary>
		/// Red color component.
		/// </summary>
		public int Red { get; }
		/// <summary>
		/// Green color component.
		/// </summary>
		public int Green { get; }
		/// <summary>
		/// Blue color component.
		/// </summary>
		public int Blue { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TrueColor"/> struct.
		/// </summary>
		/// <param name="red"></param>
		/// <param name="green"></param>
		/// <param name="blue"></param>
		public TrueColor (int red, int green, int blue)
		{
			Red = red;
			Green = green;
			Blue = blue;
		}

		/// <summary>
		/// Converts true color to console color.
		/// </summary>
		/// <returns></returns>
		public Color ToConsoleColor ()
		{
			var trueColorMap = new Dictionary<TrueColor, Color> () {
				{ new TrueColor (0,0,0),Color.Black},
				{ new TrueColor (0, 0, 0x80),Color.Blue},
				{ new TrueColor (0, 0x80, 0),Color.Green},
				{ new TrueColor (0, 0x80, 0x80),Color.Cyan},
				{ new TrueColor (0x80, 0, 0),Color.Red},
				{ new TrueColor (0x80, 0, 0x80),Color.Magenta},
				{ new TrueColor (0xC1, 0x9C, 0x00),Color.Brown},  // TODO confirm this
				{ new TrueColor (0xC0, 0xC0, 0xC0),Color.Gray},
				{ new TrueColor (0x80, 0x80, 0x80),Color.DarkGray},
				{ new TrueColor (0, 0, 0xFF),Color.BrightBlue},
				{ new TrueColor (0, 0xFF, 0),Color.BrightGreen},
				{ new TrueColor (0, 0xFF, 0xFF),Color.BrightCyan},
				{ new TrueColor (0xFF, 0, 0),Color.BrightRed},
				{ new TrueColor (0xFF, 0, 0xFF),Color.BrightMagenta },
				{ new TrueColor (0xFF, 0xFF, 0),Color.BrightYellow},
				{ new TrueColor (0xFF, 0xFF, 0xFF),Color.White},
				};
			// Iterate over all colors in the map
			var distances = trueColorMap.Select (
							k => Tuple.Create (
								// the candidate we are considering matching against (RGB)
								k.Key,

								CalculateDistance (k.Key, this)
							));

			// get the closest
			var match = distances.OrderBy (t => t.Item2).First ();
			return trueColorMap [match.Item1];
		}

		private float CalculateDistance (TrueColor color1, TrueColor color2)
		{
			// use RGB distance
			return
				Math.Abs (color1.Red - color2.Red) +
				Math.Abs (color1.Green - color2.Green) +
				Math.Abs (color1.Blue - color2.Blue);
		}
	}

	/// <summary>
	/// Attributes are used as elements that contain both a foreground and a background or platform specific features.
	/// </summary>
	/// <remarks>
	///   <see cref="Attribute"/>s are needed to map colors to terminal capabilities that might lack colors. 
	///   They encode both the foreground and the background color and are used in the <see cref="ColorScheme"/>
	///   class to define color schemes that can be used in an application.
	/// </remarks>
	[JsonConverter (typeof (AttributeJsonConverter))]
	public struct Attribute {
		/// <summary>
		/// The <see cref="ConsoleDriver"/>-specific color attribute value. If <see cref="Initialized"/> is <see langword="false"/> 
		/// the value of this property is invalid (typically because the Attribute was created before a driver was loaded)
		/// and the attribute should be re-made (see <see cref="Make(Color, Color)"/>) before it is used.
		/// </summary>
		[JsonIgnore (Condition = JsonIgnoreCondition.Always)]
		public int Value { get; }

		/// <summary>
		/// The foreground color.
		/// </summary>
		[JsonConverter (typeof (ColorJsonConverter))]
		public Color Foreground { get; }

		/// <summary>
		/// The background color.
		/// </summary>
		[JsonConverter (typeof (ColorJsonConverter))]
		public Color Background { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Attribute"/> struct with only the value passed to
		///   and trying to get the colors if defined.
		/// </summary>
		/// <param name="value">Value.</param>
		public Attribute (int value)
		{
			Color foreground = default;
			Color background = default;

			Initialized = false;
			if (Application.Driver != null) {
				Application.Driver.GetColors (value, out foreground, out background);
				Initialized = true;
			}
			Value = value;
			Foreground = foreground;
			Background = background;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Attribute"/> struct.
		/// </summary>
		/// <param name="value">Value.</param>
		/// <param name="foreground">Foreground</param>
		/// <param name="background">Background</param>
		public Attribute (int value, Color foreground, Color background)
		{
			Value = value;
			Foreground = foreground;
			Background = background;
			Initialized = true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Attribute"/> struct.
		/// </summary>
		/// <param name="foreground">Foreground</param>
		/// <param name="background">Background</param>
		public Attribute (Color foreground = new Color (), Color background = new Color ())
		{
			var make = Make (foreground, background);
			Initialized = make.Initialized;
			Value = make.Value;
			Foreground = foreground;
			Background = background;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Attribute"/> struct
		///  with the same colors for the foreground and background.
		/// </summary>
		/// <param name="color">The color.</param>
		public Attribute (Color color) : this (color, color) { }

		/// <summary>
		/// Implicit conversion from an <see cref="Attribute"/> to the underlying, driver-specific, Int32 representation
		/// of the color.
		/// </summary>
		/// <returns>The driver-specific color value stored in the attribute.</returns>
		/// <param name="c">The attribute to convert</param>
		public static implicit operator int (Attribute c)
		{
			if (!c.Initialized) throw new InvalidOperationException ("Attribute: Attributes must be initialized by a driver before use.");
			return c.Value;
		}

		/// <summary>
		/// Implicitly convert an driver-specific color value into an <see cref="Attribute"/>
		/// </summary>
		/// <returns>An attribute with the specified driver-specific color value.</returns>
		/// <param name="v">value</param>
		public static implicit operator Attribute (int v) => new Attribute (v);

		/// <summary>
		/// Creates an <see cref="Attribute"/> from the specified foreground and background colors.
		/// </summary>
		/// <remarks>
		/// If a <see cref="ConsoleDriver"/> has not been loaded (<c>Application.Driver == null</c>) this
		/// method will return an attribute with <see cref="Initialized"/> set to  <see langword="false"/>.
		/// </remarks>
		/// <returns>The new attribute.</returns>
		/// <param name="foreground">Foreground color to use.</param>
		/// <param name="background">Background color to use.</param>
		public static Attribute Make (Color foreground, Color background)
		{
			if (Application.Driver == null) {
				// Create the attribute, but show it's not been initialized
				return new Attribute (-1, foreground, background) {
					Initialized = false
				};
			}
			return Application.Driver.MakeAttribute (foreground, background);
		}

		/// <summary>
		/// Gets the current <see cref="Attribute"/> from the driver.
		/// </summary>
		/// <returns>The current attribute.</returns>
		public static Attribute Get ()
		{
			if (Application.Driver == null)
				throw new InvalidOperationException ("The Application has not been initialized");
			return Application.Driver.GetAttribute ();
		}

		/// <summary>
		/// If <see langword="true"/> the attribute has been initialized by a <see cref="ConsoleDriver"/> and 
		/// thus has <see cref="Value"/> that is valid for that driver. If <see langword="false"/> the <see cref="Foreground"/>
		/// and <see cref="Background"/> colors may have been set '-1' but
		/// the attribute has not been mapped to a <see cref="ConsoleDriver"/> specific color value.
		/// </summary>
		/// <remarks>
		/// Attributes that have not been initialized must eventually be initialized before being passed to a driver.
		/// </remarks>
		[JsonIgnore]
		public bool Initialized { get; internal set; }

		/// <summary>
		/// Returns <see langword="true"/> if the Attribute is valid (both foreground and background have valid color values).
		/// </summary>
		/// <returns></returns>
		[JsonIgnore]
		public bool HasValidColors { get => (int)Foreground > -1 && (int)Background > -1; }
	}

	/// <summary>
	/// Defines the color <see cref="Attribute"/>s for common visible elements in a <see cref="View"/>. 
	/// Containers such as <see cref="Window"/> and <see cref="FrameView"/> use <see cref="ColorScheme"/> to determine
	/// the colors used by sub-views.
	/// </summary>
	/// <remarks>
	/// See also: <see cref="Colors.ColorSchemes"/>.
	/// </remarks>
	[JsonConverter (typeof (ColorSchemeJsonConverter))]
	public class ColorScheme : IEquatable<ColorScheme> {
		Attribute _normal = new Attribute (Color.White, Color.Black);
		Attribute _focus = new Attribute (Color.White, Color.Black);
		Attribute _hotNormal = new Attribute (Color.White, Color.Black);
		Attribute _hotFocus = new Attribute (Color.White, Color.Black);
		Attribute _disabled = new Attribute (Color.White, Color.Black);

		/// <summary>
		/// Used by <see cref="Colors.SetColorScheme(ColorScheme, string)"/> and <see cref="Colors.GetColorScheme(string)"/> to track which ColorScheme 
		/// is being accessed.
		/// </summary>
		internal string schemeBeingSet = "";

		/// <summary>
		/// The foreground and background color for text when the view is not focused, hot, or disabled.
		/// </summary>
		public Attribute Normal {
			get { return _normal; }
			set {
				if (!value.HasValidColors) {
					return;
				}
				_normal = value;
			}
		}

		/// <summary>
		/// The foreground and background color for text when the view has the focus.
		/// </summary>
		public Attribute Focus {
			get { return _focus; }
			set {
				if (!value.HasValidColors) {
					return;
				}
				_focus = value;
			}
		}

		/// <summary>
		/// The foreground and background color for text when the view is highlighted (hot).
		/// </summary>
		public Attribute HotNormal {
			get { return _hotNormal; }
			set {
				if (!value.HasValidColors) {
					return;
				}
				_hotNormal = value;
			}
		}

		/// <summary>
		/// The foreground and background color for text when the view is highlighted (hot) and has focus.
		/// </summary>
		public Attribute HotFocus {
			get { return _hotFocus; }
			set {
				if (!value.HasValidColors) {
					return;
				}
				_hotFocus = value;
			}
		}

		/// <summary>
		/// The default foreground and background color for text, when the view is disabled.
		/// </summary>
		public Attribute Disabled {
			get { return _disabled; }
			set {
				if (!value.HasValidColors) {
					return;
				}
				_disabled = value;
			}
		}

		/// <summary>
		/// Compares two <see cref="ColorScheme"/> objects for equality.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns>true if the two objects are equal</returns>
		public override bool Equals (object obj)
		{
			return Equals (obj as ColorScheme);
		}

		/// <summary>
		/// Compares two <see cref="ColorScheme"/> objects for equality.
		/// </summary>
		/// <param name="other"></param>
		/// <returns>true if the two objects are equal</returns>
		public bool Equals (ColorScheme other)
		{
			return other != null &&
			       EqualityComparer<Attribute>.Default.Equals (_normal, other._normal) &&
			       EqualityComparer<Attribute>.Default.Equals (_focus, other._focus) &&
			       EqualityComparer<Attribute>.Default.Equals (_hotNormal, other._hotNormal) &&
			       EqualityComparer<Attribute>.Default.Equals (_hotFocus, other._hotFocus) &&
			       EqualityComparer<Attribute>.Default.Equals (_disabled, other._disabled);
		}

		/// <summary>
		/// Returns a hashcode for this instance.
		/// </summary>
		/// <returns>hashcode for this instance</returns>
		public override int GetHashCode ()
		{
			int hashCode = -1242460230;
			hashCode = hashCode * -1521134295 + _normal.GetHashCode ();
			hashCode = hashCode * -1521134295 + _focus.GetHashCode ();
			hashCode = hashCode * -1521134295 + _hotNormal.GetHashCode ();
			hashCode = hashCode * -1521134295 + _hotFocus.GetHashCode ();
			hashCode = hashCode * -1521134295 + _disabled.GetHashCode ();
			return hashCode;
		}

		/// <summary>
		/// Compares two <see cref="ColorScheme"/> objects for equality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns><c>true</c> if the two objects are equivalent</returns>
		public static bool operator == (ColorScheme left, ColorScheme right)
		{
			return EqualityComparer<ColorScheme>.Default.Equals (left, right);
		}

		/// <summary>
		/// Compares two <see cref="ColorScheme"/> objects for inequality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns><c>true</c> if the two objects are not equivalent</returns>
		public static bool operator != (ColorScheme left, ColorScheme right)
		{
			return !(left == right);
		}

		internal void Initialize ()
		{
			// If the new scheme was created before a driver was loaded, we need to re-make
			// the attributes
			if (!_normal.Initialized) {
				_normal = new Attribute (_normal.Foreground, _normal.Background);
			}
			if (!_focus.Initialized) {
				_focus = new Attribute (_focus.Foreground, _focus.Background);
			}
			if (!_hotNormal.Initialized) {
				_hotNormal = new Attribute (_hotNormal.Foreground, _hotNormal.Background);
			}
			if (!_hotFocus.Initialized) {
				_hotFocus = new Attribute (_hotFocus.Foreground, _hotFocus.Background);
			}
			if (!_disabled.Initialized) {
				_disabled = new Attribute (_disabled.Foreground, _disabled.Background);
			}
		}
	}

	/// <summary>
	/// The default <see cref="ColorScheme"/>s for the application.
	/// </summary>
	/// <remarks>
	/// This property can be set in a Theme to change the default <see cref="Colors"/> for the application.
	/// </remarks>
	public static class Colors {
		private class SchemeNameComparerIgnoreCase : IEqualityComparer<string> {
			public bool Equals (string x, string y)
			{
				if (x != null && y != null) {
					return string.Equals (x, y, StringComparison.InvariantCultureIgnoreCase);
				}
				return false;
			}

			public int GetHashCode (string obj)
			{
				return obj.ToLowerInvariant ().GetHashCode ();
			}
		}

		static Colors ()
		{
			ColorSchemes = Create ();
		}

		/// <summary>
		/// Creates a new dictionary of new <see cref="ColorScheme"/> objects.
		/// </summary>
		public static Dictionary<string, ColorScheme> Create ()
		{
			// Use reflection to dynamically create the default set of ColorSchemes from the list defined 
			// by the class. 
			return typeof (Colors).GetProperties ()
				.Where (p => p.PropertyType == typeof (ColorScheme))
				.Select (p => new KeyValuePair<string, ColorScheme> (p.Name, new ColorScheme ()))
				.ToDictionary (t => t.Key, t => t.Value, comparer: new SchemeNameComparerIgnoreCase ());
		}

		/// <summary>
		/// The application Toplevel color scheme, for the default Toplevel views.
		/// </summary>
		/// <remarks>
		/// <para>
		///	This API will be deprecated in the future. Use <see cref="Colors.ColorSchemes"/> instead (e.g. <c>edit.ColorScheme = Colors.ColorSchemes["TopLevel"];</c>
		/// </para>
		/// </remarks>
		public static ColorScheme TopLevel { get => GetColorScheme (); set => SetColorScheme (value); }

		/// <summary>
		/// The base color scheme, for the default Toplevel views.
		/// </summary>
		/// <remarks>
		/// <para>
		///	This API will be deprecated in the future. Use <see cref="Colors.ColorSchemes"/> instead (e.g. <c>edit.ColorScheme = Colors.ColorSchemes["Base"];</c>
		/// </para>
		/// </remarks>
		public static ColorScheme Base { get => GetColorScheme (); set => SetColorScheme (value); }

		/// <summary>
		/// The dialog color scheme, for standard popup dialog boxes
		/// </summary>
		/// <remarks>
		/// <para>
		///	This API will be deprecated in the future. Use <see cref="Colors.ColorSchemes"/> instead (e.g. <c>edit.ColorScheme = Colors.ColorSchemes["Dialog"];</c>
		/// </para>
		/// </remarks>
		public static ColorScheme Dialog { get => GetColorScheme (); set => SetColorScheme (value); }

		/// <summary>
		/// The menu bar color
		/// </summary>
		/// <remarks>
		/// <para>
		///	This API will be deprecated in the future. Use <see cref="Colors.ColorSchemes"/> instead (e.g. <c>edit.ColorScheme = Colors.ColorSchemes["Menu"];</c>
		/// </para>
		/// </remarks>
		public static ColorScheme Menu { get => GetColorScheme (); set => SetColorScheme (value); }

		/// <summary>
		/// The color scheme for showing errors.
		/// </summary>
		/// <remarks>
		/// <para>
		///	This API will be deprecated in the future. Use <see cref="Colors.ColorSchemes"/> instead (e.g. <c>edit.ColorScheme = Colors.ColorSchemes["Error"];</c>
		/// </para>
		/// </remarks>
		public static ColorScheme Error { get => GetColorScheme (); set => SetColorScheme (value); }

		static ColorScheme GetColorScheme ([CallerMemberName] string schemeBeingSet = null)
		{
			return ColorSchemes [schemeBeingSet];
		}

		static void SetColorScheme (ColorScheme colorScheme, [CallerMemberName] string schemeBeingSet = null)
		{
			ColorSchemes [schemeBeingSet] = colorScheme;
			colorScheme.schemeBeingSet = schemeBeingSet;
		}

		/// <summary>
		/// Provides the defined <see cref="ColorScheme"/>s.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof(ThemeScope), OmitClassName = true)]
		[JsonConverter(typeof(DictionaryJsonConverter<ColorScheme>))]
		public static Dictionary<string, ColorScheme> ColorSchemes { get; private set; }
	}

	/// <summary>
	/// Cursors Visibility that are displayed
	/// </summary>
	// 
	// Hexa value are set as 0xAABBCCDD where :
	//
	//     AA stand for the TERMINFO DECSUSR parameter value to be used under Linux & MacOS
	//     BB stand for the NCurses curs_set parameter value to be used under Linux & MacOS
	//     CC stand for the CONSOLE_CURSOR_INFO.bVisible parameter value to be used under Windows
	//     DD stand for the CONSOLE_CURSOR_INFO.dwSize parameter value to be used under Windows
	//
	public enum CursorVisibility {
		/// <summary>
		///	Cursor caret has default
		/// </summary>
		/// <remarks>Works under Xterm-like terminal otherwise this is equivalent to <see ref="Underscore"/>. This default directly depends of the XTerm user configuration settings so it could be Block, I-Beam, Underline with possible blinking.</remarks>
		Default = 0x00010119,

		/// <summary>
		///	Cursor caret is hidden
		/// </summary>
		Invisible = 0x03000019,

		/// <summary>
		///	Cursor caret is normally shown as a blinking underline bar _
		/// </summary>
		Underline = 0x03010119,

		/// <summary>
		///	Cursor caret is normally shown as a underline bar _
		/// </summary>
		/// <remarks>Under Windows, this is equivalent to <see ref="UnderscoreBlinking"/></remarks>
		UnderlineFix = 0x04010119,

		/// <summary>
		///	Cursor caret is displayed a blinking vertical bar |
		/// </summary>
		/// <remarks>Works under Xterm-like terminal otherwise this is equivalent to <see ref="Underscore"/></remarks>
		Vertical = 0x05010119,

		/// <summary>
		///	Cursor caret is displayed a blinking vertical bar |
		/// </summary>
		/// <remarks>Works under Xterm-like terminal otherwise this is equivalent to <see ref="Underscore"/></remarks>
		VerticalFix = 0x06010119,

		/// <summary>
		///	Cursor caret is displayed as a blinking block ▉
		/// </summary>
		Box = 0x01020164,

		/// <summary>
		///	Cursor caret is displayed a block ▉
		/// </summary>
		/// <remarks>Works under Xterm-like terminal otherwise this is equivalent to <see ref="Block"/></remarks>
		BoxFix = 0x02020164,
	}
	
	/// <summary>
	/// ConsoleDriver is an abstract class that defines the requirements for a console driver.  
	/// There are currently three implementations: <see cref="CursesDriver"/> (for Unix and Mac), <see cref="WindowsDriver"/>, and <see cref="NetDriver"/> that uses the .NET Console API.
	/// </summary>
	public abstract class ConsoleDriver {
		/// <summary>
		/// The handler fired when the terminal is resized.
		/// </summary>
		protected Action TerminalResized;

		/// <summary>
		/// The current number of columns in the terminal.
		/// </summary>
		public abstract int Cols { get; }

		/// <summary>
		/// The current number of rows in the terminal.
		/// </summary>
		public abstract int Rows { get; }

		/// <summary>
		/// The current left in the terminal.
		/// </summary>
		public abstract int Left { get; }

		/// <summary>
		/// The current top in the terminal.
		/// </summary>
		public abstract int Top { get; }

		/// <summary>
		/// Get the operation system clipboard.
		/// </summary>
		public abstract IClipboard Clipboard { get; }

		/// <summary>
		/// <para>
		/// If <see langword="false"/> (the default) the height of the Terminal.Gui application (<see cref="Rows"/>) 
		/// tracks to the height of the visible console view when the console is resized. In this case 
		/// scrolling in the console will be disabled and all <see cref="Rows"/> will remain visible.
		/// </para>
		/// <para>
		/// If <see langword="true"/> then height of the Terminal.Gui application <see cref="Rows"/> only tracks 
		/// the height of the visible console view when the console is made larger (the application will only grow in height, never shrink). 
		/// In this case console scrolling is enabled and the contents (<see cref="Rows"/> high) will scroll
		/// as the console scrolls. 
		/// </para>
		/// </summary>
		/// <remarks>
		/// NOTE: This functionaliy is currently broken on Windows Terminal.
		/// </remarks>
		public abstract bool EnableConsoleScrolling { get; set; }

		/// <summary>
		/// The format is rows, columns and 3 values on the last column: Rune, Attribute and Dirty Flag
		/// </summary>
		public virtual int [,,] Contents { get; }

		/// <summary>
		/// Initializes the driver
		/// </summary>
		/// <param name="terminalResized">Method to invoke when the terminal is resized.</param>
		public abstract void Init (Action terminalResized);
		/// <summary>
		/// Moves the cursor to the specified column and row.
		/// </summary>
		/// <param name="col">Column to move the cursor to.</param>
		/// <param name="row">Row to move the cursor to.</param>
		public abstract void Move (int col, int row);

		/// <summary>
		/// Adds the specified rune to the display at the current cursor position.
		/// </summary>
		/// <param name="rune">Rune to add.</param>
		public abstract void AddRune (Rune rune);

		/// <summary>
		/// Ensures a Rune is not a control character and can be displayed by translating characters below 0x20
		/// to equivalent, printable, Unicode chars.
		/// </summary>
		/// <param name="c">Rune to translate</param>
		/// <returns></returns>
		public static Rune MakePrintable (Rune c)
		{
			if (c <= 0x1F || (c >= 0X7F && c <= 0x9F)) {
				// ASCII (C0) control characters.
				// C1 control characters (https://www.aivosto.com/articles/control-characters.html#c1)
				return new Rune (c + 0x2400);
			}

			return c;
		}

		/// <summary>
		/// Ensures that the column and line are in a valid range from the size of the driver.
		/// </summary>
		/// <param name="col">The column.</param>
		/// <param name="row">The row.</param>
		/// <param name="clip">The clip.</param>
		/// <returns><c>true</c>if it's a valid range,<c>false</c>otherwise.</returns>
		public bool IsValidContent (int col, int row, Rect clip) =>
			col >= 0 && row >= 0 && col < Cols && row < Rows && clip.Contains (col, row);

		/// <summary>
		/// Adds the <paramref name="str"/> to the display at the cursor position.
		/// </summary>
		/// <param name="str">String.</param>
		public abstract void AddStr (ustring str);

		/// <summary>
		/// Prepare the driver and set the key and mouse events handlers.
		/// </summary>
		/// <param name="mainLoop">The main loop.</param>
		/// <param name="keyHandler">The handler for ProcessKey</param>
		/// <param name="keyDownHandler">The handler for key down events</param>
		/// <param name="keyUpHandler">The handler for key up events</param>
		/// <param name="mouseHandler">The handler for mouse events</param>
		public abstract void PrepareToRun (MainLoop mainLoop, Action<KeyEvent> keyHandler, Action<KeyEvent> keyDownHandler, Action<KeyEvent> keyUpHandler, Action<MouseEvent> mouseHandler);

		/// <summary>
		/// Updates the screen to reflect all the changes that have been done to the display buffer
		/// </summary>
		public abstract void Refresh ();

		/// <summary>
		/// Updates the location of the cursor position
		/// </summary>
		public abstract void UpdateCursor ();

		/// <summary>
		/// Retreive the cursor caret visibility
		/// </summary>
		/// <param name="visibility">The current <see cref="CursorVisibility"/></param>
		/// <returns>true upon success</returns>
		public abstract bool GetCursorVisibility (out CursorVisibility visibility);

		/// <summary>
		/// Change the cursor caret visibility
		/// </summary>
		/// <param name="visibility">The wished <see cref="CursorVisibility"/></param>
		/// <returns>true upon success</returns>
		public abstract bool SetCursorVisibility (CursorVisibility visibility);

		/// <summary>
		/// Ensure the cursor visibility
		/// </summary>
		/// <returns>true upon success</returns>
		public abstract bool EnsureCursorVisibility ();

		/// <summary>
		/// Ends the execution of the console driver.
		/// </summary>
		public abstract void End ();

		/// <summary>
		/// Resizes the clip area when the screen is resized.
		/// </summary>
		public abstract void ResizeScreen ();

		/// <summary>
		/// Reset and recreate the contents and the driver buffer.
		/// </summary>
		public abstract void UpdateOffScreen ();

		/// <summary>
		/// Redraws the physical screen with the contents that have been queued up via any of the printing commands.
		/// </summary>
		public abstract void UpdateScreen ();

		/// <summary>
		/// The current attribute the driver is using. 
		/// </summary>
		public virtual Attribute CurrentAttribute {
			get => currentAttribute;
			set {
				if (!value.Initialized && value.HasValidColors && Application.Driver != null) {
					CurrentAttribute = Application.Driver.MakeAttribute (value.Foreground, value.Background);
					return;
				}
				if (!value.Initialized) Debug.WriteLine ("ConsoleDriver.CurrentAttribute: Attributes must be initialized before use.");

				currentAttribute = value;
			}
		}

		/// <summary>
		/// Selects the specified attribute as the attribute to use for future calls to AddRune and AddString.
		/// </summary>
		/// <remarks>
		/// Implementations should call <c>base.SetAttribute(c)</c>.
		/// </remarks>
		/// <param name="c">C.</param>
		public virtual void SetAttribute (Attribute c)
		{
			CurrentAttribute = c;
		}

		/// <summary>
		/// Set Colors from limit sets of colors. Not implemented by any driver: See Issue #2300.
		/// </summary>
		/// <param name="foreground">Foreground.</param>
		/// <param name="background">Background.</param>
		public abstract void SetColors (ConsoleColor foreground, ConsoleColor background);

		// Advanced uses - set colors to any pre-set pairs, you would need to init_color
		// that independently with the R, G, B values.
		/// <summary>
		/// Advanced uses - set colors to any pre-set pairs, you would need to init_color
		/// that independently with the R, G, B values. Not implemented by any driver: See Issue #2300.
		/// </summary>
		/// <param name="foregroundColorId">Foreground color identifier.</param>
		/// <param name="backgroundColorId">Background color identifier.</param>
		public abstract void SetColors (short foregroundColorId, short backgroundColorId);

		/// <summary>
		/// Gets the foreground and background colors based on the value.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="foreground">The foreground.</param>
		/// <param name="background">The background.</param>
		/// <returns></returns>
		public abstract bool GetColors (int value, out Color foreground, out Color background);

		/// <summary>
		/// Allows sending keys without typing on a keyboard.
		/// </summary>
		/// <param name="keyChar">The character key.</param>
		/// <param name="key">The key.</param>
		/// <param name="shift">If shift key is sending.</param>
		/// <param name="alt">If alt key is sending.</param>
		/// <param name="control">If control key is sending.</param>
		public abstract void SendKeys (char keyChar, ConsoleKey key, bool shift, bool alt, bool control);

		/// <summary>
		/// Set the handler when the terminal is resized.
		/// </summary>
		/// <param name="terminalResized"></param>
		public void SetTerminalResized (Action terminalResized)
		{
			TerminalResized = terminalResized;
		}

		/// <summary>
		/// Fills the specified rectangle with the specified rune.
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="rune"></param>
		public virtual void FillRect (Rect rect, System.Rune rune = default)
		{
			for (var r = rect.Y; r < rect.Y + rect.Height; r++) {
				for (var c = rect.X; c < rect.X + rect.Width; c++) {
					Application.Driver.Move (c, r);
					Application.Driver.AddRune (rune == default ? ' ' : rune);
				}
			}
		}

		/// <summary>
		/// Enables diagnostic functions
		/// </summary>
		[Flags]
		public enum DiagnosticFlags : uint {
			/// <summary>
			/// All diagnostics off
			/// </summary>
			Off = 0b_0000_0000,
			/// <summary>
			/// When enabled, <see cref="Frame.OnDrawFrames"/> will draw a 
			/// ruler in the frame for any side with a padding value greater than 0.
			/// </summary>
			FrameRuler = 0b_0000_0001,
			/// <summary>
			/// When enabled, <see cref="Frame.OnDrawFrames"/> will draw a 
			/// 'L', 'R', 'T', and 'B' when clearing <see cref="Thickness"/>'s instead of ' '.
			/// </summary>
			FramePadding = 0b_0000_0010,
		}

		/// <summary>
		/// Set flags to enable/disable <see cref="ConsoleDriver"/> diagnostics.
		/// </summary>
		public static DiagnosticFlags Diagnostics { get; set; }

		/// <summary>
		/// Suspend the application, typically needs to save the state, suspend the app and upon return, reset the console driver.
		/// </summary>
		public abstract void Suspend ();

		Rect clip;

		/// <summary>
		/// Controls the current clipping region that AddRune/AddStr is subject to.
		/// </summary>
		/// <value>The clip.</value>
		public Rect Clip {
			get => clip;
			set => this.clip = value;
		}

		/// <summary>
		/// Start of mouse moves.
		/// </summary>
		public abstract void StartReportingMouseMoves ();

		/// <summary>
		/// Stop reporting mouses moves.
		/// </summary>
		public abstract void StopReportingMouseMoves ();

		/// <summary>
		/// Disables the cooked event processing from the mouse driver. 
		/// At startup, it is assumed mouse events are cooked. Not implemented by any driver: See Issue #2300.
		/// </summary>
		public abstract void UncookMouse ();

		/// <summary>
		/// Enables the cooked event processing from the mouse driver. Not implemented by any driver: See Issue #2300.
		/// </summary>
		public abstract void CookMouse ();

		/// <summary>
		/// Horizontal line character.
		/// </summary>
		public Rune HLine = '\u2500';

		/// <summary>
		/// Vertical line character.
		/// </summary>
		public Rune VLine = '\u2502';

		/// <summary>
		/// Stipple pattern
		/// </summary>
		public Rune Stipple = '\u2591';

		/// <summary>
		/// Diamond character
		/// </summary>
		public Rune Diamond = '\u25ca';

		/// <summary>
		/// Upper left corner
		/// </summary>
		public Rune ULCorner = '\u250C';

		/// <summary>
		/// Lower left corner
		/// </summary>
		public Rune LLCorner = '\u2514';

		/// <summary>
		/// Upper right corner
		/// </summary>
		public Rune URCorner = '\u2510';

		/// <summary>
		/// Lower right corner
		/// </summary>
		public Rune LRCorner = '\u2518';

		/// <summary>
		/// Left tee
		/// </summary>
		public Rune LeftTee = '\u251c';

		/// <summary>
		/// Right tee
		/// </summary>
		public Rune RightTee = '\u2524';

		/// <summary>
		/// Top tee
		/// </summary>
		public Rune TopTee = '\u252c';

		/// <summary>
		/// The bottom tee.
		/// </summary>
		public Rune BottomTee = '\u2534';

		/// <summary>
		/// Checkmark.
		/// </summary>
		public Rune Checked = '\u221a';

		/// <summary>
		/// Un-checked checkmark.
		/// </summary>
		public Rune UnChecked = '\u2574';

		/// <summary>
		/// Null-checked checkmark.
		/// </summary>
		public Rune NullChecked = '\u2370';

		/// <summary>
		/// Selected mark.
		/// </summary>
		public Rune Selected = '\u25cf';

		/// <summary>
		/// Un-selected selected mark.
		/// </summary>
		public Rune UnSelected = '\u25cc';

		/// <summary>
		/// Right Arrow.
		/// </summary>
		public Rune RightArrow = '\u25ba';

		/// <summary>
		/// Left Arrow.
		/// </summary>
		public Rune LeftArrow = '\u25c4';

		/// <summary>
		/// Down Arrow.
		/// </summary>
		public Rune DownArrow = '\u25bc';

		/// <summary>
		/// Up Arrow.
		/// </summary>
		public Rune UpArrow = '\u25b2';

		/// <summary>
		/// Left indicator for default action (e.g. for <see cref="Button"/>).
		/// </summary>
		public Rune LeftDefaultIndicator = '\u25e6';

		/// <summary>
		/// Right indicator for default action (e.g. for <see cref="Button"/>).
		/// </summary>
		public Rune RightDefaultIndicator = '\u25e6';

		/// <summary>
		/// Left frame/bracket (e.g. '[' for <see cref="Button"/>).
		/// </summary>
		public Rune LeftBracket = '[';

		/// <summary>
		/// Right frame/bracket (e.g. ']' for <see cref="Button"/>).
		/// </summary>
		public Rune RightBracket = ']';

		/// <summary>
		/// Blocks Segment indicator for meter views (e.g. <see cref="ProgressBar"/>.
		/// </summary>
		public Rune BlocksMeterSegment = '\u258c';

		/// <summary>
		/// Continuous Segment indicator for meter views (e.g. <see cref="ProgressBar"/>.
		/// </summary>
		public Rune ContinuousMeterSegment = '\u2588';

		/// <summary>
		/// Horizontal double line character.
		/// </summary>
		public Rune HDbLine = '\u2550';

		/// <summary>
		/// Vertical double line character.
		/// </summary>
		public Rune VDbLine = '\u2551';

		/// <summary>
		/// Upper left double corner
		/// </summary>
		public Rune ULDbCorner = '\u2554';

		/// <summary>
		/// Lower left double corner
		/// </summary>
		public Rune LLDbCorner = '\u255a';

		/// <summary>
		/// Upper right double corner
		/// </summary>
		public Rune URDbCorner = '\u2557';

		/// <summary>
		/// Lower right double corner
		/// </summary>
		public Rune LRDbCorner = '\u255d';

		/// <summary>
		/// Upper left rounded corner
		/// </summary>
		public Rune ULRCorner = '\u256d';

		/// <summary>
		/// Lower left rounded corner
		/// </summary>
		public Rune LLRCorner = '\u2570';

		/// <summary>
		/// Upper right rounded corner
		/// </summary>
		public Rune URRCorner = '\u256e';

		/// <summary>
		/// Lower right rounded corner
		/// </summary>
		public Rune LRRCorner = '\u256f';

		/// <summary>
		/// Horizontal double dashed line character.
		/// </summary>
		public Rune HDsLine = '\u254c';

		/// <summary>
		/// Vertical triple dashed line character.
		/// </summary>
		public Rune VDsLine = '\u2506';

		/// <summary>
		/// Horizontal triple dashed line character.
		/// </summary>
		public Rune HDtLine = '\u2504';

		/// <summary>
		/// Horizontal quadruple dashed line character.
		/// </summary>
		public Rune HD4Line = '\u2508';

		/// <summary>
		/// Vertical double dashed line character.
		/// </summary>
		public Rune VD2Line = '\u254e';

		/// <summary>
		/// Vertical quadruple dashed line character.
		/// </summary>
		public Rune VDtLine = '\u250a';

		/// <summary>
		/// Horizontal heavy line character.
		/// </summary>
		public Rune HThLine = '\u2501';

		/// <summary>
		/// Vertical heavy line character.
		/// </summary>
		public Rune VThLine = '\u2503';

		/// <summary>
		/// Upper left heavy corner
		/// </summary>
		public Rune ULThCorner = '\u250f';

		/// <summary>
		/// Lower left heavy corner
		/// </summary>
		public Rune LLThCorner = '\u2517';

		/// <summary>
		/// Upper right heavy corner
		/// </summary>
		public Rune URThCorner = '\u2513';

		/// <summary>
		/// Lower right heavy corner
		/// </summary>
		public Rune LRThCorner = '\u251b';

		/// <summary>
		/// Horizontal heavy double dashed line character.
		/// </summary>
		public Rune HThDsLine = '\u254d';

		/// <summary>
		/// Vertical heavy triple dashed line character.
		/// </summary>
		public Rune VThDsLine = '\u2507';

		/// <summary>
		/// Horizontal heavy triple dashed line character.
		/// </summary>
		public Rune HThDtLine = '\u2505';

		/// <summary>
		/// Horizontal heavy quadruple dashed line character.
		/// </summary>
		public Rune HThD4Line = '\u2509';

		/// <summary>
		/// Vertical heavy double dashed line character.
		/// </summary>
		public Rune VThD2Line = '\u254f';

		/// <summary>
		/// Vertical heavy quadruple dashed line character.
		/// </summary>
		public Rune VThDtLine = '\u250b';

		/// <summary>
		/// The left half line.
		/// </summary>
		public Rune HalfLeftLine = '\u2574';

		/// <summary>
		/// The up half line.
		/// </summary>
		public Rune HalfTopLine = '\u2575';

		/// <summary>
		/// The right half line.
		/// </summary>
		public Rune HalfRightLine = '\u2576';

		/// <summary>
		/// The down half line.
		/// </summary>
		public Rune HalfBottomLine = '\u2577';

		/// <summary>
		/// The heavy left half line.
		/// </summary>
		public Rune ThHalfLeftLine = '\u2578';

		/// <summary>
		/// The heavy up half line.
		/// </summary>
		public Rune ThHalfTopLine = '\u2579';

		/// <summary>
		/// The heavy right half line.
		/// </summary>
		public Rune ThHalfRightLine = '\u257a';

		/// <summary>
		/// The heavy light down half line.
		/// </summary>
		public Rune ThHalfBottomLine = '\u257b';

		/// <summary>
		/// The light left and heavy right line.
		/// </summary>
		public Rune ThRightSideLine = '\u257c';

		/// <summary>
		/// The light up and heavy down line.
		/// </summary>
		public Rune ThBottomSideLine = '\u257d';

		/// <summary>
		/// The heavy left and light right line.
		/// </summary>
		public Rune ThLeftSideLine = '\u257e';

		/// <summary>
		/// The heavy up and light down line.
		/// </summary>
		public Rune ThTopSideLine = '\u257f';

		private Attribute currentAttribute;

		/// <summary>
		/// Make the attribute for the foreground and background colors.
		/// </summary>
		/// <param name="fore">Foreground.</param>
		/// <param name="back">Background.</param>
		/// <returns></returns>
		public abstract Attribute MakeAttribute (Color fore, Color back);

		/// <summary>
		/// Gets the current <see cref="Attribute"/>.
		/// </summary>
		/// <returns>The current attribute.</returns>
		public Attribute GetAttribute () => CurrentAttribute;

		/// <summary>
		/// Make the <see cref="Colors"/> for the <see cref="ColorScheme"/>.
		/// </summary>
		/// <param name="foreground">The foreground color.</param>
		/// <param name="background">The background color.</param>
		/// <returns>The attribute for the foreground and background colors.</returns>
		public abstract Attribute MakeColor (Color foreground, Color background);

		/// <summary>
		/// Ensures all <see cref="Attribute"/>s in <see cref="Colors.ColorSchemes"/> are correctly 
		/// initialized by the driver.
		/// </summary>
		/// <remarks>
		/// This method was previsouly named CreateColors. It was reanmed to InitalizeColorSchemes when
		/// <see cref="ConfigurationManager"/> was enabled.
		/// </remarks>
		/// <param name="supportsColors">Flag indicating if colors are supported (not used).</param>
		public void InitalizeColorSchemes (bool supportsColors = true)
		{
			// Ensure all Attributes are initialized by the driver
			foreach (var s in Colors.ColorSchemes) {
				s.Value.Initialize ();
			}

			if (!supportsColors) {
				return;
			}

		}

		internal void SetAttribute (object attribute)
		{
			throw new NotImplementedException ();
		}
	}

	/// <summary>
	/// Helper class for console drivers to invoke shell commands to interact with the clipboard.
	/// Used primarily by CursesDriver, but also used in Unit tests which is why it is in
	/// ConsoleDriver.cs.
	/// </summary>
	internal static class ClipboardProcessRunner {
		public static (int exitCode, string result) Bash (string commandLine, string inputText = "", bool waitForOutput = false)
		{
			var arguments = $"-c \"{commandLine}\"";
			var (exitCode, result) = Process ("bash", arguments, inputText, waitForOutput);

			return (exitCode, result.TrimEnd ());
		}

		public static (int exitCode, string result) Process (string cmd, string arguments, string input = null, bool waitForOutput = true)
		{
			var output = string.Empty;

			using (Process process = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = cmd,
					Arguments = arguments,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					RedirectStandardInput = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				}
			}) {
				var eventHandled = new TaskCompletionSource<bool> ();
				process.Start ();
				if (!string.IsNullOrEmpty (input)) {
					process.StandardInput.Write (input);
					process.StandardInput.Close ();
				}

				if (!process.WaitForExit (5000)) {
					var timeoutError = $@"Process timed out. Command line: {process.StartInfo.FileName} {process.StartInfo.Arguments}.";
					throw new TimeoutException (timeoutError);
				}

				if (waitForOutput && process.StandardOutput.Peek () != -1) {
					output = process.StandardOutput.ReadToEnd ();
				}

				if (process.ExitCode > 0) {
					output = $@"Process failed to run. Command line: {cmd} {arguments}.
										Output: {output}
										Error: {process.StandardError.ReadToEnd ()}";
				}

				return (process.ExitCode, output);
			}
		}

		public static bool DoubleWaitForExit (this System.Diagnostics.Process process)
		{
			var result = process.WaitForExit (500);
			if (result) {
				process.WaitForExit ();
			}
			return result;
		}

		public static bool FileExists (this string value)
		{
			return !string.IsNullOrEmpty (value) && !value.Contains ("not found");
		}
	}
}
