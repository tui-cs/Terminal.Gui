using NStack;
using System;

namespace Terminal.Gui {
	/// <summary>
	/// Provides cut, copy, and paste support for the OS clipboard.
	/// </summary>
	/// <remarks>
	/// <para>
	/// On Windows, the <see cref="Clipboard"/> class uses the Windows Clipboard APIs via P/Invoke.
	/// </para>
	/// <para>
	/// On Linux, when not running under Windows Subsystem for Linux (WSL),
	/// the <see cref="Clipboard"/> class uses the xclip command line tool. If xclip is not installed,
	/// the clipboard will not work.
	/// </para>
	/// <para>
	/// On Linux, when running under Windows Subsystem for Linux (WSL),
	/// the <see cref="Clipboard"/> class launches Windows' powershell.exe via WSL interop and uses the
	/// "Set-Clipboard" and "Get-Clipboard" Powershell CmdLets. 
	/// </para>
	/// <para>
	/// On the Mac, the <see cref="Clipboard"/> class uses the MacO OS X pbcopy and pbpaste command line tools
	/// and the Mac clipboard APIs vai P/Invoke.
	/// </para>
	/// </remarks>
	public static class Clipboard {
		static ustring contents;

		/// <summary>
		/// Gets (copies from) or sets (pastes to) the contents of the OS clipboard.
		/// </summary>
		public static ustring Contents {
			get {
				try {
					if (IsSupported) {
						return contents = ustring.Make (Application.Driver.Clipboard.GetClipboardData ());
					} else {
						return contents;
					}
				} catch (Exception) {
					return contents;
				}
			}
			set {
				try {
					if (IsSupported) {
						if (value == null) {
							value = string.Empty;
						}
						Application.Driver.Clipboard.SetClipboardData (value.ToString ());
					}
					contents = value;
				} catch (NotSupportedException e) {
					throw e;
				} catch (Exception) {
					contents = value;
				}
			}
		}

		/// <summary>
		/// Returns true if the environmental dependencies are in place to interact with the OS clipboard.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public static bool IsSupported { get => Application.Driver.Clipboard.IsSupported; }

		/// <summary>
		/// Copies the contents of the OS clipboard to <paramref name="result"/> if possible.
		/// </summary>
		/// <param name="result">The contents of the OS clipboard if successful, <see cref="string.Empty"/> if not.</param>
		/// <returns><see langword="true"/> the OS clipboard was retrieved, <see langword="false"/> otherwise.</returns>
		public static bool TryGetClipboardData (out string result)
		{
			if (IsSupported && Application.Driver.Clipboard.TryGetClipboardData (out result)) {
				if (contents != result) {
					contents = result;
				}
				return true;
			}
			result = string.Empty;
			return false;
		}

		/// <summary>
		/// Pastes the <paramref name="text"/> to the OS clipboard if possible.
		/// </summary>
		/// <param name="text">The text to paste to the OS clipboard.</param>
		/// <returns><see langword="true"/> the OS clipboard was set, <see langword="false"/> otherwise.</returns>
		public static bool TrySetClipboardData (string text)
		{
			if (IsSupported && Application.Driver.Clipboard.TrySetClipboardData (text)) {
				contents = text;
				return true;
			}
			return false;
		}
	}
}
