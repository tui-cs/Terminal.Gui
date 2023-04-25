//
// TextValidateField.cs: single-line text editor with validation through providers.
//
// Authors:
//	José Miguel Perricone (jmperricone@hotmail.com)
//

using NStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Terminal.Gui.TextValidateProviders;

namespace Terminal.Gui {

	namespace TextValidateProviders {
		/// <summary>
		/// TextValidateField Providers Interface.
		/// All TextValidateField are created with a ITextValidateProvider.
		/// </summary>
		public interface ITextValidateProvider {
			/// <summary>
			/// Set that this provider uses a fixed width.
			/// e.g. Masked ones are fixed.
			/// </summary>
			bool Fixed { get; }

			/// <summary>
			/// Set Cursor position to <paramref name="pos"/>.
			/// </summary>
			/// <param name="pos"></param>
			/// <returns>Return first valid position.</returns>
			int Cursor (int pos);

			/// <summary>
			/// First valid position before <paramref name="pos"/>.
			/// </summary>
			/// <param name="pos"></param>
			/// <returns>New cursor position if any, otherwise returns <paramref name="pos"/></returns>
			int CursorLeft (int pos);

			/// <summary>
			/// First valid position after <paramref name="pos"/>.
			/// </summary>
			/// <param name="pos">Current position.</param>
			/// <returns>New cursor position if any, otherwise returns <paramref name="pos"/></returns>
			int CursorRight (int pos);

			/// <summary>
			/// Find the first valid character position.
			/// </summary>
			/// <returns>New cursor position.</returns>
			int CursorStart ();

			/// <summary>
			/// Find the last valid character position.
			/// </summary>
			/// <returns>New cursor position.</returns>
			int CursorEnd ();

			/// <summary>
			/// Deletes the current character in <paramref name="pos"/>.
			/// </summary>
			/// <param name="pos"></param>
			/// <returns>true if the character was successfully removed, otherwise false.</returns>
			bool Delete (int pos);

			/// <summary>
			/// Insert character <paramref name="ch"/> in position <paramref name="pos"/>.
			/// </summary>
			/// <param name="ch"></param>
			/// <param name="pos"></param>
			/// <returns>true if the character was successfully inserted, otherwise false.</returns>
			bool InsertAt (char ch, int pos);

			/// <summary>
			/// True if the input is valid, otherwise false.
			/// </summary>
			bool IsValid { get; }

			/// <summary>
			/// Set the input text and get the current value.
			/// </summary>
			ustring Text { get; set; }

			/// <summary>
			/// Gets the formatted string for display.
			/// </summary>
			ustring DisplayText { get; }
		}

		//////////////////////////////////////////////////////////////////////////////
		// PROVIDERS
		//////////////////////////////////////////////////////////////////////////////

		#region NetMaskedTextProvider

		/// <summary>
		/// .Net MaskedTextProvider Provider for TextValidateField.
		/// <para></para>
		/// <para><a href="https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.maskedtextprovider?view=net-5.0">Wrapper around MaskedTextProvider</a></para>
		/// <para><a href="https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.maskedtextbox.mask?view=net-5.0">Masking elements</a></para>
		/// </summary>
		public class NetMaskedTextProvider : ITextValidateProvider {
			MaskedTextProvider provider;

			/// <summary>
			/// Empty Constructor
			/// </summary>
			public NetMaskedTextProvider (string mask)
			{
				Mask = mask;
			}

			/// <summary>
			/// Mask property
			/// </summary>
			public ustring Mask {
				get {
					return provider?.Mask;
				}
				set {
					var current = provider != null ? provider.ToString (false, false) : string.Empty;
					provider = new MaskedTextProvider (value == ustring.Empty ? "&&&&&&" : value.ToString ());
					if (string.IsNullOrEmpty (current) == false) {
						provider.Set (current);
					}
				}
			}

			///<inheritdoc/>
			public ustring Text {
				get {
					return provider.ToString ();
				}
				set {
					provider.Set (value.ToString ());
				}
			}

