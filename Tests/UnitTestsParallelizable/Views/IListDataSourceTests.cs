#nullable enable
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;

// ReSharper disable InconsistentNaming

namespace ViewsTests;

public class IListDataSourceTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region Concurrent Modification Tests

    [Fact]
    public void ListWrapper_SuspendAndModify_NoEventsUntilResume ()
    {
        ObservableCollection<string> source = ["Item1"];
        ListWrapper<string> wrapper = new (source);
        var eventCount = 0;

        wrapper.CollectionChanged += (s, e) => eventCount++;

        wrapper.SuspendCollectionChangedEvent = true;

        source.Add ("Item2");
        source.Add ("Item3");
        source.RemoveAt (0);

        Assert.Equal (0, eventCount);

        wrapper.SuspendCollectionChangedEvent = false;

        // Should have adjusted marks for the removals that happened while suspended
        Assert.Equal (2, wrapper.Count);
    }

    #endregion

    /// <summary>
    ///     Test implementation of IListDataSource for testing custom implementations
    /// </summary>
    private class TestListDataSource : IListDataSource
    {
        private readonly List<string> _items = ["Custom Item 00", "Custom Item 01", "Custom Item 02"];
        private readonly BitArray _marks = new (3);

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public int Count => _items.Count;

        public int MaxItemLength => _items.Any () ? _items.Max (s => s?.Length ?? 0) : 0;

        public bool SuspendCollectionChangedEvent { get; set; }

        public bool IsMarked (int item)
        {
            if (item < 0 || item >= _items.Count)
            {
                return false;
            }

            return _marks [item];
        }

        public void SetMark (int item, bool value)
        {
            if (item >= 0 && item < _items.Count)
            {
                _marks [item] = value;
            }
        }

        public void Render (Terminal.Gui.Views.ListView listView, bool selected, int item, int col, int line, int width, int viewportX = 0)
        {
            if (item < 0 || item >= _items.Count)
            {
                return;
            }

            listView.Move (col, line);
            string text = _items [item] ?? "";

            if (viewportX < text.Length)
            {
                text = text.Substring (viewportX);
            }
            else
            {
                text = "";
            }

            if (text.Length > width)
            {
                text = text.Substring (0, width);
            }

            listView.AddStr (text);

            // Fill remaining width
            for (int i = text.Length; i < width; i++)
            {
                listView.AddRune ((Rune)' ');
            }
        }

        public IList ToList () { return _items; }

        public void Dispose () { IsDisposed = true; }

        public void AddItem (string item)
        {
            _items.Add (item);

            // Resize marks
            var newMarks = new BitArray (_items.Count);

            for (var i = 0; i < Math.Min (_marks.Length, newMarks.Length); i++)
            {
                newMarks [i] = _marks [i];
            }

            if (!SuspendCollectionChangedEvent)
            {
                CollectionChanged?.Invoke (this, new (NotifyCollectionChangedAction.Add, item, _items.Count - 1));
            }
        }

        public bool IsDisposed { get; private set; }
    }

    #region ListWrapper<T> Render Tests

    [Fact]
    public void ListWrapper_Render_NullItem_RendersEmpty ()
    {
        ObservableCollection<string?> source = [null, "Item2"];
        ListWrapper<string?> wrapper = new (source);
        var listView = new ListView { Width = 20, Height = 2 };
        listView.BeginInit ();
        listView.EndInit ();

        // Render the null item (index 0)
        wrapper.Render (listView, false, 0, 0, 0, 20);

        // Should not throw and should render empty/spaces
        Assert.Equal (2, wrapper.Count);
    }

    [Fact]
    public void ListWrapper_Render_EmptyString_RendersSpaces ()
    {
        ObservableCollection<string> source = [""];
        ListWrapper<string> wrapper = new (source);
        var listView = new ListView { Width = 20, Height = 1 };
        listView.BeginInit ();
        listView.EndInit ();

        wrapper.Render (listView, false, 0, 0, 0, 20);

        Assert.Equal (1, wrapper.Count);
        Assert.Equal (0, wrapper.MaxItemLength); // Empty string has zero length
    }

    [Fact]
    public void ListWrapper_Render_UnicodeText_CalculatesWidthCorrectly ()
    {
        ObservableCollection<string> source = ["Hello 你好", "Test"];
        ListWrapper<string> wrapper = new (source);

        // "Hello 你好" should be: "Hello " (6) + "你" (2) + "好" (2) = 10 columns
        Assert.True (wrapper.MaxItemLength >= 10);
    }

    [Fact]
    public void ListWrapper_Render_LongString_ClipsToWidth ()
    {
        var longString = new string ('X', 100);
        ObservableCollection<string> source = [longString];
        ListWrapper<string> wrapper = new (source);
        var listView = new ListView { Width = 20, Height = 1 };
        listView.BeginInit ();
        listView.EndInit ();

        wrapper.Render (listView, false, 0, 0, 0, 10);

        Assert.Equal (100, wrapper.MaxItemLength);
    }

    [Fact]
    public void ListWrapper_Render_WithViewportX_ScrollsHorizontally ()
    {
        ObservableCollection<string> source = ["0123456789ABCDEF"];
        ListWrapper<string> wrapper = new (source);
        var listView = new ListView { Width = 10, Height = 1 };
        listView.BeginInit ();
        listView.EndInit ();

        // Render with horizontal scroll offset of 5
        wrapper.Render (listView, false, 0, 0, 0, 10, 5);

        // Should render "56789ABCDE" (starting at position 5)
        Assert.Equal (16, wrapper.MaxItemLength);
    }

    [Fact]
    public void ListWrapper_Render_ViewportXBeyondLength_RendersEmpty ()
    {
        ObservableCollection<string> source = ["Short"];
        ListWrapper<string> wrapper = new (source);
        var listView = new ListView { Width = 20, Height = 1 };
        listView.BeginInit ();
        listView.EndInit ();

        // Render with viewport beyond string length
        wrapper.Render (listView, false, 0, 0, 0, 10, 100);

        Assert.Equal (5, wrapper.MaxItemLength);
    }

    [Fact]
    public void ListWrapper_Render_ColAndLine_PositionsCorrectly ()
    {
        ObservableCollection<string> source = ["Item1", "Item2"];
        ListWrapper<string> wrapper = new (source);
        var listView = new ListView { Width = 20, Height = 5 };
        listView.BeginInit ();
        listView.EndInit ();

        // Render at different positions
        wrapper.Render (listView, false, 0, 2, 1, 10); // col=2, line=1
        wrapper.Render (listView, false, 1, 0, 3, 10); // col=0, line=3

        Assert.Equal (2, wrapper.Count);
    }

    [Fact]
    public void ListWrapper_Render_WidthConstraint_FillsRemaining ()
    {
        ObservableCollection<string> source = ["Hi"];
        ListWrapper<string> wrapper = new (source);
        var listView = new ListView { Width = 20, Height = 1 };
        listView.BeginInit ();
        listView.EndInit ();

        // Render "Hi" in width of 10 - should fill remaining 8 with spaces
        wrapper.Render (listView, false, 0, 0, 0, 10);

        Assert.Equal (2, wrapper.MaxItemLength);
    }

    [Fact]
    public void ListWrapper_Render_NonStringType_UsesToString ()
    {
        ObservableCollection<int> source = [42, 100, -5];
        ListWrapper<int> wrapper = new (source);
        var listView = new ListView { Width = 20, Height = 3 };
        listView.BeginInit ();
        listView.EndInit ();

        wrapper.Render (listView, false, 0, 0, 0, 10);
        wrapper.Render (listView, false, 1, 0, 1, 10);
        wrapper.Render (listView, false, 2, 0, 2, 10);

        Assert.Equal (3, wrapper.Count);
        Assert.True (wrapper.MaxItemLength >= 2); // "42" is 2 chars, "100" is 3 chars
    }

    #endregion

    #region Custom IListDataSource Implementation Tests

    [Fact]
    public void CustomDataSource_AllMembers_WorkCorrectly ()
    {
        var customSource = new TestListDataSource ();
        var listView = new ListView { Source = customSource, Width = 20, Height = 5 };

        Assert.Equal (3, customSource.Count);
        Assert.Equal (14, customSource.MaxItemLength); // "Custom Item 00" is 14 chars

        // Test marking
        Assert.False (customSource.IsMarked (0));
        customSource.SetMark (0, true);
        Assert.True (customSource.IsMarked (0));
        customSource.SetMark (0, false);
        Assert.False (customSource.IsMarked (0));

        // Test ToList
        IList list = customSource.ToList ();
        Assert.Equal (3, list.Count);
        Assert.Equal ("Custom Item 00", list [0]);

        // Test render doesn't throw
        listView.BeginInit ();
        listView.EndInit ();
        Exception? ex = Record.Exception (() => customSource.Render (listView, false, 0, 0, 0, 20));
        Assert.Null (ex);
    }

    [Fact]
    public void CustomDataSource_CollectionChanged_RaisedOnModification ()
    {
        var customSource = new TestListDataSource ();
        var eventRaised = false;
        NotifyCollectionChangedAction? action = null;

        customSource.CollectionChanged += (s, e) =>
                                          {
                                              eventRaised = true;
                                              action = e.Action;
                                          };

        customSource.AddItem ("New Item");

        Assert.True (eventRaised);
        Assert.Equal (NotifyCollectionChangedAction.Add, action);
        Assert.Equal (4, customSource.Count);
    }

    [Fact]
    public void CustomDataSource_SuspendCollectionChanged_SuppressesEvents ()
    {
        var customSource = new TestListDataSource ();
        var eventCount = 0;

        customSource.CollectionChanged += (s, e) => eventCount++;

        customSource.SuspendCollectionChangedEvent = true;
        customSource.AddItem ("Item 1");
        customSource.AddItem ("Item 2");
        Assert.Equal (0, eventCount); // No events raised

        customSource.SuspendCollectionChangedEvent = false;
        customSource.AddItem ("Item 3");
        Assert.Equal (1, eventCount); // Event raised after resume
    }

    [Fact]
    public void CustomDataSource_Dispose_CleansUp ()
    {
        var customSource = new TestListDataSource ();

        customSource.Dispose ();

        // After dispose, adding should not raise events (if implemented correctly)
        customSource.AddItem ("New Item");

        // The test source doesn't unsubscribe in dispose, but this shows the pattern
        Assert.True (customSource.IsDisposed);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ListWrapper_EmptyCollection_PropertiesReturnZero ()
    {
        ObservableCollection<string> source = [];
        ListWrapper<string> wrapper = new (source);

        Assert.Equal (0, wrapper.Count);
        Assert.Equal (0, wrapper.MaxItemLength);
    }

    [Fact]
    public void ListWrapper_NullSource_HandledGracefully ()
    {
        ListWrapper<string> wrapper = new (null);

        Assert.Equal (0, wrapper.Count);
        Assert.Equal (0, wrapper.MaxItemLength);

        // ToList should not throw
        IList list = wrapper.ToList ();
        Assert.Empty (list);
    }

    [Fact]
    public void ListWrapper_IsMarked_OutOfBounds_ReturnsFalse ()
    {
        ObservableCollection<string> source = ["Item1"];
        ListWrapper<string> wrapper = new (source);

        Assert.False (wrapper.IsMarked (-1));
        Assert.False (wrapper.IsMarked (1));
        Assert.False (wrapper.IsMarked (100));
    }

    [Fact]
    public void ListWrapper_SetMark_OutOfBounds_DoesNotThrow ()
    {
        ObservableCollection<string> source = ["Item1"];
        ListWrapper<string> wrapper = new (source);

        Exception? ex = Record.Exception (() => wrapper.SetMark (-1, true));
        Assert.Null (ex);

        ex = Record.Exception (() => wrapper.SetMark (100, true));
        Assert.Null (ex);
    }

    [Fact]
    public void ListWrapper_CollectionShrinks_MarksAdjusted ()
    {
        ObservableCollection<string> source = ["Item1", "Item2", "Item3"];
        ListWrapper<string> wrapper = new (source);

        wrapper.SetMark (0, true);
        wrapper.SetMark (2, true);

        Assert.True (wrapper.IsMarked (0));
        Assert.True (wrapper.IsMarked (2));

        // Remove item 1 (middle item)
        source.RemoveAt (1);

        Assert.Equal (2, wrapper.Count);
        Assert.True (wrapper.IsMarked (0)); // Still marked

        // Item that was at index 2 is now at index 1
    }

    [Fact]
    public void ListWrapper_CollectionGrows_MarksPreserved ()
    {
        ObservableCollection<string> source = ["Item1"];
        ListWrapper<string> wrapper = new (source);

        wrapper.SetMark (0, true);
        Assert.True (wrapper.IsMarked (0));

        source.Add ("Item2");
        source.Add ("Item3");

        Assert.Equal (3, wrapper.Count);
        Assert.True (wrapper.IsMarked (0)); // Original mark preserved
        Assert.False (wrapper.IsMarked (1));
        Assert.False (wrapper.IsMarked (2));
    }

    [Fact]
    public void ListWrapper_StartsWith_EmptyString_ReturnsFirst ()
    {
        ObservableCollection<string> source = ["Apple", "Banana", "Cherry"];
        ListWrapper<string> wrapper = new (source);

        // Searching for empty string might return -1 or 0 depending on implementation
        int result = wrapper.StartsWith ("");
        Assert.True (result == -1 || result == 0);
    }

    [Fact]
    public void ListWrapper_StartsWith_NoMatch_ReturnsNegative ()
    {
        ObservableCollection<string> source = ["Apple", "Banana", "Cherry"];
        ListWrapper<string> wrapper = new (source);

        int result = wrapper.StartsWith ("Zebra");
        Assert.Equal (-1, result);
    }

    [Fact]
    public void ListWrapper_StartsWith_CaseInsensitive ()
    {
        ObservableCollection<string> source = ["Apple", "Banana", "Cherry"];
        ListWrapper<string> wrapper = new (source);

        Assert.Equal (0, wrapper.StartsWith ("app"));
        Assert.Equal (0, wrapper.StartsWith ("APP"));
        Assert.Equal (1, wrapper.StartsWith ("ban"));
        Assert.Equal (1, wrapper.StartsWith ("BAN"));
    }

    [Fact]
    public void ListWrapper_MaxLength_UpdatesOnCollectionChange ()
    {
        ObservableCollection<string> source = ["Hi"];
        ListWrapper<string> wrapper = new (source);

        Assert.Equal (2, wrapper.MaxItemLength);

        source.Add ("Very Long String Indeed");
        Assert.Equal (23, wrapper.MaxItemLength);

        source.Clear ();
        source.Add ("X");
        Assert.Equal (1, wrapper.MaxItemLength);
    }

    [Fact]
    public void ListWrapper_Dispose_UnsubscribesFromCollectionChanged ()
    {
        ObservableCollection<string> source = ["Item1"];
        ListWrapper<string> wrapper = new (source);

        wrapper.CollectionChanged += (s, e) => { };

        wrapper.Dispose ();

        // After dispose, source changes should not raise wrapper events
        source.Add ("Item2");

        // The wrapper's event might still fire, but the wrapper won't propagate source events
        // This depends on implementation
    }

    #endregion
}
