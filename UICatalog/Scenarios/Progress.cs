using NStack;
using System;
using System.Threading;
using Terminal.Gui;
using System.Linq;

namespace UICatalog.Scenarios {
	// 
	// This would be a great scenario to show of threading (Issue #471)
	//
	[ScenarioMetadata (Name: "Progress", Description: "Shows off ProgressBar and Threading.")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Threading"), ScenarioCategory ("Progress")]
	public class Progress : Scenario {

		class ProgressDemo : FrameView {
			const int _verticalSpace = 1;

			internal FrameView LeftFrame { get; private set; }
			internal TextField Speed { get; private set; }
			internal ProgressBar ActivityProgressBar { get; private set; }
			internal ProgressBar PulseProgressBar { get; private set; }
			internal SpinnerView Spinner { get; private set; }
			internal Action StartBtnClick;
			internal Action StopBtnClick;
			internal Action PulseBtnClick = null;
			private Label _startedLabel;
			internal bool Started { 
				get { 
					return _startedLabel.Text == "Started";
				} 
				private set {
					_startedLabel.Text = value ? "Started" : "Stopped";
				} 
			}

			internal ProgressDemo (string title) : base (title)
			{
				ColorScheme = Colors.Dialog;

				LeftFrame = new FrameView ("Settings") {
					X = 0,
					Y = 0,
					Height = Dim.Percent (100), 
					Width = Dim.Percent (25)
				};
				var lbl = new Label (1, 1, "Tick every (ms):");
				LeftFrame.Add (lbl);
				Speed = new TextField ("") {
					X = Pos.X (lbl),
					Y = Pos.Bottom (lbl),
					Width = 7,
				};
				LeftFrame.Add (Speed);

				Add (LeftFrame);

				var startButton = new Button ("Start Timer") {
					X = Pos.Right (LeftFrame) + 1,
					Y = 0,
				};
				startButton.Clicked += (s,e) => Start ();
				var pulseButton = new Button ("Pulse") {
					X = Pos.Right (startButton) + 2,
					Y = Pos.Y (startButton),
				};
				pulseButton.Clicked += (s,e) => Pulse ();
				var stopbutton = new Button ("Stop Timer") {
					X = Pos.Right (pulseButton) + 2,
					Y = Pos.Top (pulseButton),
				};
				stopbutton.Clicked += (s,e) => Stop ();

				Add (startButton);
				Add (pulseButton);
				Add (stopbutton);

				ActivityProgressBar = new ProgressBar () {
					X = Pos.Right (LeftFrame) + 1,
					Y = Pos.Bottom (startButton) + 1,
					Width = Dim.Fill () - 1,
					Height = 1,
					Fraction = 0.25F,
					ColorScheme = Colors.Error
				};
				Add (ActivityProgressBar);

				Spinner = new SpinnerView () {
					Style = new SpinnerStyle.Dots2 (),
					SpinReverse = true,
					Y = ActivityProgressBar.Y,
					Visible = false
				};
				ActivityProgressBar.Width = Dim.Fill () - Spinner.Width;
				Spinner.X = Pos.Right (ActivityProgressBar);


				Add (Spinner);

				PulseProgressBar = new ProgressBar () {
					X = Pos.Right (LeftFrame) + 1,
					Y = Pos.Bottom (ActivityProgressBar) + 1,
					Width = Dim.Fill () - Spinner.Width,
					Height = 1,
					ColorScheme = Colors.Error
				};
				Add (PulseProgressBar);

				_startedLabel = new Label ("Stopped") {
					X = Pos.Right (LeftFrame) + 1,
					Y = Pos.Bottom (PulseProgressBar),
				};
				Add (_startedLabel);

				// Explictly cause layout so the setting of Height below works
				LayoutSubviews ();

				// Set height to height of controls + spacing + frame
				Height = 2 + _verticalSpace + startButton.Frame.Height + _verticalSpace + ActivityProgressBar.Frame.Height + _verticalSpace + PulseProgressBar.Frame.Height + _verticalSpace;
			}

			internal void Start ()
			{
				Started = true;
				StartBtnClick?.Invoke ();
				Application.MainLoop.Invoke(()=>{
					Spinner.Visible = true;
					ActivityProgressBar.Width = Dim.Fill () - Spinner.Width;
					this.LayoutSubviews();
				});
			}

			internal void Stop ()
			{
				Started = false;
				StopBtnClick?.Invoke ();

				Application.MainLoop.Invoke(()=>{
					Spinner.Visible = false;
					ActivityProgressBar.Width = Dim.Fill () - Spinner.Width;
					this.LayoutSubviews();
				});
			}

			internal void Pulse ()
			{
				Spinner.Visible = true;
				if (PulseBtnClick != null) {
					PulseBtnClick?.Invoke ();

				} else {
					if (ActivityProgressBar.Fraction + 0.01F >= 1) {
						ActivityProgressBar.Fraction = 0F;
					} else {
						ActivityProgressBar.Fraction += 0.01F;
					}
					PulseProgressBar.Pulse ();
					Spinner.SetNeedsDisplay ();
				}
			}
		}

		private Timer _systemTimer = null;
		private uint _systemTimerTick = 100; // ms
		private object _mainLoopTimeout = null;
		private uint _mainLooopTimeoutTick = 100; // ms
		public override void Setup ()
		{
			// Demo #1 - Use System.Timer (and threading)
			var systemTimerDemo = new ProgressDemo ("System.Timer (threads)") {
				X = 0,
				Y = 0,
				Width = Dim.Percent (100),
			};
			systemTimerDemo.StartBtnClick = () => {
				_systemTimer?.Dispose ();
				_systemTimer = null;

				systemTimerDemo.ActivityProgressBar.Fraction = 0F;
				systemTimerDemo.PulseProgressBar.Fraction = 0F;

				_systemTimer = new Timer ((o) => {
					// Note the check for Mainloop being valid. System.Timers can run after they are Disposed.
					// This code must be defensive for that. 
					Application.MainLoop?.Invoke (() => systemTimerDemo.Pulse ());
				}, null, 0, _systemTimerTick);
			};

			systemTimerDemo.StopBtnClick = () => {
				_systemTimer?.Dispose ();
				_systemTimer = null;

				systemTimerDemo.ActivityProgressBar.Fraction = 1F;
				systemTimerDemo.PulseProgressBar.Fraction = 1F;
			};
			systemTimerDemo.Speed.Text = $"{_systemTimerTick}";
			systemTimerDemo.Speed.TextChanged += (s, a) => {
				uint result;
				if (uint.TryParse (systemTimerDemo.Speed.Text.ToString(), out result)) {
					_systemTimerTick = result;
					System.Diagnostics.Debug.WriteLine ($"{_systemTimerTick}");
					if (systemTimerDemo.Started) {
						systemTimerDemo.Start ();
					}

				} else {
					System.Diagnostics.Debug.WriteLine ("bad entry");
				}
			};
			Win.Add (systemTimerDemo);

			// Demo #2 - Use Application.MainLoop.AddTimeout (no threads)
			var mainLoopTimeoutDemo = new ProgressDemo ("Application.AddTimer (no threads)") {
				X = 0,
				Y = Pos.Bottom (systemTimerDemo),
				Width = Dim.Percent (100),
			};
			mainLoopTimeoutDemo.StartBtnClick = () => {
				mainLoopTimeoutDemo.StopBtnClick ();

				mainLoopTimeoutDemo.ActivityProgressBar.Fraction = 0F;
				mainLoopTimeoutDemo.PulseProgressBar.Fraction = 0F;

				_mainLoopTimeout = Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (_mainLooopTimeoutTick), (loop) => {
					mainLoopTimeoutDemo.Pulse ();
					
					return true;
				});
			};
			mainLoopTimeoutDemo.StopBtnClick = () => {
				if (_mainLoopTimeout != null) {
					Application.MainLoop.RemoveTimeout (_mainLoopTimeout);
					_mainLoopTimeout = null;
				}

				mainLoopTimeoutDemo.ActivityProgressBar.Fraction = 1F;
				mainLoopTimeoutDemo.PulseProgressBar.Fraction = 1F;
			};

			mainLoopTimeoutDemo.Speed.Text = $"{_mainLooopTimeoutTick}";
			mainLoopTimeoutDemo.Speed.TextChanged += (s, a) => {
				uint result;
				if (uint.TryParse (mainLoopTimeoutDemo.Speed.Text.ToString (), out result)) {
					_mainLooopTimeoutTick = result;
					if (mainLoopTimeoutDemo.Started) {
						mainLoopTimeoutDemo.Start ();
					}
				}
			};
			Win.Add (mainLoopTimeoutDemo);

			var startBoth = new Button ("Start Both") {
				X = Pos.Center (),
				Y = Pos.Bottom(mainLoopTimeoutDemo) + 1,
			};
			startBoth.Clicked += (s,e) => {
				systemTimerDemo.Start ();
				mainLoopTimeoutDemo.Start ();
			};
			Win.Add (startBoth);
		}

		protected override void Dispose (bool disposing)
		{
			foreach (var v in Win.Subviews.OfType<ProgressDemo>()) {
				v?.StopBtnClick ();
			}
			base.Dispose (disposing);
		}
	}
}