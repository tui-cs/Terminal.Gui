using System;
using System.Collections.Generic;

namespace Terminal.Gui {
	/// <summary>
	/// Extension of <see cref="Stack{T}"/> helper to work with specific <see cref="IEqualityComparer{T}"/>
	/// </summary>
	public static class StackExtensions {
		/// <summary>
		/// Replaces an stack object values that match with the value to replace.
		/// </summary>
		/// <typeparam name="T">The stack object type.</typeparam>
		/// <param name="stack">The stack object.</param>
		/// <param name="valueToReplace">Value to replace.</param>
		/// <param name="valueToReplaceWith">Value to replace with to what matches the value to replace.</param>
		/// <param name="comparer">The comparison object.</param>
		public static void Replace<T> (this Stack<T> stack, T valueToReplace,
			T valueToReplaceWith, IEqualityComparer<T> comparer = null)
		{
			comparer = comparer ?? EqualityComparer<T>.Default;

			var temp = new Stack<T> ();
			while (stack.Count > 0) {
				var value = stack.Pop ();
				if (comparer.Equals (value, valueToReplace)) {
					stack.Push (valueToReplaceWith);
					break;
				}
				temp.Push (value);
			}

			while (temp.Count > 0)
				stack.Push (temp.Pop ());
		}

		/// <summary>
		/// Swap two stack objects values that matches with the both values.
		/// </summary>
		/// <typeparam name="T">The stack object type.</typeparam>
		/// <param name="stack">The stack object.</param>
		/// <param name="valueToSwapFrom">Value to swap from.</param>
		/// <param name="valueToSwapTo">Value to swap to.</param>
		/// <param name="comparer">The comparison object.</param>
		public static void Swap<T> (this Stack<T> stack, T valueToSwapFrom,
			T valueToSwapTo, IEqualityComparer<T> comparer = null)
		{
			comparer = comparer ?? EqualityComparer<T>.Default;

			int index = stack.Count - 1;
			T [] stackArr = new T [stack.Count];
			while (stack.Count > 0) {
				var value = stack.Pop ();
				if (comparer.Equals (value, valueToSwapFrom)) {
					stackArr [index] = valueToSwapTo;
				} else if (comparer.Equals (value, valueToSwapTo)) {
					stackArr [index] = valueToSwapFrom;
				} else {
					stackArr [index] = value;
				}
				index--;
			}

			for (int i = 0; i < stackArr.Length; i++)
				stack.Push (stackArr [i]);
		}

		/// <summary>
		/// Move the first stack object value to the end.
		/// </summary>
		/// <typeparam name="T">The stack object type.</typeparam>
		/// <param name="stack">The stack object.</param>
		public static void MoveNext<T> (this Stack<T> stack)
		{
			var temp = new Stack<T> ();
			var last = stack.Pop ();
			while (stack.Count > 0) {
				var value = stack.Pop ();
				temp.Push (value);
			}
			temp.Push (last);

			while (temp.Count > 0)
				stack.Push (temp.Pop ());
		}

		/// <summary>
		/// Move the last stack object value to the top.
		/// </summary>
		/// <typeparam name="T">The stack object type.</typeparam>
		/// <param name="stack">The stack object.</param>
		public static void MovePrevious<T> (this Stack<T> stack)
		{
			var temp = new Stack<T> ();
			T first = default;
			while (stack.Count > 0) {
				var value = stack.Pop ();
				temp.Push (value);
				if (stack.Count == 1) {
					first = stack.Pop ();
				}
			}

			while (temp.Count > 0)
				stack.Push (temp.Pop ());
			stack.Push (first);
		}

		/// <summary>
		/// Find all duplicates stack objects values.
		/// </summary>
		/// <typeparam name="T">The stack object type.</typeparam>
		/// <param name="stack">The stack object.</param>
		/// <param name="comparer">The comparison object.</param>
		/// <returns>The duplicates stack object.</returns>
		public static Stack<T> FindDuplicates<T> (this Stack<T> stack, IEqualityComparer<T> comparer = null)
		{
			comparer = comparer ?? EqualityComparer<T>.Default;

			var dup = new Stack<T> ();
			T [] stackArr = stack.ToArray ();
			for (int i = 0; i < stackArr.Length; i++) {
				var value = stackArr [i];
				for (int j = i + 1; j < stackArr.Length; j++) {
					var valueToFind = stackArr [j];
					if (comparer.Equals (value, valueToFind) && !Contains (dup, valueToFind)) {
						dup.Push (value);
					}
				}
			}

			return dup;
		}

		/// <summary>
		/// Check if the stack object contains the value to find.
		/// </summary>
		/// <typeparam name="T">The stack object type.</typeparam>
		/// <param name="stack">The stack object.</param>
		/// <param name="valueToFind">Value to find.</param>
		/// <param name="comparer">The comparison object.</param>
		/// <returns><c>true</c> If the value was found.<c>false</c> otherwise.</returns>
		public static bool Contains<T> (this Stack<T> stack, T valueToFind, IEqualityComparer<T> comparer = null)
		{
			comparer = comparer ?? EqualityComparer<T>.Default;

			foreach (T obj in stack) {
				if (comparer.Equals (obj, valueToFind)) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Move the stack object value to the index.
		/// </summary>
		/// <typeparam name="T">The stack object type.</typeparam>
		/// <param name="stack">The stack object.</param>
		/// <param name="valueToMove">Value to move.</param>
		/// <param name="index">The index where to move.</param>
		/// <param name="comparer">The comparison object.</param>
		public static void MoveTo<T> (this Stack<T> stack, T valueToMove, int index = 0,
			IEqualityComparer<T> comparer = null)
		{
			if (index < 0) {
				return;
			}

			comparer = comparer ?? EqualityComparer<T>.Default;

			var temp = new Stack<T> ();
			var toMove = default (T);
			var stackCount = stack.Count;
			var count = 0;
			while (stack.Count > 0) {
				var value = stack.Pop ();
				if (comparer.Equals (value, valueToMove)) {
					toMove = value;
					break;
				}
				temp.Push (value);
				count++;
			}

			int idx = 0;
			while (stack.Count < stackCount) {
				if (count - idx == index) {
					stack.Push (toMove);
				} else {
					stack.Push (temp.Pop ());
				}
				idx++;
			}
		}
	}
}
