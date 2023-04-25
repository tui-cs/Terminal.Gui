namespace Terminal.Gui {
	/// <summary>
	/// A replacement suggestion made by <see cref="IAutocomplete"/>
	/// </summary>
	public class Suggestion {
		/// <summary>
		/// The number of characters to remove at the current cursor position
		/// before adding the <see cref="Replacement"/>
		/// </summary>
		public int Remove { get; }

		/// <summary>
		/// The user visible description for the <see cref="Replacement"/>. Typically
		/// this would be the same as <see cref="Replacement"/> but may vary in advanced
		/// use cases (e.g. Title= "ctor", Replacement = "MyClass()\n{\n}")
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// The replacement text that will be added
		/// </summary>
		public string Replacement { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="Suggestion"/> class.
		/// </summary>
		/// <param name="remove"></param>
		/// <param name="replacement"></param>
		/// <param name="title">User visible title for the suggestion or null if the same
		/// as <paramref name="replacement"/>.</param>
		public Suggestion (int remove, string replacement, string title = null)
		{
			Remove = remove;
			Replacement = replacement;
			Title = title ?? replacement;
		}
	}
}
