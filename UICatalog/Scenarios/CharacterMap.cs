#define DRAW_CONTENT
//#define BASE_DRAW_CONTENT

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using Terminal.Gui;
using static Terminal.Gui.TableView;

namespace UICatalog.Scenarios;

/// <summary>
/// This Scenario demonstrates building a custom control (a class deriving from View) that:
///   - Provides a "Character Map" application (like Windows' charmap.exe).
///   - Helps test unicode character rendering in Terminal.Gui
///   - Illustrates how to use ScrollView to do infinite scrolling
/// </summary>
[ScenarioMetadata (Name: "Character Map", Description: "Unicode viewer demonstrating the ScrollView control.")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("ScrollView")]
public class CharacterMap : Scenario {
	CharMap _charMap;
	Label _errorLabel;
	TableView _categoryList;

	public override void Setup ()
	{
		_charMap = new CharMap () {
			X = 0,
			Y = 0,
			Height = Dim.Fill ()
		};
		Win.Add (_charMap);

		var jumpLabel = new Label ("Jump To Code Point:") { X = Pos.Right (_charMap) + 1, Y = Pos.Y (_charMap) };
		Win.Add (jumpLabel);
		var jumpEdit = new TextField () { X = Pos.Right (jumpLabel) + 1, Y = Pos.Y (_charMap), Width = 10, Caption = "e.g. 01BE3" };
		Win.Add (jumpEdit);
		_errorLabel = new Label ("") { X = Pos.Right (jumpEdit) + 1, Y = Pos.Y (_charMap), ColorScheme = Colors.ColorSchemes ["error"] };
		Win.Add (_errorLabel);

		jumpEdit.TextChanged += JumpEdit_TextChanged;

		_categoryList = new TableView () {
			X = Pos.Right (_charMap),
			Y = Pos.Bottom (jumpLabel),
			Height = Dim.Fill ()
		};

		_categoryList.FullRowSelect = true;
		//jumpList.Style.ShowHeaders = false;
		//jumpList.Style.ShowHorizontalHeaderOverline = false;
		//jumpList.Style.ShowHorizontalHeaderUnderline = false;
		_categoryList.Style.ShowHorizontalBottomline = true;
		//jumpList.Style.ShowVerticalCellLines = false;
		//jumpList.Style.ShowVerticalHeaderLines = false;
		_categoryList.Style.AlwaysShowHeaders = true;

		var isDescending = false;

		_categoryList.Table = CreateCategoryTable (0, isDescending);

		// if user clicks the mouse in TableView
		_categoryList.MouseClick += (s, e) => {
			_categoryList.ScreenToCell (e.MouseEvent.X, e.MouseEvent.Y, out int? clickedCol);
			if (clickedCol != null && e.MouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked)) {
				var table = (EnumerableTableSource<UnicodeRange>)_categoryList.Table;
				var prevSelection = table.Data.ElementAt (_categoryList.SelectedRow).Category;
				isDescending = !isDescending;

				_categoryList.Table = CreateCategoryTable (clickedCol.Value, isDescending);

				table = (EnumerableTableSource<UnicodeRange>)_categoryList.Table;
				_categoryList.SelectedRow = table.Data
					.Select ((item, index) => new { item, index })
					.FirstOrDefault (x => x.item.Category == prevSelection)?.index ?? -1;
			}
		};

		var longestName = UnicodeRange.Ranges.Max (r => r.Category.GetColumns ());
		_categoryList.Style.ColumnStyles.Add (0, new ColumnStyle () { MaxWidth = longestName, MinWidth = longestName, MinAcceptableWidth = longestName });
		_categoryList.Style.ColumnStyles.Add (1, new ColumnStyle () { MaxWidth = 1, MinWidth = 6 });
		_categoryList.Style.ColumnStyles.Add (2, new ColumnStyle () { MaxWidth = 1, MinWidth = 6 });

		_categoryList.Width = _categoryList.Style.ColumnStyles.Sum (c => c.Value.MinWidth) + 4;

