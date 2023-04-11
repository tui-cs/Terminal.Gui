using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Terminal.Gui;
using Xunit;
using Xunit.Abstractions;
using static AutoInitShutdownAttribute;

namespace Terminal.Gui.ClipboardTests {
	public class ClipboardTests {
		readonly ITestOutputHelper output;

		public ClipboardTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact, AutoInitShutdown (useFakeClipboard: true, fakeClipboardAlwaysThrowsNotSupportedException: true)]
		public void IClipboard_GetClipBoardData_Throws_NotSupportedException ()
		{
			IClipboard iclip = Application.Driver.Clipboard;
			Assert.Throws<NotSupportedException> (() => iclip.GetClipboardData ());
		}

		[Fact, AutoInitShutdown (useFakeClipboard: true, fakeClipboardAlwaysThrowsNotSupportedException: true)]
		public void IClipboard_SetClipBoardData_Throws_NotSupportedException ()
		{
			IClipboard iclip = Application.Driver.Clipboard;
			Assert.Throws<NotSupportedException> (() => iclip.SetClipboardData ("foo"));
		}

		[Fact, AutoInitShutdown (useFakeClipboard: true)]
		public void Contents_Fake_Gets_Sets ()
		{
			if (!Clipboard.IsSupported) {
				output.WriteLine ($"The Clipboard not supported on this platform.");
				return;
			}

			var clipText = "The Contents_Gets_Sets unit test pasted this to the OS clipboard.";
			Clipboard.Contents = clipText;

			Application.Iteration += () => Application.RequestStop ();
			Application.Run ();

			Assert.Equal (clipText, Clipboard.Contents.ToString ());
		}

		[Fact, AutoInitShutdown (useFakeClipboard: false)]
		public void Contents_Gets_Sets ()
		{
			if (!Clipboard.IsSupported) {
				output.WriteLine ($"The Clipboard not supported on this platform.");
				return;
			}

			var clipText = "The Contents_Gets_Sets unit test pasted this to the OS clipboard.";
			Clipboard.Contents = clipText;

			Application.Iteration += () => Application.RequestStop ();
			Application.Run ();

			Assert.Equal (clipText, Clipboard.Contents.ToString ());
		}

		[Fact, AutoInitShutdown (useFakeClipboard: false)]
		public void Contents_Gets_Sets_When_IsSupportedFalse ()
		{

			if (!Clipboard.IsSupported) {
				output.WriteLine ($"The Clipboard not supported on this platform.");
				return;
			}

			var clipText = "The Contents_Gets_Sets unit test pasted this to the OS clipboard.";
			Clipboard.Contents = clipText;

			Application.Iteration += () => Application.RequestStop ();
			Application.Run ();

			Assert.Equal (clipText, Clipboard.Contents.ToString ());
		}

		[Fact, AutoInitShutdown (useFakeClipboard: true)]
		public void Contents_Fake_Gets_Sets_When_IsSupportedFalse ()
		{

			if (!Clipboard.IsSupported) {
				output.WriteLine ($"The Clipboard not supported on this platform.");
				return;
			}

			var clipText = "The Contents_Gets_Sets unit test pasted this to the OS clipboard.";
			Clipboard.Contents = clipText;

			Application.Iteration += () => Application.RequestStop ();
			Application.Run ();

			Assert.Equal (clipText, Clipboard.Contents.ToString ());
		}

		[Fact, AutoInitShutdown (useFakeClipboard: false)]
		public void IsSupported_Get ()
		{
			if (Clipboard.IsSupported) Assert.True (Clipboard.IsSupported);
			else Assert.False (Clipboard.IsSupported);
		}

		[Fact, AutoInitShutdown (useFakeClipboard: false)]
		public void TryGetClipboardData_Gets_From_OS_Clipboard ()
		{
			var clipText = "The TryGetClipboardData_Gets_From_OS_Clipboard unit test pasted this to the OS clipboard.";
			Clipboard.Contents = clipText;

			Application.Iteration += () => Application.RequestStop ();

			Application.Run ();

			if (Clipboard.IsSupported) {
				Assert.True (Clipboard.TryGetClipboardData (out string result));
				Assert.Equal (clipText, result);
			} else {
				Assert.False (Clipboard.TryGetClipboardData (out string result));
				Assert.NotEqual (clipText, result);
			}
		}

		[Fact, AutoInitShutdown (useFakeClipboard: false)]
		public void TrySetClipboardData_Sets_The_OS_Clipboard ()
		{
			var clipText = "The TrySetClipboardData_Sets_The_OS_Clipboard unit test pasted this to the OS clipboard.";
			if (Clipboard.IsSupported) Assert.True (Clipboard.TrySetClipboardData (clipText));
			else Assert.False (Clipboard.TrySetClipboardData (clipText));

			Application.Iteration += () => Application.RequestStop ();

			Application.Run ();

			if (Clipboard.IsSupported) Assert.Equal (clipText, Clipboard.Contents);
			else Assert.NotEqual (clipText, Clipboard.Contents);
		}

