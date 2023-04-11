using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui {
	/// <summary>
	/// Definition to interact with the OS clipboard.
	/// </summary>
	public interface IClipboard {
		/// <summary>
		/// Returns true if the environmental dependencies are in place to interact with the OS clipboard.
		/// </summary>
		bool IsSupported { get; }

		/// <summary>
		/// Get the operation system clipboard.
		/// </summary>
		/// <exception cref="NotSupportedException">Thrown if it was not possible to read the clipboard contents.</exception>
		string GetClipboardData ();

		/// <summary>
		/// Gets the operation system clipboard if possible.
		/// </summary>
		/// <param name="result">Clipboard contents read</param>
		/// <returns>true if it was possible to read the OS clipboard.</returns>
		bool TryGetClipboardData (out string result);

		/// <summary>
		/// Sets the operation system clipboard.
		/// </summary>
		/// <param name="text"></param>
		/// <exception cref="NotSupportedException">Thrown if it was not possible to set the clipboard contents.</exception>
		void SetClipboardData (string text);

		/// <summary>
		/// Sets the operation system clipboard if possible.
		/// </summary>
		/// <param name="text"></param>
		/// <returns>True if the clipboard content was set successfully.</returns>
		bool TrySetClipboardData (string text);
	}
}
