using System.Reactive.Disposables;
using System.Reactive.Linq;
using NStack;
using ReactiveUI;
using Terminal.Gui;
using ReactiveMarbles.ObservableEvents;

namespace ReactiveExample {
	public class LoginView : Window, IViewFor<LoginViewModel> {
		readonly CompositeDisposable _disposable = new CompositeDisposable();
		
		public LoginView (LoginViewModel viewModel) : base() {
			Title = "Reactive Extensions Example";
			ViewModel = viewModel;
			var usernameLengthLabel = UsernameLengthLabel (TitleLabel ());
			var usernameInput = UsernameInput (usernameLengthLabel);
			var passwordLengthLabel = PasswordLengthLabel (usernameInput);
			var passwordInput = PasswordInput (passwordLengthLabel);
			var validationLabel = ValidationLabel (passwordInput);
			var loginButton = LoginButton (validationLabel);
			var clearButton = ClearButton (loginButton);
			LoginProgressLabel (clearButton);
		}
		
		public LoginViewModel ViewModel { get; set; }

		protected override void Dispose (bool disposing) {
			_disposable.Dispose ();
			base.Dispose (disposing);
		}

		Label TitleLabel () {
			var label = new Label("Login Form");
			Add (label);
			return label;
		}

		TextField UsernameInput (View previous) {
			var usernameInput = new TextField (ViewModel.Username) {
				X = Pos.Left(previous),
				Y = Pos.Top(previous) + 1,
				Width = 40
			};
			ViewModel
				.WhenAnyValue (x => x.Username)
				.BindTo (usernameInput, x => x.Text)
				.DisposeWith (_disposable);
			usernameInput
				.Events ()
				.TextChanged
				.Select (old => usernameInput.Text)
				.DistinctUntilChanged ()
				.BindTo (ViewModel, x => x.Username)
				.DisposeWith (_disposable);
			Add (usernameInput);
			return usernameInput;
		}

		Label UsernameLengthLabel (View previous) {
			var usernameLengthLabel = new Label {
				X = Pos.Left(previous),
				Y = Pos.Top(previous) + 1,
				Width = 40
			};
			ViewModel
				.WhenAnyValue (x => x.UsernameLength)
				.Select (length => ustring.Make ($"Username ({length} characters)"))
				.BindTo (usernameLengthLabel, x => x.Text)
				.DisposeWith (_disposable);
			Add (usernameLengthLabel);
			return usernameLengthLabel;
		}

		TextField PasswordInput (View previous) {
			var passwordInput = new TextField (ViewModel.Password) {
				X = Pos.Left(previous),
				Y = Pos.Top(previous) + 1,
				Width = 40
			};
			ViewModel
				.WhenAnyValue (x => x.Password)
				.BindTo (passwordInput, x => x.Text)
				.DisposeWith (_disposable);
			passwordInput
				.Events ()
				.TextChanged
				.Select (old => passwordInput.Text)
				.DistinctUntilChanged ()
				.BindTo (ViewModel, x => x.Password)
				.DisposeWith (_disposable);
			Add (passwordInput);
			return passwordInput;
		}

		Label PasswordLengthLabel (View previous) {
			var passwordLengthLabel = new Label {
				X = Pos.Left(previous),
				Y = Pos.Top(previous) + 1,
				Width = 40
			};
			ViewModel
				.WhenAnyValue (x => x.PasswordLength)
				.Select (length => ustring.Make ($"Password ({length} characters)"))
				.BindTo (passwordLengthLabel, x => x.Text)
				.DisposeWith (_disposable);
			Add (passwordLengthLabel);
			return passwordLengthLabel;
		}

		Label ValidationLabel (View previous) {
			var error = ustring.Make("Please, enter user name and password.");
			var success = ustring.Make("The input is valid!");
			var validationLabel = new Label(error) {
				X = Pos.Left(previous),
				Y = Pos.Top(previous) + 1,
				Width = 40
			};
			ViewModel
				.WhenAnyValue (x => x.IsValid)	
				.Select (valid => valid ? success : error)
				.BindTo (validationLabel, x => x.Text)
				.DisposeWith (_disposable);
			ViewModel
				.WhenAnyValue (x => x.IsValid)	
				.Select (valid => valid ? Colors.Base : Colors.Error)
				.BindTo (validationLabel, x => x.ColorScheme)
				.DisposeWith (_disposable);
			Add (validationLabel);
			return validationLabel;
		}

		Label LoginProgressLabel (View previous) {
			var progress = ustring.Make ("Logging in...");
			var idle = ustring.Make ("Press 'Login' to log in.");
			var loginProgressLabel = new Label(idle) {
				X = Pos.Left(previous),
				Y = Pos.Top(previous) + 1,
				Width = 40
			};
			ViewModel
				.WhenAnyObservable (x => x.Login.IsExecuting)
				.Select (executing => executing ? progress : idle)
				.ObserveOn (RxApp.MainThreadScheduler)
				.BindTo (loginProgressLabel, x => x.Text)
				.DisposeWith (_disposable);
			Add (loginProgressLabel);
			return loginProgressLabel;
		}

		Button LoginButton (View previous) {
			var loginButton = new Button ("Login") {
				X = Pos.Left(previous),
				Y = Pos.Top(previous) + 1,
				Width = 40
			};
			loginButton
				.Events ()
				.Clicked
				.InvokeCommand (ViewModel, x => x.Login)
				.DisposeWith (_disposable);
			Add (loginButton);
			return loginButton;
		}

		Button ClearButton (View previous) {
			var clearButton = new Button("Clear") {
				X = Pos.Left(previous),
				Y = Pos.Top(previous) + 1,
				Width = 40
			};
			clearButton
				.Events ()
				.Clicked
				.InvokeCommand (ViewModel, x => x.Clear)
				.DisposeWith (_disposable);
			Add (clearButton);
			return clearButton;
		}
		
		object IViewFor.ViewModel {
			get => ViewModel;
			set => ViewModel = (LoginViewModel) value;
		}
	}
}