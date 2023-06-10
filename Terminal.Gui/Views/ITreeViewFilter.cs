namespace Terminal.Gui {

	/// <summary>
	/// Provides filtering for a <see cref="TreeView"/>.
	/// </summary>
	public interface ITreeViewFilter<T> where T : class {

		/// <summary>
		/// Return <see langword="true"/> if the <paramref name="model"/> should
		/// be included in the tree.
		/// </summary>
		bool IsMatch (T model);
	}
}