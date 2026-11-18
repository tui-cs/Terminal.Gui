
namespace UICatalog.Scenarios;

[ScenarioMetadata ("Run<T> Example", "Illustrates using Application.Run<T> to run a custom class")]
[ScenarioCategory ("Runnable")]
public class RunTExample : Scenario
{
    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();

        app.Run<ExampleWindow> ();
    }

    public class ExampleWindow : Window
    {
        public ExampleWindow ()
        {
            Title = $"Example App ({Application.GetDefaultKey (Command.Quit)} to quit)";

            // Create input components and labels
            var usernameLabel = new Label { Text = "Username:" };

            TextField usernameText = new()
            {
                // Position text field adjacent to the label
                X = Pos.Right (usernameLabel) + 1, Width

                    // Fill remaining horizontal space
                    = Dim.Fill ()
            };

            var passwordLabel = new Label
            {
                Text = "Password:", X = Pos.Left (usernameLabel), Y = Pos.Bottom (usernameLabel) + 1
            };

            var passwordText = new TextField
            {
                Secret = true,

                // align with the text box above
                X = Pos.Left (usernameText),
                Y = Pos.Top (passwordLabel),
                Width = Dim.Fill ()
            };

            // Create login button
            var btnLogin = new Button
            {
                Text = "Login",
                Y = Pos.Bottom (passwordLabel) + 1,

                // center the login button horizontally
                X = Pos.Center (),
                IsDefault = true
            };

            // When login button is clicked display a message popup
            btnLogin.Accepting += (_, _) =>
                               {
                                   if (usernameText.Text == "admin" && passwordText.Text == "password")
                                   {
                                       MessageBox.Query (App!, "Login Successful", $"Username: {usernameText.Text}", "Ok");
                                       App?.RequestStop ();
                                   }
                                   else
                                   {
                                       MessageBox.ErrorQuery (App!,
                                                              "Error Logging In",
                                                              "Incorrect username or password (hint: admin/password)",
                                                              "Ok"
                                                             );
                                   }
                               };

            // Add the views to the Window
            Add (usernameLabel, usernameText, passwordLabel, passwordText, btnLogin);
        }
    }
}