		_categoryList.SelectedCellChanged += (s, args) => {
			EnumerableTableSource<UnicodeRange> table = (EnumerableTableSource<UnicodeRange>)_categoryList.Table;
			_charMap.StartCodePoint = table.Data.ToArray () [args.NewRow].Start;
		};

		Win.Add (_categoryList);

		_charMap.SelectedCodePoint = 0;
		//jumpList.Refresh ();
		_charMap.SetFocus ();

		_charMap.Width = Dim.Fill () - _categoryList.Width;
	}


	EnumerableTableSource<UnicodeRange> CreateCategoryTable (int sortByColumn, bool descending)
	{
		Func<UnicodeRange, object> orderBy;
		var categorySort = string.Empty;
		var startSort = string.Empty;
		var endSort = string.Empty;

		var sortIndicator = descending ? CM.Glyphs.DownArrow.ToString () : CM.Glyphs.UpArrow.ToString ();
		switch (sortByColumn) {
		case 0:
			orderBy = r => r.Category;
			categorySort = sortIndicator;
			break;
		case 1:
			orderBy = r => r.Start;
			startSort = sortIndicator;
			break;
		case 2:
			orderBy = r => r.End;
			endSort = sortIndicator;
			break;
		default:
			throw new ArgumentException ("Invalid column number.");
		}

		IOrderedEnumerable<UnicodeRange> sortedRanges = descending ?
			UnicodeRange.Ranges.OrderByDescending (orderBy) :
			UnicodeRange.Ranges.OrderBy (orderBy);

		return new EnumerableTableSource<UnicodeRange> (sortedRanges, new Dictionary<string, Func<UnicodeRange, object>> ()
		{
			{ $"Category{categorySort}", s => s.Category },
			{ $"Start{startSort}", s => $"{s.Start:x5}" },
			{ $"End{endSort}", s => $"{s.End:x5}" },
		});
	}

	private void JumpEdit_TextChanged (object sender, TextChangedEventArgs e)
	{
		var jumpEdit = sender as TextField;
		if (jumpEdit.Text.Length == 0) {
			return;
		}
		uint result = 0;

		if (jumpEdit.Text.StartsWith ("U+", StringComparison.OrdinalIgnoreCase) || jumpEdit.Text.StartsWith ("\\u")) {
			try {
				result = uint.Parse (jumpEdit.Text [2..^0], NumberStyles.HexNumber);
			} catch (FormatException) {
				_errorLabel.Text = $"Invalid hex value";
				return;
			}
		} else if (jumpEdit.Text.StartsWith ("0", StringComparison.OrdinalIgnoreCase) || jumpEdit.Text.StartsWith ("\\u")) {
			try {
				result = uint.Parse (jumpEdit.Text, NumberStyles.HexNumber);
			} catch (FormatException) {
				_errorLabel.Text = $"Invalid hex value";
				return;
			}
		} else {
			try {
				result = uint.Parse (jumpEdit.Text, NumberStyles.Integer);
			} catch (FormatException) {
				_errorLabel.Text = $"Invalid value";
				return;
			}
		}
		if (result > RuneExtensions.MaxUnicodeCodePoint) {
			_errorLabel.Text = $"Beyond maximum codepoint";
			return;
		}
		_errorLabel.Text = $"U+{result:x4}";

		var table = (EnumerableTableSource<UnicodeRange>)_categoryList.Table;
		_categoryList.SelectedRow = table.Data
			.Select ((item, index) => new { item, index })
			.FirstOrDefault (x => x.item.Start <= result && x.item.End >= result)?.index ?? -1;
		_categoryList.EnsureSelectedCellIsVisible ();

		// Ensure the typed glyph is selected 
		_charMap.SelectedCodePoint = (int)result;
	}
}

class CharMap : ScrollView {

	/// <summary>
	/// Specifies the starting offset for the character map. The default is 0x2500 
	/// which is the Box Drawing characters.
	/// </summary>
	public int StartCodePoint {
		get => _start;
		set {
			_start = value;
			_selected = value;
			ContentOffset = new Point (0, (int)(_start / 16));
			SetNeedsDisplay ();
		}
	}

