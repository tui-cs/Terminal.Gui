using System.Collections.Generic;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "ComboBoxIteration", Description: "ComboBox iteration.")]
	[ScenarioCategory ("Controls"), ScenarioCategory ("ComboBox")]
	public class ComboBoxIteration : Scenario {
		public override void Setup ()
		{
			var items = new List<string> () { "one", "two", "three" };

			var lbListView = new Label () {
				AutoSize = false,
				Width = 10,
				Height = 1
			};
			Win.Add (lbListView);

			var listview = new ListView (items) {
				Y = Pos.Bottom (lbListView) + 1,
				Width = 10,
				Height = Dim.Fill (2)
			};
			Win.Add (listview);

			var lbComboBox = new Label () {
				ColorScheme = Colors.TopLevel,
				X = Pos.Right (lbListView) + 1,
				Width = Dim.Percent (40)
			};

			var comboBox = new ComboBox () {
				X = Pos.Right (listview) + 1,
				Y = Pos.Bottom (lbListView) + 1,
				Height = Dim.Fill (2),
				Width = Dim.Percent (40),
				HideDropdownListOnClick = true
			};
			comboBox.SetSource (items);

			listview.SelectedItemChanged += (s, e) => {
				lbListView.Text = items [e.Item];
				comboBox.SelectedItem = e.Item;
			};

			comboBox.OpenSelectedItem += (object sender, ListViewItemEventArgs e) => {
				if (e.Item != -1) {
					lbComboBox.Text = e.Value.ToString ();
					listview.SelectedItem = e.Item;
				}
			};
			Win.Add (lbComboBox, comboBox);
			Win.Add (new TextField { X = Pos.Right (listview) + 1, Y = Pos.Top (comboBox) + 3, Height = 1, Width = 20 });

			var btnTwo = new Button ("Two") {
				X = Pos.Right (comboBox) + 1,
			};
			btnTwo.Clicked += (s, e) => {
				items = new List<string> () { "one", "two" };
				comboBox.SetSource (items);
				listview.SetSource (items);
				listview.SelectedItem = 0;
			};
			Win.Add (btnTwo);

			var btnThree = new Button ("Three") {
				X = Pos.Right (comboBox) + 1,
				Y = Pos.Top (comboBox)
			};
			btnThree.Clicked += (s, e) => {
				items = new List<string> () { "one", "two", "three" };
				comboBox.SetSource (items);
				listview.SetSource (items);
				listview.SelectedItem = 0;
			};
			Win.Add (btnThree);

			var comboBox2 = new ComboBox (new List<string> { "First", "Second", "Third" }) {
				X = Pos.Right (btnThree) + 1,
				Y = Pos.Top (btnThree),
				Height = Dim.Fill (2),
				Width = Dim.Percent (40),
				HideDropdownListOnClick = true
			};
			Win.Add (comboBox2);

			var ckbReadOnlyComboBox = new CheckBox ("ComboBox read only") {
				X = Pos.Center (),
				Y = Pos.Bottom (btnThree) + 7,
			};
			ckbReadOnlyComboBox.Toggled += (s, e) => comboBox.ReadOnly = comboBox2.ReadOnly = (bool)e.NewValue;
			Win.Add (ckbReadOnlyComboBox);

			var btnComboBoxInDialog = new Button ("ComboBox In Dialog") {
				X = Pos.Center (),
				Y = Pos.Bottom (btnThree) + 9,
			};
			var lblCurrentItem = new Label () {
				X = Pos.Center (),
				Y = Pos.Bottom (btnThree) + 11,
			};
			btnComboBoxInDialog.Clicked += (s, e) => {
				var dialog = new Dialog ("Dialog", 20, 8);
				var cb = new ComboBox (new List<string> { "One", "Two", "Three" }) {
					HideDropdownListOnClick = false
				};
				cb.OpenSelectedItem += (s, e) => {
					Application.RequestStop ();
				};
				dialog.Add (cb);

				Application.Run (dialog);

				lblCurrentItem.Text = $"Dialog: {cb.Text}";
			};
			Win.Add (btnComboBoxInDialog, lblCurrentItem);

			var win = new Window ("ComboBox Inside Window") {
				Y = Pos.Bottom (ckbReadOnlyComboBox) + 4,
				Width = 40,
				Height = 3
			};
			var comboBox3 = new ComboBox (new List<string> { "First", "Second", "Third" }) {
				Width = Dim.Fill (),
				Height = 3,
				HideDropdownListOnClick = true
			};
			win.Add (comboBox3);
			Win.Add (win);
		}
	}
}