			///<inheritdoc/>
			public bool IsValid => provider.MaskCompleted;

			///<inheritdoc/>
			public bool Fixed => true;

			///<inheritdoc/>
			public ustring DisplayText => provider.ToDisplayString ();

			///<inheritdoc/>
			public int Cursor (int pos)
			{
				if (pos < 0) {
					return CursorStart ();
				} else if (pos > provider.Length) {
					return CursorEnd ();
				} else {
					var p = provider.FindEditPositionFrom (pos, false);
					if (p == -1) p = provider.FindEditPositionFrom (pos, true);
					return p;
				}
			}

			///<inheritdoc/>
			public int CursorStart ()
			{
				return
					provider.IsEditPosition (0)
					? 0
					: provider.FindEditPositionFrom (0, true);
			}

			///<inheritdoc/>
			public int CursorEnd ()
			{
				return
					provider.IsEditPosition (provider.Length - 1)
					? provider.Length - 1
					: provider.FindEditPositionFrom (provider.Length, false);
			}

			///<inheritdoc/>
			public int CursorLeft (int pos)
			{
				var c = provider.FindEditPositionFrom (pos - 1, false);
				return c == -1 ? pos : c;
			}

			///<inheritdoc/>
			public int CursorRight (int pos)
			{
				var c = provider.FindEditPositionFrom (pos + 1, true);
				return c == -1 ? pos : c;
			}

			///<inheritdoc/>
			public bool Delete (int pos)
			{
				return provider.Replace (' ', pos);// .RemoveAt (pos);
			}

			///<inheritdoc/>
			public bool InsertAt (char ch, int pos)
			{
				return provider.Replace (ch, pos);
			}
		}
		#endregion

		#region TextRegexProvider

		/// <summary>
		/// Regex Provider for TextValidateField.
		/// </summary>
		public class TextRegexProvider : ITextValidateProvider {
			Regex regex;
			List<Rune> text;
			List<Rune> pattern;

			/// <summary>
			/// Empty Constructor.
			/// </summary>
			public TextRegexProvider (string pattern)
			{
				Pattern = pattern;
			}

			/// <summary>
			/// Regex pattern property.
			/// </summary>
			public ustring Pattern {
				get {
					return ustring.Make (pattern);
				}
				set {
					pattern = value.ToRuneList ();
					CompileMask ();
					SetupText ();
				}
			}

			///<inheritdoc/>
			public ustring Text {
				get {
					return ustring.Make (text);
				}
				set {
					text = value != ustring.Empty ? value.ToRuneList () : null;
					SetupText ();
				}
			}

			///<inheritdoc/>
			public ustring DisplayText => Text;

			///<inheritdoc/>
			public bool IsValid {
				get {
					return Validate (text);
				}
			}

			///<inheritdoc/>
			public bool Fixed => false;

			/// <summary>
			/// When true, validates with the regex pattern on each input, preventing the input if it's not valid.
			/// </summary>
			public bool ValidateOnInput { get; set; } = true;

			bool Validate (List<Rune> text)
			{
				var match = regex.Match (ustring.Make (text).ToString ());
				return match.Success;
			}

			///<inheritdoc/>
			public int Cursor (int pos)
			{
				if (pos < 0) {
					return CursorStart ();
				} else if (pos >= text.Count) {
					return CursorEnd ();
				} else {
					return pos;
				}
			}

			///<inheritdoc/>
			public int CursorStart ()
			{
				return 0;
			}

			///<inheritdoc/>
			public int CursorEnd ()
			{
				return text.Count;
			}

			///<inheritdoc/>
			public int CursorLeft (int pos)
			{
				if (pos > 0) {
					return pos - 1;
				}
				return pos;
			}

			///<inheritdoc/>
			public int CursorRight (int pos)
			{
				if (pos < text.Count) {
					return pos + 1;
				}
				return pos;
			}

			///<inheritdoc/>
			public bool Delete (int pos)
			{
				if (text.Count > 0 && pos < text.Count) {
					text.RemoveAt (pos);
				}
				return true;
			}

