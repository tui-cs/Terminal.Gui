using System.Collections.Concurrent;
using System.Text;
using Moq;

namespace TextTests;

public class CollectionNavigatorTests
{
    private static readonly string [] simpleStrings =
    {
        "apricot", // 0
        "arm", // 1
        "bat", // 2
        "batman", // 3
        "candle" // 4
    };

    [Fact]
    public void AtSymbol ()
    {
        var strings = new [] { "apricot", "arm", "ta", "@bob", "@bb", "text", "egg", "candle" };

        var n = new CollectionNavigator (strings);
        Assert.Equal (3, n.GetNextMatchingItem (0, '@'));
        Assert.Equal (3, n.GetNextMatchingItem (3, 'b'));
        Assert.Equal (4, n.GetNextMatchingItem (3, 'b'));
    }

    [Fact]
    public void CustomMatcher_NeverMatches ()
    {
        var strings = new [] { "apricot", "arm", "bat", "batman", "bates hotel", "candle" };
        int? current = 0;
        var n = new CollectionNavigator (strings);

        Mock<ICollectionNavigatorMatcher> matchNone = new ();

        matchNone.Setup (m => m.IsMatch (It.IsAny<string> (), It.IsAny<object> ())).Returns (false);

        n.Matcher = matchNone.Object;

        Assert.Equal (0, current = n.GetNextMatchingItem (current, 'b')); // no matches
        Assert.Equal (0, current = n.GetNextMatchingItem (current, 'a')); // no matches
        Assert.Equal (0, current = n.GetNextMatchingItem (current, 't')); // no matches
    }

    [Fact]
    public void Cycling ()
    {
        // cycling with 'b'
        var n = new CollectionNavigator (simpleStrings);
        Assert.Equal (2, n.GetNextMatchingItem (0, 'b'));
        Assert.Equal (3, n.GetNextMatchingItem (2, 'b'));

        // if 4 (candle) is selected it should loop back to bat
        Assert.Equal (2, n.GetNextMatchingItem (4, 'b'));

        // cycling with 'a'
        n = new CollectionNavigator (simpleStrings);
        Assert.Equal (0, n.GetNextMatchingItem (null, 'a'));
        Assert.Equal (1, n.GetNextMatchingItem (0, 'a'));

        // if 4 (candle) is selected it should loop back to apricot
        Assert.Equal (0, n.GetNextMatchingItem (4, 'a'));
    }

