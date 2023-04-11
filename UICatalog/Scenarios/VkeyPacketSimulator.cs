using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "VkeyPacketSimulator", Description: "Simulates the Virtual Key Packet")]
	[ScenarioCategory ("Mouse and Keyboard")]
	public class VkeyPacketSimulator : Scenario {
		List<int> _keyboardStrokes = new List<int> ();
		bool _outputStarted = false;
		bool _wasUnknown = false;
		static ManualResetEventSlim _stopOutput = new ManualResetEventSlim (false);

		public override void Setup ()
		{
			var label = new Label ("Input") {
				X = Pos.Center ()
			};
			Win.Add (label);

			var btnInput = new Button ("Select Input") {
				X = Pos.AnchorEnd (16),
			};
			Win.Add (btnInput);

			const string ruler = "|123456789";

			var inputHorizontalRuler = new Label ("") {
				Y = Pos.Bottom (btnInput),
				Width = Dim.Fill (),
				ColorScheme = Colors.Error,
				AutoSize = false
			};
			Win.Add (inputHorizontalRuler);

			var inputVerticalRuler = new Label ("", TextDirection.TopBottom_LeftRight) {
				Y = Pos.Bottom (btnInput),
				Width = 1,
				ColorScheme = Colors.Error,
				AutoSize = false
			};
			Win.Add (inputVerticalRuler);

			var tvInput = new TextView {
				X = 1,
				Y = Pos.Bottom (inputHorizontalRuler),
				Width = Dim.Fill (),
				Height = Dim.Percent (50) - 1
			};
			Win.Add (tvInput);

			label = new Label ("Output") {
				X = Pos.Center (),
				Y = Pos.Bottom (tvInput)
			};
			Win.Add (label);

			var btnOutput = new Button ("Select Output") {
				X = Pos.AnchorEnd (17),
				Y = Pos.Top (label)
			};
			Win.Add (btnOutput);

			var outputHorizontalRuler = new Label ("") {
				Y = Pos.Bottom (btnOutput),
				Width = Dim.Fill (),
				ColorScheme = Colors.Error,
				AutoSize = false
			};
			Win.Add (outputHorizontalRuler);

			var outputVerticalRuler = new Label ("", TextDirection.TopBottom_LeftRight) {
				Y = Pos.Bottom (btnOutput),
				Width = 1,
				Height = Dim.Fill (),
				ColorScheme = Colors.Error,
				AutoSize = false
			};
			Win.Add (outputVerticalRuler);

			var tvOutput = new TextView {
				X = 1,
				Y = Pos.Bottom (outputHorizontalRuler),
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				ReadOnly = true
			};

			tvOutput.KeyDown += (s, e) => {
				//System.Diagnostics.Debug.WriteLine ($"Output - KeyDown: {e.KeyEvent.Key}");
				e.Handled = true;
				if (e.KeyEvent.Key == Key.Unknown) {
					_wasUnknown = true;
				}
			};

			tvOutput.KeyPress += (s, e) => {
				//System.Diagnostics.Debug.WriteLine ($"Output - KeyPress - _keyboardStrokes: {_keyboardStrokes.Count}");
				if (_outputStarted && _keyboardStrokes.Count > 0) {
					var ev = ShortcutHelper.GetModifiersKey (e.KeyEvent);
					//System.Diagnostics.Debug.WriteLine ($"Output - KeyPress: {ev}");
					if (!tvOutput.ProcessKey (e.KeyEvent)) {
						Application.MainLoop.Invoke (() => {
							MessageBox.Query ("Keys", $"'{ShortcutHelper.GetShortcutTag (ev)}' pressed!", "Ok");
						});
					}
					e.Handled = true;
					_stopOutput.Set ();
				}
				//System.Diagnostics.Debug.WriteLine ($"Output - KeyPress - _keyboardStrokes: {_keyboardStrokes.Count}");
			};

			Win.Add (tvOutput);

			tvInput.KeyDown += (s, e) => {
				//System.Diagnostics.Debug.WriteLine ($"Input - KeyDown: {e.KeyEvent.Key}");
				e.Handled = true;
				if (e.KeyEvent.Key == Key.Unknown) {
					_wasUnknown = true;
				}
			};

			KeyEventEventArgs unknownChar = null;

			tvInput.KeyPress += (s, e) => {
				if (e.KeyEvent.Key == (Key.Q | Key.CtrlMask)) {
					Application.RequestStop ();
					return;
				}
				if (e.KeyEvent.Key == Key.Unknown) {
					_wasUnknown = true;
					e.Handled = true;
					return;
				}
				if (_wasUnknown && _keyboardStrokes.Count == 1) {
					_wasUnknown = false;
				} else if (_wasUnknown && char.IsLetter ((char)e.KeyEvent.Key)) {
					_wasUnknown = false;
				} else if (!_wasUnknown && _keyboardStrokes.Count > 0) {
					e.Handled = true;
					return;
				}
				if (_keyboardStrokes.Count == 0) {
					AddKeyboardStrokes (e);
				} else {
					_keyboardStrokes.Insert (0, 0);
				}
				var ev = ShortcutHelper.GetModifiersKey (e.KeyEvent);
				//System.Diagnostics.Debug.WriteLine ($"Input - KeyPress: {ev}");
				//System.Diagnostics.Debug.WriteLine ($"Input - KeyPress - _keyboardStrokes: {_keyboardStrokes.Count}");
			};

			tvInput.KeyUp += (s, e) => {
				//System.Diagnostics.Debug.WriteLine ($"Input - KeyUp: {e.KeyEvent.Key}");
				//var ke = e.KeyEvent;
				var ke = ShortcutHelper.GetModifiersKey (e.KeyEvent);
				if (_wasUnknown && (int)ke - (int)(ke & (Key.AltMask | Key.CtrlMask | Key.ShiftMask)) != 0) {
					unknownChar = e;
				}
				e.Handled = true;
				if (!_wasUnknown && _keyboardStrokes.Count > 0) {
					_outputStarted = true;
					tvOutput.ReadOnly = false;
					tvOutput.SetFocus ();
					tvOutput.SetNeedsDisplay ();

					Task.Run (() => {
						while (_outputStarted) {
							try {
								ConsoleModifiers mod = new ConsoleModifiers ();
								if (ke.HasFlag (Key.ShiftMask)) {
									mod |= ConsoleModifiers.Shift;
								}
								if (ke.HasFlag (Key.AltMask)) {
									mod |= ConsoleModifiers.Alt;
								}
								if (ke.HasFlag (Key.CtrlMask)) {
									mod |= ConsoleModifiers.Control;
								}
								for (int i = 0; i < _keyboardStrokes.Count; i++) {
									var consoleKey = ConsoleKeyMapping.GetConsoleKeyFromKey ((uint)_keyboardStrokes [i], mod, out _, out _);
									Application.Driver.SendKeys ((char)consoleKey, ConsoleKey.Packet, mod.HasFlag (ConsoleModifiers.Shift),
										mod.HasFlag (ConsoleModifiers.Alt), mod.HasFlag (ConsoleModifiers.Control));
								}
								//}
							} catch (Exception) {
								Application.MainLoop.Invoke (() => {
									MessageBox.ErrorQuery ("Error", "Couldn't send the keystrokes!", "Ok");
									Application.RequestStop ();
								});
							}
							_stopOutput.Wait ();
							_stopOutput.Reset ();
							_keyboardStrokes.RemoveAt (0);
							if (_keyboardStrokes.Count == 0) {
								_outputStarted = false;
								Application.MainLoop.Invoke (() => {
									tvOutput.ReadOnly = true;
									tvInput.SetFocus ();
								});
							}
						}
						//System.Diagnostics.Debug.WriteLine ($"_outputStarted: {_outputStarted}");
					});
				}
			};

			btnInput.Clicked += (s,e) => {
				if (!tvInput.HasFocus && _keyboardStrokes.Count == 0) {
					tvInput.SetFocus ();
				}
			};

			btnOutput.Clicked += (s,e) => {
				if (!tvOutput.HasFocus && _keyboardStrokes.Count == 0) {
					tvOutput.SetFocus ();
				}
			};

			tvInput.SetFocus ();

			void Win_LayoutComplete (object sender, LayoutEventArgs obj)
			{
				inputHorizontalRuler.Text = outputHorizontalRuler.Text = ruler.Repeat ((int)Math.Ceiling ((double)(inputHorizontalRuler.Bounds.Width) / (double)ruler.Length)) [0..(inputHorizontalRuler.Bounds.Width)];
				inputVerticalRuler.Height = tvInput.Frame.Height + 1;
				inputVerticalRuler.Text = ruler.Repeat ((int)Math.Ceiling ((double)(inputVerticalRuler.Bounds.Height) / (double)ruler.Length)) [0..(inputVerticalRuler.Bounds.Height)];
				outputVerticalRuler.Text = ruler.Repeat ((int)Math.Ceiling ((double)(outputVerticalRuler.Bounds.Height) / (double)ruler.Length)) [0..(outputVerticalRuler.Bounds.Height)];
			}

			Win.LayoutComplete += Win_LayoutComplete;
		}

		private void AddKeyboardStrokes (KeyEventEventArgs e)
		{
			var ke = e.KeyEvent;
			var km = new KeyModifiers ();
			if (ke.IsShift) {
				km.Shift = true;
			}
			if (ke.IsAlt) {
				km.Alt = true;
			}
			if (ke.IsCtrl) {
				km.Ctrl = true;
			}
			var keyChar = ke.KeyValue;
			var mK = (int)((Key)ke.KeyValue & (Key.AltMask | Key.CtrlMask | Key.ShiftMask));
			keyChar &= ~mK;
			_keyboardStrokes.Add (keyChar);
		}
	}
}
