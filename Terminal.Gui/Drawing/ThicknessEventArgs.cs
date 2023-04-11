using System;

#nullable enable

namespace Terminal.Gui {
	/// <summary>
	/// Event arguments for the <see cref="Thickness"/> events.
	/// </summary>
	public class ThicknessEventArgs : EventArgs {

		/// <summary>
		/// Initializes a new instance of <see cref="ThicknessEventArgs"/>
		/// </summary>
		public ThicknessEventArgs ()
		{
		}

		/// <summary>
		/// The new Thickness.
		/// </summary>
		public Thickness Thickness { get; set; } = Thickness.Empty;
	}
}