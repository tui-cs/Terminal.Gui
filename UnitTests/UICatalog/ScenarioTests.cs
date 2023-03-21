using NStack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Terminal.Gui;
using UICatalog;
using UICatalog.Scenarios;
using Xunit;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace UICatalog.Tests {
	public class ScenarioTests {
		readonly ITestOutputHelper output;

		public ScenarioTests (ITestOutputHelper output)
		{
#if DEBUG_IDISPOSABLE
			Responder.Instances.Clear ();
#endif
			this.output = output;
		}

		int CreateInput (string input)
		{
			FakeConsole.MockKeyPresses.Clear ();
			// Put a QuitKey in at the end
			FakeConsole.PushMockKeyPress (Application.QuitKey);
			foreach (var c in input.Reverse ()) {
				Key key = Key.Unknown;
				if (char.IsLetter (c)) {
					key = (Key)char.ToUpper (c) | (char.IsUpper (c) ? Key.ShiftMask : (Key)0);
				} else {
					key = (Key)c;
				}
				FakeConsole.PushMockKeyPress (key);
			}
			return FakeConsole.MockKeyPresses.Count;
		}


		/// <summary>
		/// <para>
		/// This runs through all Scenarios defined in UI Catalog, calling Init, Setup, and Run.
		/// </para>
		/// <para>
		/// Should find any Scenarios which crash on load or do not respond to <see cref="Application.RequestStop()"/>.
		/// </para>
		/// </summary>
		[Fact]
		public void Run_All_Scenarios ()
		{
			List<Scenario> scenarios = Scenario.GetScenarios ();
			Assert.NotEmpty (scenarios);

			foreach (var scenario in scenarios) {
				output.WriteLine ($"Running Scenario '{scenario.GetName ()}'");

				Application.Init (new FakeDriver ());

				// Press QuitKey 
				Assert.Empty (FakeConsole.MockKeyPresses);
				// BUGBUG: For some reason ReadKey is not returning the QuitKey for some Scenarios
				// by adding this Space it seems to work.
				FakeConsole.PushMockKeyPress (Key.Space);
				FakeConsole.PushMockKeyPress (Application.QuitKey);

				// The only key we care about is the QuitKey
				Application.Top.KeyPress += (object sender, KeyEventEventArgs args) => {
					output.WriteLine ($"  Keypress: {args.KeyEvent.Key}");
					Assert.Equal (Application.QuitKey, args.KeyEvent.Key);
					args.Handled = false;
				};

				uint abortTime = 500;
				// If the scenario doesn't close within 500ms, this will force it to quit
				Func<MainLoop, bool> forceCloseCallback = (MainLoop loop) => {
					Application.RequestStop ();
					Assert.Fail ($"'{scenario.GetName ()}' failed to Quit with {Application.QuitKey} after {abortTime}ms. Force quit.");
					return false;
				};
				//output.WriteLine ($"  Add timeout to force quit after {abortTime}ms");
				_ = Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (abortTime), forceCloseCallback);

				Application.Iteration += () => {
					//output.WriteLine ($"  iteration {++iterations}");
					if (FakeConsole.MockKeyPresses.Count == 0) {
						Application.RequestStop ();
						//Assert.Fail ($"'{scenario.GetName ()}' failed to Quit with {Application.QuitKey}. Force quit.");
					}
				};

				scenario.Init ();
				scenario.Setup ();
				scenario.Run ();
				scenario.Dispose ();

				Application.Shutdown ();
#if DEBUG_IDISPOSABLE
				foreach (var inst in Responder.Instances) {
					Assert.True (inst.WasDisposed);
				}
				Responder.Instances.Clear ();
#endif
			}
#if DEBUG_IDISPOSABLE
			foreach (var inst in Responder.Instances) {
				Assert.True (inst.WasDisposed);
			}
			Responder.Instances.Clear ();
#endif
		}

		[Fact]
		public void Run_Generic ()
		{
			List<Scenario> scenarios = Scenario.GetScenarios ();
			Assert.NotEmpty (scenarios);

			var item = scenarios.FindIndex (s => s.GetName ().Equals ("Generic", StringComparison.OrdinalIgnoreCase));
			var generic = scenarios [item];

			Application.Init (new FakeDriver ());
			// BUGBUG: For some reason ReadKey is not
			// returning the QuitKey for some Scenarios
			// by adding this Space it seems to work.
			FakeConsole.PushMockKeyPress (Key.Space);
			FakeConsole.PushMockKeyPress (Application.QuitKey);

			int iterations = 0;
			Application.Iteration = () => {
				iterations++;
				output.WriteLine ($"'Generic' iteration {iterations}");
				// Stop if we run out of control...
				if (iterations == 10) {
					output.WriteLine ($"'Generic' had to be force quit!");
					Application.RequestStop ();
				}
			};

			var ms = 100;
			var abortCount = 0;
			Func<MainLoop, bool> abortCallback = (MainLoop loop) => {
				abortCount++;
				output.WriteLine ($"'Generic' abortCount {abortCount}");
				Application.RequestStop ();
				return false;
			};
			var token = Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (ms), abortCallback);

			Application.Top.KeyPress += (object sender, KeyEventEventArgs args) => {
				Assert.Equal (Key.CtrlMask | Key.Q, args.KeyEvent.Key);
			};

			generic.Init ();
			generic.Setup ();
			generic.Run ();

			Assert.Equal (0, abortCount);
			// # of key up events should match # of iterations
			Assert.Equal (1, iterations);

			generic.Dispose ();

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();

#if DEBUG_IDISPOSABLE
			foreach (var inst in Responder.Instances) {
				Assert.True (inst.WasDisposed);
			}
			Responder.Instances.Clear ();
#endif
		}

		[Fact]
		public void Run_All_Views_Tester_Scenario ()
		{
			Window _leftPane;
			ListView _classListView;
			FrameView _hostPane;

			Dictionary<string, Type> _viewClasses;
			View _curView = null;

			// Settings
			FrameView _settingsPane;
			CheckBox _computedCheckBox;
			FrameView _locationFrame;
			RadioGroup _xRadioGroup;
			TextField _xText;
			int _xVal = 0;
			RadioGroup _yRadioGroup;
			TextField _yText;
			int _yVal = 0;

			FrameView _sizeFrame;
			RadioGroup _wRadioGroup;
			TextField _wText;
			int _wVal = 0;
			RadioGroup _hRadioGroup;
			TextField _hText;
			int _hVal = 0;
			List<string> posNames = new List<String> { "Factor", "AnchorEnd", "Center", "Absolute" };
			List<string> dimNames = new List<String> { "Factor", "Fill", "Absolute" };


			Application.Init (new FakeDriver ());

			var Top = Application.Top;

			_viewClasses = GetAllViewClassesCollection ()
				.OrderBy (t => t.Name)
				.Select (t => new KeyValuePair<string, Type> (t.Name, t))
				.ToDictionary (t => t.Key, t => t.Value);

			_leftPane = new Window ("Classes") {
				X = 0,
				Y = 0,
				Width = 15,
				Height = Dim.Fill (1), // for status bar
				CanFocus = false,
				ColorScheme = Colors.TopLevel,
			};

			_classListView = new ListView (_viewClasses.Keys.ToList ()) {
				X = 0,
				Y = 0,
				Width = Dim.Fill (0),
				Height = Dim.Fill (0),
				AllowsMarking = false,
				ColorScheme = Colors.TopLevel,
			};
			_leftPane.Add (_classListView);

			_settingsPane = new FrameView ("Settings") {
				X = Pos.Right (_leftPane),
				Y = 0, // for menu
				Width = Dim.Fill (),
				Height = 10,
				CanFocus = false,
				ColorScheme = Colors.TopLevel,
			};
			_computedCheckBox = new CheckBox ("Computed Layout", true) { X = 0, Y = 0 };
			_settingsPane.Add (_computedCheckBox);

			var radioItems = new ustring [] { "Percent(x)", "AnchorEnd(x)", "Center", "At(x)" };
			_locationFrame = new FrameView ("Location (Pos)") {
				X = Pos.Left (_computedCheckBox),
				Y = Pos.Bottom (_computedCheckBox),
				Height = 3 + radioItems.Length,
				Width = 36,
			};
			_settingsPane.Add (_locationFrame);

			var label = new Label ("x:") { X = 0, Y = 0 };
			_locationFrame.Add (label);
			_xRadioGroup = new RadioGroup (radioItems) {
				X = 0,
				Y = Pos.Bottom (label),
			};
			_xText = new TextField ($"{_xVal}") { X = Pos.Right (label) + 1, Y = 0, Width = 4 };
			_locationFrame.Add (_xText);

			_locationFrame.Add (_xRadioGroup);

			radioItems = new ustring [] { "Percent(y)", "AnchorEnd(y)", "Center", "At(y)" };
			label = new Label ("y:") { X = Pos.Right (_xRadioGroup) + 1, Y = 0 };
			_locationFrame.Add (label);
			_yText = new TextField ($"{_yVal}") { X = Pos.Right (label) + 1, Y = 0, Width = 4 };
			_locationFrame.Add (_yText);
			_yRadioGroup = new RadioGroup (radioItems) {
				X = Pos.X (label),
				Y = Pos.Bottom (label),
			};
			_locationFrame.Add (_yRadioGroup);

			_sizeFrame = new FrameView ("Size (Dim)") {
				X = Pos.Right (_locationFrame),
				Y = Pos.Y (_locationFrame),
				Height = 3 + radioItems.Length,
				Width = 40,
			};

			radioItems = new ustring [] { "Percent(width)", "Fill(width)", "Sized(width)" };
			label = new Label ("width:") { X = 0, Y = 0 };
			_sizeFrame.Add (label);
			_wRadioGroup = new RadioGroup (radioItems) {
				X = 0,
				Y = Pos.Bottom (label),
			};
			_wText = new TextField ($"{_wVal}") { X = Pos.Right (label) + 1, Y = 0, Width = 4 };
			_sizeFrame.Add (_wText);
			_sizeFrame.Add (_wRadioGroup);

			radioItems = new ustring [] { "Percent(height)", "Fill(height)", "Sized(height)" };
			label = new Label ("height:") { X = Pos.Right (_wRadioGroup) + 1, Y = 0 };
			_sizeFrame.Add (label);
			_hText = new TextField ($"{_hVal}") { X = Pos.Right (label) + 1, Y = 0, Width = 4 };
			_sizeFrame.Add (_hText);

			_hRadioGroup = new RadioGroup (radioItems) {
				X = Pos.X (label),
				Y = Pos.Bottom (label),
			};
			_sizeFrame.Add (_hRadioGroup);

			_settingsPane.Add (_sizeFrame);

			_hostPane = new FrameView ("") {
				X = Pos.Right (_leftPane),
				Y = Pos.Bottom (_settingsPane),
				Width = Dim.Fill (),
				Height = Dim.Fill (1), // + 1 for status bar
				ColorScheme = Colors.Dialog,
			};

			_classListView.OpenSelectedItem += (s, a) => {
				_settingsPane.SetFocus ();
			};
			_classListView.SelectedItemChanged += (s, args) => {
				ClearClass (_curView);
				_curView = CreateClass (_viewClasses.Values.ToArray () [_classListView.SelectedItem]);
			};

			_computedCheckBox.Toggled += (s, e) => {
				if (_curView != null) {
					_curView.LayoutStyle = e.OldValue == true ? LayoutStyle.Absolute : LayoutStyle.Computed;
					_hostPane.LayoutSubviews ();
				}
			};

			_xRadioGroup.SelectedItemChanged += (s, selected) => DimPosChanged (_curView);

			_xText.TextChanged += (s, args) => {
				try {
					_xVal = int.Parse (_xText.Text.ToString ());
					DimPosChanged (_curView);
				} catch {

				}
			};

			_yText.TextChanged += (s, e) => {
				try {
					_yVal = int.Parse (_yText.Text.ToString ());
					DimPosChanged (_curView);
				} catch {

				}
			};

			_yRadioGroup.SelectedItemChanged += (s, selected) => DimPosChanged (_curView);

			_wRadioGroup.SelectedItemChanged += (s, selected) => DimPosChanged (_curView);

			_wText.TextChanged += (s, args) => {
				try {
					_wVal = int.Parse (_wText.Text.ToString ());
					DimPosChanged (_curView);
				} catch {

				}
			};

			_hText.TextChanged += (s, args) => {
				try {
					_hVal = int.Parse (_hText.Text.ToString ());
					DimPosChanged (_curView);
				} catch {

				}
			};

			_hRadioGroup.SelectedItemChanged += (s, selected) => DimPosChanged (_curView);

			Top.Add (_leftPane, _settingsPane, _hostPane);

			Top.LayoutSubviews ();

			_curView = CreateClass (_viewClasses.First ().Value);

			int iterations = 0;

			Application.Iteration += () => {
				iterations++;

				if (iterations < _viewClasses.Count) {
					_classListView.MoveDown ();
					Assert.Equal (_curView.GetType ().Name,
						_viewClasses.Values.ToArray () [_classListView.SelectedItem].Name);
				} else {
					Application.RequestStop ();
				}
			};

			Application.Run ();

			Assert.Equal (_viewClasses.Count, iterations);

			Application.Shutdown ();


			void DimPosChanged (View view)
			{
				if (view == null) {
					return;
				}

				var layout = view.LayoutStyle;

				try {
					view.LayoutStyle = LayoutStyle.Absolute;

					switch (_xRadioGroup.SelectedItem) {
					case 0:
						view.X = Pos.Percent (_xVal);
						break;
					case 1:
						view.X = Pos.AnchorEnd (_xVal);
						break;
					case 2:
						view.X = Pos.Center ();
						break;
					case 3:
						view.X = Pos.At (_xVal);
						break;
					}

					switch (_yRadioGroup.SelectedItem) {
					case 0:
						view.Y = Pos.Percent (_yVal);
						break;
					case 1:
						view.Y = Pos.AnchorEnd (_yVal);
						break;
					case 2:
						view.Y = Pos.Center ();
						break;
					case 3:
						view.Y = Pos.At (_yVal);
						break;
					}

					switch (_wRadioGroup.SelectedItem) {
					case 0:
						view.Width = Dim.Percent (_wVal);
						break;
					case 1:
						view.Width = Dim.Fill (_wVal);
						break;
					case 2:
						view.Width = Dim.Sized (_wVal);
						break;
					}

					switch (_hRadioGroup.SelectedItem) {
					case 0:
						view.Height = Dim.Percent (_hVal);
						break;
					case 1:
						view.Height = Dim.Fill (_hVal);
						break;
					case 2:
						view.Height = Dim.Sized (_hVal);
						break;
					}
				} catch (Exception e) {
					MessageBox.ErrorQuery ("Exception", e.Message, "Ok");
				} finally {
					view.LayoutStyle = layout;
				}
				UpdateTitle (view);
			}

			void UpdateSettings (View view)
			{
				var x = view.X.ToString ();
				var y = view.Y.ToString ();
				_xRadioGroup.SelectedItem = posNames.IndexOf (posNames.Where (s => x.Contains (s)).First ());
				_yRadioGroup.SelectedItem = posNames.IndexOf (posNames.Where (s => y.Contains (s)).First ());
				_xText.Text = $"{view.Frame.X}";
				_yText.Text = $"{view.Frame.Y}";

				var w = view.Width.ToString ();
				var h = view.Height.ToString ();
				_wRadioGroup.SelectedItem = dimNames.IndexOf (dimNames.Where (s => w.Contains (s)).First ());
				_hRadioGroup.SelectedItem = dimNames.IndexOf (dimNames.Where (s => h.Contains (s)).First ());
				_wText.Text = $"{view.Frame.Width}";
				_hText.Text = $"{view.Frame.Height}";
			}

			void UpdateTitle (View view)
			{
				_hostPane.Title = $"{view.GetType ().Name} - {view.X.ToString ()}, {view.Y.ToString ()}, {view.Width.ToString ()}, {view.Height.ToString ()}";
			}

			List<Type> GetAllViewClassesCollection ()
			{
				List<Type> types = new List<Type> ();
				foreach (Type type in typeof (View).Assembly.GetTypes ()
				 .Where (myType => myType.IsClass && !myType.IsAbstract && myType.IsPublic && myType.IsSubclassOf (typeof (View)))) {
					types.Add (type);
				}
				return types;
			}

			void ClearClass (View view)
			{
				// Remove existing class, if any
				if (view != null) {
					view.LayoutComplete -= LayoutCompleteHandler;
					_hostPane.Remove (view);
					view.Dispose ();
					_hostPane.Clear ();
				}
			}

			View CreateClass (Type type)
			{
				// If we are to create a generic Type
				if (type.IsGenericType) {

					// For each of the <T> arguments
					List<Type> typeArguments = new List<Type> ();

					// use <object>
					foreach (var arg in type.GetGenericArguments ()) {
						typeArguments.Add (typeof (object));
					}

					// And change what type we are instantiating from MyClass<T> to MyClass<object>
					type = type.MakeGenericType (typeArguments.ToArray ());
				}
				// Instantiate view
				var view = (View)Activator.CreateInstance (type);

				//_curView.X = Pos.Center ();
				//_curView.Y = Pos.Center ();
				view.Width = Dim.Percent (75);
				view.Height = Dim.Percent (75);

				// Set the colorscheme to make it stand out if is null by default
				if (view.ColorScheme == null) {
					view.ColorScheme = Colors.Base;
				}

				// If the view supports a Text property, set it so we have something to look at
				if (view.GetType ().GetProperty ("Text") != null) {
					try {
						view.GetType ().GetProperty ("Text")?.GetSetMethod ()?.Invoke (view, new [] { ustring.Make ("Test Text") });
					} catch (TargetInvocationException e) {
						MessageBox.ErrorQuery ("Exception", e.InnerException.Message, "Ok");
						view = null;
					}
				}

				// If the view supports a Title property, set it so we have something to look at
				if (view != null && view.GetType ().GetProperty ("Title") != null) {
					view?.GetType ().GetProperty ("Title")?.GetSetMethod ()?.Invoke (view, new [] { ustring.Make ("Test Title") });
				}

				// If the view supports a Source property, set it so we have something to look at
				if (view != null && view.GetType ().GetProperty ("Source") != null && view.GetType ().GetProperty ("Source").PropertyType == typeof (Terminal.Gui.IListDataSource)) {
					var source = new ListWrapper (new List<ustring> () { ustring.Make ("Test Text #1"), ustring.Make ("Test Text #2"), ustring.Make ("Test Text #3") });
					view?.GetType ().GetProperty ("Source")?.GetSetMethod ()?.Invoke (view, new [] { source });
				}

				// Set Settings
				_computedCheckBox.Checked = view.LayoutStyle == LayoutStyle.Computed;

				// Add
				_hostPane.Add (view);
				//DimPosChanged ();
				_hostPane.LayoutSubviews ();
				_hostPane.Clear ();
				_hostPane.SetNeedsDisplay ();
				UpdateSettings (view);
				UpdateTitle (view);

				view.LayoutComplete += LayoutCompleteHandler;

				return view;
			}

			void LayoutCompleteHandler (object sender, LayoutEventArgs args)
			{
				UpdateTitle (_curView);
			}
		}
	}
}
