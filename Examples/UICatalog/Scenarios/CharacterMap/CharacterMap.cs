#nullable enable

using System.Globalization;
using System.Text;

namespace UICatalog.Scenarios;

/// <summary>
///     This Scenario demonstrates building a custom control (a class deriving from View) that: - Provides a
///     "Character Map" application (like Windows' charmap.exe). - Helps test unicode character rendering in Terminal.Gui -
///     Illustrates how to do infinite scrolling
/// </summary>
[ScenarioMetadata ("Character Map", "Unicode viewer. Demos infinite content drawing and scrolling.")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Drawing")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Scrolling")]
[ScenarioCategory ("Unicode")]
public class CharacterMap : Scenario
{
    private Label? _errorLabel;
    private TableView? _categoryList;
    private CharMap? _charMap;
    private OptionSelector? _unicodeCategorySelector;
    private int _sortedColumn;

    public override List<Key> GetDemoKeyStrokes (IApplication? app)
    {
        List<Key> keys = [];

        for (var i = 0; i < 200; i++)
        {
            keys.Add (Key.CursorDown);
        }

        // Category table
        keys.Add (Key.Tab.WithShift);

        // Block elements
        keys.Add (Key.B);
        keys.Add (Key.L);

        keys.Add (Key.Tab);

        for (var i = 0; i < 200; i++)
        {
            keys.Add (Key.CursorLeft);
        }

        return keys;
    }

    // Don't create a Window, just return the top-level view
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window top = new ();
        top.BorderStyle = LineStyle.None;

        _categoryList = new TableView { X = Pos.AnchorEnd (), Height = Dim.Fill () };
        _charMap = new CharMap { X = 0, Y = 1, Height = Dim.Fill (), Width = Dim.Fill (_categoryList!) };

        MenuBar menu = new ()
        {
            Width = Dim.Fill (_categoryList),
            Menus =
            [
                new MenuBarItem (Strings.menuFile,
                                 new MenuItem []
                                 {
                                     new (Strings.cmdQuit, $"{Application.GetDefaultKey (Command.Quit)}", () => _charMap?.App?.RequestStop ())
                                 }),
                new MenuBarItem ("_Options", [CreateMenuShowWidth (), CreateMenuUnicodeCategorySelector ()])
            ]
        };

        Label jumpLabel = new () { X = Pos.Left (_categoryList), HotKeySpecifier = (Rune)'_', Text = "_Jump To:" };

        TextField jumpEdit = new ()
        {
            X = Pos.Right (jumpLabel) + 1,
            Y = Pos.Top (jumpLabel),
            Width = 17,
            Height = 1,
            Title = "e.g. 01BE3 or ✈"
        };

        _errorLabel = new Label
        {
            X = Pos.Right (jumpEdit),
            Y = Pos.Top (jumpLabel),
            SchemeName = "error",
            Text = "err",
            Visible = false
        };
        _categoryList.Y = Pos.Bottom (jumpLabel);

        _charMap.ValueChanged += (_, args) =>
                                 {
                                     if (Rune.IsValid (args.NewValue.Value))
                                     {
                                         jumpEdit.Text = args.NewValue.ToString ();
                                     }
                                     else
                                     {
                                         jumpEdit.Text = $"U+{args.NewValue.Value:x5}";
                                     }
                                 };

        jumpEdit.Accepting += JumpEditOnAccept;

        _categoryList.FullRowSelect = true;
        _categoryList.MultiSelect = false;

        _categoryList.Style.ShowVerticalCellLines = false;
        _categoryList.Style.AlwaysShowHeaders = true;

        var isDescending = false;
        _sortedColumn = 0;

        _categoryList.Table = CreateCategoryTable (0, isDescending);