	public event EventHandler<ListViewItemEventArgs> SelectedCodePointChanged;

	/// <summary>
	/// Specifies the starting offset for the character map. The default is 0x2500 
	/// which is the Box Drawing characters.
	/// </summary>
	public int SelectedCodePoint {
		get => _selected;
		set {
			_selected = value;
			var col = Cursor.X;
			var row = Cursor.Y;
			var height = (Bounds.Height / ROW_HEIGHT) - (ShowHorizontalScrollIndicator ? 2 : 1);
			if (row + ContentOffset.Y < 0) {
				// Moving up.
				ContentOffset = new Point (ContentOffset.X, row);
			} else if (row + ContentOffset.Y >= height) {
				// Moving down.
				ContentOffset = new Point (ContentOffset.X, Math.Min (row, row - height + ROW_HEIGHT));
			}
			var width = (Bounds.Width / COLUMN_WIDTH * COLUMN_WIDTH) - (ShowVerticalScrollIndicator ? RowLabelWidth + 1 : RowLabelWidth);
			if (col + ContentOffset.X < 0) {
				// Moving left.
				ContentOffset = new Point (col, ContentOffset.Y);
			} else if (col + ContentOffset.X >= width) {
				// Moving right.
				ContentOffset = new Point (Math.Min (col, col - width + COLUMN_WIDTH), ContentOffset.Y);
			}
			SetNeedsDisplay ();
			SelectedCodePointChanged?.Invoke (this, new ListViewItemEventArgs (SelectedCodePoint, null));
		}
	}

	public Point Cursor {
		get {
			var row = SelectedCodePoint / 16;
			var col = (SelectedCodePoint - row * 16) * COLUMN_WIDTH;
			return new Point (col, row);
		}
		set => throw new NotImplementedException ();
	}

	public override void PositionCursor ()
	{
		if (HasFocus && Cursor.X + ContentOffset.X + RowLabelWidth + 1 >= RowLabelWidth &&
			Cursor.X + ContentOffset.X + RowLabelWidth + 1 < Bounds.Width - (ShowVerticalScrollIndicator ? 1 : 0) &&
			Cursor.Y + ContentOffset.Y + 1 > 0 &&
			Cursor.Y + ContentOffset.Y + 1 < Bounds.Height - (ShowHorizontalScrollIndicator ? 1 : 0)) {

			Driver.SetCursorVisibility (CursorVisibility.Default);
			Move (Cursor.X + ContentOffset.X + RowLabelWidth + 1, Cursor.Y + ContentOffset.Y + 1);
		} else {
			Driver.SetCursorVisibility (CursorVisibility.Invisible);
		}
	}


	int _start = 0;
	int _selected = 0;

	public const int COLUMN_WIDTH = 3;
	public const int ROW_HEIGHT = 1;

	public static int MaxCodePoint => 0x10FFFF;

	public static int RowLabelWidth => $"U+{MaxCodePoint:x5}".Length + 1;
	public static int RowWidth => RowLabelWidth + (COLUMN_WIDTH * 16);

