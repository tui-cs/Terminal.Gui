#nullable enable
using System.Data;
using System.Globalization;
using System.Text;

// ReSharper disable StringLiteralTypo

namespace UICatalog.Scenarios;

[ScenarioMetadata ("TableEditor", "Implements data table editor using the TableView control.")]
[ScenarioCategory ("TableView")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Dialogs")]
[ScenarioCategory ("Text and Formatting")]
public class TableEditor : Scenario
{
    private readonly HashSet<FileSystemInfo>? _checkedFileSystemInfos = [];
    private readonly List<IDisposable>? _toDispose = [];

    private readonly List<UnicodeRange>? _ranges =
    [
        new UnicodeRange (0x0000, 0x001F, "ASCII Control Characters"),
        new UnicodeRange (0x0080, 0x009F, "C0 Control Characters"),
        new UnicodeRange (0x1100, 0x11ff, "Hangul Jamo"), // This is where wide chars tend to start

        new UnicodeRange (0x20A0, 0x20CF, "Currency Symbols"),
        new UnicodeRange (0x2100, 0x214F, "Letterlike Symbols"),
        new UnicodeRange (0x2190, 0x21ff, "Arrows"),
        new UnicodeRange (0x2200, 0x22ff, "Mathematical symbols"),
        new UnicodeRange (0x2300, 0x23ff, "Miscellaneous Technical"),
        new UnicodeRange (0x2500, 0x25ff, "Box Drawing & Geometric Shapes"),
        new UnicodeRange (0x2600, 0x26ff, "Miscellaneous Symbols"),
        new UnicodeRange (0x2700, 0x27ff, "Dingbats"),
        new UnicodeRange (0x2800, 0x28ff, "Braille"),
        new UnicodeRange (0x2b00, 0x2bff, "Miscellaneous Symbols and Arrows"),
        new UnicodeRange (0xFB00, 0xFb4f, "Alphabetic Presentation Forms"),
        new UnicodeRange (0x12400, 0x1240f, "Cuneiform Numbers and Punctuation"),
        new UnicodeRange ((uint)(Terminal.Gui.Views.UnicodeRange.Ranges.Max (r => r.End) - 16),
                          (uint)Terminal.Gui.Views.UnicodeRange.Ranges.Max (r => r.End),
                          "End"),
        new UnicodeRange (0x0020, 0x007F, "Basic Latin"),
        new UnicodeRange (0x00A0, 0x00FF, "Latin-1 Supplement"),
        new UnicodeRange (0x0100, 0x017F, "Latin Extended-A"),
        new UnicodeRange (0x0180, 0x024F, "Latin Extended-B"),
        new UnicodeRange (0x0250, 0x02AF, "IPA Extensions"),
        new UnicodeRange (0x02B0, 0x02FF, "Spacing Modifier Letters"),
        new UnicodeRange (0x0300, 0x036F, "Combining Diacritical Marks"),
        new UnicodeRange (0x0370, 0x03FF, "Greek and Coptic"),
        new UnicodeRange (0x0400, 0x04FF, "Cyrillic"),
        new UnicodeRange (0x0500, 0x052F, "Cyrillic Supplementary"),
        new UnicodeRange (0x0530, 0x058F, "Armenian"),
        new UnicodeRange (0x0590, 0x05FF, "Hebrew"),
        new UnicodeRange (0x0600, 0x06FF, "Arabic"),
        new UnicodeRange (0x0700, 0x074F, "Syriac"),
        new UnicodeRange (0x0780, 0x07BF, "Thaana"),
        new UnicodeRange (0x0900, 0x097F, "Devanagari"),
        new UnicodeRange (0x0980, 0x09FF, "Bengali"),
        new UnicodeRange (0x0A00, 0x0A7F, "Gurmukhi"),
        new UnicodeRange (0x0A80, 0x0AFF, "Gujarati"),
        new UnicodeRange (0x0B00, 0x0B7F, "Oriya"),
        new UnicodeRange (0x0B80, 0x0BFF, "Tamil"),
        new UnicodeRange (0x0C00, 0x0C7F, "Telugu"),
        new UnicodeRange (0x0C80, 0x0CFF, "Kannada"),
        new UnicodeRange (0x0D00, 0x0D7F, "Malayalam"),
        new UnicodeRange (0x0D80, 0x0DFF, "Sinhala"),
        new UnicodeRange (0x0E00, 0x0E7F, "Thai"),
        new UnicodeRange (0x0E80, 0x0EFF, "Lao"),
        new UnicodeRange (0x0F00, 0x0FFF, "Tibetan"),
        new UnicodeRange (0x1000, 0x109F, "Myanmar"),
        new UnicodeRange (0x10A0, 0x10FF, "Georgian"),
        new UnicodeRange (0x1100, 0x11FF, "Hangul Jamo"),
        new UnicodeRange (0x1200, 0x137F, "Ethiopic"),
        new UnicodeRange (0x13A0, 0x13FF, "Cherokee"),
        new UnicodeRange (0x1400, 0x167F, "Unified Canadian Aboriginal Syllabics"),
        new UnicodeRange (0x1680, 0x169F, "Ogham"),
        new UnicodeRange (0x16A0, 0x16FF, "Runic"),
        new UnicodeRange (0x1700, 0x171F, "Tagalog"),
        new UnicodeRange (0x1720, 0x173F, "Hanunoo"),
        new UnicodeRange (0x1740, 0x175F, "Buhid"),
        new UnicodeRange (0x1760, 0x177F, "Tagbanwa"),
        new UnicodeRange (0x1780, 0x17FF, "Khmer"),
        new UnicodeRange (0x1800, 0x18AF, "Mongolian"),
        new UnicodeRange (0x1900, 0x194F, "Limbu"),
        new UnicodeRange (0x1950, 0x197F, "Tai Le"),
        new UnicodeRange (0x19E0, 0x19FF, "Khmer Symbols"),
        new UnicodeRange (0x1D00, 0x1D7F, "Phonetic Extensions"),
        new UnicodeRange (0x1E00, 0x1EFF, "Latin Extended Additional"),
        new UnicodeRange (0x1F00, 0x1FFF, "Greek Extended"),
        new UnicodeRange (0x2000, 0x206F, "General Punctuation"),
        new UnicodeRange (0x2070, 0x209F, "Superscripts and Subscripts"),
        new UnicodeRange (0x20A0, 0x20CF, "Currency Symbols"),
        new UnicodeRange (0x20D0, 0x20FF, "Combining Diacritical Marks for Symbols"),
        new UnicodeRange (0x2100, 0x214F, "Letterlike Symbols"),
        new UnicodeRange (0x2150, 0x218F, "Number Forms"),
        new UnicodeRange (0x2190, 0x21FF, "Arrows"),
        new UnicodeRange (0x2200, 0x22FF, "Mathematical Operators"),
        new UnicodeRange (0x2300, 0x23FF, "Miscellaneous Technical"),
        new UnicodeRange (0x2400, 0x243F, "Control Pictures"),
        new UnicodeRange (0x2440, 0x245F, "Optical Character Recognition"),
        new UnicodeRange (0x2460, 0x24FF, "Enclosed Alphanumerics"),
        new UnicodeRange (0x2500, 0x257F, "Box Drawing"),
        new UnicodeRange (0x2580, 0x259F, "Block Elements"),
        new UnicodeRange (0x25A0, 0x25FF, "Geometric Shapes"),
        new UnicodeRange (0x2600, 0x26FF, "Miscellaneous Symbols"),
        new UnicodeRange (0x2700, 0x27BF, "Dingbats"),
        new UnicodeRange (0x27C0, 0x27EF, "Miscellaneous Mathematical Symbols-A"),
        new UnicodeRange (0x27F0, 0x27FF, "Supplemental Arrows-A"),
        new UnicodeRange (0x2800, 0x28FF, "Braille Patterns"),
        new UnicodeRange (0x2900, 0x297F, "Supplemental Arrows-B"),
        new UnicodeRange (0x2980, 0x29FF, "Miscellaneous Mathematical Symbols-B"),
        new UnicodeRange (0x2A00, 0x2AFF, "Supplemental Mathematical Operators"),
        new UnicodeRange (0x2B00, 0x2BFF, "Miscellaneous Symbols and Arrows"),
        new UnicodeRange (0x2E80, 0x2EFF, "CJK Radicals Supplement"),
        new UnicodeRange (0x2F00, 0x2FDF, "Kangxi Radicals"),
        new UnicodeRange (0x2FF0, 0x2FFF, "Ideographic Description Characters"),
        new UnicodeRange (0x3000, 0x303F, "CJK Symbols and Punctuation"),
        new UnicodeRange (0x3040, 0x309F, "Hiragana"),
        new UnicodeRange (0x30A0, 0x30FF, "Katakana"),
        new UnicodeRange (0x3100, 0x312F, "Bopomofo"),
        new UnicodeRange (0x3130, 0x318F, "Hangul Compatibility Jamo"),
        new UnicodeRange (0x3190, 0x319F, "Kanbun"),
        new UnicodeRange (0x31A0, 0x31BF, "Bopomofo Extended"),
        new UnicodeRange (0x31F0, 0x31FF, "Katakana Phonetic Extensions"),
        new UnicodeRange (0x3200, 0x32FF, "Enclosed CJK Letters and Months"),
        new UnicodeRange (0x3300, 0x33FF, "CJK Compatibility"),
        new UnicodeRange (0x3400, 0x4DBF, "CJK Unified Ideographs Extension A"),
        new UnicodeRange (0x4DC0, 0x4DFF, "Yijing Hexagram Symbols"),
        new UnicodeRange (0x4E00, 0x9FFF, "CJK Unified Ideographs"),
        new UnicodeRange (0xA000, 0xA48F, "Yi Syllables"),
        new UnicodeRange (0xA490, 0xA4CF, "Yi Radicals"),
        new UnicodeRange (0xAC00, 0xD7AF, "Hangul Syllables"),
        new UnicodeRange (0xD800, 0xDB7F, "High Surrogates"),
        new UnicodeRange (0xDB80, 0xDBFF, "High Private Use Surrogates"),
        new UnicodeRange (0xDC00, 0xDFFF, "Low Surrogates"),
        new UnicodeRange (0xE000, 0xF8FF, "Private Use Area"),
        new UnicodeRange (0xF900, 0xFAFF, "CJK Compatibility Ideographs"),
        new UnicodeRange (0xFB00, 0xFB4F, "Alphabetic Presentation Forms"),
        new UnicodeRange (0xFB50, 0xFDFF, "Arabic Presentation Forms-A"),
        new UnicodeRange (0xFE00, 0xFE0F, "Variation Selectors"),
        new UnicodeRange (0xFE20, 0xFE2F, "Combining Half Marks"),
        new UnicodeRange (0xFE30, 0xFE4F, "CJK Compatibility Forms"),
        new UnicodeRange (0xFE50, 0xFE6F, "Small Form Variants"),
        new UnicodeRange (0xFE70, 0xFEFF, "Arabic Presentation Forms-B"),
        new UnicodeRange (0xFF00, 0xFFEF, "Halfwidth and Fullwidth Forms"),
        new UnicodeRange (0xFFF0, 0xFFFF, "Specials"),
        new UnicodeRange (0x10000, 0x1007F, "Linear B Syllabary"),
        new UnicodeRange (0x10080, 0x100FF, "Linear B Ideograms"),
        new UnicodeRange (0x10100, 0x1013F, "Aegean Numbers"),
        new UnicodeRange (0x10300, 0x1032F, "Old Italic"),
        new UnicodeRange (0x10330, 0x1034F, "Gothic"),
        new UnicodeRange (0x10380, 0x1039F, "Ugaritic"),
        new UnicodeRange (0x10400, 0x1044F, "Deseret"),
        new UnicodeRange (0x10450, 0x1047F, "Shavian"),
        new UnicodeRange (0x10480, 0x104AF, "Osmanya"),
        new UnicodeRange (0x10800, 0x1083F, "Cypriot Syllabary"),
        new UnicodeRange (0x1D000, 0x1D0FF, "Byzantine Musical Symbols"),
        new UnicodeRange (0x1D100, 0x1D1FF, "Musical Symbols"),
        new UnicodeRange (0x1D300, 0x1D35F, "Tai Xuan Jing Symbols"),
        new UnicodeRange (0x1D400, 0x1D7FF, "Mathematical Alphanumeric Symbols"),
        new UnicodeRange (0x1F600, 0x1F532, "Emojis Symbols"),
        new UnicodeRange (0x20000, 0x2A6DF, "CJK Unified Ideographs Extension B"),
        new UnicodeRange (0x2F800, 0x2FA1F, "CJK Compatibility Ideographs Supplement"),
        new UnicodeRange (0xE0000, 0xE007F, "Tags")
    ];

    private IApplication? _app;

    private Scheme? _alternatingScheme;
    private DataTable? _currentTable;
    private Scheme? _redScheme;
    private Scheme? _redSchemeAlt;
    private TableView? _tableView;

    /// <summary>
    ///     Builds a simple table in which cell values contents are the index of the cell.  This helps to test that
    ///     scrolling etc. is working correctly and not skipping out any rows/columns when paging
    /// </summary>
    /// <param name="cols"></param>
    /// <param name="rows"></param>
    /// <returns></returns>
    public static DataTable BuildSimpleDataTable (int cols, int rows)
    {
        var dt = new DataTable ();

        for (var c = 0; c < cols; c++)
        {
            dt.Columns.Add ("Col" + c);
        }

        for (var r = 0; r < rows; r++)
        {
            DataRow newRow = dt.NewRow ();

            for (var c = 0; c < cols; c++)
            {
                newRow [c] = $"R{r}C{c}";
            }

            dt.Rows.Add (newRow);
        }

        return dt;
    }

    public override void Main ()
    {

        // Init
        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        // Setup - Create a top-level application window and configure it.
        using Runnable appWindow = new ();

        _tableView = new TableView { X = 0, Y = 1, Width = Dim.Fill (), Height = Dim.Fill (1) };

        var menu = new MenuBar ();

        // File menu
        menu.Add (new MenuBarItem (Strings.menuFile,
                                   [
                                       new MenuItem { Title = "_OpenBigExample", Action = () => OpenExample (true) },
                                       new MenuItem { Title = "_OpenSmallExample", Action = () => OpenExample (false) },
                                       new MenuItem { Title = "Open_WideColumnExample", Action = OpenWideColumnExample },
                                       new MenuItem { Title = "OpenCharacter_Map", Action = OpenUnicodeMap },
                                       new MenuItem { Title = "OpenTreeExample", Action = OpenTreeExample },
                                       new MenuItem { Title = "_CloseExample", Action = CloseExample },
                                       new MenuItem { Title = Strings.cmdQuit, Action = Quit }
                                   ]));

        // View menu - created with helper method due to complexity
        menu.Add (CreateViewMenu ());

        // Column menu
        menu.Add (new MenuBarItem ("_Column",
                                   [
                                       new MenuItem { Title = "_Set Max Width", Action = SetMaxWidth },
                                       new MenuItem { Title = "_Set Min Width", Action = SetMinWidth },
                                       new MenuItem { Title = "_Set MinAcceptableWidth", Action = SetMinAcceptableWidth },
                                       new MenuItem { Title = "_Set All MinAcceptableWidth=1", Action = SetMinAcceptableWidthToOne }
                                   ]));

        appWindow.Add (menu);

        var selectedCellLabel = new Label { Text = "0,0" };

        var statusBar = new StatusBar ([
                                           new Shortcut (Application.GetDefaultKey (Command.Quit), "Quit", Quit),
                                           new Shortcut (Key.F2, "OpenExample", () => OpenExample (true)),
                                           new Shortcut (Key.F3, "CloseExample", CloseExample),
                                           new Shortcut (Key.F4, "OpenSimple", () => OpenSimple (true)),
                                           new Shortcut { HelpText = "Cell:", CommandView = selectedCellLabel }
                                       ]) { AlignmentModes = AlignmentModes.IgnoreFirstOrLast };
        appWindow.Add (statusBar);

        appWindow.Add (_tableView);

        _tableView!.ValueChanged += (_, _) => { selectedCellLabel.Text = $"{_tableView!.Value?.SelectedCell.Y ?? 0},{_tableView!.Value?.SelectedCell.X ?? 0}"; };
        _tableView!.Accepted += EditCurrentCell;
        _tableView!.KeyDown += TableViewKeyPress;

        // SetupScrollBar ();

        _redScheme = new Scheme
        {
            Disabled = appWindow.GetAttributeForRole (VisualRole.Disabled),
            HotFocus = appWindow.GetAttributeForRole (VisualRole.HotFocus),
            Focus = appWindow.GetAttributeForRole (VisualRole.Focus),
            Normal = new Attribute (Color.Red, appWindow.GetAttributeForRole (VisualRole.Normal).Background)
        };

        _alternatingScheme = new Scheme
        {
            Disabled = appWindow.GetAttributeForRole (VisualRole.Disabled),
            HotFocus = appWindow.GetAttributeForRole (VisualRole.HotFocus),
            Focus = appWindow.GetAttributeForRole (VisualRole.Focus),
            Normal = new Attribute (Color.White, Color.BrightBlue)
        };

        _redSchemeAlt = new Scheme
        {
            Disabled = appWindow.GetAttributeForRole (VisualRole.Disabled),
            HotFocus = appWindow.GetAttributeForRole (VisualRole.HotFocus),
            Focus = appWindow.GetAttributeForRole (VisualRole.Focus),
            Normal = new Attribute (Color.Red, Color.BrightBlue)
        };

        // if user clicks the mouse in TableView
        _tableView!.Activating += (_, e) =>
                                  {
                                      if (_currentTable == null)
                                      {
                                          return;
                                      }

                                      // Only handle mouse clicks
                                      if (e.Context?.Binding is not MouseBinding { MouseEvent: { } mouse })
                                      {
                                          return;
                                      }

                                      _tableView!.ScreenToCell (mouse.Position!.Value, out int? clickedCol);

                                      if (clickedCol == null)
                                      {
                                          return;
                                      }

                                      if (mouse.Flags.HasFlag (MouseFlags.LeftButtonClicked))
                                      {
                                          // left click in a header
                                          SortColumn (clickedCol.Value);
                                      }
                                      else if (mouse.Flags.HasFlag (MouseFlags.RightButtonClicked))
                                      {
                                          // right click in a header
                                          ShowHeaderContextMenu (clickedCol.Value, mouse);
                                      }
                                  };

        // Run - Start the application.
        app.Run (appWindow);
    }

    protected override void Dispose (bool disposing)
    {
        base.Dispose (disposing);

        foreach (IDisposable d in _toDispose!)
        {
            d.Dispose ();
        }
    }

    private DataTable BuildUnicodeMap ()
    {
        var dt = new DataTable ();

        // add cols called 0 to 9
        for (var i = 0; i < 10; i++)
        {
            DataColumn col = dt.Columns.Add (i.ToString (), typeof (uint));
            ColumnStyle style = _tableView!.Style.GetOrCreateColumnStyle (col.Ordinal);

            style.RepresentationGetter = RuneToString;
        }

        // add cols called a to z
        for (int i = 'a'; i < 'a' + 26; i++)
        {
            DataColumn col = dt.Columns.Add (((char)i).ToString (), typeof (uint));
            ColumnStyle style = _tableView!.Style.GetOrCreateColumnStyle (col.Ordinal);
            style.RepresentationGetter = RuneToString;
        }

        // now add table contents
        List<uint> runes = [];

        foreach (UnicodeRange range in _ranges!)
        {
            for (uint i = range.Start; i <= range.End; i++)
            {
                runes.Add (i);
            }
        }

        DataRow? dr = null;

        for (var i = 0; i < runes.Count; i++)
        {
            if (dr == null || i % dt.Columns.Count == 0)
            {
                dr = dt.Rows.Add ();
            }

            dr [i % dt.Columns.Count] = runes [i];
        }

        return dt;
    }

    private void CheckOrUncheckFile (FileSystemInfo info, bool check)
    {
        if (check)
        {
            _checkedFileSystemInfos!.Add (info);
        }
        else
        {
            _checkedFileSystemInfos!.Remove (info);
        }
    }

    private void ClearColumnStyles ()
    {
        _tableView!.Style.ColumnStyles.Clear ();
        _tableView!.Update ();
    }

    private void CloseExample () => _tableView!.Table = null;

    private MenuBarItem CreateViewMenu ()
    {
        // Store checkbox references for the toggle methods to access
        Dictionary<string, CheckBox> checkboxes = new ();

        var viewMenuBarItem = new MenuBarItem ("_View",
                                               [
                                                   CreateCheckBoxMenuItem ("ShowHeaders",
                                                                           "_ShowHeaders",
                                                                           _tableView!.Style.ShowHeaders,
                                                                           state =>
                                                                           {
                                                                               _tableView!.Style.ShowHeaders = state;
                                                                               _tableView!.Update ();
                                                                           }),
                                                   CreateCheckBoxMenuItem ("AlwaysShowHeaders",
                                                                           "_AlwaysShowHeaders",
                                                                           _tableView!.Style.AlwaysShowHeaders,
                                                                           state =>
                                                                           {
                                                                               _tableView!.Style.AlwaysShowHeaders = state;
                                                                               _tableView!.Update ();
                                                                           }),
                                                   CreateCheckBoxMenuItem ("HeaderOverline",
                                                                           "_HeaderOverLine",
                                                                           _tableView!.Style.ShowHorizontalHeaderOverline,
                                                                           state =>
                                                                           {
                                                                               _tableView!.Style.ShowHorizontalHeaderOverline = state;
                                                                               _tableView!.Update ();
                                                                           }),
                                                   CreateCheckBoxMenuItem ("HeaderMidline",
                                                                           "_HeaderMidLine",
                                                                           _tableView!.Style.ShowVerticalHeaderLines,
                                                                           state =>
                                                                           {
                                                                               _tableView!.Style.ShowVerticalHeaderLines = state;
                                                                               _tableView!.Update ();
                                                                           }),
                                                   CreateCheckBoxMenuItem ("HeaderUnderline",
                                                                           "_HeaderUnderLine",
                                                                           _tableView!.Style.ShowHorizontalHeaderUnderline,
                                                                           state =>
                                                                           {
                                                                               _tableView!.Style.ShowHorizontalHeaderUnderline = state;
                                                                               _tableView!.Update ();
                                                                           }),
                                                   CreateCheckBoxMenuItem ("BottomLine",
                                                                           "_BottomLine",
                                                                           _tableView!.Style.ShowHorizontalBottomLine,
                                                                           state =>
                                                                           {
                                                                               _tableView!.Style.ShowHorizontalBottomLine = state;
                                                                               _tableView!.Update ();
                                                                           }),
                                                   CreateCheckBoxMenuItem ("FullRowSelect",
                                                                           "_FullRowSelect",
                                                                           _tableView!.FullRowSelect,
                                                                           state =>
                                                                           {
                                                                               _tableView!.FullRowSelect = state;
                                                                               _tableView!.Update ();
                                                                           }),
                                                   CreateCheckBoxMenuItem ("CellLines",
                                                                           "_CellLines",
                                                                           _tableView!.Style.ShowVerticalCellLines,
                                                                           state =>
                                                                           {
                                                                               _tableView!.Style.ShowVerticalCellLines = state;
                                                                               _tableView!.Update ();
                                                                           }),
                                                   CreateCheckBoxMenuItem ("VerticalHeaderLines",
                                                                           "_VerticalHeaderLines",
                                                                           _tableView!.Style.ShowVerticalHeaderLines,
                                                                           state =>
                                                                           {
                                                                               _tableView!.Style.ShowVerticalHeaderLines = state;
                                                                               _tableView!.Update ();
                                                                           }),
                                                   CreateCheckBoxMenuItem ("ExpandLastColumn",
                                                                           "_ExpandLastColumn",
                                                                           _tableView!.Style.ExpandLastColumn,
                                                                           state =>
                                                                           {
                                                                               _tableView!.Style.ExpandLastColumn = state;
                                                                               _tableView!.Update ();
                                                                           }),
                                                   CreateCheckBoxMenuItem ("AlwaysUseNormalColorForVerticalCellLines",
                                                                           "_AlwaysUseNormalColorForVerticalCellLines",
                                                                           _tableView!.Style.AlwaysUseNormalColorForVerticalCellLines,
                                                                           state =>
                                                                           {
                                                                               _tableView!.Style.AlwaysUseNormalColorForVerticalCellLines = state;
                                                                               _tableView!.Update ();
                                                                           }),
                                                   CreateCheckBoxMenuItem ("SmoothScrolling",
                                                                           "_SmoothHorizontalScrolling",
                                                                           _tableView!.Style.SmoothHorizontalScrolling,
                                                                           state =>
                                                                           {
                                                                               _tableView!.Style.SmoothHorizontalScrolling = state;
                                                                               _tableView!.Update ();
                                                                           }),
                                                   new Line (),
                                                   CreateOptionSelectorMenuItem ("_ScrollBarVisibilityMode",
                                                                                 ScrollBarVisibilityMode.None,
                                                                                 state =>
                                                                                 {
                                                                                     switch (state)
                                                                                     {
                                                                                         case ScrollBarVisibilityMode.Auto:
                                                                                         case ScrollBarVisibilityMode.Always:

                                                                                             if (!_tableView.ViewportSettings.HasFlag (ViewportSettingsFlags
                                                                                                     .HasScrollBars))
                                                                                             {
                                                                                                 _tableView.ViewportSettings |=
                                                                                                     ViewportSettingsFlags.HasScrollBars;
                                                                                             }

                                                                                             break;

                                                                                         case ScrollBarVisibilityMode.Manual:
                                                                                         case ScrollBarVisibilityMode.None:

                                                                                             if (_tableView.ViewportSettings.HasFlag (ViewportSettingsFlags
                                                                                                     .HasScrollBars))
                                                                                             {
                                                                                                 _tableView.ViewportSettings &=
                                                                                                     ~ViewportSettingsFlags.HasScrollBars;
                                                                                             }

                                                                                             break;
                                                                                     }

                                                                                     switch (state)
                                                                                     {
                                                                                         case ScrollBarVisibilityMode.Manual:
                                                                                             _tableView!.HorizontalScrollBar.VisibilityMode =
                                                                                                 ScrollBarVisibilityMode.Manual;

                                                                                             _tableView!.VerticalScrollBar.VisibilityMode =
                                                                                                 ScrollBarVisibilityMode.Manual;

                                                                                             break;

                                                                                         case ScrollBarVisibilityMode.Auto:
                                                                                             _tableView!.HorizontalScrollBar.VisibilityMode =
                                                                                                 ScrollBarVisibilityMode.Auto;

                                                                                             _tableView!.VerticalScrollBar.VisibilityMode =
                                                                                                 ScrollBarVisibilityMode.Auto;

                                                                                             break;

                                                                                         case ScrollBarVisibilityMode.Always:
                                                                                             _tableView!.HorizontalScrollBar.VisibilityMode =
                                                                                                 ScrollBarVisibilityMode.Always;

                                                                                             _tableView!.VerticalScrollBar.VisibilityMode =
                                                                                                 ScrollBarVisibilityMode.Always;

                                                                                             break;

                                                                                         case ScrollBarVisibilityMode.None:
                                                                                             _tableView!.HorizontalScrollBar.VisibilityMode =
                                                                                                 ScrollBarVisibilityMode.None;

                                                                                             _tableView!.VerticalScrollBar.VisibilityMode =
                                                                                                 ScrollBarVisibilityMode.None;

                                                                                             break;
                                                                                     }
                                                                                     _tableView!.Update ();
                                                                                 }),
                                                   new Line (),
                                                   CreateCheckBoxMenuItem ("UseAllRowsForContentCalculation",
                                                                           "_UseAllRowsForContentCalculation",
                                                                           _tableView!.UseAllRowsForContentCalculation,
                                                                           state =>
                                                                           {
                                                                               _tableView!.UseAllRowsForContentCalculation = state;
                                                                               _tableView!.Update ();
                                                                           }),
                                                   new MenuItem
                                                   {
                                                       Title = "_AllLines",
                                                       Action = () =>
                                                                {
                                                                    _tableView!.Style.ShowHorizontalHeaderOverline = true;
                                                                    _tableView!.Style.ShowVerticalHeaderLines = true;
                                                                    _tableView!.Style.ShowHorizontalHeaderUnderline = true;
                                                                    _tableView!.Style.ShowVerticalCellLines = true;

                                                                    checkboxes ["HeaderOverline"].Value = CheckState.Checked;
                                                                    checkboxes ["HeaderMidline"].Value = CheckState.Checked;
                                                                    checkboxes ["HeaderUnderline"].Value = CheckState.Checked;
                                                                    checkboxes ["CellLines"].Value = CheckState.Checked;

                                                                    _tableView!.Update ();
                                                                }
                                                   },
                                                   new MenuItem
                                                   {
                                                       Title = "_NoLines",
                                                       Action = () =>
                                                                {
                                                                    _tableView!.Style.ShowHorizontalHeaderOverline = false;
                                                                    _tableView!.Style.ShowVerticalHeaderLines = false;
                                                                    _tableView!.Style.ShowHorizontalHeaderUnderline = false;
                                                                    _tableView!.Style.ShowVerticalCellLines = false;

                                                                    checkboxes ["HeaderOverline"].Value = CheckState.UnChecked;
                                                                    checkboxes ["HeaderMidline"].Value = CheckState.UnChecked;
                                                                    checkboxes ["HeaderUnderline"].Value = CheckState.UnChecked;
                                                                    checkboxes ["CellLines"].Value = CheckState.UnChecked;

                                                                    _tableView!.Update ();
                                                                }
                                                   },
                                                   CreateCheckBoxMenuItem ("Checkboxes",
                                                                           "_Checkboxes",
                                                                           false,
                                                                           state =>
                                                                           {
                                                                               if (state)
                                                                               {
                                                                                   ToggleCheckboxes (false);
                                                                                   checkboxes ["Radioboxes"].Value = CheckState.UnChecked;
                                                                               }
                                                                               else if (HasCheckboxes ())
                                                                               {
                                                                                   ToggleCheckboxes (false);
                                                                               }
                                                                           }),
                                                   CreateCheckBoxMenuItem ("Radioboxes",
                                                                           "_Radioboxes",
                                                                           false,
                                                                           state =>
                                                                           {
                                                                               if (state)
                                                                               {
                                                                                   ToggleCheckboxes (true);
                                                                                   checkboxes ["Checkboxes"].Value = CheckState.UnChecked;
                                                                               }
                                                                               else if (HasCheckboxes ())
                                                                               {
                                                                                   ToggleCheckboxes (true);
                                                                               }
                                                                           }),
                                                   CreateCheckBoxMenuItem ("AlternatingColors",
                                                                           "Alternating Colors",
                                                                           false,
                                                                           state =>
                                                                           {
                                                                               if (state)
                                                                               {
                                                                                   _tableView!.Style.RowColorGetter =
                                                                                       a => a.RowIndex % 2 == 0 ? _alternatingScheme : null;
                                                                               }
                                                                               else
                                                                               {
                                                                                   _tableView!.Style.RowColorGetter = null;
                                                                               }

                                                                               _tableView!.SetNeedsDraw ();
                                                                           }),
                                                   CreateCheckBoxMenuItem ("Cursor",
                                                                           "Invert Selected Cell First Character",
                                                                           _tableView!.Style.InvertSelectedCellFirstCharacter,
                                                                           state =>
                                                                           {
                                                                               _tableView!.Style.InvertSelectedCellFirstCharacter = state;
                                                                               _tableView!.SetNeedsDraw ();
                                                                           }),
                                                   new MenuItem { Title = "_ClearColumnStyles", Action = ClearColumnStyles },
                                                   new MenuItem { Title = "Sho_w All Columns", Action = ShowAllColumns }
                                               ]);

        return viewMenuBarItem;

        MenuItem CreateCheckBoxMenuItem (string key, string title, bool initialState, Action<bool> onToggle)
        {
            CheckBox checkBox = new () { Title = title, Value = initialState ? CheckState.Checked : CheckState.UnChecked };

            checkBox.ValueChanged += (_, _) => onToggle (checkBox.Value == CheckState.Checked);

            MenuItem item = new () { CommandView = checkBox };

            item.Accepting += (_, e) =>
                              {
                                  checkBox.AdvanceCheckState ();
                                  e.Handled = true;
                              };

            checkboxes [key] = checkBox;

            return item;
        }

        MenuItem CreateOptionSelectorMenuItem<T> (string title, T initialState, Action<T> onActivate) where T : struct, Enum
        {
            OptionSelector<T> optionSelector = new ()
            {
                Title = title, Labels = Enum.GetValues<T> ().Select (v => v.ToString ()).ToArray (), Value = initialState
            };

            optionSelector.ValueChanged += (_, e) => onActivate (e.Value!.Value);

            MenuItem item = new () { CommandView = optionSelector, HelpText = title };

            item.Accepting += (s, e) =>
                              {
                                  (s as View)?.App?.Mouse.UngrabMouse ();
                                  e.Handled = true;
                              };

            return item;
        }
    }

    private void EditCurrentCell (object? sender, CommandEventArgs args)
    {
        if (_tableView?.Table is not DataTableSource || _currentTable == null)
        {
            return;
        }

        int col = _tableView.Value?.SelectedCell.X ?? 0;
        int row = _tableView.Value?.SelectedCell.Y ?? 0;

        int tableCol = ToTableCol (col);

        if (tableCol < 0)
        {
            return;
        }

        object o = _currentTable.Rows [row] [tableCol];

        string title = o is uint u ? GetUnicodeCategory (u) + $"(0x{o:X4})" : "Enter new value";

        var oldValue = _currentTable.Rows [row] [tableCol].ToString ();

        var ok = new Button { Text = Strings.btnOk };
        var cancel = new Button { Text = Strings.btnCancel };
        var d = new Dialog { Title = title, Buttons = [cancel, ok] };
        var lbl = new Label { X = 0, Y = 1, Text = _tableView!.Table!.ColumnNames [col] };
        var tf = new TextField { Text = oldValue!, X = 0, Y = 2, Width = Dim.Fill (0, 50) };

        d.Add (lbl, tf);
        tf.SetFocus ();

        _app?.Run (d);
        bool okPressed = d.Result == 1;
        d.Dispose ();

        if (!okPressed)
        {
            return;
        }

        try
        {
            _currentTable.Rows [row] [tableCol] = string.IsNullOrWhiteSpace (tf.Text) ? DBNull.Value : tf.Text;
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery ((sender as View)?.App!, "Failed to set text", ex.Message, "Ok");
        }

        _tableView!.Update ();
    }

    private IEnumerable<FileSystemInfo> GetChildren (FileSystemInfo arg)
    {
        try
        {
            return arg is DirectoryInfo d ? d.GetFileSystemInfos () : Enumerable.Empty<FileSystemInfo> ();
        }
        catch (Exception)
        {
            // Permission denied etc
            return [];
        }
    }

    private int? GetColumn ()
    {
        if (_tableView!.Table == null)
        {
            return null;
        }

        if (_tableView!.Value is null || _tableView!.Value.SelectedCell.X > _tableView!.Table.Columns)
        {
            return null;
        }

        return _tableView!.Value.SelectedCell.X;
    }

    private string GetHumanReadableFileSize (FileSystemInfo fsi)
    {
        if (fsi is not FileInfo fi)
        {
            return string.Empty;
        }

        long value = fi.Length;
        CultureInfo culture = CultureInfo.CurrentUICulture;

        return GetHumanReadableFileSize (value, culture);
    }

    private string GetHumanReadableFileSize (long value, CultureInfo culture)
    {
        const long BYTE_CONVERSION = 1024;

        string [] sizeSuffixes =
        [
            "bytes",
            "KB",
            "MB",
            "GB",
            "TB",
            "PB",
            "EB",
            "ZB",
            "YB"
        ];

        switch (value)
        {
            case < 0: return "-" + GetHumanReadableFileSize (-value, culture);

            case 0: return "0.0 bytes";
        }

        var mag = (int)Math.Log (value, BYTE_CONVERSION);
        double adjustedSize = value / Math.Pow (1000, mag);

        return string.Format (culture.NumberFormat, "{0:n2} {1}", adjustedSize, sizeSuffixes [mag]);
    }

    private string GetProposedNewSortOrder (int clickedCol, out bool isAsc)
    {
        // work out new sort order
        string? sort = _currentTable?.DefaultView.Sort;
        string colName = _tableView!.Table!.ColumnNames [clickedCol];

        if (sort?.EndsWith ("ASC") ?? false)
        {
            sort = $"{colName} DESC";
            isAsc = false;
        }
        else
        {
            sort = $"{colName} ASC";
            isAsc = true;
        }

        return sort;
    }

    private string GetUnicodeCategory (uint u) => _ranges!.FirstOrDefault (r => u >= r.Start && u <= r.End)?.Category ?? "Unknown";
    private bool HasCheckboxes () => _tableView!.Table is CheckBoxTableSourceWrapperBase;

    private void HideColumn (int clickedCol)
    {
        ColumnStyle style = _tableView!.Style.GetOrCreateColumnStyle (clickedCol);
        style.Visible = false;
        _tableView!.Update ();
    }

    private void OpenExample (bool big)
    {
        SetTable (TableView.BuildDemoDataTable (big ? 30 : 5, big ? 1000 : 5));
        SetDemoTableStyles ();
    }

    private void OpenSimple (bool big) => SetTable (BuildSimpleDataTable (big ? 30 : 5, big ? 1000 : 5));

    private void OpenTreeExample ()
    {
        _tableView!.Style.ColumnStyles.Clear ();

        TreeView<FileSystemInfo> tree = new () { AspectGetter = f => f.Name, TreeBuilder = new DelegateTreeBuilder<FileSystemInfo> (GetChildren, _ => false) };

        TreeTableSource<FileSystemInfo> source = new (_tableView,
                                                      "Name",
                                                      tree,
                                                      new Dictionary<string, Func<FileSystemInfo, object>>
                                                      {
                                                          { "Extension", f => f.Extension },
                                                          { "CreationTime", f => f.CreationTime },
                                                          { "FileSize", GetHumanReadableFileSize }
                                                      });

        bool isWindows = OperatingSystem.IsWindows ();

        try
        {
            foreach (DriveInfo di in DriveInfo.GetDrives ())
            {
                if (isWindows && di is { DriveType: DriveType.Network, IsReady: false })
                {
                    continue;
                }

                tree.AddObject (new DirectoryInfo (di.Name));
            }
        }
        catch (Exception e)
        {
            MessageBox.ErrorQuery (_tableView?.App!, "Could not find local drives", e.Message, "Ok");
        }

        _tableView!.Table = source;

        _toDispose?.Add (tree);
    }

    private void OpenUnicodeMap ()
    {
        SetTable (BuildUnicodeMap ());
        _tableView?.Update ();
    }

    // Demonstrates the fix for #5072: a column with very wide content used to consume all viewport
    // space and push later columns off-screen. With the fix, "Description" is clamped so "Status" and
    // "Owner" remain visible at their header widths.
    private void OpenWideColumnExample ()
    {
        DataTable dt = new ();
        dt.Columns.Add ("Id", typeof (int));
        dt.Columns.Add ("Description", typeof (string));
        dt.Columns.Add ("Status", typeof (string));
        dt.Columns.Add ("Owner", typeof (string));

        string [] statuses = ["Open", "InProgress", "Blocked", "Done"];
        string [] owners = ["Alice", "Bob", "Carol", "Dan"];

        for (var i = 0; i < 25; i++)
        {
            dt.Rows.Add (i, $"Row {i}: " + new string ('x', 120 + i % 40), statuses [i % statuses.Length], owners [i % owners.Length]);
        }

        SetTable (dt);

        // Clear any styles inherited from a previous example
        _tableView!.Style.ColumnStyles.Clear ();
        _tableView!.Update ();
    }

    private void Quit () => _tableView?.App?.RequestStop ();

    private void RunColumnWidthDialog (int? col, string prompt, Action<ColumnStyle, int> setter, Func<ColumnStyle, int> getter)
    {
        if (col == null)
        {
            return;
        }

        var d = new Dialog { Title = prompt, Buttons = [new Button { Title = Strings.btnCancel }, new Button { Title = Strings.btnOk }] };

        ColumnStyle style = _tableView!.Style.GetOrCreateColumnStyle (col.Value);

        var lbl = new Label { X = 0, Y = 0, Text = $"{_tableView!.Table!.ColumnNames [col.Value]}: " };
        var tf = new TextField { Text = getter (style).ToString (), X = Pos.Right (lbl), Y = 0, Width = 20 };

        d.Add (lbl, tf);
        tf.SetFocus ();

        _tableView.App?.Run (d);
        bool accepted = d.Result == 1;
        d.Dispose ();

        if (!accepted)
        {
            return;
        }

        try
        {
            setter (style, int.Parse (tf.Text));
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery (_tableView.App!, "Failed to set", ex.Message, "Ok");
        }

        _tableView!.Update ();
    }

    private string RuneToString (object o) => Rune.TryCreate ((uint)o, out Rune rune) ? rune.ToString () : " ";

    private void SetDemoTableStyles ()
    {
        _tableView!.Style.ColumnStyles.Clear ();

        var alignMid = new ColumnStyle { Alignment = Alignment.Center };
        var alignRight = new ColumnStyle { Alignment = Alignment.End };

        var dateFormatStyle = new ColumnStyle
        {
            Alignment = Alignment.End, RepresentationGetter = v => v is DateTime d ? d.ToString ("yyyy-MM-dd") : v.ToString () ?? string.Empty
        };

        var negativeRight = new ColumnStyle
        {
            Format = "0.##",
            MinWidth = 10,
            AlignmentGetter = v => v is double d
                                       ?

                                       // align negative values right
                                       d < 0
                                           ? Alignment.End
                                           :

                                           // align positive values left
                                           Alignment.Start
                                       :

                                       // not a double
                                       Alignment.Start,
            ColorGetter = a => a.CellValue is double d
                                   ?

                                   // color 0 and negative values red
                                   d <= 0.0000001
                                       ? a.RowIndex % 2 == 0 ? _redSchemeAlt : _redScheme
                                       :

                                       // use normal scheme for positive values
                                       null
                                   :

                                   // not a double
                                   null
        };

        _tableView!.Style.ColumnStyles.Add (_currentTable!.Columns ["DateCol"]!.Ordinal, dateFormatStyle);
        _tableView!.Style.ColumnStyles.Add (_currentTable!.Columns ["DoubleCol"]!.Ordinal, negativeRight);
        _tableView!.Style.ColumnStyles.Add (_currentTable!.Columns ["NullsCol"]!.Ordinal, alignMid);
        _tableView!.Style.ColumnStyles.Add (_currentTable!.Columns ["IntCol"]!.Ordinal, alignRight);

        _tableView!.Update ();
    }

    private void SetMaxWidth ()
    {
        int? col = GetColumn ();
        RunColumnWidthDialog (col, "MaxWidth", (s, v) => s.MaxWidth = v, s => s.MaxWidth);
    }

    private void SetMinAcceptableWidth ()
    {
        int? col = GetColumn ();

        RunColumnWidthDialog (col, "MinAcceptableWidth", (s, v) => s.MinAcceptableWidth = v, s => s.MinAcceptableWidth);
    }

    private void SetMinAcceptableWidthToOne ()
    {
        for (var i = 0; i < _tableView!.Table!.Columns; i++)
        {
            ColumnStyle style = _tableView!.Style.GetOrCreateColumnStyle (i);
            style.MinAcceptableWidth = 1;
        }
    }

    private void SetMinWidth ()
    {
        int? col = GetColumn ();
        RunColumnWidthDialog (col, "MinWidth", (s, v) => s.MinWidth = v, s => s.MinWidth);
    }

    private void SetTable (DataTable dataTable) => _tableView!.Table = new DataTableSource (_currentTable = dataTable);

    // private void SetupScrollBar ()
    // {
    //    var scrollBar = new ScrollBarView (_tableView, true);

    //    scrollBar.ChangedPosition += (s, e) =>
    //                                 {
    //                                     _tableView!.RowOffset = scrollBar.Position;

    //                                     if (_tableView!.RowOffset != scrollBar.Position)
    //                                     {
    //                                         scrollBar.Position = _tableView!.RowOffset;
    //                                     }

    //                                     _tableView!.SetNeedsDraw ();
    //                                 };
    //    /*
    //    scrollBar.OtherScrollBarView.ChangedPosition += (s,e) => {
    //        tableView.LeftItem = scrollBar.OtherScrollBarView.Position;
    //        if (tableView.LeftItem != scrollBar.OtherScrollBarView.Position) {
    //            scrollBar.OtherScrollBarView.Position = tableView.LeftItem;
    //        }
    //        tableView.SetNeedsDraw ();
    //    };*/

    //    _tableView!.DrawingContent += (s, e) =>
    //                              {
    //                                  scrollBar.Size = _tableView!.Table?.Rows ?? 0;
    //                                  scrollBar.Position = _tableView!.RowOffset;

    //                                  //scrollBar.OtherScrollBarView.Size = tableView.Maxlength - 1;
    //                                  //scrollBar.OtherScrollBarView.Position = tableView.LeftItem;
    //                                  scrollBar.Refresh ();
    //                              };
    // }

    private void ShowAllColumns ()
    {
        foreach (KeyValuePair<int, ColumnStyle> colStyle in _tableView!.Style.ColumnStyles)
        {
            colStyle.Value.Visible = true;
        }

        _tableView!.Update ();
    }

    private void ShowHeaderContextMenu (int clickedCol, Mouse e)
    {
        if (HasCheckboxes () && clickedCol == 0)
        {
            return;
        }

        string sort = GetProposedNewSortOrder (clickedCol, out bool isAsc);
        string colName = _tableView!.Table!.ColumnNames [clickedCol];

        PopoverMenu contextMenu = new ([
                                           new MenuItem ($"Hide {TrimArrows (colName)}", "", () => HideColumn (clickedCol)),
                                           new MenuItem ($"Sort {StripArrows (sort)}", "", () => SortColumn (clickedCol, sort, isAsc))
                                       ]);

        // Registering with the PopoverManager will ensure that the context menu is closed when the view is no longer focused
        // and the context menu is disposed when it is closed.
        e.View?.App!.Popovers?.Register (contextMenu);
        contextMenu.MakeVisible (new Point (e.ScreenPosition.X + 1, e.ScreenPosition.Y + 1));
    }

    private void SortColumn (int clickedCol)
    {
        string sort = GetProposedNewSortOrder (clickedCol, out bool isAsc);

        // don't try to sort on the toggled column
        if (HasCheckboxes () && clickedCol == 0)
        {
            return;
        }

        SortColumn (clickedCol, sort, isAsc);
    }

    private void SortColumn (int clickedCol, string sort, bool isAsc)
    {
        if (_currentTable == null)
        {
            return;
        }

        // set a sort order
        _currentTable.DefaultView.Sort = sort;

        // copy the rows from the view
        DataTable sortedCopy = _currentTable.DefaultView.ToTable ();
        _currentTable.Rows.Clear ();

        foreach (DataRow r in sortedCopy.Rows)
        {
            _currentTable.ImportRow (r);
        }

        foreach (DataColumn col in _currentTable.Columns)
        {
            // remove any lingering sort indicator
            col.ColumnName = TrimArrows (col.ColumnName);

            // add a new one if this the one that is being sorted
            if (col.Ordinal == clickedCol)
            {
                col.ColumnName += isAsc ? Glyphs.UpArrow : Glyphs.DownArrow;
            }
        }

        _tableView!.Update ();
    }

    private string StripArrows (string columnName) => columnName.Replace ($"{Glyphs.DownArrow}", "").Replace ($"{Glyphs.UpArrow}", "");

    private void TableViewKeyPress (object? sender, Key e)
    {
        if (_currentTable == null)
        {
            return;
        }

        if (e.KeyCode != KeyCode.Delete)
        {
            return;
        }

        if (_tableView!.FullRowSelect)
        {
            // Delete button deletes all rows when in full row mode
            foreach (int toRemove in _tableView!.GetAllSelectedCells ().Select (p => p.Y).Distinct ().OrderByDescending (i => i))
            {
                _currentTable.Rows.RemoveAt (toRemove);
            }
        }
        else
        {
            // otherwise set all selected cells to null
            foreach (Point pt in _tableView!.GetAllSelectedCells ())
            {
                _currentTable.Rows [pt.Y] [pt.X] = DBNull.Value;
            }
        }

        _tableView!.Update ();
        e.Handled = true;
    }

    private void ToggleCheckboxes (bool radio)
    {
        if (_tableView!.Table is CheckBoxTableSourceWrapperBase wrapper)
        {
            // unwrap it to remove check boxes
            _tableView!.Table = wrapper.Wrapping;

            // if toggling off checkboxes/radio
            if (wrapper.UseRadioButtons == radio)
            {
                return;
            }
        }

        ITableSource source;

        // Either toggling on checkboxes/radio or switching from radio to checkboxes (or vice versa)
        if (_tableView!.Table is TreeTableSource<FileSystemInfo> treeSource)
        {
            source = new CheckBoxTableSourceWrapperByObject<FileSystemInfo> (_tableView, treeSource, _checkedFileSystemInfos!.Contains, CheckOrUncheckFile)
            {
                UseRadioButtons = radio
            };
        }
        else
        {
            source = new CheckBoxTableSourceWrapperByIndex (_tableView, _tableView!.Table!) { UseRadioButtons = radio };
        }

        _tableView!.Table = source;
    }

    private int ToTableCol (int col)
    {
        if (HasCheckboxes ())
        {
            return col - 1;
        }

        return col;
    }

    private string TrimArrows (string columnName) => columnName.TrimEnd ((char)Glyphs.UpArrow.Value, (char)Glyphs.DownArrow.Value);

    public class UnicodeRange (uint start, uint end, string category)
    {
        public string Category { get; } = category;
        public uint End { get; } = end;
        public uint Start { get; } = start;
    }
}
