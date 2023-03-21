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
using Terminal.Gui.Configuration;
using static Terminal.Gui.Configuration.ConfigurationManager;

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

		/// <summary>
		/// Defines the default border styling for <see cref="Dialog"/>. Can be configured via <see cref="ConfigurationManager"/>.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
		public static Border DefaultBorder { get; set; } = new Border () {
			BorderStyle = BorderStyle.Single,
			DrawMarginFrame = false,
			Effect3D = true,
			Effect3DOffset = new Point (1, 1),
		};

		internal List<Button> buttons = new List<Button> ();
		const int padding = 0;

		/// <summary>
		/// Initializes a new instance of the <see cref="Dialog"/> class using <see cref="LayoutStyle.Computed"/> positioning 
		/// and an optional set of <see cref="Button"/>s to display
		/// </summary>
		/// <param name="title">Title for the dialog.</param>
		/// <param name="width">Width for the dialog.</param>
		/// <param name="height">Height for the dialog.</param>
		/// <param name="buttons">Optional buttons to lay out at the bottom of the dialog.</param>
		/// <remarks>
		/// if <c>width</c> and <c>height</c> are both 0, the Dialog will be vertically and horizontally centered in the
		/// container and the size will be 85% of the container. 
		/// After initialization use <c>X</c>, <c>Y</c>, <c>Width</c>, and <c>Height</c> to override this with a location or size.
		/// </remarks>
		/// <remarks>
		/// Use the constructor that does not take a <c>width</c> and <c>height</c> instead.
		/// </remarks>
		public Dialog (ustring title, int width, int height, params Button [] buttons) : base (title: title, padding: padding)
		{
			X = Pos.Center ();
			Y = Pos.Center ();

			if (width == 0 & height == 0) {
				Width = Dim.Percent (85);
				Height = Dim.Percent (85);
			} else {
				Width = width;
				Height = height;
			}

			ColorScheme = Colors.Dialog;
			Modal = true;
			ButtonAlignment = DefaultButtonAlignment;
			if (Border == null) {
				Border = DefaultBorder;
				Border.Title = title;
			}

			if (buttons != null) {
				foreach (var b in buttons) {
					this.buttons.Add (b);
					Add (b);
				}
			}

			LayoutStarted += (s, args) => {
				LayoutStartedHandler ();
			};
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Dialog"/> class using <see cref="LayoutStyle.Computed"/>.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Te Dialog will be vertically and horizontally centered in the container and the size will be 85% of the container. 
		/// After initialization use <c>X</c>, <c>Y</c>, <c>Width</c>, and <c>Height</c> to override this with a location or size.
		/// </para>
		/// <para>
		/// Use <see cref="AddButton(Button)"/> to add buttons to the dialog.
		/// </para>
		/// </remarks>
		public Dialog () : this (title: string.Empty, width: 0, height: 0, buttons: null) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Dialog"/> class using <see cref="LayoutStyle.Computed"/> positioning 
		/// and with an optional set of <see cref="Button"/>s to display
		/// </summary>
		/// <param name="title">Title for the dialog.</param>
		/// <param name="buttons">Optional buttons to lay out at the bottom of the dialog.</param>
		/// <remarks>
		/// Te Dialog will be vertically and horizontally centered in the container and the size will be 85% of the container. 
		/// After initialization use <c>X</c>, <c>Y</c>, <c>Width</c>, and <c>Height</c> to override this with a location or size.
		/// </remarks>
		public Dialog (ustring title, params Button [] buttons) : this (title: title, width: 0, height: 0, buttons: buttons) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Dialog"/> class using <see cref="LayoutStyle.Computed"/> positioning, 
		/// with a <see cref="Border"/> and with an optional set of <see cref="Button"/>s to display
		/// </summary>
		/// <param name="title">Title for the dialog.</param>
		/// <param name="border">The border.</param>
		/// <param name="buttons">Optional buttons to lay out at the bottom of the dialog.</param>
		public Dialog (ustring title, Border border, params Button [] buttons)
			: this (title: title, width: 0, height: 0, buttons: buttons)
		{
			Initialize (title, border);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Dialog"/> class using <see cref="LayoutStyle.Computed"/> positioning, 
		/// with a <see cref="Border"/> and with an optional set of <see cref="Button"/>s to display
		/// </summary>
		/// <param name="title">Title for the dialog.</param>
		/// <param name="width">Width for the dialog.</param>
		/// <param name="height">Height for the dialog.</param>
		/// <param name="border">The border.</param>
		/// <param name="buttons">Optional buttons to lay out at the bottom of the dialog.</param>
		public Dialog (ustring title, int width, int height, Border border, params Button [] buttons)
			: this (title: title, width: width, height: height, buttons: buttons)
		{
			Initialize (title, border);
		}

		void Initialize (ustring title, Border border)
		{
			if (border != null) {
				Border = border;
				Border.Title = title;
			}
		}

		/// <summary>
		/// Adds a <see cref="Button"/> to the <see cref="Dialog"/>, its layout will be controlled by the <see cref="Dialog"/>
		/// </summary>
		/// <param name="button">Button to add.</param>
		public void AddButton (Button button)
		{
			if (button == null)
				return;

			buttons.Add (button);
			Add (button);
			SetNeedsDisplay ();
			LayoutSubviews ();
		}

		// Get the width of all buttons, not including any spacing
		internal int GetButtonsWidth ()
		{
			if (buttons.Count == 0) {
				return 0;
			}
			return buttons.Select (b => b.Bounds.Width).Sum ();
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

		void LayoutStartedHandler ()
		{
			if (buttons.Count == 0 || !IsInitialized) return;

			int shiftLeft = 0;

			int buttonsWidth = GetButtonsWidth ();
			switch (ButtonAlignment) {
			case ButtonAlignments.Center:
				// Center Buttons
				shiftLeft = (Bounds.Width - buttonsWidth - buttons.Count - 2) / 2 + 1;
				for (int i = buttons.Count - 1; i >= 0; i--) {
					Button button = buttons [i];
					shiftLeft += button.Frame.Width + (i == buttons.Count - 1 ? 0 : 1);
					if (shiftLeft > -1) {
						button.X = Pos.AnchorEnd (shiftLeft);
					} else {
						button.X = Frame.Width - shiftLeft;
					}
					button.Y = Pos.AnchorEnd (1);
				}
				break;

			case ButtonAlignments.Justify:
				// Justify Buttons
				// leftmost and rightmost buttons are hard against edges. The rest are evenly spaced.

				var spacing = (int)Math.Ceiling ((double)(Bounds.Width - buttonsWidth - (Border.DrawMarginFrame ? 2 : 0)) / (buttons.Count - 1));
				for (int i = buttons.Count - 1; i >= 0; i--) {
					Button button = buttons [i];
					if (i == buttons.Count - 1) {
						shiftLeft += button.Frame.Width;
						button.X = Pos.AnchorEnd (shiftLeft);
					} else {
						if (i == 0) {
							// first (leftmost) button - always hard flush left
							var left = Bounds.Width - ((Border.DrawMarginFrame ? 2 : 0) + Border.BorderThickness.Left + Border.BorderThickness.Right);
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
