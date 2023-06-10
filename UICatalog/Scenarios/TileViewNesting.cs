using System;
using Terminal.Gui;
using System.Linq;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Tile View Nesting", Description: "Demonstrates recursive nesting of TileViews")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("LineView")]
	public class TileViewNesting : Scenario {

		private View workArea;
		private TextField textField;
		private CheckBox cbHorizontal;
		private CheckBox cbBorder;
		private CheckBox cbTitles;
		private CheckBox cbUseLabels;

		bool loaded = false;
		int viewsCreated;
		int viewsToCreate;

		/// <summary>
		/// Setup the scenario.
		/// </summary>
		public override void Setup ()
		{
			// Scenario Windows.
			Win.Title = this.GetName ();
			Win.Y = 1;

			var lblViews = new Label ("Number Of Views:");
			textField = new TextField {
				X = Pos.Right (lblViews),
				Width = 10,
				Text = "2",
			};

			textField.TextChanged += (s,e) => SetupTileView ();

			cbHorizontal = new CheckBox ("Horizontal") {
				X = Pos.Right (textField) + 1
			};
			cbHorizontal.Toggled += (s, e) => SetupTileView ();

			cbBorder = new CheckBox ("Border") {
				X = Pos.Right (cbHorizontal) + 1
			};
			cbBorder.Toggled += (s, e) => SetupTileView ();

			cbTitles = new CheckBox ("Titles") {
				X = Pos.Right (cbBorder) + 1
			};
			cbTitles.Toggled += (s,e) => SetupTileView ();

			cbUseLabels = new CheckBox ("Use Labels") {
				X = Pos.Right (cbTitles) + 1
			};
			cbUseLabels.Toggled += (s, e) => SetupTileView ();

			workArea = new View {
				X = 0,
				Y = 1,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};

			var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuItem ("_Quit", "", () => Quit()),
			}) });

			Win.Add (lblViews);
			Win.Add (textField);
			Win.Add (cbHorizontal);
			Win.Add (cbBorder);
			Win.Add (cbTitles);
			Win.Add (cbUseLabels);
			Win.Add (workArea);

			SetupTileView ();

			Application.Top.Add (menu);

			Win.Loaded += (s,e) => loaded = true;
		}

		private void SetupTileView ()
		{
			int numberOfViews = GetNumberOfViews ();

			bool? titles = cbTitles.Checked;
			bool? border = cbBorder.Checked;
			bool? startHorizontal = cbHorizontal.Checked;

			foreach(var sub in workArea.Subviews) {
				sub.Dispose ();
			}
			workArea.RemoveAll ();

			if (numberOfViews <= 0) {
				return;
			}

			var root = CreateTileView (1, (bool)startHorizontal ?
					Orientation.Horizontal :
					Orientation.Vertical);

			root.Tiles.ElementAt (0).ContentView.Add (CreateContentControl (1));
			root.Tiles.ElementAt (0).Title = (bool)cbTitles.Checked ? $"View 1" : string.Empty;
			root.Tiles.ElementAt (1).ContentView.Add (CreateContentControl (2));
			root.Tiles.ElementAt (1).Title = (bool)cbTitles.Checked ? $"View 2" : string.Empty;

			root.LineStyle = (bool)border ? LineStyle.Rounded : LineStyle.None;

			workArea.Add (root);

			if (numberOfViews == 1) {
				root.Tiles.ElementAt (1).ContentView.Visible = false;
			}

			if (numberOfViews > 2) {

				viewsCreated = 2;
				viewsToCreate = numberOfViews;
				AddMoreViews (root);
			}

			if (loaded) {
				workArea.LayoutSubviews ();
			}
		}

		private View CreateContentControl (int number)
		{
			return (bool)cbUseLabels.Checked ?
				CreateLabelView (number) :
				CreateTextView (number);
		}

		private View CreateLabelView (int number)
		{
			return new Label {
				Width = Dim.Fill (),
				Height = 1,
				AutoSize = false,
				Text = number.ToString ().Repeat (1000),
				CanFocus = true,
			};
		}
		private View CreateTextView (int number)
		{
			return new TextView {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Text = number.ToString ().Repeat (1000),
				AllowsTab = false,
				//WordWrap = true,  // TODO: This is very slow (like 10s to render with 45 views)
			};
		}

		private void AddMoreViews (TileView to)
		{
			if (viewsCreated == viewsToCreate) {
				return;
			}
			if (!(to.Tiles.ElementAt (0).ContentView is TileView)) {
				Split (to, true);
			}

			if (!(to.Tiles.ElementAt (1).ContentView is TileView)) {
				Split (to, false);
			}

			if (to.Tiles.ElementAt (0).ContentView is TileView && to.Tiles.ElementAt (1).ContentView is TileView) {

				AddMoreViews ((TileView)to.Tiles.ElementAt (0).ContentView);
				AddMoreViews ((TileView)to.Tiles.ElementAt (1).ContentView);
			}

		}

		private void Split (TileView to, bool left)
		{
			if (viewsCreated == viewsToCreate) {
				return;
			}

			TileView newView;

			if (left) {
				to.TrySplitTile (0, 2, out newView);

			} else {
				to.TrySplitTile (1, 2, out newView);
			}

			viewsCreated++;

			// During splitting the old Title will have been migrated to View1 so we only need
			// to set the Title on View2 (the one that gets our new TextView)
			newView.Tiles.ElementAt (1).Title = (bool)cbTitles.Checked ? $"View {viewsCreated}" : string.Empty;

			// Flip orientation
			newView.Orientation = to.Orientation == Orientation.Vertical ?
				Orientation.Horizontal :
				Orientation.Vertical;

			newView.Tiles.ElementAt (1).ContentView.Add (CreateContentControl (viewsCreated));
		}

		private TileView CreateTileView (int titleNumber, Orientation orientation)
		{
			var toReturn = new TileView {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				// flip the orientation
				Orientation = orientation
			};

			toReturn.Tiles.ElementAt (0).Title = (bool)cbTitles.Checked ? $"View {titleNumber}" : string.Empty;
			toReturn.Tiles.ElementAt (1).Title = (bool)cbTitles.Checked ? $"View {titleNumber + 1}" : string.Empty;

			return toReturn;
		}

		private int GetNumberOfViews ()
		{
			if (int.TryParse (textField.Text, out var views) && views >= 0) {

				return views;
			} else {
				return 0;
			}
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}