        // if user clicks the mouse in TableView
        _categoryList.Activating += (_, e) =>
                                    {
                                        // Only handle mouse clicks
                                        if (e.Context?.Binding is not MouseBinding { MouseEvent: { } mouse })
                                        {
                                            return;
                                        }

                                        _categoryList.ScreenToCell (mouse.Position!.Value, out int? clickedCol);

                                        if (clickedCol == null || !mouse.Flags.HasFlag (MouseFlags.LeftButtonClicked))
                                        {
                                            return;
                                        }
                                        EnumerableTableSource<UnicodeRange> table = (EnumerableTableSource<UnicodeRange>)_categoryList.Table!;
                                        string prevSelection = table.Data.ElementAt (_categoryList.Value?.SelectedCell.Y ?? 0).Category;
                                        isDescending = !isDescending;
                                        _sortedColumn = clickedCol.Value;

                                        _categoryList.Table = CreateCategoryTable (clickedCol.Value, isDescending);

                                        table = (EnumerableTableSource<UnicodeRange>)_categoryList.Table!;

                                        _categoryList.SetSelection (0,
                                                                    table.Data.Select ((item, index) => new { item, index })
                                                                         .FirstOrDefault (x => x.item.Category == prevSelection)
                                                                         ?.index
                                                                    ?? 0,
                                                                    false);
                                    };

        int longestName = UnicodeRange.Ranges.Max (r => r.Category.GetColumns ());

        // Apply italic styling to the sorted column header
        HeaderColorGetterDelegate headerColorGetter = args =>
                                                      {
                                                          if (args.Column != _sortedColumn)
                                                          {
                                                              return null;
                                                          }

                                                          Scheme baseScheme = args.RowScheme;

                                                          return new Scheme
                                                          {
                                                              Normal = new Attribute (baseScheme.Normal.Foreground, baseScheme.Normal.Background, TextStyle.Italic),
                                                              Focus = new Attribute (baseScheme.Focus.Foreground, baseScheme.Focus.Background, TextStyle.Italic)
                                                          };
                                                      };

        _categoryList.Style.ColumnStyles.Add (0, new ColumnStyle { MaxWidth = longestName, MinWidth = longestName, MinAcceptableWidth = longestName, HeaderColorGetter = headerColorGetter });
        _categoryList.Style.ColumnStyles.Add (1, new ColumnStyle { MaxWidth = 1, MinWidth = 6, HeaderColorGetter = headerColorGetter });
        _categoryList.Style.ColumnStyles.Add (2, new ColumnStyle { MaxWidth = 1, MinWidth = 6, HeaderColorGetter = headerColorGetter });

        _categoryList.Width = _categoryList.Style.ColumnStyles.Sum (c => c.Value.MinWidth) + 4;

        _categoryList.ValueChanged += (_, args) =>
                                      {
                                          if (args.NewValue is null)
                                          {
                                              return;
                                          }

                                          EnumerableTableSource<UnicodeRange> table = (EnumerableTableSource<UnicodeRange>)_categoryList.Table!;
                                          _charMap.StartCodePoint = table.Data.ToArray () [args.NewValue.SelectedCell.Y].Start;
                                          jumpEdit.Text = $"U+{_charMap.SelectedCodePoint:x5}";
                                      };

        top.Add (menu, _charMap, jumpLabel, jumpEdit, _errorLabel, _categoryList);

        _charMap.SelectedCodePoint = 0;
        _charMap.SetFocus ();

        app.Run (top);

        return;