			///<inheritdoc/>
			public bool InsertAt (char ch, int pos)
			{
				var aux = text.ToList ();
				aux.Insert (pos, ch);
				if (Validate (aux) || ValidateOnInput == false) {
					text.Insert (pos, ch);
					return true;
				}
				return false;
			}

			void SetupText ()
			{
				if (text != null && IsValid) {
					return;
				}

				text = new List<Rune> ();
			}

			/// <summary>
			/// Compiles the regex pattern for validation./>
			/// </summary>
			private void CompileMask ()
			{
				regex = new Regex (ustring.Make (pattern).ToString (), RegexOptions.Compiled);
			}
		}
		#endregion
	}

	/// <summary>
	/// Text field that validates input through a  <see cref="ITextValidateProvider"/>
	/// </summary>
	public class TextValidateField : View {

		ITextValidateProvider provider;
		int cursorPosition = 0;

		/// <summary>
		/// Initializes a new instance of the <see cref="TextValidateField"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		public TextValidateField () : this (null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextValidateField"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		public TextValidateField (ITextValidateProvider provider)
		{
			if (provider != null) {
				Provider = provider;
			}

			Initialize ();
		}

		void Initialize ()
		{
			Height = 1;
			CanFocus = true;

			// Things this view knows how to do
			AddCommand (Command.LeftHome, () => { HomeKeyHandler (); return true; });
			AddCommand (Command.RightEnd, () => { EndKeyHandler (); return true; });
			AddCommand (Command.DeleteCharRight, () => { DeleteKeyHandler (); return true; });
			AddCommand (Command.DeleteCharLeft, () => { BackspaceKeyHandler (); return true; });
			AddCommand (Command.Left, () => { CursorLeft (); return true; });
			AddCommand (Command.Right, () => { CursorRight (); return true; });

			// Default keybindings for this view
			AddKeyBinding (Key.Home, Command.LeftHome);
			AddKeyBinding (Key.End, Command.RightEnd);

			AddKeyBinding (Key.Delete, Command.DeleteCharRight);
			AddKeyBinding (Key.DeleteChar, Command.DeleteCharRight);

			AddKeyBinding (Key.Backspace, Command.DeleteCharLeft);
			AddKeyBinding (Key.CursorLeft, Command.Left);
			AddKeyBinding (Key.CursorRight, Command.Right);
		}

