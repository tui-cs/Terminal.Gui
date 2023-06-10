using System.Text;
using System;

namespace Terminal.Gui {

	public partial class View {
		string _text;

		/// <summary>
		///   The text displayed by the <see cref="View"/>.
		/// </summary>
		/// <remarks>
		/// <para>
		///  The text will be drawn before any subviews are drawn.
		/// </para>
		/// <para>
		///  The text will be drawn starting at the view origin (0, 0) and will be formatted according
		///  to <see cref="TextAlignment"/> and <see cref="TextDirection"/>. 
		/// </para>
		/// <para>
		///  The text will word-wrap to additional lines if it does not fit horizontally. If <see cref="Bounds"/>'s height
		///  is 1, the text will be clipped.	
		///  </para>
		/// <para>
		///  Set the <see cref="HotKeySpecifier"/> to enable hotkey support. To disable hotkey support set <see cref="HotKeySpecifier"/> to
		///  <c>(Rune)0xffff</c>.
		/// </para>
		/// </remarks>
		public virtual string Text {
			get => _text;
			set {
				_text = value;
				SetHotKey ();
				UpdateTextFormatterText ();
				//TextFormatter.Format ();
				OnResizeNeeded ();

#if DEBUG
				if (_text != null && string.IsNullOrEmpty (Id)) {
					Id = _text;
				}
#endif
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="Gui.TextFormatter"/> used to format <see cref="Text"/>.
		/// </summary>
		public TextFormatter TextFormatter { get; set; }

		void TextFormatter_HotKeyChanged (object sender, KeyChangedEventArgs e)
		{
			HotKeyChanged?.Invoke (this, e);
		}

		/// <summary>
		/// Can be overridden if the <see cref="Terminal.Gui.TextFormatter.Text"/> has
		///  different format than the default.
		/// </summary>
		protected virtual void UpdateTextFormatterText ()
		{
			if (TextFormatter != null) {
				TextFormatter.Text = _text;
			}
		}

		/// <summary>
		/// Gets or sets whether trailing spaces at the end of word-wrapped lines are preserved
		/// or not when <see cref="TextFormatter.WordWrap"/> is enabled. 
		/// If <see langword="true"/> trailing spaces at the end of wrapped lines will be removed when 
		/// <see cref="Text"/> is formatted for display. The default is <see langword="false"/>.
		/// </summary>
		public virtual bool PreserveTrailingSpaces {
			get => TextFormatter.PreserveTrailingSpaces;
			set {
				if (TextFormatter.PreserveTrailingSpaces != value) {
					TextFormatter.PreserveTrailingSpaces = value;
					TextFormatter.NeedsFormat = true;
				}
			}
		}

		/// <summary>
		/// Gets or sets how the View's <see cref="Text"/> is aligned horizontally when drawn. Changing this property will redisplay the <see cref="View"/>.
		/// </summary>
		/// <value>The text alignment.</value>
		public virtual TextAlignment TextAlignment {
			get => TextFormatter.Alignment;
			set {
				TextFormatter.Alignment = value;
				UpdateTextFormatterText ();
				OnResizeNeeded ();
			}
		}

		/// <summary>
		/// Gets or sets how the View's <see cref="Text"/> is aligned vertically when drawn. Changing this property will redisplay the <see cref="View"/>.
		/// </summary>
		/// <value>The text alignment.</value>
		public virtual VerticalTextAlignment VerticalTextAlignment {
			get => TextFormatter.VerticalAlignment;
			set {
				TextFormatter.VerticalAlignment = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Gets or sets the direction of the View's <see cref="Text"/>. Changing this property will redisplay the <see cref="View"/>.
		/// </summary>
		/// <value>The text alignment.</value>
		public virtual TextDirection TextDirection {
			get => TextFormatter.Direction;
			set {
				UpdateTextDirection (value);
				TextFormatter.Direction = value;
			}
		}

		private void UpdateTextDirection (TextDirection newDirection)
		{
			var directionChanged = TextFormatter.IsHorizontalDirection (TextFormatter.Direction)
			    != TextFormatter.IsHorizontalDirection (newDirection);
			TextFormatter.Direction = newDirection;

			var isValidOldAutoSize = AutoSize && IsValidAutoSize (out var _);

			UpdateTextFormatterText ();

			if ((!ForceValidatePosDim && directionChanged && AutoSize)
			    || (ForceValidatePosDim && directionChanged && AutoSize && isValidOldAutoSize)) {
				OnResizeNeeded ();
			} else if (directionChanged && IsAdded) {
				SetWidthHeight (Bounds.Size);
				SetMinWidthHeight ();
			} else {
				SetMinWidthHeight ();
			}
			TextFormatter.Size = GetSizeNeededForTextAndHotKey ();
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Gets the width or height of the <see cref="Terminal.Gui.TextFormatter.HotKeySpecifier"/> characters 
		/// in the <see cref="Text"/> property.
		/// </summary>
		/// <remarks>
		/// Only the first hotkey specifier found in <see cref="Text"/> is supported.
		/// </remarks>
		/// <param name="isWidth">If <see langword="true"/> (the default) the width required for the hotkey specifier is returned. Otherwise the height is returned.</param>
		/// <returns>The number of characters required for the <see cref="Terminal.Gui.TextFormatter.HotKeySpecifier"/>. If the text direction specified
		/// by <see cref="TextDirection"/> does not match the <paramref name="isWidth"/> parameter, <c>0</c> is returned.</returns>
		public int GetHotKeySpecifierLength (bool isWidth = true)
		{
			if (isWidth) {
				return TextFormatter.IsHorizontalDirection (TextDirection) &&
				    TextFormatter.Text?.Contains ((char)HotKeySpecifier.Value) == true
				    ? Math.Max (HotKeySpecifier.GetColumns (), 0) : 0;
			} else {
				return TextFormatter.IsVerticalDirection (TextDirection) &&
				    TextFormatter.Text?.Contains ((char)HotKeySpecifier.Value) == true
				    ? Math.Max (HotKeySpecifier.GetColumns(), 0) : 0;
			}
		}

		/// <summary>
		/// Gets the dimensions required for <see cref="Text"/> ignoring a <see cref="Terminal.Gui.TextFormatter.HotKeySpecifier"/>.
		/// </summary>
		/// <returns></returns>
		public Size GetSizeNeededForTextWithoutHotKey ()
		{
			return new Size (TextFormatter.Size.Width - GetHotKeySpecifierLength (),
			    TextFormatter.Size.Height - GetHotKeySpecifierLength (false));
		}

		/// <summary>
		/// Gets the dimensions required for <see cref="Text"/> accounting for a <see cref="Terminal.Gui.TextFormatter.HotKeySpecifier"/> .
		/// </summary>
		/// <returns></returns>
		public Size GetSizeNeededForTextAndHotKey ()
		{
			if (string.IsNullOrEmpty (TextFormatter.Text)) {

				if (!IsInitialized) return Size.Empty;

				return Bounds.Size;
			}

			// BUGBUG: This IGNORES what Text is set to, using on only the current View size. This doesn't seem to make sense.
			// BUGBUG: This uses Frame; in v2 it should be Bounds
			return new Size (_frame.Size.Width + GetHotKeySpecifierLength (),
					 _frame.Size.Height + GetHotKeySpecifierLength (false));
		}

	}
}