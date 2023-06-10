using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "WizardAsView", Description: "Shows using the Wizard class in an non-modal way")]
	[ScenarioCategory ("Wizards")]
	public class WizardAsView : Scenario {

		public override void Init ()
		{
			Application.Init ();

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Restart Configuration...", "", () => MessageBox.Query ("Wizaard", "Are you sure you want to reset the Wizard and start over?", "Ok", "Cancel")),
					new MenuItem ("Re_boot Server...", "", () => MessageBox.Query ("Wizaard", "Are you sure you want to reboot the server start over?", "Ok", "Cancel")),
					new MenuItem ("_Shutdown Server...", "", () => MessageBox.Query ("Wizaard", "Are you sure you want to cancel setup and shutdown?", "Ok", "Cancel")),
				})
			});
			Application.Top.Add (menu);

			// No need for a Title because the border is disabled
			var wizard = new Wizard () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};

			// Set Mdoal to false to cause the Wizard class to render without a frame and
			// behave like an non-modal View (vs. a modal/pop-up Window).
			wizard.Modal = false;

			wizard.MovingBack += (s, args) => {
				//args.Cancel = true;
				//actionLabel.Text = "Moving Back";
			};

			wizard.MovingNext += (s, args) => {
				//args.Cancel = true;
				//actionLabel.Text = "Moving Next";
			};

			wizard.Finished += (s, args) => {
				//args.Cancel = true;
				MessageBox.Query ("Setup Wizard", "Finished", "Ok");
				Application.RequestStop ();
			};

			wizard.Cancelled += (s, args) => {
				var btn = MessageBox.Query ("Setup Wizard", "Are you sure you want to cancel?", "Yes", "No");
				args.Cancel = btn == 1;
				if (btn == 0) {
					Application.RequestStop ();
				}
			};

			// Add 1st step
			var firstStep = new WizardStep () { Title = "End User License Agreement" };
			wizard.AddStep (firstStep);
			firstStep.NextButtonText = "Accept!";
			firstStep.HelpText = "This is the End User License Agreement.\n\n\n\n\n\nThis is a test of the emergency broadcast system. This is a test of the emergency broadcast system.\nThis is a test of the emergency broadcast system.\n\n\nThis is a test of the emergency broadcast system.\n\nThis is a test of the emergency broadcast system.\n\n\n\nThe end of the EULA.";

			// Add 2nd step
			var secondStep = new WizardStep () { Title = "Second Step" };
			wizard.AddStep (secondStep);
			secondStep.HelpText = "This is the help text for the Second Step.\n\nPress the button to change the Title.\n\nIf First Name is empty the step will prevent moving to the next step.";

			var buttonLbl = new Label () { Text = "Second Step Button: ", X = 0, Y = 0 };
			var button = new Button () {
				Text = "Press Me to Rename Step",
				X = Pos.Right (buttonLbl),
				Y = Pos.Top (buttonLbl)
			};
			button.Clicked += (s, e) => {
				secondStep.Title = "2nd Step";
				MessageBox.Query ("Wizard Scenario", "This Wizard Step's title was changed to '2nd Step'", "Ok");
			};
			secondStep.Add (buttonLbl, button);
			var lbl = new Label () { Text = "First Name: ", X = Pos.Left (buttonLbl), Y = Pos.Bottom (buttonLbl) };
			var firstNameField = new TextField () { Text = "Number", Width = 30, X = Pos.Right (lbl), Y = Pos.Top (lbl) };
			secondStep.Add (lbl, firstNameField);
			lbl = new Label () { Text = "Last Name:  ", X = Pos.Left (buttonLbl), Y = Pos.Bottom (lbl) };
			var lastNameField = new TextField () { Text = "Six", Width = 30, X = Pos.Right (lbl), Y = Pos.Top (lbl) };
			secondStep.Add (lbl, lastNameField);

			// Add last step
			var lastStep = new WizardStep () { Title = "The last step" };
			wizard.AddStep (lastStep);
			lastStep.HelpText = "The wizard is complete!\n\nPress the Finish button to continue.\n\nPressing Esc will cancel.";

			Application.Top.Add (wizard);
			Application.Run (Application.Top);
		}

		public override void Run ()
		{
			// Do nothing in the override because we call Application.Run above
			// (just to make it clear how the Top is being run and not the Wizard).
		}
	}
}
