namespace Terminal.Gui {
	/// <summary>
	/// Describes the context in which icons are being sought
	/// during <see cref="FileDialogIconGetterArgs"/>.
	/// </summary>
	public enum FileDialogIconGetterContext {

		/// <summary>
		/// Icon will be used in the tree view
		/// </summary>
		Tree,

		/// <summary>
		/// Icon will be used in the main table area of the dialog
		/// </summary>
		Table
	}
}