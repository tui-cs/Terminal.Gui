//
// Dialog.cs: Dialog box
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using NStack;
using Terminal.Gui;
using static Terminal.Gui.ConfigurationManager;

namespace Terminal.Gui {
	/// <summary>
	/// The <see cref="Dialog"/> <see cref="View"/> is a <see cref="Window"/> that by default is centered and contains one 
	/// or more <see cref="Button"/>s. It defaults to the <see cref="Colors.Dialog"/> color scheme and has a 1 cell padding around the edges.
	/// </summary>
	/// <remarks>
	///  To run the <see cref="Dialog"/> modally, create the <see cref="Dialog"/>, and pass it to <see cref="Application.Run(Func{Exception, bool})"/>. 
	///  This will execute the dialog until it terminates via the [ESC] or [CTRL-Q] key, or when one of the views
	///  or buttons added to the dialog calls <see cref="Application.RequestStop"/>.
	/// </remarks>
	public class Dialog : Window {
		/// <summary>
		/// The default <see cref="ButtonAlignments"/> for <see cref="Dialog"/>. 
		/// </summary>
		/// <remarks>
		/// This property can be set in a Theme.
		/// </remarks>
		[SerializableConfigurationProperty (Scope = typeof (ThemeScope)), JsonConverter (typeof (JsonStringEnumConverter))]
		public static ButtonAlignments DefaultButtonAlignment { get; set; } = ButtonAlignments.Center;

		// TODO: Reenable once border/borderframe design is settled
		/// <summary>
		/// Defines the default border styling for <see cref="Dialog"/>. Can be configured via <see cref="ConfigurationManager"/>.
		/// </summary>
		//[SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
		//public static Border DefaultBorder { get; set; } = new Border () {
		//	LineStyle = LineStyle.Single,
		//};

		internal List<Button> buttons = new List<Button> ();

		/// <summary>
		/// Initializes a new instance of the <see cref="Dialog"/> class using <see cref="LayoutStyle.Computed"/> positioning 
		/// with no <see cref="Button"/>s.
		/// </summary>
		/// <remarks>
		/// By default, <see cref="View.X"/> and <see cref="View.Y"/> are set to <c>Pos.Center ()</c> and <see cref="View.Width"/> and <see cref="View.Height"/> are set 
		/// to <c>Width = Dim.Percent (85)</c>, centering the Dialog vertically and horizontally. 
		/// </remarks>
		public Dialog () : this (null) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Dialog"/> class using <see cref="LayoutStyle.Computed"/> positioning 
		/// and an optional set of <see cref="Button"/>s to display
		/// </summary>
		/// <param name="buttons">Optional buttons to lay out at the bottom of the dialog.</param>
		/// <remarks>
		/// By default, <see cref="View.X"/> and <see cref="View.Y"/> are set to <c>Pos.Center ()</c> and <see cref="View.Width"/> and <see cref="View.Height"/> are set 
		/// to <c>Width = Dim.Percent (85)</c>, centering the Dialog vertically and horizontally. 
		/// </remarks>
		public Dialog (params Button [] buttons) : base ()
		{
			SetInitialProperties (buttons);
		}

		private void SetInitialProperties (Button [] buttons)
		{
			X = Pos.Center ();
			Y = Pos.Center ();

			Width = Dim.Percent (85);
			Height = Dim.Percent (85);

			ColorScheme = Colors.Dialog;

			Modal = true;
			ButtonAlignment = DefaultButtonAlignment;

			if (buttons != null) {
				foreach (var b in buttons) {
					AddButton (b);
				}
			}

			LayoutComplete += (s, args) => {
				LayoutButtons ();
			};


		}

		/// <summary>
		/// Adds a <see cref="Button"/> to the <see cref="Dialog"/>, its layout will be controlled by the <see cref="Dialog"/>
		/// </summary>
		/// <param name="button">Button to add.</param>
		public void AddButton (Button button)
		{
			if (button == null) {
				return;
			}

			//button.AutoSize = false; // BUGBUG: v2 - Hack to get around autosize not accounting for Margin?
			buttons.Add (button);
			Add (button);
			SetNeedsDisplay ();
			LayoutSubviews ();
		}

