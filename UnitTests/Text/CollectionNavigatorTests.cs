using System;
using System.Threading;
using Terminal.Gui;
using Xunit;

namespace Terminal.Gui.TextTests {
	public class CollectionNavigatorTests {
		static string [] simpleStrings = new string []{
		    "appricot", // 0
		    "arm",      // 1
		    "bat",      // 2
		    "batman",   // 3
		    "candle"    // 4
		  };

		[Fact]
		public void ShouldAcceptNegativeOne ()
		{
			var n = new CollectionNavigator (simpleStrings);

			// Expect that index of -1 (i.e. no selection) should work correctly
			// and select the first entry of the letter 'b'
			Assert.Equal (2, n.GetNextMatchingItem (-1, 'b'));
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
			Assert.Equal (0, n.GetNextMatchingItem (-1, 'a'));
			Assert.Equal (1, n.GetNextMatchingItem (0, 'a'));

			// if 4 (candle) is selected it should loop back to appricot
			Assert.Equal (0, n.GetNextMatchingItem (4, 'a'));
		}

		[Fact]
		public void FullText ()
		{
			var strings = new string []{
			    "appricot",
			    "arm",
			    "ta",
			    "target",
			    "text",
			    "egg",
			    "candle"
			  };

			var n = new CollectionNavigator (strings);
			int current = 0;
			Assert.Equal (strings.IndexOf ("ta"), current = n.GetNextMatchingItem (current, 't'));

			// should match "te" in "text"
			Assert.Equal (strings.IndexOf ("text"), current = n.GetNextMatchingItem (current, 'e'));

			// still matches text
			Assert.Equal (strings.IndexOf ("text"), current = n.GetNextMatchingItem (current, 'x'));

			// nothing starts texa so it should NOT jump to appricot
			Assert.Equal (strings.IndexOf ("text"), current = n.GetNextMatchingItem (current, 'a'));

			Thread.Sleep (n.TypingDelay + 100);
			// nothing starts "texa". Since were past timedelay we DO jump to appricot
			Assert.Equal (strings.IndexOf ("appricot"), current = n.GetNextMatchingItem (current, 'a'));
		}

		[Fact]
		public void Unicode ()
		{
			var strings = new string []{
			    "appricot",
			    "arm",
			    "ta",
			    "丗丙业丞",
			    "丗丙丛",
			    "text",
			    "egg",
			    "candle"
			  };

			var n = new CollectionNavigator (strings);
			int current = 0;
			Assert.Equal (strings.IndexOf ("丗丙业丞"), current = n.GetNextMatchingItem (current, '丗'));

			// 丗丙业丞 is as good a match as 丗丙丛
			// so when doing multi character searches we should
			// prefer to stay on the same index unless we invalidate
			// our typed text
			Assert.Equal (strings.IndexOf ("丗丙业丞"), current = n.GetNextMatchingItem (current, '丙'));

			// No longer matches 丗丙业丞 and now only matches 丗丙丛
			// so we should move to the new match
			Assert.Equal (strings.IndexOf ("丗丙丛"), current = n.GetNextMatchingItem (current, '丛'));

			// nothing starts "丗丙丛a". Since were still in the timedelay we do not jump to appricot
			Assert.Equal (strings.IndexOf ("丗丙丛"), current = n.GetNextMatchingItem (current, 'a'));

			Thread.Sleep (n.TypingDelay + 100);
			// nothing starts "丗丙丛a". Since were past timedelay we DO jump to appricot
			Assert.Equal (strings.IndexOf ("appricot"), current = n.GetNextMatchingItem (current, 'a'));
		}

		[Fact]
		public void AtSymbol ()
		{
			var strings = new string []{
			    "appricot",
			    "arm",
			    "ta",
			    "@bob",
			    "@bb",
			    "text",
			    "egg",
			    "candle"
			  };

			var n = new CollectionNavigator (strings);
			Assert.Equal (3, n.GetNextMatchingItem (0, '@'));
			Assert.Equal (3, n.GetNextMatchingItem (3, 'b'));
			Assert.Equal (4, n.GetNextMatchingItem (3, 'b'));
		}

		[Fact]
		public void Word ()
		{
			var strings = new string []{
			    "appricot",
			    "arm",
			    "bat",
			    "batman",
			    "bates hotel",
			    "candle"
			  };
			int current = 0;
			var n = new CollectionNavigator (strings);
			Assert.Equal (strings.IndexOf ("bat"), current = n.GetNextMatchingItem (current, 'b')); // match bat
			Assert.Equal (strings.IndexOf ("bat"), current = n.GetNextMatchingItem (current, 'a')); // match bat
			Assert.Equal (strings.IndexOf ("bat"), current = n.GetNextMatchingItem (current, 't')); // match bat
			Assert.Equal (strings.IndexOf ("bates hotel"), current = n.GetNextMatchingItem (current, 'e')); // match bates hotel
			Assert.Equal (strings.IndexOf ("bates hotel"), current = n.GetNextMatchingItem (current, 's')); // match bates hotel
			Assert.Equal (strings.IndexOf ("bates hotel"), current = n.GetNextMatchingItem (current, ' ')); // match bates hotel
		}

