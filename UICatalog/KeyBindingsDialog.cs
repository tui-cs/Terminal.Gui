using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace UICatalog {

	class KeyBindingsDialog : Dialog {

		static Dictionary<Command,Key> CurrentBindings = new Dictionary<Command,Key>();
		private Command[] commands;
		private ListView commandsListView;
		private Label keyLabel;

		/// <summary>
		/// Tracks views as they are created in UICatalog so that their keybindings can
		/// be managed.
		/// </summary>
		private class ViewTracker {

			public static ViewTracker Instance;

			/// <summary>
			/// All views seen so far and a bool to indicate if we have applied keybindings to them
			/// </summary>
			Dictionary<View, bool> knownViews = new Dictionary<View, bool> ();

			private object lockKnownViews = new object ();
			private Dictionary<Command, Key> keybindings;

			public ViewTracker (View top)
			{
				RecordView (top);

				// Refresh known windows
				Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (100), (m) => {

					lock (lockKnownViews) {
						RecordView (Application.Top);

						ApplyKeyBindingsToAllKnownViews ();
					}

					return true;
				});
			}

			private void RecordView (View view)
			{
				if (!knownViews.ContainsKey (view)) {
					knownViews.Add (view, false);
				}

				// may already have subviews that were added to it
				// before we got to it
				foreach (var sub in view.Subviews) {
					RecordView (sub);
				}
				// TODO: BUG: Based on my new understanding of Added event I think this is wrong
				// (and always was wrong). Parents don't get to be told when new views are added
				// to them

				view.Added += (s,e)=>RecordView(e.Child);
			}

			internal static void Initialize ()
			{
				Instance = new ViewTracker (Application.Top);
			}

			internal void StartUsingNewKeyMap (Dictionary<Command, Key> currentBindings)
			{
				lock (lockKnownViews) {

					// change our knowledge of what keys to bind
					this.keybindings = currentBindings;

					// Mark that we have not applied the key bindings yet to any views
					foreach (var view in knownViews.Keys) {
						knownViews [view] = false;
					}
				}
			}

			private void ApplyKeyBindingsToAllKnownViews ()
			{
				if(keybindings == null) {
					return;
				}

				// Key is the view Value is whether we have already done it
				foreach (var viewDone in knownViews) {

					var view = viewDone.Key;
					var done = viewDone.Value;

					if (done) {
						// we have already applied keybindings to this view
						continue;
					}

					var supported = new HashSet<Command>(view.GetSupportedCommands ());

					foreach (var kvp in keybindings) {
						
						// if the view supports the keybinding
						if(supported.Contains(kvp.Key))
						{
							// if the key was bound to any other commands clear that
							view.ClearKeyBinding (kvp.Key);
							view.AddKeyBinding (kvp.Value,kvp.Key);
						}

						// mark that we have done this view so don't need to set keybindings again on it
						knownViews [view] = true;
					}
				}
			}
		}

		public KeyBindingsDialog () : base()
		{
			Title = "Keybindings";
			Height = 50;
			Width = 10;
			if (ViewTracker.Instance == null) {
				ViewTracker.Initialize ();
			}
			
			// known commands that views can support
			commands = Enum.GetValues (typeof (Command)).Cast<Command>().ToArray();

			commandsListView = new ListView (commands) {
				Width = Dim.Percent (50),
				Height = Dim.Percent (100) - 1,
			};

			Add (commandsListView);

			keyLabel = new Label () {
				Text = "Key: None",
				Width = Dim.Fill(),
				X = Pos.Percent(50),
				Y = 0
			};
			Add (keyLabel);

			var btnChange = new Button ("Ch_ange") {
				X = Pos.Percent (50),
				Y = 1,
			};
			Add (btnChange);
			btnChange.Clicked += RemapKey;

			var close = new Button ("Ok");
			close.Clicked += (s,e) => {
				Application.RequestStop ();
				ViewTracker.Instance.StartUsingNewKeyMap (CurrentBindings);
			};
			AddButton (close);

			var cancel = new Button ("Cancel");
			cancel.Clicked += (s,e)=>Application.RequestStop();
			AddButton (cancel);

			// Register event handler as the last thing in constructor to prevent early calls
			// before it is even shown (e.g. OnEnter)
			commandsListView.SelectedItemChanged += CommandsListView_SelectedItemChanged;

			// Setup to show first ListView entry
			SetTextBoxToShowBinding (commands.First());
		}

		private void RemapKey (object sender, EventArgs e)
		{
			var cmd = commands [commandsListView.SelectedItem];
			Key? key = null;

			// prompt user to hit a key
			var dlg = new Dialog () { Title = "Enter Key" };
			dlg.KeyPress += (s, k) => {
				key = k.KeyEvent.Key;
				Application.RequestStop ();
			};
			Application.Run (dlg);

			if(key.HasValue) {
				CurrentBindings [cmd] = key.Value;
				SetTextBoxToShowBinding (cmd);
			}
		}

		private void SetTextBoxToShowBinding (Command cmd)
		{
			if (CurrentBindings.ContainsKey (cmd)) {
				keyLabel.Text = "Key: " + CurrentBindings [cmd].ToString ();
			} else {
				keyLabel.Text = "Key: None";
			}
			SetNeedsDisplay ();
		}

		private void CommandsListView_SelectedItemChanged (object sender, ListViewItemEventArgs obj)
		{
			SetTextBoxToShowBinding ((Command)obj.Value);
		}
	}
}
