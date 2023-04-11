using System.Text;

namespace Terminal.Gui {
	/// <summary>
	/// Extension helper of <see cref="System.String"/> to work with specific text manipulation./>
	/// </summary>
	public static class StringExtensions {
		/// <summary>
		/// Repeats the <paramref name="instr"/> <paramref name="n"/>  times.
		/// </summary>
		/// <param name="instr">The text to repeat.</param>
		/// <param name="n">Number of times to repeat the text.</param>
		/// <returns>
		///  The text repeated if <paramref name="n"/> is greater than zero, 
		///  otherwise <see langword="null"/>.
		/// </returns>
		public static string Repeat (this string instr, int n)
		{
			if (n <= 0) {
				return null;
			}

			if (string.IsNullOrEmpty (instr) || n == 1) {
				return instr;
			}

			return new StringBuilder (instr.Length * n)
				.Insert (0, instr, n)
				.ToString ();
		}
	}
}
