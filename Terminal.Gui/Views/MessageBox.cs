using NStack;
using System;
using System.Collections.Generic;
using Terminal.Gui;
using static Terminal.Gui.ConfigurationManager;

namespace Terminal.Gui {
	/// <summary>
	/// MessageBox displays a modal message to the user, with a title, a message and a series of options that the user can choose from.
	/// </summary>
	/// <para>
	///   The difference between the <see cref="Query(ustring, ustring, ustring[])"/> and <see cref="ErrorQuery(ustring, ustring, ustring[])"/> 
	///   method is the default set of colors used for the message box.
	/// </para>
	/// <para>
	/// The following example pops up a <see cref="MessageBox"/> with the specified title and text, plus two <see cref="Button"/>s.
	/// The value -1 is returned when the user cancels the <see cref="MessageBox"/> by pressing the ESC key.
	/// </para>
	/// <example>
	/// <code lang="c#">
	/// var n = MessageBox.Query ("Quit Demo", "Are you sure you want to quit this demo?", "Yes", "No");
	/// if (n == 0)
	///    quit = true;
	/// else
	///    quit = false;
	/// </code>
	/// </example>
	public static class MessageBox {
		/// <summary>
		/// Presents a normal <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="width">Width for the window.</param>
		/// <param name="height">Height for the window.</param>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// Use <see cref="Query(ustring, ustring, ustring[])"/> instead; it automatically sizes the MessageBox based on the contents.
		/// </remarks>
		public static int Query (int width, int height, ustring title, ustring message, params ustring [] buttons)
		{
			return QueryFull (false, width, height, title, message, 0, true, buttons);
		}

		/// <summary>
		/// Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// The message box will be vertically and horizontally centered in the container and the size will be automatically determined
		/// from the size of the message and buttons.
		/// </remarks>
		public static int Query (ustring title, ustring message, params ustring [] buttons)
		{
			return QueryFull (false, 0, 0, title, message, 0, true, buttons);
		}

		/// <summary>
		/// Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="width">Width for the window.</param>
		/// <param name="height">Height for the window.</param>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// Use <see cref="ErrorQuery(ustring, ustring, ustring[])"/> instead; it automatically sizes the MessageBox based on the contents.
		/// </remarks>
		public static int ErrorQuery (int width, int height, ustring title, ustring message, params ustring [] buttons)
		{
			return QueryFull (true, width, height, title, message, 0, true, buttons);
		}

		/// <summary>
		/// Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// The message box will be vertically and horizontally centered in the container and the size will be automatically determined
		/// from the size of the title, message. and buttons.
		/// </remarks>
		public static int ErrorQuery (ustring title, ustring message, params ustring [] buttons)
		{
			return QueryFull (true, 0, 0, title, message, 0, true, buttons);
		}

		/// <summary>
		/// Presents a normal <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="width">Width for the window.</param>
		/// <param name="height">Height for the window.</param>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="defaultButton">Index of the default button.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// Use <see cref="Query(ustring, ustring, ustring[])"/> instead; it automatically sizes the MessageBox based on the contents.
		/// </remarks>
		public static int Query (int width, int height, ustring title, ustring message, int defaultButton = 0, params ustring [] buttons)
		{
			return QueryFull (false, width, height, title, message, defaultButton, true, buttons);
		}

		/// <summary>
		/// Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="defaultButton">Index of the default button.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// The message box will be vertically and horizontally centered in the container and the size will be automatically determined
		/// from the size of the message and buttons.
		/// </remarks>
		public static int Query (ustring title, ustring message, int defaultButton = 0, params ustring [] buttons)
		{
			return QueryFull (false, 0, 0, title, message, defaultButton, true, buttons);
		}

		/// <summary>
		/// Presents a normal <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="width">Width for the window.</param>
		/// <param name="height">Height for the window.</param>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="defaultButton">Index of the default button.</param>
		/// <param name="wrapMessagge">If wrap the message or not.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// Use <see cref="Query(ustring, ustring, ustring[])"/> instead; it automatically sizes the MessageBox based on the contents.
		/// </remarks>
		public static int Query (int width, int height, ustring title, ustring message, int defaultButton = 0, bool wrapMessagge = true, params ustring [] buttons)
		{
			return QueryFull (false, width, height, title, message, defaultButton, wrapMessagge, buttons);
		}

		/// <summary>
		/// Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="defaultButton">Index of the default button.</param>
		/// <param name="wrapMessage">If wrap the message or not.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// The message box will be vertically and horizontally centered in the container and the size will be automatically determined
		/// from the size of the message and buttons.
		/// </remarks>
		public static int Query (ustring title, ustring message, int defaultButton = 0, bool wrapMessage = true, params ustring [] buttons)
		{
			return QueryFull (false, 0, 0, title, message, defaultButton, wrapMessage, buttons);
		}


		/// <summary>
		/// Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="width">Width for the window.</param>
		/// <param name="height">Height for the window.</param>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="defaultButton">Index of the default button.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// Use <see cref="ErrorQuery(ustring, ustring, ustring[])"/> instead; it automatically sizes the MessageBox based on the contents.
		/// </remarks>
		public static int ErrorQuery (int width, int height, ustring title, ustring message, int defaultButton = 0, params ustring [] buttons)
		{
			return QueryFull (true, width, height, title, message, defaultButton, true, buttons);
		}

