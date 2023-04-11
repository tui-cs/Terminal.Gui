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
				lbComboBox.Text = e.Value.ToString ();
				if (e.Item != -1) {
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

			var lbComboBox2 = new Label () {
				ColorScheme = Colors.TopLevel,
				X = Pos.Right (btnThree) + 1,
				Width = Dim.Percent (40)
			};

			var comboBox2 = new ComboBox (new List<string> { "First", "Second", "Third" }) {
				X = Pos.Right (btnThree) + 1,
				Y = Pos.Top (btnThree),
				Height = Dim.Fill (2),
				Width = Dim.Percent (40),
				HideDropdownListOnClick = true
			};
			comboBox2.OpenSelectedItem += (object sender, ListViewItemEventArgs e) => {
				lbComboBox2.Text = e.Value.ToString ();
			};
			Win.Add (lbComboBox2, comboBox2);

			var ckbReadOnlyComboBox = new CheckBox ("ComboBox read only") {
				X = Pos.Center (),
				Y = Pos.Bottom (btnThree) + 7,
			};
			ckbReadOnlyComboBox.Toggled += (s, e) => comboBox.ReadOnly = comboBox2.ReadOnly = (bool)e.NewValue;
			Win.Add (ckbReadOnlyComboBox);

			var btnComboBoxInDialog = new Button ("ComboBox Alone Inside Dialog") {
				X = Pos.Center (),
				Y = Pos.Bottom (btnThree) + 9,
			};
			var lblCurrentItem = new Label ("Dialog: ") {
				X = Pos.Center (),
				Y = Pos.Bottom (btnThree) + 11,
			};
			btnComboBoxInDialog.Clicked += (s, e) => {
				var dialog = new Dialog () { Width = 20, Height = 8, Title = "Dialog" };
				var cb = new ComboBox (new List<string> { "One", "Two", "Three" }) {
					HideDropdownListOnClick = false
				};
				cb.OpenSelectedItem += (s, e) => Application.RequestStop ();
				dialog.Add (cb);

				Application.Run (dialog);

				lblCurrentItem.Text = $"Dialog: {cb.Text}";
			};
			Win.Add (btnComboBoxInDialog, lblCurrentItem);

			var btnCustomDialog = new Button ("Custom Dialog") {
				X = Pos.Center (),
				Y = Pos.Bottom (btnThree) + 13,
			};
			btnCustomDialog.Clicked += (s, e) => {
				Application.Run<CustomDialog> ();
			};
			Win.Add (btnCustomDialog);

			var win = new Window () {
				Y = Pos.Bottom (ckbReadOnlyComboBox) + 4,
				Width = 40,
				Height = 3,
				Title = "ComboBox Inside Window"
			};
			var comboBox3 = new ComboBox (
				new List<string> { "X", "Y", "Width", "Height", "Left", "Top", "Right", "Bottom", "Point", "Size", "Pos", "Dim" }) {
				Width = Dim.Fill (),
				Height = 3,
				HideDropdownListOnClick = true,
				ReadOnly = true
			};
			win.Add (comboBox3);
			Win.Add (win);
		}

		private class CustomDialog : Dialog {
			public CustomDialog ()
			{
				Title = "Custom Dialog";
				Width = 30;
				Height = 9;

				Button btnComboBox = null;
				btnComboBox = new Button ("Set Combo") {
					X = Pos.AnchorEnd () - Pos.Function (BtnComboBox_Width)
				};
				int BtnComboBox_Width ()
				{
					return btnComboBox == null ? 0 : btnComboBox.Frame.Width;
				}
				var tfComboBox = new TextField () {
					Width = Dim.Fill (1) - Dim.Function (BtnComboBox_Width)
				};
				var comboBox = new ComboBox (new List<string> () { "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten" }) {
					Y = Pos.Bottom (tfComboBox) + 1,
					Width = Dim.Fill ()
				};
				comboBox.OpenSelectedItem += (object sender, ListViewItemEventArgs e) => tfComboBox.Text = e.Value.ToString ();
				Add (btnComboBox, tfComboBox, comboBox);

				Button btnComboBox2 = null;
				btnComboBox2 = new Button ("Set Combo2") {
					X = Pos.AnchorEnd () - Pos.Function (BtnComboBox2_Width),
					Y = Pos.Bottom (comboBox) + 1,
				};
				int BtnComboBox2_Width ()
				{
					return btnComboBox2 == null ? 0 : btnComboBox2.Frame.Width;
				}
				var tfComboBox2 = new TextField () {
					Y = Pos.Bottom (comboBox) + 1,
					Width = Dim.Fill (1) - Dim.Function (BtnComboBox2_Width)
				};
				var comboBox2 = new ComboBox (new List<string> { "First", "Second", "Third", "Fourth", "Fifth", "Sixth", "Seventh", "Eighth", "Ninth", "Tenth" }) {
					Y = Pos.Bottom (tfComboBox2) + 1,
					Width = Dim.Fill (),
					ReadOnly = true
				};
				comboBox2.OpenSelectedItem += (object sender, ListViewItemEventArgs e) => {
					tfComboBox2.Text = e.Value.ToString ();
				};
				Add (btnComboBox2, tfComboBox2, comboBox2);

			}
		}
	}
}