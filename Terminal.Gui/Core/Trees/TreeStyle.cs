using System;

namespace Terminal.Gui.Trees {
	/// <summary>
	/// Defines rendering options that affect how the tree is displayed.
	/// </summary>
	public class TreeStyle {

		/// <summary>
		/// <see langword="true"/> to render vertical lines under expanded nodes to show which node belongs to which 
		/// parent. <see langword="false"/> to use only whitespace.
		/// </summary>
		/// <value></value>
		public bool ShowBranchLines { get; set; } = true;

		/// <summary>
		/// Symbol to use for branch nodes that can be expanded to indicate this to the user. 
		/// Defaults to '+'. Set to null to hide.
		/// </summary>
		public Rune? ExpandableSymbol { get; set; } = '+';

		/// <summary>
		/// Symbol to use for branch nodes that can be collapsed (are currently expanded).
		/// Defaults to '-'. Set to null to hide.
		/// </summary>
		public Rune? CollapseableSymbol { get; set; } = '-';

		/// <summary>
		/// Set to <see langword="true"/> to highlight expand/collapse symbols in hot key color.
		/// </summary>
		public bool ColorExpandSymbol { get; set; }

		/// <summary>
		/// Invert console colours used to render the expand symbol.
		/// </summary>
		public bool InvertExpandSymbolColors { get; set; }

		/// <summary>
		/// <see langword="true"/> to leave the last row of the control free for overwritting (e.g. by a scrollbar)
		/// When <see langword="true"/> scrolling will be triggered on the second last row of the control rather than.
		/// the last.
		/// </summary>
		/// <value></value>
		public bool LeaveLastRow { get; set; }

		/// <summary>
		/// Set to <see langword="true"/> to cause the selected item to be rendered with only the <see cref="Branch{T}.Model"/> text
		/// to be highlighted. If <see langword="false"/> (the default), the entire row will be highlighted.
		/// </summary>
		public bool HighlightModelTextOnly { get; set; } = false;
	}
}