//
// MainLoop.cs: IMainLoopDriver and MainLoop for Terminal.Gui
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
using System;

namespace Terminal.Gui; 

/// <summary>
/// Provides data for timers running manipulation.
/// </summary>
public sealed class Timeout {
	/// <summary>
	/// Time to wait before invoke the callback.
	/// </summary>
	public TimeSpan Span;
	/// <summary>
	/// The function that will be invoked.
	/// </summary>
	public Func<MainLoop, bool> Callback;
}