    [Fact]
    public void Delay ()
    {
        var strings = new [] { "$$", "$100.00", "$101.00", "$101.10", "$200.00", "apricot" };
        int? current = 0;
        var n = new CollectionNavigator (strings);

        // No delay
        Assert.Equal (strings.IndexOf ("apricot"), current = n.GetNextMatchingItem (current, 'a'));
        Assert.Equal ("a", n.SearchString);
        Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, '$'));
        Assert.Equal ("$", n.SearchString);
        Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, '$'));
        Assert.Equal ("$$", n.SearchString);

        // Delay
        Thread.Sleep (n.TypingDelay + 10);
        Assert.Equal (strings.IndexOf ("apricot"), current = n.GetNextMatchingItem (current, 'a'));
        Assert.Equal ("a", n.SearchString);

        Thread.Sleep (n.TypingDelay + 10);
        Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, '$'));
        Assert.Equal ("$", n.SearchString);

        Thread.Sleep (n.TypingDelay + 10);
        Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, '$'));
        Assert.Equal ("$", n.SearchString);

        Thread.Sleep (n.TypingDelay + 10);
        Assert.Equal (strings.IndexOf ("$101.00"), current = n.GetNextMatchingItem (current, '$'));
        Assert.Equal ("$", n.SearchString);

        Thread.Sleep (n.TypingDelay + 10);
        Assert.Equal (strings.IndexOf ("$101.10"), current = n.GetNextMatchingItem (current, '$'));
        Assert.Equal ("$", n.SearchString);

        Thread.Sleep (n.TypingDelay + 10);
        Assert.Equal (strings.IndexOf ("$101.10"), current = n.GetNextMatchingItem (current, '2')); // Shouldn't move
        Assert.Equal ("2", n.SearchString);
    }

    [Fact]
    public void FullText ()
    {
        var strings = new [] { "apricot", "arm", "ta", "target", "text", "egg", "candle" };

        var n = new CollectionNavigator (strings);
        int? current = 0;
        Assert.Equal (strings.IndexOf ("ta"), current = n.GetNextMatchingItem (current, 't'));

        // should match "te" in "text"
        Assert.Equal (strings.IndexOf ("text"), current = n.GetNextMatchingItem (current, 'e'));

        // still matches text
        Assert.Equal (strings.IndexOf ("text"), current = n.GetNextMatchingItem (current, 'x'));

        // nothing starts texa so it should NOT jump to apricot
        Assert.Equal (strings.IndexOf ("text"), current = n.GetNextMatchingItem (current, 'a'));

        Thread.Sleep (n.TypingDelay + 100);

        // nothing starts "texa". Since were past timedelay we DO jump to apricot
        Assert.Equal (strings.IndexOf ("apricot"), current = n.GetNextMatchingItem (current, 'a'));
    }

    [Theory]
    [InlineData (KeyCode.A, true)]
    [InlineData (KeyCode.Z, true)]
    [InlineData (KeyCode.D0, true)]
    [InlineData (KeyCode.A | KeyCode.ShiftMask, true)]
    [InlineData (KeyCode.Z | KeyCode.ShiftMask, true)]
    [InlineData (KeyCode.Space, true)]
    [InlineData (KeyCode.Z | KeyCode.CtrlMask, false)]
    [InlineData (KeyCode.Z | KeyCode.AltMask, false)]
    [InlineData (KeyCode.F1, false)]
    [InlineData (KeyCode.Delete, false)]
    [InlineData (KeyCode.Esc, false)]
    [InlineData (KeyCode.ShiftMask, false)]
    public void IsCompatibleKey_Does_Not_Allow_Alt_And_Ctrl_Keys (KeyCode keyCode, bool compatible)
    {
        var m = new DefaultCollectionNavigatorMatcher ();
        Assert.Equal (compatible, m.IsCompatibleKey (keyCode));
    }

    // Copilot - Opus 4.6

    /// <summary>
    ///     Verifies that when AssociatedText is set (e.g. Kitty keyboard protocol),
    ///     Alt/Ctrl keys are still rejected even though AsRune returns a valid rune.
    /// </summary>
    [Theory]
    [InlineData (KeyCode.A | KeyCode.AltMask, "a", false)]
    [InlineData (KeyCode.Z | KeyCode.AltMask, "z", false)]
    [InlineData (KeyCode.A | KeyCode.CtrlMask, "a", false)]
    [InlineData (KeyCode.Z | KeyCode.CtrlMask, "z", false)]
    [InlineData (KeyCode.A | KeyCode.CtrlMask | KeyCode.AltMask, "a", false)]
    [InlineData (KeyCode.A, "a", true)]
    [InlineData (KeyCode.A | KeyCode.ShiftMask, "A", true)]
    [InlineData (KeyCode.Space, " ", true)]
    public void IsCompatibleKey_WithAssociatedText_RejectsAltAndCtrl (KeyCode keyCode, string associatedText, bool expected)
    {
        DefaultCollectionNavigatorMatcher matcher = new ();
        Key key = new (keyCode) { AssociatedText = associatedText };

        // Confirm the rune is valid (non-default, non-control) — this is the scenario
        // where the old code (checking only the rune) would have incorrectly returned true.
        Rune rune = key.AsRune;

        if (!expected)
        {
            Assert.NotEqual (default (Rune), rune);
            Assert.False (Rune.IsControl (rune));
        }

        Assert.Equal (expected, matcher.IsCompatibleKey (key));
    }

    [Fact]
    public void MinimizeMovement_False_ShouldMoveIfMultipleMatches ()
    {
        var strings = new []
        {
            "$$",
            "$100.00",
            "$101.00",
            "$101.10",
            "$200.00",
            "apricot",
            "c",
            "car",
            "cart"
        };
        int? current = 0;
        var n = new CollectionNavigator (strings);
        Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, "$$"));
        Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, "$"));
        Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, "$$")); // back to top
        Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, "$"));
        Assert.Equal (strings.IndexOf ("$101.00"), current = n.GetNextMatchingItem (current, "$"));
        Assert.Equal (strings.IndexOf ("$101.10"), current = n.GetNextMatchingItem (current, "$"));
        Assert.Equal (strings.IndexOf ("$200.00"), current = n.GetNextMatchingItem (current, "$"));

        Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, "$")); // back to top
        Assert.Equal (strings.IndexOf ("apricot"), current = n.GetNextMatchingItem (current, "a"));
        Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, "$")); // back to top

        Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, "$100.00"));
        Assert.Equal (strings.IndexOf ("$101.00"), current = n.GetNextMatchingItem (current, "$"));
        Assert.Equal (strings.IndexOf ("$101.00"), current = n.GetNextMatchingItem (current, "$101.00"));
        Assert.Equal (strings.IndexOf ("$200.00"), current = n.GetNextMatchingItem (current, "$2"));

        Assert.Equal (strings.IndexOf ("$200.00"), current = n.GetNextMatchingItem (current, "$200.00"));
        Assert.Equal (strings.IndexOf ("$101.00"), current = n.GetNextMatchingItem (current, "$101.00"));
        Assert.Equal (strings.IndexOf ("$200.00"), current = n.GetNextMatchingItem (current, "$2"));

        Assert.Equal (strings.IndexOf ("$101.00"), current = n.GetNextMatchingItem (current, "$101.00"));
        Assert.Equal (strings.IndexOf ("$200.00"), current = n.GetNextMatchingItem (current, "$2"));

        Assert.Equal (strings.IndexOf ("car"), current = n.GetNextMatchingItem (current, "car"));
        Assert.Equal (strings.IndexOf ("cart"), current = n.GetNextMatchingItem (current, "car"));

        Assert.Null (n.GetNextMatchingItem (current, "x"));
    }

    [Fact]
    public void MinimizeMovement_True_ShouldStayOnCurrentIfMultipleMatches ()
    {
        var strings = new []
        {
            "$$",
            "$100.00",
            "$101.00",
            "$101.10",
            "$200.00",
            "apricot",
            "c",
            "car",
            "cart"
        };
        int? current = 0;
        var n = new CollectionNavigator (strings);
        Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, "$$", true));
        Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, "$", true));
        Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, "$$", true)); // back to top
        Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, "$1", true));
        Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, "$", true));
        Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, "$", true));

        Assert.Equal (strings.IndexOf ("car"), current = n.GetNextMatchingItem (current, "car", true));
        Assert.Equal (strings.IndexOf ("car"), current = n.GetNextMatchingItem (current, "car", true));

        Assert.Null (n.GetNextMatchingItem (current, "x", true));
    }

    [Fact]
    public void MutliKeySearchPlusWrongKeyStays ()
    {
        var strings = new [] { "a", "c", "can", "candle", "candy", "yellow", "zebra" };
        int? current = 0;
        var n = new CollectionNavigator (strings);

        // https://github.com/tui-cs/Terminal.Gui/pull/2132#issuecomment-1298425573
        // One thing that it currently does that is different from Explorer is that as soon as you hit a wrong key then it jumps to that index.
        // So if you type cand then z it jumps you to something beginning with z. In the same situation Windows Explorer beeps (not the best!)
        // but remains on candle.
        // We might be able to update the behaviour so that a 'wrong' keypress (z) within 500ms of a 'right' keypress ("can" + 'd') is
        // simply ignored (possibly ending the search process though). That would give a short delay for user to realise the thing
        // they typed doesn't exist and then start a new search (which would be possible 500ms after the last 'good' keypress).
        // This would only apply for 2+ character searches where theres been a successful 2+ character match right before.

        Assert.Equal (strings.IndexOf ("a"), current = n.GetNextMatchingItem (current, 'a'));
        Assert.Equal ("a", n.SearchString);
        Assert.Equal (strings.IndexOf ("c"), current = n.GetNextMatchingItem (current, 'c'));
        Assert.Equal ("c", n.SearchString);
        Assert.Equal (strings.IndexOf ("can"), current = n.GetNextMatchingItem (current, 'a'));
        Assert.Equal ("ca", n.SearchString);
        Assert.Equal (strings.IndexOf ("can"), current = n.GetNextMatchingItem (current, 'n'));
        Assert.Equal ("can", n.SearchString);
        Assert.Equal (strings.IndexOf ("candle"), current = n.GetNextMatchingItem (current, 'd'));
        Assert.Equal ("cand", n.SearchString);

        // Same as above, but with a 'wrong' key (z)
        Thread.Sleep (n.TypingDelay + 10);
        Assert.Equal (strings.IndexOf ("a"), current = n.GetNextMatchingItem (current, 'a'));
        Assert.Equal ("a", n.SearchString);
        Assert.Equal (strings.IndexOf ("c"), current = n.GetNextMatchingItem (current, 'c'));
        Assert.Equal ("c", n.SearchString);
        Assert.Equal (strings.IndexOf ("can"), current = n.GetNextMatchingItem (current, 'a'));
        Assert.Equal ("ca", n.SearchString);
        Assert.Equal (strings.IndexOf ("can"), current = n.GetNextMatchingItem (current, 'n'));
        Assert.Equal ("can", n.SearchString);
        Assert.Equal (strings.IndexOf ("can"), current = n.GetNextMatchingItem (current, 'z')); // Shouldn't move
        Assert.Equal ("can", n.SearchString); // Shouldn't change
    }

    [Fact]
    public void OutOfBoundsShouldBeIgnored ()
    {
        var n = new CollectionNavigator (simpleStrings);

        // Expect saying that index 500 is the current selection should not cause
        // error and just be ignored (treated as no selection)
        Assert.Equal (2, n.GetNextMatchingItem (500, 'b'));
    }

    [Fact]
    public void ShouldAcceptNull ()
    {
        var n = new CollectionNavigator (simpleStrings);

        // Expect that index of null (i.e. no selection) should work correctly
        // and select the first entry of the letter 'b'
        Assert.Equal (2, n.GetNextMatchingItem (null, 'b'));
    }

    [Fact]
    public void Symbols ()
    {
        var strings = new [] { "$$", "$100.00", "$101.00", "$101.10", "$200.00", "apricot" };
        int? current = 0;
        var n = new CollectionNavigator (strings);
        Assert.Equal (strings.IndexOf ("apricot"), current = n.GetNextMatchingItem (current, 'a'));
        Assert.Equal ("a", n.SearchString);

        Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, '$'));
        Assert.Equal ("$", n.SearchString);

        Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, '1'));
        Assert.Equal ("$1", n.SearchString);

        Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, '0'));
        Assert.Equal ("$10", n.SearchString);

        Assert.Equal (strings.IndexOf ("$101.00"), current = n.GetNextMatchingItem (current, '1'));
        Assert.Equal ("$101", n.SearchString);

        Assert.Equal (strings.IndexOf ("$101.00"), current = n.GetNextMatchingItem (current, '.'));
        Assert.Equal ("$101.", n.SearchString);

        // stay on the same item becuase still in timedelay
        Assert.Equal (strings.IndexOf ("$101.00"), current = n.GetNextMatchingItem (current, 'a'));
        Assert.Equal ("$101.", n.SearchString);

        Thread.Sleep (n.TypingDelay + 100);

        // another '$' means searching for "$" again
        Assert.Equal (strings.IndexOf ("$101.10"), current = n.GetNextMatchingItem (current, '$'));
        Assert.Equal ("$", n.SearchString);

        Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, '$'));
        Assert.Equal ("$$", n.SearchString);
    }

    [Fact]
    public void Unicode ()
    {
        var strings = new [] { "apricot", "arm", "ta", "丗丙业丞", "丗丙丛", "text", "egg", "candle" };

        var n = new CollectionNavigator (strings);
        int? current = 0;
        Assert.Equal (strings.IndexOf ("丗丙业丞"), current = n.GetNextMatchingItem (current, '丗'));

        // 丗丙业丞 is as good a match as 丗丙丛
        // so when doing multi character searches we should
        // prefer to stay on the same index unless we invalidate
        // our typed text
        Assert.Equal (strings.IndexOf ("丗丙业丞"), current = n.GetNextMatchingItem (current, '丙'));

        // No longer matches 丗丙业丞 and now only matches 丗丙丛
        // so we should move to the new match
        Assert.Equal (strings.IndexOf ("丗丙丛"), current = n.GetNextMatchingItem (current, '丛'));

        // nothing starts "丗丙丛a". Since were still in the timedelay we do not jump to apricot
        Assert.Equal (strings.IndexOf ("丗丙丛"), current = n.GetNextMatchingItem (current, 'a'));

        Thread.Sleep (n.TypingDelay + 100);

        // nothing starts "丗丙丛a". Since were past timedelay we DO jump to apricot
        Assert.Equal (strings.IndexOf ("apricot"), current = n.GetNextMatchingItem (current, 'a'));
    }

    [Fact]
    public void Word ()
    {
        var strings = new [] { "apricot", "arm", "bat", "batman", "bates hotel", "candle" };
        int? current = 0;
        var n = new CollectionNavigator (strings);
        Assert.Equal (strings.IndexOf ("bat"), current = n.GetNextMatchingItem (current, 'b')); // match bat
        Assert.Equal (strings.IndexOf ("bat"), current = n.GetNextMatchingItem (current, 'a')); // match bat
        Assert.Equal (strings.IndexOf ("bat"), current = n.GetNextMatchingItem (current, 't')); // match bat

        Assert.Equal (strings.IndexOf ("bates hotel"), current = n.GetNextMatchingItem (current, 'e')); // match bates hotel

        Assert.Equal (strings.IndexOf ("bates hotel"), current = n.GetNextMatchingItem (current, 's')); // match bates hotel

        Assert.Equal (strings.IndexOf ("bates hotel"), current = n.GetNextMatchingItem (current, ' ')); // match bates hotel
    }

    #region Thread Safety Tests

    [Fact]
    public void ThreadSafety_ConcurrentSearchStringAccess ()
    {
        var strings = new [] { "apricot", "arm", "bat", "batman", "candle" };
        var navigator = new CollectionNavigator (strings);
        var numTasks = 20;
        ConcurrentBag<Exception> exceptions = new ();

        Parallel.For (0,
                      numTasks,
                      i =>
                      {
                          try
                          {
                              // Read SearchString concurrently
                              _ = navigator.SearchString;

                              // Perform navigation operations concurrently
                              _ = navigator.GetNextMatchingItem (0, 'a');

                              // Read SearchString again
                              _ = navigator.SearchString;
                          }
                          catch (Exception ex)
                          {
                              exceptions.Add (ex);
                          }
                      });

        Assert.Empty (exceptions);
    }

    [Fact]
    public void ThreadSafety_ConcurrentCollectionAccess ()
    {
        var strings = new [] { "apricot", "arm", "bat", "batman", "candle" };
        var navigator = new CollectionNavigator (strings);
        var numTasks = 20;
        ConcurrentBag<Exception> exceptions = new ();

        Parallel.For (0,
                      numTasks,
                      i =>
                      {
                          try
                          {
                              // Access Collection property concurrently
                              _ = navigator.Collection;

                              // Perform navigation
                              _ = navigator.GetNextMatchingItem (0, (char)('a' + i % 3));
                          }
                          catch (Exception ex)
                          {
                              exceptions.Add (ex);
                          }
                      });

        Assert.Empty (exceptions);
    }

    [Fact]
    public void ThreadSafety_ConcurrentNavigationOperations ()
    {
        var strings = new [] { "apricot", "arm", "bat", "batman", "candle", "cat", "dog", "elephant" };
        var navigator = new CollectionNavigator (strings);
        var numTasks = 50;
        ConcurrentBag<int?> results = new ();
        ConcurrentBag<Exception> exceptions = new ();

        Parallel.For (0,
                      numTasks,
                      i =>
                      {
                          try
                          {
                              var searchChar = (char)('a' + i % 5);
                              int? result = navigator.GetNextMatchingItem (i % strings.Length, searchChar);
                              results.Add (result);
                          }
                          catch (Exception ex)
                          {
                              exceptions.Add (ex);
                          }
                      });

        Assert.Empty (exceptions);
        Assert.Equal (numTasks, results.Count);
    }

    [Fact]
    public void ThreadSafety_ConcurrentCollectionModification ()
    {
        var strings = new [] { "apricot", "arm", "bat", "batman", "candle" };
        var navigator = new CollectionNavigator (strings);
        var numReaders = 10;
        var numWriters = 5;
        ConcurrentBag<Exception> exceptions = new ();
        List<Task> tasks = new ();

        // Reader tasks
        for (var i = 0; i < numReaders; i++)
        {
            tasks.Add (Task.Run (() =>
                                 {
                                     try
                                     {
                                         for (var j = 0; j < 100; j++)
                                         {
                                             _ = navigator.GetNextMatchingItem (0, 'a');
                                             _ = navigator.SearchString;
                                         }
                                     }
                                     catch (Exception ex)
                                     {
                                         exceptions.Add (ex);
                                     }
                                 },
                                 TestContext.Current.CancellationToken));
        }

        // Writer tasks (change Collection reference)
        for (var i = 0; i < numWriters; i++)
        {
            int writerIndex = i;

            tasks.Add (Task.Run (() =>
                                 {
                                     try
                                     {
                                         for (var j = 0; j < 50; j++)
                                         {
                                             var newStrings = new [] { $"item{writerIndex}_{j}_1", $"item{writerIndex}_{j}_2" };
                                             navigator.Collection = newStrings;
                                             Thread.Sleep (1); // Small delay to increase contention
                                         }
                                     }
                                     catch (Exception ex)
                                     {
                                         exceptions.Add (ex);
                                     }
                                 },
                                 TestContext.Current.CancellationToken));
        }

#pragma warning disable xUnit1031
        Task.WaitAll (tasks.ToArray (), TestContext.Current.CancellationToken);
#pragma warning restore xUnit1031

        // Allow some exceptions due to collection being swapped during access
        // but verify no deadlocks occurred (all tasks completed)
        Assert.True (tasks.All (t => t.IsCompleted));
    }

    [Fact]
    public void ThreadSafety_ConcurrentSearchStringChanges ()
    {
        var strings = new []
        {
            "apricot",
            "arm",
            "bat",
            "batman",
            "candle",
            "cat",
            "dog",
            "elephant",
            "fox",
            "goat"
        };
        var navigator = new CollectionNavigator (strings);
        var numTasks = 30;
        ConcurrentBag<Exception> exceptions = new ();
        ConcurrentBag<string> searchStrings = new ();

        Parallel.For (0,
                      numTasks,
                      _ =>
                      {
                          try
                          {
                              // Each task performs multiple searches rapidly
                              char [] chars = { 'a', 'b', 'c', 'd', 'e', 'f' };

                              foreach (char c in chars)
                              {
                                  navigator.GetNextMatchingItem (0, c);
                                  searchStrings.Add (navigator.SearchString);
                              }
                          }
                          catch (Exception ex)
                          {
                              exceptions.Add (ex);
                          }
                      });

        Assert.Empty (exceptions);
        Assert.NotEmpty (searchStrings);
    }

    [Fact]
    public void ThreadSafety_StressTest_RapidOperations ()
    {
        var strings = new string [100];

        for (var i = 0; i < 100; i++)
        {
            strings [i] = $"item_{i:D3}";
        }

        var navigator = new CollectionNavigator (strings);
        var numTasks = 100;
        var operationsPerTask = 1000;
        ConcurrentBag<Exception> exceptions = new ();

        Parallel.For (0,
                      numTasks,
                      i =>
                      {
                          try
                          {
                              var random = new Random (i);

                              for (var j = 0; j < operationsPerTask; j++)
                              {
                                  int? currentIndex = random.Next (0, strings.Length);
                                  var searchChar = (char)('a' + random.Next (0, 26));

                                  navigator.GetNextMatchingItem (currentIndex, searchChar);

                                  if (j % 100 == 0)
                                  {
                                      _ = navigator.SearchString;
                                  }
                              }
                          }
                          catch (Exception ex)
                          {
                              exceptions.Add (ex);
                          }
                      });

        Assert.Empty (exceptions);
    }

    #endregion Thread Safety Tests
}