		// Disabling this test for now because it is not reliable 
#if false
		[Fact, AutoInitShutdown (useFakeClipboard: false)]
		public void Contents_Copies_From_OS_Clipboard ()
		{
			if (!Clipboard.IsSupported) {
				output.WriteLine ($"The Clipboard not supported on this platform.");
				return;
			}

			var clipText = "The Contents_Copies_From_OS_Clipboard unit test pasted this to the OS clipboard.";
			var failed = false;
			var getClipText = "";

			Application.Iteration += () => {
				int exitCode = 0;
				string result = "";
				output.WriteLine ($"Pasting to OS clipboard: {clipText}...");

				if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows)) {
					(exitCode, result) = ClipboardProcessRunner.Process ("powershell.exe", $"-command \"Set-Clipboard -Value \\\"{clipText}\\\"\"");
					output.WriteLine ($"  Windows: powershell.exe Set-Clipboard: exitCode = {exitCode}, result = {result}");
					getClipText = Clipboard.Contents.ToString ();

				} else if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX)) {
					(exitCode, result) = ClipboardProcessRunner.Process ("pbcopy", string.Empty, clipText);
					output.WriteLine ($"  OSX: pbcopy: exitCode = {exitCode}, result = {result}");
					getClipText = Clipboard.Contents.ToString ();

				} else if (RuntimeInformation.IsOSPlatform (OSPlatform.Linux)) {
					if (Is_WSL_Platform ()) {
						try {
							// This runs the WINDOWS version of powershell.exe via WSL.
							(exitCode, result) = ClipboardProcessRunner.Process ("powershell.exe", $"-noprofile -command \"Set-Clipboard -Value \\\"{clipText}\\\"\"");
							output.WriteLine ($"  WSL: powershell.exe Set-Clipboard: exitCode = {exitCode}, result = {result}");
						} catch {
							failed = true;
						}

						if (!failed) {
							// If we set the OS clipboard via Powershell, then getting Contents should return the same text.
							getClipText = Clipboard.Contents.ToString ();
							output.WriteLine ($"  WSL: Clipboard.Contents: {getClipText}");
						}
						Application.RequestStop ();
						return;
					}

					if (failed = xclipExists () == false) {
						// if xclip doesn't exist then exit.
						output.WriteLine ($"  WSL: no xclip found.");
						Application.RequestStop ();
						return;
					}

					// If we get here, powershell didn't work and xclip exists...
					(exitCode, result) = ClipboardProcessRunner.Process ("bash", $"-c \"xclip -sel clip -i\"", clipText);
					output.WriteLine ($"  Linux: bash xclip -sel clip -i: exitCode = {exitCode}, result = {result}");

					if (!failed) {
						getClipText = Clipboard.Contents.ToString ();
						output.WriteLine ($"  Linux via xclip: Clipboard.Contents: {getClipText}");
					}
				}

				Application.RequestStop ();
			};

			Application.Run ();

			if (!failed) 				Assert.Equal (clipText, getClipText);
		}

		[Fact, AutoInitShutdown (useFakeClipboard: false)]
		public void Contents_Pastes_To_OS_Clipboard ()
		{
			if (!Clipboard.IsSupported) {
				output.WriteLine ($"The Clipboard not supported on this platform.");
				return;
			}

			var clipText = "The Contents_Pastes_To_OS_Clipboard unit test pasted this via Clipboard.Contents.";
			var clipReadText = "";
			var failed = false;

			Application.Iteration += () => {
				Clipboard.Contents = clipText;

				int exitCode = 0;
				output.WriteLine ($"Getting OS clipboard...");

				if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows)) {
					(exitCode, clipReadText) = ClipboardProcessRunner.Process ("powershell.exe", "-noprofile -command \"Get-Clipboard\"");
					output.WriteLine ($"  Windows: powershell.exe Get-Clipboard: exitCode = {exitCode}, result = {clipReadText}");

				} else if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX)) {
					(exitCode, clipReadText) = ClipboardProcessRunner.Process ("pbpaste", "");
					output.WriteLine ($"  OSX: pbpaste: exitCode = {exitCode}, result = {clipReadText}");

				} else if (RuntimeInformation.IsOSPlatform (OSPlatform.Linux)) {
					if (Is_WSL_Platform ()) {
						(exitCode, clipReadText) = ClipboardProcessRunner.Process ("powershell.exe", "-noprofile -command \"Get-Clipboard\"");
						output.WriteLine ($"  WSL: powershell.exe Get-Clipboard: exitCode = {exitCode}, result = {clipReadText}");
						if (exitCode == 0) {
							Application.RequestStop ();
							return;
						}
						failed = true;
					}

					if (failed = xclipExists () == false) {
						// xclip doesn't exist then exit.
						Application.RequestStop ();
						return;
					}

					(exitCode, clipReadText) = ClipboardProcessRunner.Process ("bash", $"-c \"xclip -sel clip -o\"");
					output.WriteLine ($"  Linux: bash xclip -sel clip -o: exitCode = {exitCode}, result = {clipReadText}");
					Assert.Equal (0, exitCode);
				}

				Application.RequestStop ();
			};
			
			Application.Run ();

			if (!failed) 				Assert.Equal (clipText, clipReadText.TrimEnd ());

		}
#endif

		bool Is_WSL_Platform ()
		{
			var (_, result) = ClipboardProcessRunner.Process ("bash", $"-c \"uname -a\"");
			return result.Contains ("microsoft") && result.Contains ("WSL");
		}

		bool xclipExists ()
		{
			try {
				var (_, result) = ClipboardProcessRunner.Process ("bash", $"-c \"which xclip\"");
				return result.TrimEnd () != "";
			} catch (Exception) {
				return false;
			}
		}

	}
}
