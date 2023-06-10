using System;
using System.Collections;
using System.Text.Json.Serialization;
using System.Text;
using Terminal.Gui;
using static Terminal.Gui.ConfigurationManager;

namespace Terminal.Gui {

	/// <summary>
	/// A <see cref="Toplevel"/> <see cref="View"/> with <see cref="View.BorderStyle"/> set to <see cref="LineStyle.Single"/>. 
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is a helper class to simplify creating a <see cref="Toplevel"/> with a border.
	/// </para>
	/// </remarks>
	public class Window : Toplevel {
		/// <summary>
		/// Initializes a new instance of the <see cref="Window"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		public Window () : base () {
			SetInitialProperties ();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Window"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		public Window (Rect frame) : base (frame)
		{
			SetInitialProperties ();
		}

		// TODO: enable this
		///// <summary>
		///// The default <see cref="LineStyle"/> for <see cref="Window"/>'s border. The default is <see cref="LineStyle.Single"/>.
		///// </summary>
		///// <remarks>
		///// This property can be set in a Theme to change the default <see cref="LineStyle"/> for all <see cref="Window"/>s. 
		///// </remarks>
		/////[SerializableConfigurationProperty (Scope = typeof (ThemeScope)), JsonConverter (typeof (JsonStringEnumConverter))]
		////public static ColorScheme DefaultColorScheme { get; set; } = Colors.Base;

		/// <summary>
		/// The default <see cref="LineStyle"/> for <see cref="Window"/>'s border. The default is <see cref="LineStyle.Single"/>.
		/// </summary>
		/// <remarks>
		/// This property can be set in a Theme to change the default <see cref="LineStyle"/> for all <see cref="Window"/>s. 
		/// </remarks>
		[SerializableConfigurationProperty (Scope = typeof (ThemeScope)), JsonConverter (typeof (JsonStringEnumConverter))]
		public static LineStyle DefaultBorderStyle { get; set; } = LineStyle.Single;

		void SetInitialProperties ()
		{
			CanFocus = true;
			ColorScheme = Colors.Base; // TODO: make this a theme property
			BorderStyle = DefaultBorderStyle;
		}

		// TODO: Are these overrides really needed? 
		/// <inheritdoc/>
		public override void Add (View view)
		{
			base.Add (view);
			if (view.CanFocus) {
				CanFocus = true;
			}
			AddMenuStatusBar (view);
		}

		/// <inheritdoc/>
		public override void Remove (View view)
		{
			if (view == null) {
				return;
			}

			SetNeedsDisplay ();
			base.Remove (view);
			RemoveMenuStatusBar (view);

		}
	}
}
