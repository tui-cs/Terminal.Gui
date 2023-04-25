using NStack;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Terminal.Gui {

	/// <summary>
	/// Control that hosts multiple sub views, presenting a single one at once
	/// </summary>
	public partial class TabView : View {
		private Tab selectedTab;

		/// <summary>
		/// The default <see cref="MaxTabTextWidth"/> to set on new <see cref="TabView"/> controls
		/// </summary>
		public const uint DefaultMaxTabTextWidth = 30;

		/// <summary>
		/// This sub view is the 2 or 3 line control that represents the actual tabs themselves
		/// </summary>
		TabRowView tabsBar;

		/// <summary>
		/// This sub view is the main client area of the current tab.  It hosts the <see cref="Tab.View"/> 
		/// of the tab, the <see cref="SelectedTab"/>
		/// </summary>
		View contentView;
		private List<Tab> tabs = new List<Tab> ();

		/// <summary>
		/// All tabs currently hosted by the control
		/// </summary>
		/// <value></value>
		public IReadOnlyCollection<Tab> Tabs { get => tabs.AsReadOnly (); }

		/// <summary>
		/// When there are too many tabs to render, this indicates the first
		/// tab to render on the screen.
		/// </summary>
		/// <value></value>
		public int TabScrollOffset { get; set; }

		/// <summary>
		/// The maximum number of characters to render in a Tab header.  This prevents one long tab 
		/// from pushing out all the others.
		/// </summary>
		public uint MaxTabTextWidth { get; set; } = DefaultMaxTabTextWidth;

		/// <summary>
		/// Event for when <see cref="SelectedTab"/> changes
		/// </summary>
		public event EventHandler<TabChangedEventArgs> SelectedTabChanged;

		/// <summary>
		/// Event fired when a <see cref="TabView.Tab"/> is clicked.  Can be used to cancel navigation,
		/// show context menu (e.g. on right click) etc.
		/// </summary>
		public event EventHandler<TabMouseEventArgs> TabClicked;

		/// <summary>
		/// The currently selected member of <see cref="Tabs"/> chosen by the user
		/// </summary>
		/// <value></value>
		public Tab SelectedTab {
			get => selectedTab;
			set {

				var old = selectedTab;

				if (selectedTab != null) {

					if (selectedTab.View != null) {
						// remove old content
						contentView.Remove (selectedTab.View);
					}
				}

				selectedTab = value;

				if (value != null) {

					// add new content
					if (selectedTab.View != null) {
						contentView.Add (selectedTab.View);
					}
				}

				EnsureSelectedTabIsVisible ();

				if (old != value) {
					OnSelectedTabChanged (old, value);
				}

			}
		}

		/// <summary>
		/// Render choices for how to display tabs.  After making changes, call <see cref="ApplyStyleChanges()"/>
		/// </summary>
		/// <value></value>
		public TabStyle Style { get; set; } = new TabStyle ();

		/// <summary>
		/// Initializes a <see cref="TabView"/> class using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		public TabView () : base ()
		{
			CanFocus = true;
			contentView = new View ();
			tabsBar = new TabRowView (this);

			ApplyStyleChanges ();

			base.Add (tabsBar);
			base.Add (contentView);

			// Things this view knows how to do
			AddCommand (Command.Left, () => { SwitchTabBy (-1); return true; });
			AddCommand (Command.Right, () => { SwitchTabBy (1); return true; });
			AddCommand (Command.LeftHome, () => { SelectedTab = Tabs.FirstOrDefault (); return true; });
			AddCommand (Command.RightEnd, () => { SelectedTab = Tabs.LastOrDefault (); return true; });

			// Default keybindings for this view
			AddKeyBinding (Key.CursorLeft, Command.Left);
			AddKeyBinding (Key.CursorRight, Command.Right);
			AddKeyBinding (Key.Home, Command.LeftHome);
			AddKeyBinding (Key.End, Command.RightEnd);
		}

		/// <summary>
		/// Updates the control to use the latest state settings in <see cref="Style"/>.
		/// This can change the size of the client area of the tab (for rendering the 
		/// selected tab's content).  This method includes a call 
		/// to <see cref="View.SetNeedsDisplay()"/>
		/// </summary>
		public void ApplyStyleChanges ()
		{
			contentView.X = Style.ShowBorder ? 1 : 0;
			contentView.Width = Dim.Fill (Style.ShowBorder ? 1 : 0);

			if (Style.TabsOnBottom) {
				// Tabs are along the bottom so just dodge the border
				contentView.Y = Style.ShowBorder ? 1 : 0;

				// Fill client area leaving space at bottom for tabs
				contentView.Height = Dim.Fill (GetTabHeight (false));

				var tabHeight = GetTabHeight (false);
				tabsBar.Height = tabHeight;

				tabsBar.Y = Pos.Percent (100) - tabHeight;

			} else {

				// Tabs are along the top

				var tabHeight = GetTabHeight (true);

				//move content down to make space for tabs
				contentView.Y = tabHeight;

				// Fill client area leaving space at bottom for border
				contentView.Height = Dim.Fill (Style.ShowBorder ? 1 : 0);

				// The top tab should be 2 or 3 rows high and on the top

				tabsBar.Height = tabHeight;

				// Should be able to just use 0 but switching between top/bottom tabs repeatedly breaks in ValidatePosDim if just using the absolute value 0
				tabsBar.Y = Pos.Percent (0);
			}

			SetNeedsDisplay ();
		}


		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			Move (0, 0);
			Driver.SetAttribute (GetNormalColor ());

			if (Style.ShowBorder) {
				
				// How much space do we need to leave at the bottom to show the tabs
				int spaceAtBottom = Math.Max (0, GetTabHeight (false) - 1);
				int startAtY = Math.Max (0, GetTabHeight (true) - 1);

				Border.DrawFrame (new Rect (0, startAtY, bounds.Width,
				Math.Max (bounds.Height - spaceAtBottom - startAtY, 0)), false);
			}

			if (Tabs.Any ()) {
				tabsBar.Redraw (tabsBar.Bounds);
				contentView.SetNeedsDisplay ();
				var savedClip = contentView.ClipToBounds ();
				contentView.Redraw (contentView.Bounds);
				Driver.Clip = savedClip;
			}
		}

		/// <summary>
		/// Disposes the control and all <see cref="Tabs"/>
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);

			// The selected tab will automatically be disposed but
			// any tabs not visible will need to be manually disposed

			foreach (var tab in Tabs) {
				if (!Equals (SelectedTab, tab)) {
					tab.View?.Dispose ();
				}

			}
		}

		/// <summary>
		/// Raises the <see cref="SelectedTabChanged"/> event
		/// </summary>
		protected virtual void OnSelectedTabChanged (Tab oldTab, Tab newTab)
		{

			SelectedTabChanged?.Invoke (this, new TabChangedEventArgs (oldTab, newTab));
		}

		/// <inheritdoc/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{
			if (HasFocus && CanFocus && Focused == tabsBar) {
				var result = InvokeKeybindings (keyEvent);
				if (result != null)
					return (bool)result;
			}

			return base.ProcessKey (keyEvent);
		}

		/// <summary>
		/// Changes the <see cref="SelectedTab"/> by the given <paramref name="amount"/>.  
		/// Positive for right, negative for left.  If no tab is currently selected then
		/// the first tab will become selected
		/// </summary>
		/// <param name="amount"></param>
		public void SwitchTabBy (int amount)
		{
			if (Tabs.Count == 0) {
				return;
			}

			// if there is only one tab anyway or nothing is selected
			if (Tabs.Count == 1 || SelectedTab == null) {
				SelectedTab = Tabs.ElementAt (0);
				SetNeedsDisplay ();
				return;
			}

			var currentIdx = Tabs.IndexOf (SelectedTab);

			// Currently selected tab has vanished!
			if (currentIdx == -1) {
				SelectedTab = Tabs.ElementAt (0);
				SetNeedsDisplay ();
				return;
			}

			var newIdx = Math.Max (0, Math.Min (currentIdx + amount, Tabs.Count - 1));

			SelectedTab = tabs [newIdx];
			SetNeedsDisplay ();

			EnsureSelectedTabIsVisible ();
		}

		/// <summary>
		/// Updates <see cref="TabScrollOffset"/> to be a valid index of <see cref="Tabs"/>
		/// </summary>
		/// <remarks>Changes will not be immediately visible in the display until you call <see cref="View.SetNeedsDisplay()"/></remarks>
		public void EnsureValidScrollOffsets ()
		{
			TabScrollOffset = Math.Max (Math.Min (TabScrollOffset, Tabs.Count - 1), 0);
		}

		/// <summary>
		/// Updates <see cref="TabScrollOffset"/> to ensure that <see cref="SelectedTab"/> is visible
		/// </summary>
		public void EnsureSelectedTabIsVisible ()
		{
			if (SelectedTab == null) {
				return;
			}

			// if current viewport does not include the selected tab
			if (!CalculateViewport (Bounds).Any (r => Equals (SelectedTab, r.Tab))) {

				// Set scroll offset so the first tab rendered is the
				TabScrollOffset = Math.Max (0, Tabs.IndexOf (SelectedTab));
			}
		}

		/// <summary>
		/// Returns the number of rows occupied by rendering the tabs, this depends 
		/// on <see cref="TabStyle.ShowTopLine"/> and can be 0 (e.g. if 
		/// <see cref="TabStyle.TabsOnBottom"/> and you ask for <paramref name="top"/>).
		/// </summary>
		/// <param name="top">True to measure the space required at the top of the control,
		/// false to measure space at the bottom</param>
		/// <returns></returns>
		private int GetTabHeight (bool top)
		{
			if (top && Style.TabsOnBottom) {
				return 0;
			}

			if (!top && !Style.TabsOnBottom) {
				return 0;
			}

			return Style.ShowTopLine ? 3 : 2;
		}

		/// <summary>
		/// Returns which tabs to render at each x location
		/// </summary>
		/// <returns></returns>
		private IEnumerable<TabToRender> CalculateViewport (Rect bounds)
		{
			int i = 1;

			// Starting at the first or scrolled to tab
			foreach (var tab in Tabs.Skip (TabScrollOffset)) {

				// while there is space for the tab
				var tabTextWidth = tab.Text.Sum (c => Rune.ColumnWidth (c));

				string text = tab.Text.ToString ();

				// The maximum number of characters to use for the tab name as specified
				// by the user (MaxTabTextWidth).  But not more than the width of the view
				// or we won't even be able to render a single tab!
				var maxWidth = Math.Max (0, Math.Min (bounds.Width - 3, MaxTabTextWidth));

				// if tab view is width <= 3 don't render any tabs
				if (maxWidth == 0) {
					yield return new TabToRender (i, tab, string.Empty, Equals (SelectedTab, tab), 0);
					break;
				}

				if (tabTextWidth > maxWidth) {
					text = tab.Text.ToString ().Substring (0, (int)maxWidth);
					tabTextWidth = (int)maxWidth;
				}

				// if there is not enough space for this tab
				if (i + tabTextWidth >= bounds.Width) {
					break;
				}

				// there is enough space!
				yield return new TabToRender (i, tab, text, Equals (SelectedTab, tab), tabTextWidth);
				i += tabTextWidth + 1;
			}
		}

		/// <summary>
		/// Adds the given <paramref name="tab"/> to <see cref="Tabs"/>
		/// </summary>
		/// <param name="tab"></param>
		/// <param name="andSelect">True to make the newly added Tab the <see cref="SelectedTab"/></param>
		public void AddTab (Tab tab, bool andSelect)
		{
			if (tabs.Contains (tab)) {
				return;
			}

			tabs.Add (tab);

			if (SelectedTab == null || andSelect) {
				SelectedTab = tab;

				EnsureSelectedTabIsVisible ();

				tab.View?.SetFocus ();
			}

			SetNeedsDisplay ();
		}

		/// <summary>
		/// Removes the given <paramref name="tab"/> from <see cref="Tabs"/>.
		/// Caller is responsible for disposing the tab's hosted <see cref="Tab.View"/>
		/// if appropriate.
		/// </summary>
		/// <param name="tab"></param>
		public void RemoveTab (Tab tab)
		{
			if (tab == null || !tabs.Contains (tab)) {
				return;
			}

			// what tab was selected before closing
			var idx = tabs.IndexOf (tab);

			tabs.Remove (tab);

			// if the currently selected tab is no longer a member of Tabs
			if (SelectedTab == null || !Tabs.Contains (SelectedTab)) {
				// select the tab closest to the one that disappeared
				var toSelect = Math.Max (idx - 1, 0);

				if (toSelect < Tabs.Count) {
					SelectedTab = Tabs.ElementAt (toSelect);
				} else {
					SelectedTab = Tabs.LastOrDefault ();
				}

			}

			EnsureSelectedTabIsVisible ();
			SetNeedsDisplay ();
		}

		#region Nested Types

		private class TabToRender {
			public int X { get; set; }
			public Tab Tab { get; set; }

			/// <summary>
			/// True if the tab that is being rendered is the selected one
			/// </summary>
			/// <value></value>
			public bool IsSelected { get; set; }
			public int Width { get; }
			public string TextToRender { get; }

			public TabToRender (int x, Tab tab, string textToRender, bool isSelected, int width)
			{
				X = x;
				Tab = tab;
				IsSelected = isSelected;
				Width = width;
				TextToRender = textToRender;
			}
		}

		private class TabRowView : View {

			readonly TabView host;

			public TabRowView (TabView host)
			{
				this.host = host;

				CanFocus = true;
				Height = 1;
				Width = Dim.Fill ();
			}

			public override bool OnEnter (View view)
			{
				Driver.SetCursorVisibility (CursorVisibility.Invisible);
				return base.OnEnter (view);
			}

			public override void Redraw (Rect bounds)
			{
				var tabLocations = host.CalculateViewport (bounds).ToArray ();
				var width = bounds.Width;
				Driver.SetAttribute (GetNormalColor ());

				if (host.Style.ShowTopLine) {
					RenderOverline (tabLocations, width);
				}

				RenderTabLine (tabLocations, width);

				RenderUnderline (tabLocations, width);
				Driver.SetAttribute (GetNormalColor ());

			}

			/// <summary>
			/// Renders the line of the tabs that does not adjoin the content
			/// </summary>
			/// <param name="tabLocations"></param>
			/// <param name="width"></param>
			private void RenderOverline (TabToRender [] tabLocations, int width)
			{
				// if tabs are on the bottom draw the side of the tab that doesn't border the content area at the bottom otherwise the top
				int y = host.Style.TabsOnBottom ? 2 : 0;

				Move (0, y);

				var selected = tabLocations.FirstOrDefault (t => t.IsSelected);

				// Clear out everything
				Driver.AddStr (new string (' ', width));

				// Nothing is selected... odd but we are done
				if (selected == null) {
					return;
				}

				Move (selected.X - 1, y);
				Driver.AddRune (host.Style.TabsOnBottom ? Driver.LLCorner : Driver.ULCorner);

				for (int i = 0; i < selected.Width; i++) {

					if (selected.X + i > width) {
						// we ran out of space horizontally
						return;
					}

					Driver.AddRune (Driver.HLine);
				}

				// Add the end of the selected tab
				Driver.AddRune (host.Style.TabsOnBottom ? Driver.LRCorner : Driver.URCorner);

			}

			/// <summary>
			/// Renders the line with the tab names in it
			/// </summary>
			/// <param name="tabLocations"></param>
			/// <param name="width"></param>
			private void RenderTabLine (TabToRender [] tabLocations, int width)
			{
				int y;

				if (host.Style.TabsOnBottom) {

					y = 1;
				} else {
					y = host.Style.ShowTopLine ? 1 : 0;
				}


				// clear any old text
				Move (0, y);
				Driver.AddStr (new string (' ', width));

				foreach (var toRender in tabLocations) {

					if (toRender.IsSelected) {
						Move (toRender.X - 1, y);
						Driver.AddRune (Driver.VLine);
					}

					Move (toRender.X, y);

					// if tab is the selected one and focus is inside this control
					if (toRender.IsSelected && host.HasFocus) {

						if (host.Focused == this) {

							// if focus is the tab bar ourself then show that they can switch tabs
							Driver.SetAttribute (ColorScheme.HotFocus);

						} else {

							// Focus is inside the tab
							Driver.SetAttribute (ColorScheme.HotNormal);
						}
					}

					Driver.AddStr (toRender.TextToRender);
					Driver.SetAttribute (GetNormalColor ());

					if (toRender.IsSelected) {
						Driver.AddRune (Driver.VLine);
					}
				}
			}

			/// <summary>
			/// Renders the line of the tab that adjoins the content of the tab
			/// </summary>
			/// <param name="tabLocations"></param>
			/// <param name="width"></param>
			private void RenderUnderline (TabToRender [] tabLocations, int width)
			{
				int y = GetUnderlineYPosition ();

				Move (0, y);

				// If host has no border then we need to draw the solid line first (then we draw gaps over the top)
				if (!host.Style.ShowBorder) {

					for (int x = 0; x < width; x++) {
						Driver.AddRune (Driver.HLine);
					}

				}
				var selected = tabLocations.FirstOrDefault (t => t.IsSelected);

				if (selected == null) {
					return;
				}

				Move (selected.X - 1, y);

				Driver.AddRune (selected.X == 1 ? Driver.VLine :
			(host.Style.TabsOnBottom ? Driver.URCorner : Driver.LRCorner));

				Driver.AddStr (new string (' ', selected.Width));

				Driver.AddRune (selected.X + selected.Width == width - 1 ?
		     Driver.VLine :
				(host.Style.TabsOnBottom ? Driver.ULCorner : Driver.LLCorner));

				// draw scroll indicators

				// if there are more tabs to the left not visible
				if (host.TabScrollOffset > 0) {
					Move (0, y);

					// indicate that
					Driver.AddRune (Driver.LeftArrow);
				}

				// if there are more tabs to the right not visible
				if (ShouldDrawRightScrollIndicator (tabLocations)) {
					Move (width - 1, y);

					// indicate that
					Driver.AddRune (Driver.RightArrow);
				}
			}

			private bool ShouldDrawRightScrollIndicator (TabToRender [] tabLocations)
			{
				return tabLocations.LastOrDefault ()?.Tab != host.Tabs.LastOrDefault ();
			}

			private int GetUnderlineYPosition ()
			{
				if (host.Style.TabsOnBottom) {

					return 0;
				} else {

					return host.Style.ShowTopLine ? 2 : 1;
				}
			}

			public override bool MouseEvent (MouseEvent me)
			{
				var hit = ScreenToTab (me.X, me.Y);

				bool isClick = me.Flags.HasFlag (MouseFlags.Button1Clicked) ||
					me.Flags.HasFlag (MouseFlags.Button2Clicked) ||
					me.Flags.HasFlag (MouseFlags.Button3Clicked);

				if (isClick) {
					host.OnTabClicked (new TabMouseEventArgs (hit, me));

					// user canceled click
					if (me.Handled) {
						return true;
					}
				}

				if (!me.Flags.HasFlag (MouseFlags.Button1Clicked) &&
				!me.Flags.HasFlag (MouseFlags.Button1DoubleClicked) &&
				!me.Flags.HasFlag (MouseFlags.Button1TripleClicked))
					return false;

				if (!HasFocus && CanFocus) {
					SetFocus ();
				}

				if (me.Flags.HasFlag (MouseFlags.Button1Clicked) ||
				me.Flags.HasFlag (MouseFlags.Button1DoubleClicked) ||
				me.Flags.HasFlag (MouseFlags.Button1TripleClicked)) {

					var scrollIndicatorHit = ScreenToScrollIndicator (me.X, me.Y);

					if (scrollIndicatorHit != 0) {

						host.SwitchTabBy (scrollIndicatorHit);

						SetNeedsDisplay ();
						return true;
					}

					if (hit != null) {
						host.SelectedTab = hit;
						SetNeedsDisplay ();
						return true;
					}
				}

				return false;
			}

			/// <summary>
			/// Calculates whether scroll indicators are visible and if so whether the click
			/// was on one of them.
			/// </summary>
			/// <param name="x"></param>
			/// <param name="y"></param>
			/// <returns>-1 for click in scroll left, 1 for scroll right or 0 for no hit</returns>
			private int ScreenToScrollIndicator (int x, int y)
			{
				// scroll indicator is showing
				if (host.TabScrollOffset > 0 && x == 0) {

					return y == GetUnderlineYPosition () ? -1 : 0;
				}

				// scroll indicator is showing
				if (x == Bounds.Width - 1 && ShouldDrawRightScrollIndicator (host.CalculateViewport (Bounds).ToArray ())) {

					return y == GetUnderlineYPosition () ? 1 : 0;
				}

				return 0;
			}

			/// <summary>
			/// Translates the client coordinates of a click into a tab when the click is on top of a tab
			/// </summary>
			/// <param name="x"></param>
			/// <param name="y"></param>
			/// <returns></returns>
			public Tab ScreenToTab (int x, int y)
			{
				var tabs = host.CalculateViewport (Bounds);

				return tabs.LastOrDefault (t => x >= t.X && x < t.X + t.Width)?.Tab;
			}
		}

		/// <summary>
		/// Raises the <see cref="TabClicked"/> event.
		/// </summary>
		/// <param name="tabMouseEventArgs"></param>
		protected virtual private void OnTabClicked (TabMouseEventArgs tabMouseEventArgs)
		{
			TabClicked?.Invoke (this, tabMouseEventArgs);
		}

		/// <summary>
		/// A single tab in a <see cref="TabView"/>
		/// </summary>
		public class Tab {
			private ustring text;

			/// <summary>
			/// The text to display in a <see cref="TabView"/>
			/// </summary>
			/// <value></value>
			public ustring Text { get => text ?? "Unamed"; set => text = value; }

			/// <summary>
			/// The control to display when the tab is selected
			/// </summary>
			/// <value></value>
			public View View { get; set; }

			/// <summary>
			/// Creates a new unamed tab with no controls inside
			/// </summary>
			public Tab ()
			{

			}

			/// <summary>
			/// Creates a new tab with the given text hosting a view
			/// </summary>
			/// <param name="text"></param>
			/// <param name="view"></param>
			public Tab (string text, View view)
			{
				this.Text = text;
				this.View = view;
			}
		}

		/// <summary>
		/// Describes render stylistic selections of a <see cref="TabView"/>
		/// </summary>
		public class TabStyle {

			/// <summary>
			/// True to show the top lip of tabs.  False to directly begin with tab text during 
			/// rendering.  When true header line occupies 3 rows, when false only 2.
			/// Defaults to true.
			/// 
			/// <para>When <see cref="TabsOnBottom"/> is enabled this instead applies to the
			///  bottommost line of the control</para>
			/// </summary> 
			public bool ShowTopLine { get; set; } = true;

			/// <summary>
			/// True to show a solid box around the edge of the control.  Defaults to true.
			/// </summary>
			public bool ShowBorder { get; set; } = true;

			/// <summary>
			/// True to render tabs at the bottom of the view instead of the top
			/// </summary>
			public bool TabsOnBottom { get; set; } = false;

		}
		#endregion
	}
}
