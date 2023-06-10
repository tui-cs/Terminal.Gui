using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Text;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ReactiveExample {
	//
	// This view model can be easily shared across different UI frameworks.
	// For example, if you have a WPF or XF app with view models written
	// this way, you can easily port your app to Terminal.Gui by implementing
	// the views with Terminal.Gui classes and ReactiveUI bindings.
	//
	// We mark the view model with the [DataContract] attributes and this
	// allows you to save the view model class to the disk, and then to read
	// the view model from the disk, making your app state persistent.
	// See also: https://www.reactiveui.net/docs/handbook/data-persistence/
	//
	[DataContract]
	public class LoginViewModel : ReactiveObject {
		readonly ObservableAsPropertyHelper<int> _usernameLength;
		readonly ObservableAsPropertyHelper<int> _passwordLength;
		readonly ObservableAsPropertyHelper<bool> _isValid;
		
		public LoginViewModel () {
			var canLogin = this.WhenAnyValue (
				x => x.Username, 
				x => x.Password,
				(username, password) =>
					!string.IsNullOrEmpty (username) &&
					!string.IsNullOrEmpty (password));
			
			_isValid = canLogin.ToProperty (this, x => x.IsValid);
			Login = ReactiveCommand.CreateFromTask (
				() => Task.Delay (TimeSpan.FromSeconds (1)),
				canLogin);
			
			_usernameLength = this
				.WhenAnyValue (x => x.Username)
				.Select (name => name.Length)
				.ToProperty (this, x => x.UsernameLength);
			_passwordLength = this
				.WhenAnyValue (x => x.Password)
				.Select (password => password.Length)
				.ToProperty (this, x => x.PasswordLength);
			
			Clear = ReactiveCommand.Create (() => { });
			Clear.Subscribe (unit => {
				Username = string.Empty;
				Password = string.Empty;
			});
		}
		
		[Reactive, DataMember]
		public string Username { get; set; } = string.Empty;
		
		[Reactive, DataMember]
		public string Password { get; set; } = string.Empty;
		
		[IgnoreDataMember]
		public int UsernameLength => _usernameLength.Value;
		
		[IgnoreDataMember]
		public int PasswordLength => _passwordLength.Value;

		[IgnoreDataMember]
		public ReactiveCommand<Unit, Unit> Login { get; }
		
		[IgnoreDataMember]
		public ReactiveCommand<Unit, Unit> Clear { get; }
		
		[IgnoreDataMember]
		public bool IsValid => _isValid.Value;
	}
}