		/// <summary>
		/// Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="defaultButton">Index of the default button.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// The message box will be vertically and horizontally centered in the container and the size will be automatically determined
		/// from the size of the title, message. and buttons.
		/// </remarks>
		public static int ErrorQuery (ustring title, ustring message, int defaultButton = 0, params ustring [] buttons)
		{
			return QueryFull (true, 0, 0, title, message, defaultButton, true, buttons);
		}

		/// <summary>
		/// Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="width">Width for the window.</param>
		/// <param name="height">Height for the window.</param>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="defaultButton">Index of the default button.</param>
		/// <param name="wrapMessagge">If wrap the message or not.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// Use <see cref="ErrorQuery(ustring, ustring, ustring[])"/> instead; it automatically sizes the MessageBox based on the contents.
		/// </remarks>
		public static int ErrorQuery (int width, int height, ustring title, ustring message, int defaultButton = 0, bool wrapMessagge = true, params ustring [] buttons)
		{
			return QueryFull (true, width, height, title, message, defaultButton, wrapMessagge, buttons);
		}

		/// <summary>
		/// Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="defaultButton">Index of the default button.</param>
		/// <param name="wrapMessagge">If wrap the message or not.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// The message box will be vertically and horizontally centered in the container and the size will be automatically determined
		/// from the size of the title, message. and buttons.
		/// </remarks>
		public static int ErrorQuery (ustring title, ustring message, int defaultButton = 0, bool wrapMessagge = true, params ustring [] buttons)
		{
			return QueryFull (true, 0, 0, title, message, defaultButton, wrapMessagge, buttons);
		}

		/// <summary>
		/// Defines the default border styling for <see cref="Dialog"/>. Can be configured via <see cref="ConfigurationManager"/>.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
		public static LineStyle DefaultBorderStyle { get; set; } = LineStyle.Single;

		static int QueryFull (bool useErrorColors, int width, int height, ustring title, ustring message,
			int defaultButton = 0, bool wrapMessage = true, params ustring [] buttons)
		{
			// Create button array for Dialog
			int count = 0;
			List<Button> buttonList = new List<Button> ();
			if (buttons != null) {
				if (defaultButton > buttons.Length - 1) {
					defaultButton = buttons.Length - 1;
				}
				foreach (var s in buttons) {
					var b = new Button (s);
					if (count == defaultButton) {
						b.IsDefault = true;
					}
					buttonList.Add (b);
					count++;
				}

			}
			
			Dialog d;
			d = new Dialog (buttonList.ToArray ()) {
				Title = title,
				BorderStyle = DefaultBorderStyle,
				Width = Dim.Percent (60),
				Height = 5 // Border + one line of text + vspace + buttons
			};

			if (width != 0) {
				d.Width = width;
			} 
			
			if (height != 0) {
				d.Height = height;
			}
		
			if (useErrorColors) {
				d.ColorScheme = Colors.Error;
			} else {
				d.ColorScheme = Colors.Dialog;
			}

			var messageLabel = new Label () {
				AutoSize = false,
				Text = message,
				TextAlignment = TextAlignment.Centered,
				X = 0,
				Y = 0,
				Width = Dim.Fill (0),
				Height = Dim.Fill (1)
			};
			messageLabel.TextFormatter.WordWrap = wrapMessage; // BUGBUG: This does nothing as it's not implemented by TextFormatter!
			d.Add (messageLabel);
			
			d.Loaded += (s, e) => {
				if (width != 0 || height != 0) {
					return;
				}
				// TODO: replace with Dim.Fit when implemented
				var maxBounds = d.SuperView?.Bounds ?? Application.Top.Bounds;
				messageLabel.TextFormatter.Size = new Size (maxBounds.Size.Width - d.GetFramesThickness ().Horizontal, maxBounds.Size.Height - d.GetFramesThickness ().Vertical);
				var msg = messageLabel.TextFormatter.Format ();
				var messageSize = messageLabel.TextFormatter.GetFormattedSize ();

				// Ensure the width fits the text + buttons
				var newWidth = Math.Max (width, Math.Max (messageSize.Width + d.GetFramesThickness ().Horizontal,
								d.GetButtonsWidth () + d.buttons.Count + d.GetFramesThickness ().Horizontal));
				if (newWidth > d.Frame.Width) {
					d.Width = newWidth;
				}
				// Ensure height fits the text + vspace + buttons
				d.Height = Math.Max (height, messageSize.Height + 2 + d.GetFramesThickness ().Vertical);
				d.SetRelativeLayout (d.SuperView?.Frame ?? Application.Top.Frame);
			};

			// Setup actions
			Clicked = -1;
			for (int n = 0; n < buttonList.Count; n++) {
				int buttonId = n;
				var b = buttonList [n];
				b.Clicked += (s, e) => {
					Clicked = buttonId;
					Application.RequestStop ();
				};
				if (b.IsDefault) {
					b.SetFocus ();
				}
			}

			// Run the modal; do not shutdown the mainloop driver when done
			Application.Run (d);
			return Clicked;
		}

		/// <summary>
		/// The index of the selected button, or -1 if the user pressed ESC to close the dialog.
		/// This is useful for web based console where by default there is no SynchronizationContext or TaskScheduler.
		/// </summary>
		public static int Clicked { get; private set; } = -1;
	}
}