	public CharMap ()
	{
		ColorScheme = Colors.Dialog;
		CanFocus = true;
		ContentSize = new Size (CharMap.RowWidth, (int)(MaxCodePoint / 16 + (ShowHorizontalScrollIndicator ? 2 : 1)));

		AddCommand (Command.ScrollUp, () => {
			if (SelectedCodePoint >= 16) {
				SelectedCodePoint -= 16;
			}
			return true;
		});
		AddCommand (Command.ScrollDown, () => {
			if (SelectedCodePoint < MaxCodePoint - 16) {
				SelectedCodePoint += 16;
			}
			return true;
		});
		AddCommand (Command.ScrollLeft, () => {
			if (SelectedCodePoint > 0) {
				SelectedCodePoint--;
			}
			return true;
		});
		AddCommand (Command.ScrollRight, () => {
			if (SelectedCodePoint < MaxCodePoint) {
				SelectedCodePoint++;
			}
			return true;
		});
		AddCommand (Command.PageUp, () => {
			var page = (Bounds.Height / ROW_HEIGHT - 1) * 16;
			SelectedCodePoint -= Math.Min (page, SelectedCodePoint);
			return true;
		});
		AddCommand (Command.PageDown, () => {
			var page = (Bounds.Height / ROW_HEIGHT - 1) * 16;
			SelectedCodePoint += Math.Min (page, MaxCodePoint - SelectedCodePoint);
			return true;
		});
		AddCommand (Command.TopHome, () => {
			SelectedCodePoint = 0;
			return true;
		});
		AddCommand (Command.BottomEnd, () => {
			SelectedCodePoint = MaxCodePoint;
			return true;
		});
		AddKeyBinding (Key.Enter, Command.Accept);
		AddCommand (Command.Accept, () => {
			ShowDetails ();
			return true;
		});

		MouseClick += Handle_MouseClick;
	}

	private void CopyCodePoint () => Clipboard.Contents = $"U+{SelectedCodePoint:x5}";
	private void CopyGlyph () => Clipboard.Contents = $"{new Rune (SelectedCodePoint)}";

	public override void OnDrawContent (Rect contentArea)
	{
		if (ShowHorizontalScrollIndicator && ContentSize.Height < (int)(MaxCodePoint / 16 + 2)) {
			ContentSize = new Size (CharMap.RowWidth, (int)(MaxCodePoint / 16 + 2));
			var width = (Bounds.Width / COLUMN_WIDTH * COLUMN_WIDTH) - (ShowVerticalScrollIndicator ? RowLabelWidth + 1 : RowLabelWidth);
			if (Cursor.X + ContentOffset.X >= width) {
				// Snap to the selected glyph.
				ContentOffset = new Point (Math.Min (Cursor.X, Cursor.X - width + COLUMN_WIDTH), ContentOffset.Y == -ContentSize.Height + Bounds.Height ? ContentOffset.Y - 1 : ContentOffset.Y);
			} else {
				ContentOffset = new Point (ContentOffset.X - Cursor.X, ContentOffset.Y == -ContentSize.Height + Bounds.Height ? ContentOffset.Y - 1 : ContentOffset.Y);
			}
		} else if (!ShowHorizontalScrollIndicator && ContentSize.Height > (int)(MaxCodePoint / 16 + 1)) {
			ContentSize = new Size (CharMap.RowWidth, (int)(MaxCodePoint / 16 + 1));
			// Snap 1st column into view if it's been scrolled horizontally
			ContentOffset = new Point (0, ContentOffset.Y < -ContentSize.Height + Bounds.Height ? ContentOffset.Y - 1 : ContentOffset.Y);
		}
		base.OnDrawContent (contentArea);
	}

