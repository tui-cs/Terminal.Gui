//------------------------------------------------------------------------------
// Windows Terminal supports Unicode and Emoji characters, but by default
// conhost shells (e.g., PowerShell and cmd.exe) do not. See
// <https://spectreconsole.net/best-practices>.
//------------------------------------------------------------------------------

using System;

namespace Terminal.Gui {
	/// <summary>
	/// A <see cref="View"/> which displays (by default) a spinning line character.
	/// </summary>
	/// <remarks>
	/// By default animation only occurs when you call <see cref="View.SetNeedsDisplay()"/>.
	/// Use <see cref="AutoSpin"/> to make the automate calls to <see cref="View.SetNeedsDisplay()"/>.
	/// </remarks>
	public class SpinnerView : View {
		private const int DEFAULT_DELAY = 130;
		private static readonly SpinnerStyle DEFAULT_STYLE = new SpinnerStyle.Line ();

		private SpinnerStyle _style = DEFAULT_STYLE;
		private int _delay = DEFAULT_STYLE.SpinDelay;
		private bool _bounce = DEFAULT_STYLE.SpinBounce;
		private string [] _sequence = DEFAULT_STYLE.Sequence;
		private bool _bounceReverse = false;
		private int _currentIdx = 0;
		private DateTime _lastRender = DateTime.MinValue;
		private object _timeout;

		/// <summary>
		/// Gets or sets the Style used to animate the spinner.
		/// </summary>
		public SpinnerStyle Style { get => _style; set => SetStyle (value); }

		/// <summary>
		/// Gets or sets the animation frames used to animate the spinner.
		/// </summary>
		public string [] Sequence { get => _sequence; set => SetSequence (value); }

		/// <summary>
		/// Gets or sets the number of milliseconds to wait between characters
		/// in the animation.
		/// </summary>
		/// <remarks>This is the maximum speed the spinner will rotate at.  You still need to
		/// call <see cref="View.SetNeedsDisplay()"/> or <see cref="SpinnerView.AutoSpin"/> to
		/// advance/start animation.</remarks>
		public int SpinDelay { get => _delay; set => SetDelay (value); }

		/// <summary>
		/// Gets or sets whether spinner should go back and forth through the frames rather than
		/// going to the end and starting again at the beginning.
		/// </summary>
		public bool SpinBounce { get => _bounce; set => SetBounce (value); }

		/// <summary>
		/// Gets or sets whether spinner should go through the frames in reverse order.
		/// If SpinBounce is true, this sets the starting order.
		/// </summary>
		public bool SpinReverse { get; set; } = false;

		/// <summary>
		/// Gets whether the current spinner style contains emoji or other special characters.
		/// Does not check Custom sequences.
		/// </summary>
		public bool HasSpecialCharacters { get => _style.HasSpecialCharacters; }

		/// <summary>
		/// Gets whether the current spinner style contains only ASCII characters.  Also checks Custom sequences.
		/// </summary>
		public bool IsAsciiOnly { get => GetIsAsciiOnly (); }

		/// <summary>
		/// Creates a new instance of the <see cref="SpinnerView"/> class.
		/// </summary>
		public SpinnerView ()
		{
			Width = 1;
			Height = 1;
			_delay = DEFAULT_DELAY;
			_bounce = false;
			SpinReverse = false;
			SetStyle (DEFAULT_STYLE);
		}

		private void SetStyle (SpinnerStyle style)
		{
			if (style is not null) {
				_style = style;
				_sequence = style.Sequence;
				_delay = style.SpinDelay;
				_bounce = style.SpinBounce;
				Width = GetSpinnerWidth ();
			}
		}

		private void SetSequence (string [] frames)
		{
			if (frames is not null && frames.Length > 0) {
				_style = new SpinnerStyle.Custom ();
				_sequence = frames;
				Width = GetSpinnerWidth ();
			}
		}

		private void SetDelay (int delay)
		{
			if (delay > -1) {
				_delay = delay;
			}
		}

		private void SetBounce (bool bounce)
		{
			_bounce = bounce;
		}

		private int GetSpinnerWidth ()
		{
			int max = 0;
			if (_sequence is not null && _sequence.Length > 0) {
				foreach (string frame in _sequence) {
					if (frame.Length > max) {
						max = frame.Length;
					}
				}
			}
			return max;
		}

		private bool GetIsAsciiOnly ()
		{
			if (HasSpecialCharacters) {
				return false;
			}
			if (_sequence is not null && _sequence.Length > 0) {
				foreach (string frame in _sequence) {
					foreach (char c in frame) {
						if (!char.IsAscii (c)) {
							return false;
						}
					}
				}
				return true;
			}
			return true;
		}

		/// <inheritdoc/>
		public override void OnDrawContent (Rect contentArea)
		{
			if (DateTime.Now - _lastRender > TimeSpan.FromMilliseconds (SpinDelay)) {
				//_currentIdx = (_currentIdx + 1) % Sequence.Length;
				if (Sequence is not null && Sequence.Length > 1) {
					int d = 1;
					if ((_bounceReverse && !SpinReverse) || (!_bounceReverse && SpinReverse)) {
						d = -1;
					}
					_currentIdx += d;

					if (_currentIdx >= Sequence.Length) {
						if (SpinBounce) {
							if (SpinReverse) {
								_bounceReverse = false;
							} else {
								_bounceReverse = true;
							}
							_currentIdx = Sequence.Length - 1;
						} else {
							_currentIdx = 0;
						}
					}
					if (_currentIdx < 0) {
						if (SpinBounce) {
							if (SpinReverse) {
								_bounceReverse = true;
							} else {
								_bounceReverse = false;
							}
							_currentIdx = 1;
						} else {
							_currentIdx = Sequence.Length - 1;
						}
					}
					Text = "" + Sequence [_currentIdx]; //.EnumerateRunes;
				}
				_lastRender = DateTime.Now;
			}

			base.OnDrawContent (contentArea);
		}

		/// <summary>
		/// Gets or sets whether spinning should occur automatically or be manually
		/// triggered (e.g. from a background task).
		/// </summary>
		public bool AutoSpin {
			get {
				return _timeout != null;
			}
			set {
				if (value) {
					AddAutoSpinTimeout ();
				} else {
					RemoveAutoSpinTimeout ();
				}
			}
		}

		private void AddAutoSpinTimeout ()
		{
			if (_timeout != null) {
				return;
			}

			_timeout = Application.MainLoop.AddTimeout (
				TimeSpan.FromMilliseconds (SpinDelay), (m) => {
					Application.MainLoop.Invoke (this.SetNeedsDisplay);
					return true;
				});
		}


		private void RemoveAutoSpinTimeout ()
		{
			if (_timeout != null) {
				Application.MainLoop.RemoveTimeout (_timeout);
				_timeout = null;
			}
		}




		/// <inheritdoc/>
		protected override void Dispose (bool disposing)
		{
			RemoveAutoSpinTimeout ();

			base.Dispose (disposing);
		}
	}
}