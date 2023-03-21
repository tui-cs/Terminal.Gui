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
		private Label foregroundColorLabel;

		/// <summary>
		/// Background color Label.
		/// </summary>
		private Label backgroundColorLabel;

		/// <summary>
		/// Demo label.
		/// </summary>
		private Label demoLabel;

		/// <summary>
		/// Setup the scenario.
		/// </summary>
		public override void Setup ()
		{
			// Scenario Window's.
			Win.Title = this.GetName ();

			// Forground ColorPicker.
			foregroundColorPicker = new ColorPicker ("Foreground Color");
			foregroundColorPicker.X = 0;
			foregroundColorPicker.Y = 0;
			foregroundColorPicker.ColorChanged += ForegroundColor_ColorChanged;
			Win.Add (foregroundColorPicker);

			foregroundColorLabel = new Label ();
			foregroundColorLabel.X = Pos.Left(foregroundColorPicker);
			foregroundColorLabel.Y = Pos.Bottom (foregroundColorPicker) + 1;
			Win.Add (foregroundColorLabel);

			// Background ColorPicker.
			backgroundColorPicker = new ColorPicker ();
			backgroundColorPicker.Text = "Background Color";
			backgroundColorPicker.X = Pos.AnchorEnd () - (Pos.Right (backgroundColorPicker) - Pos.Left (backgroundColorPicker));
			backgroundColorPicker.Y = 0;
			backgroundColorPicker.ColorChanged += BackgroundColor_ColorChanged;
			Win.Add (backgroundColorPicker);

			backgroundColorLabel = new Label ();
			backgroundColorLabel.X = Pos.AnchorEnd () - (Pos.Right (backgroundColorLabel) - Pos.Left (backgroundColorLabel));
			backgroundColorLabel.Y = Pos.Bottom (backgroundColorPicker) + 1;
			Win.Add (backgroundColorLabel);

			// Demo Label.
			demoLabel = new Label ("Lorem Ipsum");
			demoLabel.X = Pos.Center ();
			demoLabel.Y = 1;
			Win.Add (demoLabel);

			// Set default colors.
			backgroundColorPicker.SelectedColor = demoLabel.SuperView.ColorScheme.Normal.Background;
			foregroundColorPicker.SelectedColor = demoLabel.SuperView.ColorScheme.Normal.Foreground;
		}

		/// <summary>
		/// Fired when foreground color is changed.
		/// </summary>
		private void ForegroundColor_ColorChanged (object sender, EventArgs e)
		{
			UpdateColorLabel (foregroundColorLabel, foregroundColorPicker);
			UpdateDemoLabel ();
		}

		/// <summary>
		/// Fired when background color is changed.
		/// </summary>
		private void BackgroundColor_ColorChanged (object sender, EventArgs e)
		{
			UpdateColorLabel (backgroundColorLabel, backgroundColorPicker);
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
		private void UpdateDemoLabel () => demoLabel.ColorScheme = new ColorScheme () {
			Normal = new Terminal.Gui.Attribute (foregroundColorPicker.SelectedColor, backgroundColorPicker.SelectedColor)
		};
	}
}