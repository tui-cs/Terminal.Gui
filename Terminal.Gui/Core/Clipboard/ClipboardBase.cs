using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui {
	/// <summary>
	/// Shared abstract class to enforce rules from the implementation of the <see cref="IClipboard"/> interface.
	/// </summary>
	public abstract class ClipboardBase : IClipboard {
		/// <summary>
		/// Returns true if the environmental dependencies are in place to interact with the OS clipboard
		/// </summary>
		public abstract bool IsSupported { get; }

		/// <summary>
		/// Returns the contents of the OS clipboard if possible.
		/// </summary>
		/// <returns>The contents of the OS clipboard if successful.</returns>
		/// <exception cref="NotSupportedException">Thrown if it was not possible to copy from the OS clipboard.</exception>
		public string GetClipboardData ()
		{
			try {
				return GetClipboardDataImpl ();
			} catch (NotSupportedException ex) {
				throw new NotSupportedException ("Failed to copy from the OS clipboard.", ex);
			}
		}

		/// <summary>
		/// Returns the contents of the OS clipboard if possible. Implemented by <see cref="ConsoleDriver"/>-specific subclasses.
		/// </summary>
		/// <returns>The contents of the OS clipboard if successful.</returns>
		/// <exception cref="NotSupportedException">Thrown if it was not possible to copy from the OS clipboard.</exception>
		protected abstract string GetClipboardDataImpl ();

		/// <summary>
		/// Pastes the <paramref name="text"/> to the OS clipboard if possible.
		/// </summary>
		/// <param name="text">The text to paste to the OS clipboard.</param>
		/// <exception cref="NotSupportedException">Thrown if it was not possible to paste to the OS clipboard.</exception>
		public void SetClipboardData (string text)
		{
			try {
				SetClipboardDataImpl (text);
			} catch (NotSupportedException ex) {
				throw new NotSupportedException ("Failed to paste to the OS clipboard.", ex);
			}
		}

		/// <summary>
		/// Pastes the <paramref name="text"/> to the OS clipboard if possible. Implemented by <see cref="ConsoleDriver"/>-specific subclasses.
		/// </summary>
		/// <param name="text">The text to paste to the OS clipboard.</param>
		/// <exception cref="NotSupportedException">Thrown if it was not possible to paste to the OS clipboard.</exception>
		protected abstract void SetClipboardDataImpl (string text);

		/// <summary>
		/// Copies the contents of the OS clipboard to <paramref name="result"/> if possible.
		/// </summary>
		/// <param name="result">The contents of the OS clipboard if successful, <see cref="string.Empty"/> if not.</param>
		/// <returns><see langword="true"/> the OS clipboard was retrieved, <see langword="false"/> otherwise.</returns>
		public bool TryGetClipboardData (out string result)
		{
			// Don't even try to read because environment is not set up.
			if (!IsSupported) {
				result = null;
				return false;
			}

			try {
				result = GetClipboardDataImpl ();
				while (result == null) {
					result = GetClipboardDataImpl ();
				}
				return true;
			} catch (NotSupportedException ex) {
				System.Diagnostics.Debug.WriteLine ($"TryGetClipboardData: {ex.Message}");
				result = null;
				return false;
			}
		}

		/// <summary>
		/// Pastes the <paramref name="text"/> to the OS clipboard if possible.
		/// </summary>
		/// <param name="text">The text to paste to the OS clipboard.</param>
		/// <returns><see langword="true"/> the OS clipboard was set, <see langword="false"/> otherwise.</returns>
		public bool TrySetClipboardData (string text)
		{
			// Don't even try to set because environment is not set up
			if (!IsSupported) {
				return false;
			}

			try {
				SetClipboardDataImpl (text);
				return true;
			} catch (NotSupportedException ex) {
				System.Diagnostics.Debug.WriteLine ($"TrySetClipboardData: {ex.Message}");
				return false;
			}
		}
	}
}
