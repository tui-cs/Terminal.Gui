using Terminal.Gui;
using System;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Color Picker", Description: "Color Picker.")]
	[ScenarioCategory ("Colors")]
	[ScenarioCategory ("Controls")]
	public class ColorPickers : Scenario {
		/// <summary>
		/// Foreground ColorPicker.
		/// </summary>
		private ColorPicker foregroundColorPicker;

		/// <summary>
		/// Background ColorPicker.
		/// </summary>
		private ColorPicker backgroundColorPicker;

		/// <summary>
		/// Foreground color label.
		/// </summary>
		private Label _foregroundColorLabel;

		/// <summary>
		/// Background color Label.
		/// </summary>
		private Label _backgroundColorLabel;

		/// <summary>
		/// Demo label.
		/// </summary>
		private View _demoView;

		/// <summary>
		/// Setup the scenario.
		/// </summary>
		public override void Setup ()
		{
			// Scenario Window's.
			Win.Title = this.GetName ();

			// Foreground ColorPicker.
			foregroundColorPicker = new ColorPicker () {
				Title = "Foreground Color",
				X = 0,
				Y = 0,
				BoxHeight = 3,
				BoxWidth = 6,
				BorderStyle = LineStyle.Single
			};
			foregroundColorPicker.ColorChanged += ForegroundColor_ColorChanged;
			Win.Add (foregroundColorPicker);

			_foregroundColorLabel = new Label {
				X = Pos.Left (foregroundColorPicker),
				Y = Pos.Bottom (foregroundColorPicker) + 1
			};
			Win.Add (_foregroundColorLabel);

			// Background ColorPicker.
			backgroundColorPicker = new ColorPicker () { 
				Title = "Background Color",
				Y = 0,
				BoxHeight = 1,
				BoxWidth = 4,
				BorderStyle = LineStyle.Single
			};
			backgroundColorPicker.X = Pos.AnchorEnd () - (Pos.Right (backgroundColorPicker) - Pos.Left (backgroundColorPicker));
			backgroundColorPicker.ColorChanged += BackgroundColor_ColorChanged;
			Win.Add (backgroundColorPicker);

			_backgroundColorLabel = new Label ();
			_backgroundColorLabel.X = Pos.AnchorEnd () - (Pos.Right (_backgroundColorLabel) - Pos.Left (_backgroundColorLabel));
			_backgroundColorLabel.Y = Pos.Bottom (backgroundColorPicker) + 1;
			Win.Add (_backgroundColorLabel);

			// Demo Label.
			_demoView = new View () {
				Title = "Color Sample",
				Text = "Lorem Ipsum",
				TextAlignment = TextAlignment.Centered,
				VerticalTextAlignment = VerticalTextAlignment.Middle,
				BorderStyle = LineStyle.Heavy,
				X = Pos.Center (),
				Y = Pos.Center (),
				Height = 5,
				Width = 20
			};
			Win.Add (_demoView);

			// Set default colors.
			backgroundColorPicker.SelectedColor = _demoView.SuperView.ColorScheme.Normal.Background;
			foregroundColorPicker.SelectedColor = _demoView.SuperView.ColorScheme.Normal.Foreground;
		}

		/// <summary>
		/// Fired when foreground color is changed.
		/// </summary>
		private void ForegroundColor_ColorChanged (object sender, EventArgs e)
		{
			UpdateColorLabel (_foregroundColorLabel, foregroundColorPicker);
			UpdateDemoLabel ();
		}

		/// <summary>
		/// Fired when background color is changed.
		/// </summary>
		private void BackgroundColor_ColorChanged (object sender, EventArgs e)
		{
			UpdateColorLabel (_backgroundColorLabel, backgroundColorPicker);
			UpdateDemoLabel ();
		}

		/// <summary>
		/// Update a color label from his ColorPicker.
		/// </summary>
		private void UpdateColorLabel (Label label, ColorPicker colorPicker)
		{
			label.Clear ();
			label.Text = $"{colorPicker.SelectedColor} - {(int)colorPicker.SelectedColor}";
		}

		/// <summary>
		/// Update Demo Label.
		/// </summary>
		private void UpdateDemoLabel () => _demoView.ColorScheme = new ColorScheme () {
			Normal = new Terminal.Gui.Attribute (foregroundColorPicker.SelectedColor, backgroundColorPicker.SelectedColor)
		};
	}
}