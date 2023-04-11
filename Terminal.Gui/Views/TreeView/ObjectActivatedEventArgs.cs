namespace Terminal.Gui {
	/// <summary>
	/// Event args for the <see cref="TreeView{T}.ObjectActivated"/> event
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ObjectActivatedEventArgs<T> where T : class {

		/// <summary>
		/// The tree in which the activation occurred
		/// </summary>
		/// <value></value>
		public TreeView<T> Tree { get; }

		/// <summary>
		/// The object that was selected at the time of activation
		/// </summary>
		/// <value></value>
		public T ActivatedObject { get; }


		/// <summary>
		/// Creates a new instance documenting activation of the <paramref name="activated"/> object
		/// </summary>
		/// <param name="tree">Tree in which the activation is happening</param>
		/// <param name="activated">What object is being activated</param>
		public ObjectActivatedEventArgs (TreeView<T> tree, T activated)
		{
			Tree = tree;
			ActivatedObject = activated;
		}
	}
}