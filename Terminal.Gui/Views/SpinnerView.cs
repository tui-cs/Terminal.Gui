using System;

namespace Terminal.Gui {

	/// <summary>
	/// A 1x1 <see cref="View"/> based on <see cref="Label"/> which displays a spinning
	/// line character.
	/// </summary>
	/// <remarks>
	/// By default animation only occurs when you call <see cref="View.SetNeedsDisplay()"/>.
	/// Use <see cref="AutoSpin"/> to make the automate calls to <see cref="View.SetNeedsDisplay()"/>.
	/// </remarks>
	public class SpinnerView : Label {
		private Rune [] _runes = new Rune [] { '|', '/', '\u2500', '\\' };
		private int _currentIdx = 0;
		private DateTime _lastRender = DateTime.MinValue;
		private object _timeout;

		/// <summary>
		/// Gets or sets the number of milliseconds to wait between characters
		/// in the spin.  Defaults to 250.
		/// </summary>
		/// <remarks>This is the maximum speed the spinner will rotate at.  You still need to
		/// call <see cref="View.SetNeedsDisplay()"/> or <see cref="SpinnerView.AutoSpin"/> to
		/// advance/start animation.</remarks>
		public int SpinDelayInMilliseconds { get; set; } = 250;

		/// <summary>
		/// Creates a new instance of the <see cref="SpinnerView"/> class.
		/// </summary>
		public SpinnerView ()
		{
			Width = 1; Height = 1;
		}

		/// <inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			if (DateTime.Now - _lastRender > TimeSpan.FromMilliseconds (SpinDelayInMilliseconds)) {
				_currentIdx = (_currentIdx + 1) % _runes.Length;
				Text = "" + _runes [_currentIdx];
				_lastRender = DateTime.Now;
			}

			base.Redraw (bounds);
		}

		/// <summary>
		/// Automates spinning
		/// </summary>
		public void AutoSpin ()
		{
			if (_timeout != null) {
				return;
			}

			_timeout = Application.MainLoop.AddTimeout (
				TimeSpan.FromMilliseconds (SpinDelayInMilliseconds), (m) => {
					Application.MainLoop.Invoke (this.SetNeedsDisplay);
					return true;
				});
		}

		/// <inheritdoc/>
		protected override void Dispose (bool disposing)
		{
			if (_timeout != null) {
				Application.MainLoop.RemoveTimeout (_timeout);
				_timeout = null;
			}

			base.Dispose (disposing);
		}
	}
}