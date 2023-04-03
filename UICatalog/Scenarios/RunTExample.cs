using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Run<T> Example", Description: "Illustrates using Application.Run<T> to run a custom class")]
	[ScenarioCategory ("Top Level Windows")]
	public class RunTExample : Scenario {
		public override void Setup ()
		{
			// No need to call Init if Application.Run<T> is used
		}

		public override void Run ()
		{
			Application.Run<ExampleWindow> ();
		}

		public class ExampleWindow : Window {
			public TextField usernameText;

			public ExampleWindow ()
			{
				Title = $"Example App ({Application.QuitKey} to quit)";

				// Create input components and labels
				var usernameLabel = new Label () {
					Text = "Username:"
				};

				usernameText = new TextField ("") {
					// Position text field adjacent to the label
					X = Pos.Right (usernameLabel) + 1,

					// Fill remaining horizontal space
					Width = Dim.Fill (),
				};

				var passwordLabel = new Label () {
					Text = "Password:",
					X = Pos.Left (usernameLabel),
					Y = Pos.Bottom (usernameLabel) + 1
				};

				var passwordText = new TextField ("") {
					Secret = true,
					// align with the text box above
					X = Pos.Left (usernameText),
					Y = Pos.Top (passwordLabel),
					Width = Dim.Fill (),
				};

				// Create login button
				var btnLogin = new Button () {
					Text = "Login",
					Y = Pos.Bottom (passwordLabel) + 1,
					// center the login button horizontally
					X = Pos.Center (),
					IsDefault = true,
				};

				// When login button is clicked display a message popup
				btnLogin.Clicked += (s,e) => {
					if (usernameText.Text == "admin" && passwordText.Text == "password") {
						MessageBox.Query ("Login Successful", $"Username: {usernameText.Text}", "Ok");
						Application.RequestStop ();
					} else {
						MessageBox.ErrorQuery ("Error Logging In", "Incorrect username or password (hint: admin/password)", "Ok");
					}
				};

				// Add the views to the Window
				Add (usernameLabel, usernameText, passwordLabel, passwordText, btnLogin);
			}
		}

	}
}