	//public void CharMap_DrawContent (object s, DrawEventArgs a)
	public override void OnDrawContentComplete (Rect contentArea)
	{
		Rect viewport = new Rect (ContentOffset,
			new Size (Math.Max (Bounds.Width - (ShowVerticalScrollIndicator ? 1 : 0), 0),
				Math.Max (Bounds.Height - (ShowHorizontalScrollIndicator ? 1 : 0), 0)));

		var oldClip = ClipToBounds ();
		if (ShowHorizontalScrollIndicator) {
			// ClipToBounds doesn't know about the scroll indicators, so if off, subtract one from height
			Driver.Clip = new Rect (Driver.Clip.Location, new Size (Driver.Clip.Width, Driver.Clip.Height - 1));
		}
		if (ShowVerticalScrollIndicator) {
			// ClipToBounds doesn't know about the scroll indicators, so if off, subtract one from width
			Driver.Clip = new Rect (Driver.Clip.Location, new Size (Driver.Clip.Width - 1, Driver.Clip.Height));
		}

		var cursorCol = Cursor.X;
		var cursorRow = Cursor.Y;

		Driver.SetAttribute (GetHotNormalColor ());
		Move (0, 0);
		Driver.AddStr (new string (' ', RowLabelWidth + 1));
		for (int hexDigit = 0; hexDigit < 16; hexDigit++) {
			var x = ContentOffset.X + RowLabelWidth + (hexDigit * COLUMN_WIDTH);
			if (x > RowLabelWidth - 2) {
				Move (x, 0);
				Driver.SetAttribute (GetHotNormalColor ());
				Driver.AddStr (" ");
				Driver.SetAttribute (HasFocus && (cursorCol + RowLabelWidth == x) ? ColorScheme.HotFocus : GetHotNormalColor ());
				Driver.AddStr ($"{hexDigit:x}");
				Driver.SetAttribute (GetHotNormalColor ());
				Driver.AddStr (" ");
			}
		}

		var firstColumnX = viewport.X + RowLabelWidth;
		for (int row = -ContentOffset.Y, y = 1; row <= (-ContentOffset.Y) + (Bounds.Height / ROW_HEIGHT); row++, y += ROW_HEIGHT) {
			var val = (row) * 16;
			if (val > MaxCodePoint) {
				continue;
			}
			Move (firstColumnX + COLUMN_WIDTH, y);
			Driver.SetAttribute (GetNormalColor ());
			for (int col = 0; col < 16; col++) {
				var x = firstColumnX + COLUMN_WIDTH * col + 1;
				Move (x, y);
				if (cursorRow + ContentOffset.Y + 1 == y && cursorCol + ContentOffset.X + firstColumnX + 1 == x && !HasFocus) {
					Driver.SetAttribute (GetFocusColor ());
				}

				if (char.IsSurrogate ((char)(val + col))) {
					Driver.AddRune (Rune.ReplacementChar);
				} else {
					Driver.AddRune (new Rune (val + col));
				}

				if (cursorRow + ContentOffset.Y + 1 == y && cursorCol + ContentOffset.X + firstColumnX + 1 == x && !HasFocus) {
					Driver.SetAttribute (GetNormalColor ());
				}
			}
			Move (0, y);
			Driver.SetAttribute (HasFocus && (cursorRow + ContentOffset.Y + 1 == y) ? ColorScheme.HotFocus : ColorScheme.HotNormal);
			var rowLabel = $"U+{val / 16:x5}_ ";
			Driver.AddStr (rowLabel);
		}
		Driver.Clip = oldClip;
	}

	ContextMenu _contextMenu = new ContextMenu ();
	void Handle_MouseClick (object sender, MouseEventEventArgs args)
	{
		var me = args.MouseEvent;
		if (me.Flags == MouseFlags.ReportMousePosition || (me.Flags != MouseFlags.Button1Clicked &&
			me.Flags != MouseFlags.Button1DoubleClicked)) { // && me.Flags != _contextMenu.MouseFlags)) {
			return;
		}

		if (me.X < RowLabelWidth) {
			return;
		}

		if (me.Y < 1) {
			return;
		}

		var row = me.Y - 1;
		var col = (me.X - RowLabelWidth - ContentOffset.X) / COLUMN_WIDTH;
		if (row < 0 || row > Bounds.Height || col < 0 || col > 15) {
			return;
		}

		var val = (row - ContentOffset.Y) * 16 + col;
		if (val > MaxCodePoint) {
			return;
		}

		if (me.Flags == MouseFlags.Button1Clicked) {
			SelectedCodePoint = val;
			return;
		}

		if (me.Flags == MouseFlags.Button1DoubleClicked) {
			SelectedCodePoint = val;
			ShowDetails ();
			return;
		}

		if (me.Flags == _contextMenu.MouseFlags) {
			SelectedCodePoint = val;
			_contextMenu = new ContextMenu (me.X + 1, me.Y + 1,
				new MenuBarItem (new MenuItem [] {
					new MenuItem ("_Copy Glyph", "", () => CopyGlyph (), null, null, Key.C | Key.CtrlMask),
					new MenuItem ("Copy Code _Point", "", () => CopyCodePoint (), null, null, Key.C | Key.ShiftMask | Key.CtrlMask),
				}) {

				}
			);
			_contextMenu.Show ();
		}
	}