		// Get the width of all buttons, not including any Margin.
		internal int GetButtonsWidth ()
		{
			if (buttons.Count == 0) {
				return 0;
			}
			//var widths = buttons.Select (b => b.TextFormatter.GetFormattedSize ().Width + b.BorderFrame.Thickness.Horizontal + b.Padding.Thickness.Horizontal);
			var widths = buttons.Select (b => b.Frame.Width);
			return widths.Sum ();
		}

		/// <summary>
		/// Determines the horizontal alignment of the Dialog buttons.
		/// </summary>
		public enum ButtonAlignments {
			/// <summary>
			/// Center-aligns the buttons (the default).
			/// </summary>
			Center = 0,

			/// <summary>
			/// Justifies the buttons
			/// </summary>
			Justify,

			/// <summary>
			/// Left-aligns the buttons
			/// </summary>
			Left,

			/// <summary>
			/// Right-aligns the buttons
			/// </summary>
			Right
		}


		/// <summary>
		/// Determines how the <see cref="Dialog"/> <see cref="Button"/>s are aligned along the 
		/// bottom of the dialog. 
		/// </summary>
		public ButtonAlignments ButtonAlignment { get; set; }

		void LayoutButtons ()
		{
			if (buttons.Count == 0 || !IsInitialized) return;

			int shiftLeft = 0;

			int buttonsWidth = GetButtonsWidth ();
			switch (ButtonAlignment) {
			case ButtonAlignments.Center:
				// Center Buttons
				shiftLeft = (Bounds.Width - buttonsWidth - buttons.Count - 1) / 2 + 1;
				for (int i = buttons.Count - 1; i >= 0; i--) {
					Button button = buttons [i];
					shiftLeft += button.Frame.Width + (i == buttons.Count - 1 ? 0 : 1);
					if (shiftLeft > -1) {
						button.X = Pos.AnchorEnd (shiftLeft);
					} else {
						button.X = Bounds.Width - shiftLeft;
					}
					button.Y = Pos.AnchorEnd (1);
				}
				break;

			case ButtonAlignments.Justify:
				// Justify Buttons
				// leftmost and rightmost buttons are hard against edges. The rest are evenly spaced.

				var spacing = (int)Math.Ceiling ((double)(Bounds.Width - buttonsWidth) / (buttons.Count - 1));
				for (int i = buttons.Count - 1; i >= 0; i--) {
					Button button = buttons [i];
					if (i == buttons.Count - 1) {
						shiftLeft += button.Frame.Width;
						button.X = Pos.AnchorEnd (shiftLeft);
					} else {
						if (i == 0) {
							// first (leftmost) button 
							var left = Bounds.Width;
							button.X = Pos.AnchorEnd (left);
						} else {
							shiftLeft += button.Frame.Width + (spacing);
							button.X = Pos.AnchorEnd (shiftLeft);
						}
					}
					button.Y = Pos.AnchorEnd (1);
				}
				break;

			case ButtonAlignments.Left:
				// Left Align Buttons
				var prevButton = buttons [0];
				prevButton.X = 0;
				prevButton.Y = Pos.AnchorEnd (1);
				for (int i = 1; i < buttons.Count; i++) {
					Button button = buttons [i];
					button.X = Pos.Right (prevButton) + 1;
					button.Y = Pos.AnchorEnd (1);
					prevButton = button;
				}
				break;

			case ButtonAlignments.Right:
				// Right align buttons
				shiftLeft = buttons [buttons.Count - 1].Frame.Width;
				buttons [buttons.Count - 1].X = Pos.AnchorEnd (shiftLeft);
				buttons [buttons.Count - 1].Y = Pos.AnchorEnd (1);
				for (int i = buttons.Count - 2; i >= 0; i--) {
					Button button = buttons [i];
					shiftLeft += button.Frame.Width + 1;
					button.X = Pos.AnchorEnd (shiftLeft);
					button.Y = Pos.AnchorEnd (1);
				}
				break;
			}
		}

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent kb)
		{
			switch (kb.Key) {
			case Key.Esc:
				Application.RequestStop (this);
				return true;
			}
			return base.ProcessKey (kb);
		}
	}
}
