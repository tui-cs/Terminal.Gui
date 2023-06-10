using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui {

	/// <inheritdoc/>
	/// <remarks>This implementation is based on a static <see cref="Collection"/> of objects.</remarks>
	public class CollectionNavigator : CollectionNavigatorBase {
		/// <summary>
		/// The collection of objects to search. <see cref="object.ToString()"/> is used to search the collection.
		/// </summary>
		public IList Collection { get; set; }

		/// <summary>
		/// Constructs a new CollectionNavigator.
		/// </summary>
		public CollectionNavigator () { }

		/// <summary>
		/// Constructs a new CollectionNavigator for the given collection.
		/// </summary>
		/// <param name="collection"></param>
		public CollectionNavigator (IList collection) => Collection = collection;

		/// <inheritdoc/>
		protected override object ElementAt (int idx)
		{
			return Collection [idx];
		}

		/// <inheritdoc/>
		protected override int GetCollectionLength ()
		{
			return Collection.Count;
		}

	}
}
