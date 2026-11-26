# ListView Deep Dive

[ListView](xref:Terminal.Gui.Views.ListView) shows a scrollable list. Each item can be selected and activated. By default every item is one row and the text comes from `ToString`.

To give items, headers, and footers more than one row, or to sort items by clicking a column header, use the APIs on this page. A cell is painted as text. It is not a subview, so it does not take focus or receive mouse input on its own.

The UICatalog scenario **ListView With MultiLine** exercises these APIs.

## Contents

- [Items](#items)
- [Cell text](#cell-text)
- [Alignment](#alignment)
- [Column headers](#column-headers)
- [Sorting](#sorting)
- [Column footers](#column-footers)
- [Scrolling and the mouse](#scrolling-and-the-mouse)
- [Custom data sources](#custom-data-sources)

## Items

| API | Default | Role |
| --- | --- | --- |
| <xref:Terminal.Gui.Views.ListView.ItemHeight> | `1` | Rows every item occupies while `ItemAutoHeight` is `false`. A value below 1 throws. |
| <xref:Terminal.Gui.Views.ListView.ItemAutoHeight> | `false` | Each visible item grows to its wrapped lines and is at least one row. `ItemHeight` does not change that height. |
| <xref:Terminal.Gui.Views.ListView.ItemsHorizontalAlignment> | `Start` | Horizontal alignment used when that column has no <xref:Terminal.Gui.Views.ListView.ColumnHeader>. |
| <xref:Terminal.Gui.Views.ListView.ItemsVerticalAlignment> | `Start` | Vertical placement of a cell's lines used when that column has no `ColumnHeader`. |

To give every item the same number of rows, set `ItemHeight`. To size each visible item from its own text, set `ItemAutoHeight` to `true`.

`ItemAutoHeight` measures the items in the viewport and the trailing page that keeps the last item reachable. The rest of the list is not measured. In a row with several columns, the item is as tall as its tallest column.

A line break starts another row of the same column when the item is taller than one line. A height of one shows only the first line. A line wider than the column wraps onto the remaining rows. Text past the item height is clipped.

```csharp
ListView listView = new ()
{
    Width = 40,
    Height = 12,
    ItemHeight = 2,
    ShowColumnHeaders = true,
    ColumnHeaders =
    [
        new ListView.ColumnHeader { HeaderName = "Name", Width = 12 },
        new ListView.ColumnHeader { HeaderName = "Notes", Width = 24 }
    ]
};

ObservableCollection<string []> rows =
[
    ["Alpha", "First item"],
    ["Beta", "Second item\nwith another line"]
];

listView.SetSource (rows);
```

Set `ItemAutoHeight` after the source is assigned, or at construction. Changing it measures the content width again before the content size updates. Wrapped lines fit the viewport, so <xref:Terminal.Gui.ViewBase.View.HorizontalScrollBar> follows that width and a scrolled horizontal offset is pulled back inside the new content. Turning auto height off restores the widest unwrapped item and the scrollbar.

To show that scrollbar, include <xref:Terminal.Gui.ViewBase.ViewportSettingsFlags.HasHorizontalScrollBar> in `ViewportSettings`.

## Cell text

`ColumnHeaders` names the columns. Index `i` is column `i`.

| Item | Column text |
| --- | --- |
| `string` | The whole string is column 0. A line break stays in that column. |
| `IList`, including `string[]` | Element `i` is column `i`. `MemberType` and `MemberFormat` apply to that element. A line break inside the element stays in that column. |
| Any other object | The value of `ColumnHeader.MemberName`. |

`ColumnMargin` is the gap between columns. The default is 2. `ShowMarks` reserves two columns for the mark.

`ColumnHeader.Width` sets the column width:

| `Width` | Result |
| --- | --- |
| `0` | Size from the widest unwrapped line of the header, the cells, and the footer. |
| Greater than `0` | Fixed width. |
| `-1` | The column is hidden. |

A fixed width wins over automatic sizing. An automatic column uses the measured content width, and the last one also grows to fill the viewport when the content is narrower. A header taller than one row does not shrink that width. When `ItemAutoHeight` is on or `ItemHeight` is greater than 1, an automatic column is kept inside the viewport so the item text can wrap. Header and footer width still use the widest unwrapped line. The `Footer0` fallback is not measured into a column.

<xref:Terminal.Gui.ViewBase.Alignment.Fill> occupies the whole column, including a short or empty cell, so the next column starts on its own boundary.

## Alignment

`Start`, `End`, `Center`, and `Fill` apply to items, headers, and footers.

| Alignment | Placement |
| --- | --- |
| `Start` | First line at the top. |
| `End` | Last line at the bottom. Lines that do not fit are clipped from the top. |
| `Center` | Lines are centered. Lines that do not fit keep the first lines. |
| `Fill` | Lines are spread across the band. A single line stays at the top. Extra rows after an even split go in the later gaps. |

A set `ColumnHeader` uses `MemberHorizontalAlignment` and `MemberVerticalAlignment` for that column's cells. Those values replace `ItemsHorizontalAlignment` and `ItemsVerticalAlignment`.

## Column headers

Set `ShowColumnHeaders` to pin a header band above the items. The default is `false`.

With no `ColumnHeaders`, the band is one row and shows `Col0`. `HeadersAlignment` and `HeadersVerticalAlignment` (both `Start`) place that fallback. They do not apply once a `ColumnHeader` is set.

| API | Default | Role |
| --- | --- | --- |
| `HeaderName` | `null` | Header text. An empty name uses `MemberName`, then `Col{index}`. |
| `HeaderHorizontalAlignment` | `Center` | Horizontal alignment of each header line. |
| `HeaderVerticalAlignment` | `Start` | Vertical placement inside `Height`. |
| `Height` | `1` | Rows this header occupies. A value below 1 throws. |
| `MemberName` | `null` | Property read from a non-list item. |
| `MemberType` | `string` | Type used to format the cell and a footer that inherits the format. |
| `MemberFormat` | `null` | Composite format, for example `{0:N2}`. |
| `MemberHorizontalAlignment` | `Start` | Horizontal alignment of the cells. |
| `MemberVerticalAlignment` | `Start` | Vertical placement of the cell lines. |

The band is as tall as the tallest `Height`, and at least one row when headers are shown and no columns are configured. Rows past a shorter column are blank.

A line break in `HeaderName` starts another header row when `Height` is greater than 1. A height of one shows only the first line and clips a wider title. When `Height` is greater than 1 and the column is narrower than the title, the title wraps. A line break stays a hard break.

`SetSource<T>(null)` still clears the source, using the same rules as before. Drawing still paints the header, and the footer when footers are shown, and then stops. Assign `Source = null` to detach every data source.

```csharp
listView.ShowColumnHeaders = true;
listView.ColumnHeaders =
[
    new ListView.ColumnHeader
    {
        HeaderName = "Scenario\nName",
        HeaderHorizontalAlignment = Alignment.Center,
        HeaderVerticalAlignment = Alignment.Start,
        Height = 2,
        Width = 16
    }
];
```

## Sorting

Set <xref:Terminal.Gui.Views.ListView.AllowsSorting> to `true` to let a left click on a configured column header sort a source supplied with `SetSource` or `SetSourceAsync`. The property defaults to `false`.

Sorting requires `ShowColumnHeaders` and at least one `ColumnHeader`. The first click on a header sorts that column ascending and displays <xref:Terminal.Gui.Drawing.Glyphs.UpArrow>. A second click on the same header sorts descending and displays <xref:Terminal.Gui.Drawing.Glyphs.DownArrow>. Clicking another header starts a new ascending sort. Clicking a footer or a header while `AllowsSorting` is `false` does not change the selection or order.

The column value follows the same rules as cell text. String items use column 0, list and array items use the element at the column index, and other items use `ColumnHeader.MemberName`. `MemberType` converts the value before comparison, which lets a numeric column sort by number rather than by its formatted text. Equal values retain source order. Same-type values that implement <xref:System.IComparable> use that comparison; otherwise the current culture compares the values as text. `MemberFormat` controls display only and does not control sorting.

Sorting changes the display order exposed by `ListWrapper<T>.ToList`; it does not reorder the caller's `ObservableCollection<T>`. Selection and marks follow the corresponding source items, including after collection changes. The active sort is reapplied when the source collection changes. Set `AllowsSorting` to `false` to restore source order and remove the direction glyph.

```csharp
ObservableCollection<string> items = ["Beta", "Alpha", "Gamma"];

ListView listView = new ()
{
    AllowsSorting = true,
    ShowColumnHeaders = true,
    ColumnHeaders =
    [
        new ListView.ColumnHeader
        {
            HeaderName = "Name",
            HeaderHorizontalAlignment = Alignment.Start,
            Width = 12
        }
    ]
};

listView.SetSource (items);
```

Only the built-in `ListWrapper<T>` installed by `SetSource` supports this behavior. A custom `IListDataSource` is responsible for its own ordering.

## Column footers

Set `ShowColumnFooters` to draw a footer band. The default is `false`. `ListView` does not compute totals. The caller stores the footer text.

| API | Default | Role |
| --- | --- | --- |
| `ShowColumnFooters` | `false` | Draws the band. |
| `FooterPlacement` | `Bottom` | `Bottom` pins the band and removes those rows from the item area. `AfterLastItem` draws it on the rows after the last item and scrolls with the content. |
| `ColumnFooters` | `null` | Index `i` is the footer for column `i`. |
| `FootersHorizontalAlignment` | `Start` | Used when a footer sets no alignment and no header is available. Also places `Footer0`. |
| `FootersVerticalAlignment` | `Start` | Same fallback for vertical placement. |
| `ColumnFooter.FooterName` | `null` | Footer text. A numeric format parses this text. |
| `ColumnFooter.HorizontalAlignment` | `null` | `null` inherits `MemberHorizontalAlignment`. A set value wins. |
| `ColumnFooter.VerticalAlignment` | `null` | `null` inherits `MemberVerticalAlignment`. |
| `ColumnFooter.Format` | `null` | `null` inherits `MemberFormat`. An empty string is an explicit format and leaves the text unchanged. |
| `ColumnFooter.Height` | `1` | Rows this footer occupies. A value below 1 throws. |

`FooterName` is formatted with the resolved format and the header `MemberType`, using the current culture. A value that cannot be converted is drawn unchanged.

A line break in `FooterName` starts another footer row when `Height` is greater than 1. A height of one shows only the first line. A line wider than the column wraps onto the extra rows. The band is as tall as the tallest footer.

When footers are shown and `ColumnFooters` is null or empty, the band is one full-width row showing `Footer0`, placed by `FootersHorizontalAlignment`.

To pin the band to the bottom of the `ListView`, leave `FooterPlacement` at `Bottom`. To draw it after the last item, set `FooterPlacement` to `AfterLastItem`.

```csharp
listView.ColumnHeaders =
[
    new ListView.ColumnHeader { HeaderName = "Label", Width = 12 },
    new ListView.ColumnHeader
    {
        HeaderName = "Count",
        MemberType = typeof (int),
        MemberFormat = "{0:00}",
        MemberHorizontalAlignment = Alignment.End,
        Width = 8
    }
];
listView.ShowColumnFooters = true;
listView.FooterPlacement = ListView.ColumnFooterPlacement.Bottom;
listView.ColumnFooters =
[
    new ListView.ColumnFooter { FooterName = "Total", Format = "" },
    new ListView.ColumnFooter { FooterName = "3" }
];
```

The second footer leaves `Format` unset, so it inherits `{0:00}` and `int`. The drawn text is `03`. `Format = ""` on the first footer keeps `Total` as written.

`ShowColumnFooters == false` leaves drawing and the content size unchanged.

## Scrolling and the mouse

`Viewport.Y` is the index of the top item, not a screen row. Header rows and a footer pinned to the bottom are subtracted from the item area. Page down, the last page, and mouse hit-testing use that area. An `AfterLastItem` footer does not take rows away from the items. The content height grows so that footer can scroll into view.

A click on a footer row does not change `SelectedItem`. A header click does not select the header row; when `AllowsSorting` is `true`, the selected source item follows the sort to its new display index. With `ItemAutoHeight`, a click selects the item that owns that visual row, including a later wrapped line of the same item.

## Custom data sources

`SetSource` and `ListWrapper<T>` provide the column, wrap, header, footer, and sorting behavior on this page. A custom `IListDataSource` still draws through `Render` and controls its own order. For that source, `ItemAutoHeight` keeps `ItemHeight` as the row height.