	public static string ToCamelCase (string str)
	{
		if (string.IsNullOrEmpty (str)) {
			return str;
		}

		TextInfo textInfo = new CultureInfo ("en-US", false).TextInfo;

		str = textInfo.ToLower (str);
		str = textInfo.ToTitleCase (str);

		return str;
	}

	void ShowDetails ()
	{
		var client = new UcdApiClient ();
		string decResponse = string.Empty;

		var waitIndicator = new Dialog (new Button ("Cancel")) {
			Title = "Getting Code Point Information",
			X = Pos.Center (),
			Y = Pos.Center (),
			Height = 7,
			Width = 50
		};
		var errorLabel = new Label () {
			Text = UcdApiClient.BaseUrl,
			AutoSize = false,
			X = 0,
			Y = 1,
			Width = Dim.Fill (),
			Height = Dim.Fill (1),
			TextAlignment = TextAlignment.Centered
		};
		var spinner = new SpinnerView () {
			X = Pos.Center (),
			Y = Pos.Center (),
			Style = new SpinnerStyle.Aesthetic (),

		};
		spinner.AutoSpin = true;
		waitIndicator.Add (errorLabel);
		waitIndicator.Add (spinner);
		waitIndicator.Ready += async (s, a) => {
			try {
				decResponse = await client.GetCodepointDec ((int)SelectedCodePoint);
			} catch (HttpRequestException e) {
				(s as Dialog).Text = e.Message;
				Application.MainLoop.Invoke (() => {
					spinner.Visible = false;
					errorLabel.Text = e.Message;
					errorLabel.ColorScheme = Colors.ColorSchemes ["Error"];
					errorLabel.Visible = true;
				});
			}
			(s as Dialog)?.RequestStop ();
		};
		Application.Run (waitIndicator);

		if (!string.IsNullOrEmpty (decResponse)) {
			string name = string.Empty;

			using (JsonDocument document = JsonDocument.Parse (decResponse)) {
				JsonElement root = document.RootElement;

				// Get a property by name and output its value
				if (root.TryGetProperty ("name", out JsonElement nameElement)) {
					name = nameElement.GetString ();
				}

				//// Navigate to a nested property and output its value
				//if (root.TryGetProperty ("property3", out JsonElement property3Element)
				//&& property3Element.TryGetProperty ("nestedProperty", out JsonElement nestedPropertyElement)) {
				//	Console.WriteLine (nestedPropertyElement.GetString ());
				//}
			}

			var title = $"{ToCamelCase (name)} - {new Rune (SelectedCodePoint)} U+{SelectedCodePoint:x4}";
			switch (MessageBox.Query (title, decResponse, "Copy _Glyph", "Copy Code _Point", "Cancel")) {
			case 0:
				CopyGlyph ();
				break;
			case 1:
				CopyCodePoint ();
				break;
			}
		} else {
			MessageBox.ErrorQuery ("Code Point API", $"{UcdApiClient.BaseUrl} did not return a result.", "Ok");
		}
		// BUGBUG: This is a workaround for some weird ScrollView related mouse grab bug
		Application.GrabMouse (this);
	}


	public override bool OnEnter (View view)
	{
		if (IsInitialized) {
			Application.Driver.SetCursorVisibility (CursorVisibility.Default);
		}
		return base.OnEnter (view);
	}
}

public class UcdApiClient {
	private static readonly HttpClient httpClient = new HttpClient ();
	public const string BaseUrl = "https://ucdapi.org/unicode/latest/";

	public async Task<string> GetCodepointHex (string hex)
	{
		var response = await httpClient.GetAsync ($"{BaseUrl}codepoint/hex/{hex}");
		response.EnsureSuccessStatusCode ();
		return await response.Content.ReadAsStringAsync ();
	}