		/// <summary>
		/// Provider
		/// </summary>
		public ITextValidateProvider Provider {
			get => provider;
			set {
				provider = value;
				if (provider.Fixed == true) {
					this.Width = provider.DisplayText == ustring.Empty ? 10 : Text.Length;
				}
				HomeKeyHandler ();
				SetNeedsDisplay ();
			}
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent mouseEvent)
		{
			if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed)) {

				var c = provider.Cursor (mouseEvent.X - GetMargins (Frame.Width).left);
				if (provider.Fixed == false && TextAlignment == TextAlignment.Right && Text.Length > 0) {
					c += 1;
				}
				cursorPosition = c;
				SetFocus ();
				SetNeedsDisplay ();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Text
		/// </summary>
		public new ustring Text {
			get {
				if (provider == null) {
					return ustring.Empty;
				}

				return provider.Text;
			}
			set {
				if (provider == null) {
					return;
				}
				provider.Text = value;

				SetNeedsDisplay ();
			}
		}

		///<inheritdoc/>
		public override void PositionCursor ()
		{
			var (left, _) = GetMargins (Frame.Width);

			// Fixed = true, is for inputs thar have fixed width, like masked ones.
			// Fixed = false, is for normal input.
			// When it's right-aligned and it's a normal input, the cursor behaves differently.
			if (provider?.Fixed == false && TextAlignment == TextAlignment.Right) {
				Move (cursorPosition + left - 1, 0);
			} else {
				Move (cursorPosition + left, 0);
			}
		}

		/// <summary>
		/// Margins for text alignment.
		/// </summary>
		/// <param name="width">Total width</param>
		/// <returns>Left and right margins</returns>
		(int left, int right) GetMargins (int width)
		{
			var count = Text.Length;
			var total = width - count;
			switch (TextAlignment) {
			case TextAlignment.Left:
				return (0, total);
			case TextAlignment.Centered:
				return (total / 2, (total / 2) + (total % 2));
			case TextAlignment.Right:
				return (total, 0);
			default:
				return (0, total);
			}
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			if (provider == null) {
				Move (0, 0);
				Driver.AddStr ("Error: ITextValidateProvider not set!");
				return;
			}

			var bgcolor = !IsValid ? Color.BrightRed : ColorScheme.Focus.Background;
			var textColor = new Attribute (ColorScheme.Focus.Foreground, bgcolor);

			var (margin_left, margin_right) = GetMargins (bounds.Width);

			Move (0, 0);

			// Left Margin
			Driver.SetAttribute (textColor);
			for (int i = 0; i < margin_left; i++) {
				Driver.AddRune (' ');
			}

			// Content
			Driver.SetAttribute (textColor);
			// Content
			for (int i = 0; i < provider.DisplayText.Length; i++) {
				Driver.AddRune (provider.DisplayText [i]);
			}

			// Right Margin
			Driver.SetAttribute (textColor);
			for (int i = 0; i < margin_right; i++) {
				Driver.AddRune (' ');
			}
		}

		/// <summary>
		/// Try to move the cursor to the left.
		/// </summary>
		/// <returns>True if moved.</returns>
		bool CursorLeft ()
		{
			var current = cursorPosition;
			cursorPosition = provider.CursorLeft (cursorPosition);
			SetNeedsDisplay ();
			return current != cursorPosition;
		}

		/// <summary>
		/// Try to move the cursor to the right.
		/// </summary>
		/// <returns>True if moved.</returns>
		bool CursorRight ()
		{
			var current = cursorPosition;
			cursorPosition = provider.CursorRight (cursorPosition);
			SetNeedsDisplay ();
			return current != cursorPosition;
		}

		/// <summary>
		/// Delete char at cursor position - 1, moving the cursor.
		/// </summary>
		/// <returns></returns>
		bool BackspaceKeyHandler ()
		{
			if (provider.Fixed == false && TextAlignment == TextAlignment.Right && cursorPosition <= 1) {
				return false;
			}
			cursorPosition = provider.CursorLeft (cursorPosition);
			provider.Delete (cursorPosition);
			SetNeedsDisplay ();
			return true;
		}

		/// <summary>
		/// Deletes char at current position.
		/// </summary>
		/// <returns></returns>
		bool DeleteKeyHandler ()
		{
			if (provider.Fixed == false && TextAlignment == TextAlignment.Right) {
				cursorPosition = provider.CursorLeft (cursorPosition);
			}
			provider.Delete (cursorPosition);
			SetNeedsDisplay ();
			return true;
		}

		/// <summary>
		/// Moves the cursor to first char.
		/// </summary>
		/// <returns></returns>
		bool HomeKeyHandler ()
		{
			cursorPosition = provider.CursorStart ();
			SetNeedsDisplay ();
			return true;
		}

		/// <summary>
		/// Moves the cursor to the last char.
		/// </summary>
		/// <returns></returns>
		bool EndKeyHandler ()
		{
			cursorPosition = provider.CursorEnd ();
			SetNeedsDisplay ();
			return true;
		}

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent kb)
		{
			if (provider == null) {
				return false;
			}

			var result = InvokeKeybindings (kb);
			if (result != null)
				return (bool)result;

			if (kb.Key < Key.Space || kb.Key > Key.CharMask)
				return false;

			var key = new Rune ((uint)kb.KeyValue);

			var inserted = provider.InsertAt ((char)key, cursorPosition);

			if (inserted) {
				CursorRight ();
			}

			return true;
		}

		/// <summary>
		/// This property returns true if the input is valid.
		/// </summary>
		public virtual bool IsValid {
			get {
				if (provider == null) {
					return false;
				}

				return provider.IsValid;
			}
		}
	}
}
