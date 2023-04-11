//
// Button.cs: Button control
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	///   Button is a <see cref="View"/> that provides an item that invokes an <see cref="Action"/> when activated by the user.
	/// </summary>
	/// <remarks>
	/// <para>
	///   Provides a button showing text invokes an <see cref="Action"/> when clicked on with a mouse
	///   or when the user presses SPACE, ENTER, or hotkey. The hotkey is the first letter or digit following the first underscore ('_') 
	///   in the button text. 
	/// </para>
	/// <para>
	///   Use <see cref="View.HotKeySpecifier"/> to change the hotkey specifier from the default of ('_'). 
	/// </para>
	/// <para>
	///   If no hotkey specifier is found, the first uppercase letter encountered will be used as the hotkey.
	/// </para>
	/// <para>
	///   When the button is configured as the default (<see cref="IsDefault"/>) and the user presses
	///   the ENTER key, if no other <see cref="View"/> processes the <see cref="KeyEvent"/>, the <see cref="Button"/>'s
	///   <see cref="Action"/> will be invoked.
	/// </para>
	/// </remarks>
	public class Button : View {
		bool is_default;
		Rune _leftBracket;
		Rune _rightBracket;
		Rune _leftDefault;
		Rune _rightDefault;

		/// <summary>
		///   Initializes a new instance of <see cref="Button"/> using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <remarks>
		///   The width of the <see cref="Button"/> is computed based on the
		///   text length. The height will always be 1.
		/// </remarks>
		public Button () : this (text: string.Empty, is_default: false) { }

		/// <summary>
		///   Initializes a new instance of <see cref="Button"/> using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <remarks>
		///   The width of the <see cref="Button"/> is computed based on the
		///   text length. The height will always be 1.
		/// </remarks>
		/// <param name="text">The button's text</param>
		/// <param name="is_default">
		///   If <c>true</c>, a special decoration is used, and the user pressing the enter key 
		///   in a <see cref="Dialog"/> will implicitly activate this button.
		/// </param>
		public Button (ustring text, bool is_default = false) : base (text)
		{
			SetInitialProperties (text, is_default);
		}

		/// <summary>
		///   Initializes a new instance of <see cref="Button"/> using <see cref="LayoutStyle.Absolute"/> layout, based on the given text
		/// </summary>
		/// <remarks>
		///   The width of the <see cref="Button"/> is computed based on the
		///   text length. The height will always be 1.
		/// </remarks>
		/// <param name="x">X position where the button will be shown.</param>
		/// <param name="y">Y position where the button will be shown.</param>
		/// <param name="text">The button's text</param>
		public Button (int x, int y, ustring text) : this (x, y, text, false) { }

		/// <summary>
		///   Initializes a new instance of <see cref="Button"/> using <see cref="LayoutStyle.Absolute"/> layout, based on the given text.
		/// </summary>
		/// <remarks>
		///   The width of the <see cref="Button"/> is computed based on the
		///   text length. The height will always be 1.
		/// </remarks>
		/// <param name="x">X position where the button will be shown.</param>
		/// <param name="y">Y position where the button will be shown.</param>
		/// <param name="text">The button's text</param>
		/// <param name="is_default">
		///   If <c>true</c>, a special decoration is used, and the user pressing the enter key 
		///   in a <see cref="Dialog"/> will implicitly activate this button.
		/// </param>
		public Button (int x, int y, ustring text, bool is_default)
		    : base (new Rect (x, y, text.RuneCount + 4 + (is_default ? 2 : 0), 1), text)
		{
			SetInitialProperties (text, is_default);
		}
		// TODO: v2 - Remove constructors with parameters
		/// <summary>
		/// Private helper to set the initial properties of the View that were provided via constructors.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="is_default"></param>
		void SetInitialProperties (ustring text, bool is_default)
		{
			TextAlignment = TextAlignment.Centered;
			VerticalTextAlignment = VerticalTextAlignment.Middle;

			HotKeySpecifier = new Rune ('_');

			_leftBracket = new Rune (Driver != null ? Driver.LeftBracket : '[');
			_rightBracket = new Rune (Driver != null ? Driver.RightBracket : ']');
			_leftDefault = new Rune (Driver != null ? Driver.LeftDefaultIndicator : '<');
			_rightDefault = new Rune (Driver != null ? Driver.RightDefaultIndicator : '>');

			CanFocus = true;
			AutoSize = true;
			this.is_default = is_default;
			Text = text ?? string.Empty;

			OnResizeNeeded ();

			// Things this view knows how to do
			AddCommand (Command.Accept, () => AcceptKey ());

			// Default keybindings for this view
			AddKeyBinding (Key.Enter, Command.Accept);
			AddKeyBinding (Key.Space, Command.Accept);
			if (HotKey != Key.Null) {
				AddKeyBinding (Key.Space | HotKey, Command.Accept);
			}
		}

		/// <summary>
		/// Gets or sets whether the <see cref="Button"/> is the default action to activate in a dialog.
		/// </summary>
		/// <value><c>true</c> if is default; otherwise, <c>false</c>.</value>
		public bool IsDefault {
			get => is_default;
			set {
				is_default = value;
				UpdateTextFormatterText ();
				OnResizeNeeded ();
			}
		}

		/// <inheritdoc/>
		public override Key HotKey {
			get => base.HotKey;
			set {
				if (base.HotKey != value) {
					var v = value == Key.Unknown ? Key.Null : value;
					if (base.HotKey != Key.Null && ContainsKeyBinding (Key.Space | base.HotKey)) {
						if (v == Key.Null) {
							ClearKeybinding (Key.Space | base.HotKey);
						} else {
							ReplaceKeyBinding (Key.Space | base.HotKey, Key.Space | v);
						}
					} else if (v != Key.Null) {
						AddKeyBinding (Key.Space | v, Command.Accept);
					}
					base.HotKey = TextFormatter.HotKey = v;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public bool NoDecorations {get;set;}

		/// <summary>
		/// 
		/// </summary>
		public bool NoPadding {get;set;}

		/// <inheritdoc/>
		protected override void UpdateTextFormatterText ()
		{
			if(NoDecorations)
			{
				TextFormatter.Text = Text;
			}
			else
			if (IsDefault)
				TextFormatter.Text = ustring.Make (_leftBracket) + ustring.Make (_leftDefault) + " " + Text + " " + ustring.Make (_rightDefault) + ustring.Make (_rightBracket);
			else
			{
				if(NoPadding)
				{
					TextFormatter.Text = ustring.Make (_leftBracket) + Text + ustring.Make (_rightBracket);
				}
				else
				{
					TextFormatter.Text = ustring.Make (_leftBracket) + " " + Text + " " + ustring.Make (_rightBracket);
				}
			}
				
		}

		///<inheritdoc/>
		public override bool ProcessHotKey (KeyEvent kb)
		{
			if (!Enabled) {
				return false;
			}

			return ExecuteHotKey (kb);
		}

		///<inheritdoc/>
		public override bool ProcessColdKey (KeyEvent kb)
		{
			if (!Enabled) {
				return false;
			}

			return ExecuteColdKey (kb);
		}

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent kb)
		{
			if (!Enabled) {
				return false;
			}

			var result = InvokeKeybindings (kb);
			if (result != null)
				return (bool)result;

			return base.ProcessKey (kb);
		}

		bool ExecuteHotKey (KeyEvent ke)
		{
			if (ke.Key == (Key.AltMask | HotKey)) {
				return AcceptKey ();
			}
			return false;
		}

		bool ExecuteColdKey (KeyEvent ke)
		{
			if (IsDefault && ke.KeyValue == '\n') {
				return AcceptKey ();
			}
			return ExecuteHotKey (ke);
		}

		bool AcceptKey ()
		{
			if (!HasFocus) {
				SetFocus ();
			}
			OnClicked ();
			return true;
		}

		/// <summary>
		/// Virtual method to invoke the <see cref="Clicked"/> event.
		/// </summary>
		public virtual void OnClicked ()
		{
			Clicked?.Invoke (this, EventArgs.Empty);
		}

		/// <summary>
		///   Clicked <see cref="Action"/>, raised when the user clicks the primary mouse button within the Bounds of this <see cref="View"/>
		///   or if the user presses the action key while this view is focused. (TODO: IsDefault)
		/// </summary>
		/// <remarks>
		///   Client code can hook up to this event, it is
		///   raised when the button is activated either with
		///   the mouse or the keyboard.
		/// </remarks>
		public event EventHandler Clicked;

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (me.Flags == MouseFlags.Button1Clicked) {
				if (CanFocus && Enabled) {
					if (!HasFocus) {
						SetFocus ();
						SetNeedsDisplay ();
						Redraw (Bounds);
					}
					OnClicked ();
				}

				return true;
			}
			return false;
		}

		///<inheritdoc/>
		public override void PositionCursor ()
		{
			if (HotKey == Key.Unknown && Text != "") {
				for (int i = 0; i < TextFormatter.Text.RuneCount; i++) {
					if (TextFormatter.Text [i] == Text [0]) {
						Move (i, 0);
						return;
					}
				}
			}
			base.PositionCursor ();
		}

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

			return base.OnEnter (view);
		}
	}
}