		[Fact]
		public void Symbols ()
		{
			var strings = new string []{
			    "$$",
			    "$100.00",
			    "$101.00",
			    "$101.10",
			    "$200.00",
			    "appricot"
			  };
			int current = 0;
			var n = new CollectionNavigator (strings);
			Assert.Equal (strings.IndexOf ("appricot"), current = n.GetNextMatchingItem (current, 'a'));
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
		public void Delay ()
		{
			var strings = new string []{
			    "$$",
			    "$100.00",
			    "$101.00",
			    "$101.10",
			    "$200.00",
			    "appricot"
			  };
			int current = 0;
			var n = new CollectionNavigator (strings);

			// No delay
			Assert.Equal (strings.IndexOf ("appricot"), current = n.GetNextMatchingItem (current, 'a'));
			Assert.Equal ("a", n.SearchString);
			Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, '$'));
			Assert.Equal ("$", n.SearchString);
			Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, '$'));
			Assert.Equal ("$$", n.SearchString);

			// Delay 
			Thread.Sleep (n.TypingDelay + 10);
			Assert.Equal (strings.IndexOf ("appricot"), current = n.GetNextMatchingItem (current, 'a'));
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
		public void MutliKeySearchPlusWrongKeyStays ()
		{
			var strings = new string []{
				"a",
			    "c",
			    "can",
			    "candle",
			    "candy",
			    "yellow",
				"zebra"
			  };
			int current = 0;
			var n = new CollectionNavigator (strings);

			// https://github.com/gui-cs/Terminal.Gui/pull/2132#issuecomment-1298425573
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
		public void MinimizeMovement_False_ShouldMoveIfMultipleMatches ()
		{
			var strings = new string [] {
				"$$",
				"$100.00",
				"$101.00",
				"$101.10",
				"$200.00",
				"appricot",
				"c",
				"car",
				"cart",
			};
			int current = 0;
			var n = new CollectionNavigator (strings);
			Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, "$$", false));
			Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, "$", false));
			Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, "$$", false)); // back to top
			Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, "$", false));
			Assert.Equal (strings.IndexOf ("$101.00"), current = n.GetNextMatchingItem (current, "$", false));
			Assert.Equal (strings.IndexOf ("$101.10"), current = n.GetNextMatchingItem (current, "$", false));
			Assert.Equal (strings.IndexOf ("$200.00"), current = n.GetNextMatchingItem (current, "$", false));

			Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, "$", false)); // back to top
			Assert.Equal (strings.IndexOf ("appricot"), current = n.GetNextMatchingItem (current, "a", false));
			Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, "$", false)); // back to top

			Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, "$100.00", false));
			Assert.Equal (strings.IndexOf ("$101.00"), current = n.GetNextMatchingItem (current, "$", false));
			Assert.Equal (strings.IndexOf ("$101.00"), current = n.GetNextMatchingItem (current, "$101.00", false));
			Assert.Equal (strings.IndexOf ("$200.00"), current = n.GetNextMatchingItem (current, "$2", false));

			Assert.Equal (strings.IndexOf ("$200.00"), current = n.GetNextMatchingItem (current, "$200.00", false));
			Assert.Equal (strings.IndexOf ("$101.00"), current = n.GetNextMatchingItem (current, "$101.00", false));
			Assert.Equal (strings.IndexOf ("$200.00"), current = n.GetNextMatchingItem (current, "$2", false));

			Assert.Equal (strings.IndexOf ("$101.00"), current = n.GetNextMatchingItem (current, "$101.00", false));
			Assert.Equal (strings.IndexOf ("$200.00"), current = n.GetNextMatchingItem (current, "$2", false));

			Assert.Equal (strings.IndexOf ("car"), current = n.GetNextMatchingItem (current, "car", false));
			Assert.Equal (strings.IndexOf ("cart"), current = n.GetNextMatchingItem (current, "car", false));

			Assert.Equal (-1, current = n.GetNextMatchingItem (current, "x", false));
		}

		[Fact]
		public void MinimizeMovement_True_ShouldStayOnCurrentIfMultipleMatches ()
		{
			var strings = new string [] {
				"$$",
				"$100.00",
				"$101.00",
				"$101.10",
				"$200.00",
				"appricot",
				"c",
				"car",
				"cart",
			};
			int current = 0;
			var n = new CollectionNavigator (strings);
			Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, "$$", true));
			Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, "$", true));
			Assert.Equal (strings.IndexOf ("$$"), current = n.GetNextMatchingItem (current, "$$", true)); // back to top
			Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, "$1", true));
			Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, "$", true));
			Assert.Equal (strings.IndexOf ("$100.00"), current = n.GetNextMatchingItem (current, "$", true));

			Assert.Equal (strings.IndexOf ("car"), current = n.GetNextMatchingItem (current, "car", true));
			Assert.Equal (strings.IndexOf ("car"), current = n.GetNextMatchingItem (current, "car", true));

			Assert.Equal (-1, current = n.GetNextMatchingItem (current, "x", true));
		}
	}
}
