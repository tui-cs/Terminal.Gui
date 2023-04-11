using System;
using System.Collections.Generic;

namespace Terminal.Gui {

	static class ReadOnlyCollectionExtensions {

		public static int IndexOf<T> (this IReadOnlyCollection<T> self, Func<T, bool> predicate)
		{
			int i = 0;
			foreach (T element in self) {
				if (predicate (element))
					return i;
				i++;
			}
			return -1;
		}
		public static int IndexOf<T> (this IReadOnlyCollection<T> self, T toFind)
		{
			int i = 0;
			foreach (T element in self) {
				if (Equals (element, toFind))
					return i;
				i++;
			}
			return -1;
		}
	}
}
