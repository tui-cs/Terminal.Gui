using System;
using System.Collections.Generic;

namespace Terminal.Gui {
	/// <summary>
	/// Implementation of <see cref="ITreeBuilder{T}"/> that uses user defined functions
	/// </summary>
	public class DelegateTreeBuilder<T> : TreeBuilder<T> {
		private Func<T, IEnumerable<T>> childGetter;
		private Func<T, bool> canExpand;

		/// <summary>
		/// Constructs an implementation of <see cref="ITreeBuilder{T}"/> that calls the user 
		/// defined method <paramref name="childGetter"/> to determine children
		/// </summary>
		/// <param name="childGetter"></param>
		/// <returns></returns>
		public DelegateTreeBuilder (Func<T, IEnumerable<T>> childGetter) : base (false)
		{
			this.childGetter = childGetter;
		}

		/// <summary>
		/// Constructs an implementation of <see cref="ITreeBuilder{T}"/> that calls the user 
		/// defined method <paramref name="childGetter"/> to determine children 
		/// and <paramref name="canExpand"/> to determine expandability
		/// </summary>
		/// <param name="childGetter"></param>
		/// <param name="canExpand"></param>
		/// <returns></returns>
		public DelegateTreeBuilder (Func<T, IEnumerable<T>> childGetter, Func<T, bool> canExpand) : base (true)
		{
			this.childGetter = childGetter;
			this.canExpand = canExpand;
		}

		/// <summary>
		/// Returns whether a node can be expanded based on the delegate passed during construction
		/// </summary>
		/// <param name="toExpand"></param>
		/// <returns></returns>
		public override bool CanExpand (T toExpand)
		{
			return canExpand?.Invoke (toExpand) ?? base.CanExpand (toExpand);
		}

		/// <summary>
		/// Returns children using the delegate method passed during construction
		/// </summary>
		/// <param name="forObject"></param>
		/// <returns></returns>
		public override IEnumerable<T> GetChildren (T forObject)
		{
			return childGetter.Invoke (forObject);
		}
	}
}