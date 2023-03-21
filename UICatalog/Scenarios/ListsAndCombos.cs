using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Terminal.Gui;
using NStack;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "ListView & ComboBox", Description: "Demonstrates a ListView populating a ComboBox that acts as a filter.")]
	[ScenarioCategory ("Controls"), ScenarioCategory ("ListView"), ScenarioCategory ("ComboBox")]
	public class ListsAndCombos : Scenario {

		public override void Setup ()
		{
			//TODO: Duplicated code in Demo.cs Consider moving to shared assembly
			var items = new List<ustring> ();
			foreach (var dir in new [] { "/etc", @$"{Environment.GetEnvironmentVariable ("SystemRoot")}\System32" }) {
				if (Directory.Exists (dir)) {
					items = Directory.GetFiles (dir).Union(Directory.GetDirectories(dir))
						.Select (Path.GetFileName)
						.Where (x => char.IsLetterOrDigit (x [0]))
						.OrderBy (x => x).Select(x => ustring.Make(x)).ToList() ;
				}
			}

			// ListView
			var lbListView = new Label ("Listview") {
				ColorScheme = Colors.TopLevel,
				X = 0,
				Width = Dim.Percent (40)
			};

			var listview = new ListView (items) {
				X = 0,
				Y = Pos.Bottom (lbListView) + 1,
				Height = Dim.Fill(2),
				Width = Dim.Percent (40)
			};
			listview.SelectedItemChanged += (object s, ListViewItemEventArgs e) => lbListView.Text = items [listview.SelectedItem];
			Win.Add (lbListView, listview);

			var _scrollBar = new ScrollBarView (listview, true);

			_scrollBar.ChangedPosition += (s,e) => {
				listview.TopItem = _scrollBar.Position;
				if (listview.TopItem != _scrollBar.Position) {
					_scrollBar.Position = listview.TopItem;
				}
				listview.SetNeedsDisplay ();
			};

			_scrollBar.OtherScrollBarView.ChangedPosition += (s,e) => {
				listview.LeftItem = _scrollBar.OtherScrollBarView.Position;
				if (listview.LeftItem != _scrollBar.OtherScrollBarView.Position) {
					_scrollBar.OtherScrollBarView.Position = listview.LeftItem;
				}
				listview.SetNeedsDisplay ();
			};

			listview.DrawContent += (s,e) => {
				_scrollBar.Size = listview.Source.Count - 1;
				_scrollBar.Position = listview.TopItem;
				_scrollBar.OtherScrollBarView.Size = listview.Maxlength - 1;
				_scrollBar.OtherScrollBarView.Position = listview.LeftItem;
				_scrollBar.Refresh ();
			};

			// ComboBox
			var lbComboBox = new Label ("ComboBox") {
				ColorScheme = Colors.TopLevel,
				X = Pos.Right (lbListView) + 1,
				Width = Dim.Percent(40)
			};

			var comboBox = new ComboBox () {
				X = Pos.Right (listview) + 1,
				Y = Pos.Bottom (lbListView) + 1,
				Height = Dim.Fill (2),
				Width = Dim.Percent(40)
			};
			comboBox.SetSource (items);

			comboBox.SelectedItemChanged += (object s, ListViewItemEventArgs text) => lbComboBox.Text = text.Value.ToString ();
			Win.Add (lbComboBox, comboBox);

			var scrollBarCbx = new ScrollBarView (comboBox.Subviews [1], true);

			scrollBarCbx.ChangedPosition += (s,e) => {
				((ListView)comboBox.Subviews [1]).TopItem = scrollBarCbx.Position;
				if (((ListView)comboBox.Subviews [1]).TopItem != scrollBarCbx.Position) {
					scrollBarCbx.Position = ((ListView)comboBox.Subviews [1]).TopItem;
				}
				comboBox.SetNeedsDisplay ();
			};

			scrollBarCbx.OtherScrollBarView.ChangedPosition += (s,e) => {
				((ListView)comboBox.Subviews [1]).LeftItem = scrollBarCbx.OtherScrollBarView.Position;
				if (((ListView)comboBox.Subviews [1]).LeftItem != scrollBarCbx.OtherScrollBarView.Position) {
					scrollBarCbx.OtherScrollBarView.Position = ((ListView)comboBox.Subviews [1]).LeftItem;
				}
				comboBox.SetNeedsDisplay ();
			};

			comboBox.DrawContent += (s,e) => {
				scrollBarCbx.Size = comboBox.Source.Count;
				scrollBarCbx.Position = ((ListView)comboBox.Subviews [1]).TopItem;
				scrollBarCbx.OtherScrollBarView.Size = ((ListView)comboBox.Subviews [1]).Maxlength - 1;
				scrollBarCbx.OtherScrollBarView.Position = ((ListView)comboBox.Subviews [1]).LeftItem;
				scrollBarCbx.Refresh ();
			};


			var btnMoveUp = new Button ("Move _Up") {
				X = 1,
				Y = Pos.Bottom(lbListView),
			};
			btnMoveUp.Clicked += (s,e) => {
				listview.MoveUp ();
			};

			var btnMoveDown = new Button ("Move _Down") {
				X = Pos.Right (btnMoveUp) + 1,
				Y = Pos.Bottom (lbListView),
			};
			btnMoveDown.Clicked += (s,e) => {
				listview.MoveDown ();
			};

			Win.Add (btnMoveUp, btnMoveDown);
		}
	}
}