        void JumpEditOnAccept (object? sender, CommandEventArgs e)
        {
            if (jumpEdit.Text.Length == 0)
            {
                return;
            }

            _errorLabel.Visible = true;

            uint result;

            if (jumpEdit.Text.Length == 1)
            {
                result = (uint)jumpEdit.Text.ToRunes () [0].Value;
            }
            else if (jumpEdit.Text.StartsWith ("U+", StringComparison.OrdinalIgnoreCase) || jumpEdit.Text.StartsWith ("\\u", StringComparison.Ordinal))
            {
                try
                {
                    result = uint.Parse (jumpEdit.Text [2..], NumberStyles.HexNumber);
                }
                catch (FormatException)
                {
                    _errorLabel.Text = "Invalid hex value";

                    return;
                }
            }
            else if (jumpEdit.Text.StartsWith ("0", StringComparison.OrdinalIgnoreCase) || jumpEdit.Text.StartsWith ("\\u", StringComparison.Ordinal))
            {
                try
                {
                    result = uint.Parse (jumpEdit.Text, NumberStyles.HexNumber);
                }
                catch (FormatException)
                {
                    _errorLabel.Text = "Invalid hex value";

                    return;
                }
            }
            else
            {
                try
                {
                    result = uint.Parse (jumpEdit.Text, NumberStyles.Integer);
                }
                catch (FormatException)
                {
                    _errorLabel.Text = "Invalid value";

                    return;
                }
            }

            if (result > RuneExtensions.MaxUnicodeCodePoint)
            {
                _errorLabel.Text = "Beyond maximum codepoint";

                return;
            }

            _errorLabel.Visible = false;

            EnumerableTableSource<UnicodeRange> table = (EnumerableTableSource<UnicodeRange>)_categoryList!.Table!;

            _categoryList.SetSelection (0,
                                        table.Data.Select ((item, index) => new { item, index })
                                             .FirstOrDefault (x => x.item.Start <= result && x.item.End >= result)
                                             ?.index
                                        ?? 0,
                                        false);
            _categoryList.EnsureCursorIsVisible ();

            // Ensure the typed glyph is selected
            _charMap.SelectedCodePoint = (int)result;
            _charMap.SetFocus ();

            // Cancel the event to prevent ENTER from being handled elsewhere
            e.Handled = true;
        }
    }

    private EnumerableTableSource<UnicodeRange> CreateCategoryTable (int sortByColumn, bool descending)
    {
        Func<UnicodeRange, object> orderBy;
        var categorySort = string.Empty;
        var startSort = string.Empty;
        var endSort = string.Empty;

        string sortIndicator = descending ? Glyphs.DownArrow.ToString () : Glyphs.UpArrow.ToString ();

        switch (sortByColumn)
        {
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

        IOrderedEnumerable<UnicodeRange> sortedRanges = descending ? UnicodeRange.Ranges.OrderByDescending (orderBy) : UnicodeRange.Ranges.OrderBy (orderBy);

        return new EnumerableTableSource<UnicodeRange> (sortedRanges,
                                                        new Dictionary<string, Func<UnicodeRange, object>>
                                                        {
                                                            { $"Category{categorySort}", s => s.Category },
                                                            { $"Start{startSort}", s => $"{s.Start:x5}" },
                                                            { $"End{endSort}", s => $"{s.End:x5}" }
                                                        });
    }

    private MenuItem CreateMenuShowWidth ()
    {
        CheckBox cb = new () { Title = "_Show Glyph Width", Value = _charMap!.ShowGlyphWidths ? CheckState.Checked : CheckState.None };
        var item = new MenuItem { CommandView = cb };

        item.Action += () => { _charMap?.ShowGlyphWidths = cb.Value == CheckState.Checked; };

        return item;
    }

    private MenuItem CreateMenuUnicodeCategorySelector ()
    {
        // First option is "All" (no filter), followed by all UnicodeCategory names
        string [] allCategoryNames = Enum.GetNames<UnicodeCategory> ();
        var options = new string [allCategoryNames.Length + 1];
        options [0] = "All";
        Array.Copy (allCategoryNames, 0, options, 1, allCategoryNames.Length);

        // TODO: Add a "None" option
        OptionSelector<UnicodeCategory> selector = new ();

        _unicodeCategorySelector = selector;

        selector.Value = null;
        _charMap!.ShowUnicodeCategory = null;

        selector.ValueChanged += (_, e) => _charMap.ShowUnicodeCategory = e.Value;

        return new MenuItem { CommandView = selector };
    }
}
