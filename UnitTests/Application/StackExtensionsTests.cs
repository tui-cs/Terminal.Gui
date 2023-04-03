using System;
using System.Collections.Generic;
using Terminal.Gui;
using Xunit;

namespace Terminal.Gui.ApplicationTests {
	public class StackExtensionsTests {
		[Fact]
		public void Stack_Toplevels_CreateToplevels ()
		{
			Stack<Toplevel> toplevels = CreateToplevels ();

			int index = toplevels.Count - 1;
			foreach (var top in toplevels) {
				if (top.GetType () == typeof (Toplevel)) 					Assert.Equal ("Top", top.Id);
else 					Assert.Equal ($"w{index}", top.Id);
				index--;
			}

			var tops = toplevels.ToArray ();

			Assert.Equal ("w4", tops [0].Id);
			Assert.Equal ("w3", tops [1].Id);
			Assert.Equal ("w2", tops [2].Id);
			Assert.Equal ("w1", tops [3].Id);
			Assert.Equal ("Top", tops [^1].Id);
		}

		[Fact]
		public void Stack_Toplevels_Replace ()
		{
			Stack<Toplevel> toplevels = CreateToplevels ();

			var valueToReplace = new Window () { Id = "w1" };
			var valueToReplaceWith = new Window () { Id = "new" };
			var comparer = new ToplevelEqualityComparer ();

			toplevels.Replace (valueToReplace, valueToReplaceWith, comparer);

			var tops = toplevels.ToArray ();

			Assert.Equal ("w4", tops [0].Id);
			Assert.Equal ("w3", tops [1].Id);
			Assert.Equal ("w2", tops [2].Id);
			Assert.Equal ("new", tops [3].Id);
			Assert.Equal ("Top", tops [^1].Id);
		}

		[Fact]
		public void Stack_Toplevels_Swap ()
		{
			Stack<Toplevel> toplevels = CreateToplevels ();

			var valueToSwapFrom = new Window () { Id = "w3" };
			var valueToSwapTo = new Window () { Id = "w1" };
			var comparer = new ToplevelEqualityComparer ();
			toplevels.Swap (valueToSwapFrom, valueToSwapTo, comparer);

			var tops = toplevels.ToArray ();

			Assert.Equal ("w4", tops [0].Id);
			Assert.Equal ("w1", tops [1].Id);
			Assert.Equal ("w2", tops [2].Id);
			Assert.Equal ("w3", tops [3].Id);
			Assert.Equal ("Top", tops [^1].Id);
		}

		[Fact]
		public void Stack_Toplevels_MoveNext ()
		{
			Stack<Toplevel> toplevels = CreateToplevels ();

			toplevels.MoveNext ();

			var tops = toplevels.ToArray ();

			Assert.Equal ("w3", tops [0].Id);
			Assert.Equal ("w2", tops [1].Id);
			Assert.Equal ("w1", tops [2].Id);
			Assert.Equal ("Top", tops [3].Id);
			Assert.Equal ("w4", tops [^1].Id);
		}

		[Fact]
		public void Stack_Toplevels_MovePrevious ()
		{
			Stack<Toplevel> toplevels = CreateToplevels ();

			toplevels.MovePrevious ();

			var tops = toplevels.ToArray ();

			Assert.Equal ("Top", tops [0].Id);
			Assert.Equal ("w4", tops [1].Id);
			Assert.Equal ("w3", tops [2].Id);
			Assert.Equal ("w2", tops [3].Id);
			Assert.Equal ("w1", tops [^1].Id);
		}

		[Fact]
		public void ToplevelEqualityComparer_GetHashCode ()
		{
			Stack<Toplevel> toplevels = CreateToplevels ();

			// Only allows unique keys
			var hCodes = new HashSet<int> ();

			foreach (var top in toplevels) 				Assert.True (hCodes.Add (top.GetHashCode ()));
		}

		[Fact]
		public void Stack_Toplevels_FindDuplicates ()
		{
			Stack<Toplevel> toplevels = CreateToplevels ();
			var comparer = new ToplevelEqualityComparer ();

			toplevels.Push (new Toplevel () { Id = "w4" });
			toplevels.Push (new Toplevel () { Id = "w1" });

			var dup = toplevels.FindDuplicates (comparer).ToArray ();

			Assert.Equal ("w4", dup [0].Id);
			Assert.Equal ("w1", dup [^1].Id);
		}

		[Fact]
		public void Stack_Toplevels_Contains ()
		{
			Stack<Toplevel> toplevels = CreateToplevels ();
			var comparer = new ToplevelEqualityComparer ();

			Assert.True (toplevels.Contains (new Window () { Id = "w2" }, comparer));
			Assert.False (toplevels.Contains (new Toplevel () { Id = "top2" }, comparer));
		}

		[Fact]
		public void Stack_Toplevels_MoveTo ()
		{
			Stack<Toplevel> toplevels = CreateToplevels ();

			var valueToMove = new Window () { Id = "w1" };
			var comparer = new ToplevelEqualityComparer ();

			toplevels.MoveTo (valueToMove, 1, comparer);

			var tops = toplevels.ToArray ();

			Assert.Equal ("w4", tops [0].Id);
			Assert.Equal ("w1", tops [1].Id);
			Assert.Equal ("w3", tops [2].Id);
			Assert.Equal ("w2", tops [3].Id);
			Assert.Equal ("Top", tops [^1].Id);
		}

		[Fact]
		public void Stack_Toplevels_MoveTo_From_Last_To_Top ()
		{
			Stack<Toplevel> toplevels = CreateToplevels ();

			var valueToMove = new Window () { Id = "Top" };
			var comparer = new ToplevelEqualityComparer ();

			toplevels.MoveTo (valueToMove, 0, comparer);

			var tops = toplevels.ToArray ();

			Assert.Equal ("Top", tops [0].Id);
			Assert.Equal ("w4", tops [1].Id);
			Assert.Equal ("w3", tops [2].Id);
			Assert.Equal ("w2", tops [3].Id);
			Assert.Equal ("w1", tops [^1].Id);
		}


		private Stack<Toplevel> CreateToplevels ()
		{
			var toplevels = new Stack<Toplevel> ();

			toplevels.Push (new Toplevel () { Id = "Top" });
			toplevels.Push (new Window () { Id = "w1" });
			toplevels.Push (new Window () { Id = "w2" });
			toplevels.Push (new Window () { Id = "w3" });
			toplevels.Push (new Window () { Id = "w4" });

			return toplevels;
		}
	}
}
