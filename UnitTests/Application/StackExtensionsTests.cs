using System;
using System.Collections.Generic;
using Terminal.Gui;
using Xunit;

namespace Terminal.Gui.ApplicationTests {
	public class StackExtensionsTests {
		[Fact]
		public void Stack_Toplevels_CreateToplevels ()
		{
			Stack<Toplevel> Toplevels = CreateToplevels ();

			int index = Toplevels.Count - 1;
			foreach (var top in Toplevels) {
				if (top.GetType () == typeof (Toplevel)) 					Assert.Equal ("Top", top.Id);
else 					Assert.Equal ($"w{index}", top.Id);
				index--;
			}

			var tops = Toplevels.ToArray ();

			Assert.Equal ("w4", tops [0].Id);
			Assert.Equal ("w3", tops [1].Id);
			Assert.Equal ("w2", tops [2].Id);
			Assert.Equal ("w1", tops [3].Id);
			Assert.Equal ("Top", tops [^1].Id);
		}

		[Fact]
		public void Stack_Toplevels_Replace ()
		{
			Stack<Toplevel> Toplevels = CreateToplevels ();

			var valueToReplace = new Window () { Id = "w1" };
			var valueToReplaceWith = new Window () { Id = "new" };
			var comparer = new ToplevelEqualityComparer ();

			Toplevels.Replace (valueToReplace, valueToReplaceWith, comparer);

			var tops = Toplevels.ToArray ();

			Assert.Equal ("w4", tops [0].Id);
			Assert.Equal ("w3", tops [1].Id);
			Assert.Equal ("w2", tops [2].Id);
			Assert.Equal ("new", tops [3].Id);
			Assert.Equal ("Top", tops [^1].Id);
		}

		[Fact]
		public void Stack_Toplevels_Swap ()
		{
			Stack<Toplevel> Toplevels = CreateToplevels ();

			var valueToSwapFrom = new Window () { Id = "w3" };
			var valueToSwapTo = new Window () { Id = "w1" };
			var comparer = new ToplevelEqualityComparer ();
			Toplevels.Swap (valueToSwapFrom, valueToSwapTo, comparer);

			var tops = Toplevels.ToArray ();

			Assert.Equal ("w4", tops [0].Id);
			Assert.Equal ("w1", tops [1].Id);
			Assert.Equal ("w2", tops [2].Id);
			Assert.Equal ("w3", tops [3].Id);
			Assert.Equal ("Top", tops [^1].Id);
		}

		[Fact]
		public void Stack_Toplevels_MoveNext ()
		{
			Stack<Toplevel> Toplevels = CreateToplevels ();

			Toplevels.MoveNext ();

			var tops = Toplevels.ToArray ();

			Assert.Equal ("w3", tops [0].Id);
			Assert.Equal ("w2", tops [1].Id);
			Assert.Equal ("w1", tops [2].Id);
			Assert.Equal ("Top", tops [3].Id);
			Assert.Equal ("w4", tops [^1].Id);
		}

		[Fact]
		public void Stack_Toplevels_MovePrevious ()
		{
			Stack<Toplevel> Toplevels = CreateToplevels ();

			Toplevels.MovePrevious ();

			var tops = Toplevels.ToArray ();

			Assert.Equal ("Top", tops [0].Id);
			Assert.Equal ("w4", tops [1].Id);
			Assert.Equal ("w3", tops [2].Id);
			Assert.Equal ("w2", tops [3].Id);
			Assert.Equal ("w1", tops [^1].Id);
		}

		[Fact]
		public void ToplevelEqualityComparer_GetHashCode ()
		{
			Stack<Toplevel> Toplevels = CreateToplevels ();

			// Only allows unique keys
			var hCodes = new HashSet<int> ();

			foreach (var top in Toplevels) 				Assert.True (hCodes.Add (top.GetHashCode ()));
		}

		[Fact]
		public void Stack_Toplevels_FindDuplicates ()
		{
			Stack<Toplevel> Toplevels = CreateToplevels ();
			var comparer = new ToplevelEqualityComparer ();

			Toplevels.Push (new Toplevel () { Id = "w4" });
			Toplevels.Push (new Toplevel () { Id = "w1" });

			var dup = Toplevels.FindDuplicates (comparer).ToArray ();

			Assert.Equal ("w4", dup [0].Id);
			Assert.Equal ("w1", dup [^1].Id);
		}

		[Fact]
		public void Stack_Toplevels_Contains ()
		{
			Stack<Toplevel> Toplevels = CreateToplevels ();
			var comparer = new ToplevelEqualityComparer ();

			Assert.True (Toplevels.Contains (new Window () { Id = "w2" }, comparer));
			Assert.False (Toplevels.Contains (new Toplevel () { Id = "top2" }, comparer));
		}

		[Fact]
		public void Stack_Toplevels_MoveTo ()
		{
			Stack<Toplevel> Toplevels = CreateToplevels ();

			var valueToMove = new Window () { Id = "w1" };
			var comparer = new ToplevelEqualityComparer ();

			Toplevels.MoveTo (valueToMove, 1, comparer);

			var tops = Toplevels.ToArray ();

			Assert.Equal ("w4", tops [0].Id);
			Assert.Equal ("w1", tops [1].Id);
			Assert.Equal ("w3", tops [2].Id);
			Assert.Equal ("w2", tops [3].Id);
			Assert.Equal ("Top", tops [^1].Id);
		}

		[Fact]
		public void Stack_Toplevels_MoveTo_From_Last_To_Top ()
		{
			Stack<Toplevel> Toplevels = CreateToplevels ();

			var valueToMove = new Window () { Id = "Top" };
			var comparer = new ToplevelEqualityComparer ();

			Toplevels.MoveTo (valueToMove, 0, comparer);

			var tops = Toplevels.ToArray ();

			Assert.Equal ("Top", tops [0].Id);
			Assert.Equal ("w4", tops [1].Id);
			Assert.Equal ("w3", tops [2].Id);
			Assert.Equal ("w2", tops [3].Id);
			Assert.Equal ("w1", tops [^1].Id);
		}

		private Stack<Toplevel> CreateToplevels ()
		{
			var Toplevels = new Stack<Toplevel> ();

			Toplevels.Push (new Toplevel () { Id = "Top" });
			Toplevels.Push (new Window () { Id = "w1" });
			Toplevels.Push (new Window () { Id = "w2" });
			Toplevels.Push (new Window () { Id = "w3" });
			Toplevels.Push (new Window () { Id = "w4" });

			return Toplevels;
		}
	}
}
