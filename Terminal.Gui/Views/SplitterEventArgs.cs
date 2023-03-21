using System;

namespace Terminal.Gui {
	/// <summary>
	/// Provides data for <see cref="TileView"/> events.
	/// </summary>
	public class SplitterEventArgs : EventArgs {

		/// <summary>
		/// Creates a new instance of the <see cref="SplitterEventArgs"/> class.
		/// </summary>
		/// <param name="tileView"><see cref="TileView"/> in which splitter is being moved.</param>
		/// <param name="idx">Index of the splitter being moved in <see cref="TileView.SplitterDistances"/>.</param>
		/// <param name="splitterDistance">The new <see cref="Pos"/> of the splitter line.</param>
		public SplitterEventArgs (TileView tileView, int idx, Pos splitterDistance)
		{
			SplitterDistance = splitterDistance;
			TileView = tileView;
			Idx = idx;
		}

		/// <summary>
		/// New position of the splitter line (see <see cref="TileView.SplitterDistances"/>).
		/// </summary>
		public Pos SplitterDistance { get; }

		/// <summary>
		/// Container (sender) of the event.
		/// </summary>
		public TileView TileView { get; }

		/// <summary>
		/// Gets the index of the splitter that is being moved. This can be
		/// used to index <see cref="TileView.SplitterDistances"/>
		/// </summary>
		public int Idx { get; }
	}
}