	public async Task<string> GetCodepointDec (int dec)
	{
		var response = await httpClient.GetAsync ($"{BaseUrl}codepoint/dec/{dec}");
		response.EnsureSuccessStatusCode ();
		return await response.Content.ReadAsStringAsync ();
	}

	public async Task<string> GetChars (string chars)
	{
		var response = await httpClient.GetAsync ($"{BaseUrl}chars/{Uri.EscapeDataString (chars)}");
		response.EnsureSuccessStatusCode ();
		return await response.Content.ReadAsStringAsync ();
	}

	public async Task<string> GetCharsName (string chars)
	{
		var response = await httpClient.GetAsync ($"{BaseUrl}chars/{Uri.EscapeDataString (chars)}/name");
		response.EnsureSuccessStatusCode ();
		return await response.Content.ReadAsStringAsync ();
	}
}


class UnicodeRange {
	public int Start;
	public int End;
	public string Category;
	public UnicodeRange (int start, int end, string category)
	{
		this.Start = start;
		this.End = end;
		this.Category = category;
	}

	public static List<UnicodeRange> GetRanges ()
	{
		var ranges = (from r in typeof (UnicodeRanges).GetProperties (System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
			      let urange = r.GetValue (null) as System.Text.Unicode.UnicodeRange
			      let name = string.IsNullOrEmpty (r.Name) ? $"U+{urange.FirstCodePoint:x5}-U+{urange.FirstCodePoint + urange.Length:x5}" : r.Name
			      where name != "None" && name != "All"
			      select new UnicodeRange (urange.FirstCodePoint, urange.FirstCodePoint + urange.Length, name));

		// .NET 8.0 only supports BMP in UnicodeRanges: https://learn.microsoft.com/en-us/dotnet/api/system.text.unicode.unicoderanges?view=net-8.0
		var nonBmpRanges = new List<UnicodeRange> {

			new UnicodeRange (0x1F130, 0x1F149   ,"Squared Latin Capital Letters"),
			new UnicodeRange (0x12400, 0x1240f   ,"Cuneiform Numbers and Punctuation"),
			new UnicodeRange (0x1FA00, 0x1FA0f   ,"Chess Symbols"),
			new UnicodeRange (0x10000, 0x1007F   ,"Linear B Syllabary"),
			new UnicodeRange (0x10080, 0x100FF   ,"Linear B Ideograms"),
			new UnicodeRange (0x10100, 0x1013F   ,"Aegean Numbers"),
			new UnicodeRange (0x10300, 0x1032F   ,"Old Italic"),
			new UnicodeRange (0x10330, 0x1034F   ,"Gothic"),
			new UnicodeRange (0x10380, 0x1039F   ,"Ugaritic"),
			new UnicodeRange (0x10400, 0x1044F   ,"Deseret"),
			new UnicodeRange (0x10450, 0x1047F   ,"Shavian"),
			new UnicodeRange (0x10480, 0x104AF   ,"Osmanya"),
			new UnicodeRange (0x10800, 0x1083F   ,"Cypriot Syllabary"),
			new UnicodeRange (0x1D000, 0x1D0FF   ,"Byzantine Musical Symbols"),
			new UnicodeRange (0x1D100, 0x1D1FF   ,"Musical Symbols"),
			new UnicodeRange (0x1D300, 0x1D35F   ,"Tai Xuan Jing Symbols"),
			new UnicodeRange (0x1D400, 0x1D7FF   ,"Mathematical Alphanumeric Symbols"),
			new UnicodeRange (0x1F600, 0x1F532   ,"Emojis Symbols"),
			new UnicodeRange (0x20000, 0x2A6DF   ,"CJK Unified Ideographs Extension B"),
			new UnicodeRange (0x2F800, 0x2FA1F   ,"CJK Compatibility Ideographs Supplement"),
			new UnicodeRange (0xE0000, 0xE007F   ,"Tags"),
		};

		return ranges.Concat (nonBmpRanges).OrderBy (r => r.Category).ToList ();
	}

	public static List<UnicodeRange> Ranges = GetRanges ();
}