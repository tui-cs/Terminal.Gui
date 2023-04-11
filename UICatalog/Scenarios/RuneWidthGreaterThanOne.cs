using System;
using System.Globalization;
using System.Text;
using System.Threading;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "RuneWidthGreaterThanOne", Description: "Test rune width greater than one")]
	[ScenarioCategory ("Controls")]
	public class RuneWidthGreaterThanOne : Scenario {
		private Label _label;
		private TextField _text;
		private Button _button;
		private Label _labelR;
		private Label _labelV;
		private Window _win;
		private string _lastRunesUsed;

		public override void Init ()
		{
			Application.Init ();

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("Padding", new MenuItem [] {
					new MenuItem ("With Padding", "", () => _win.Padding.Thickness = new Thickness (1)),
					new MenuItem ("Without Padding", "", () =>_win.Padding.Thickness = new Thickness (0))
				}),
				new MenuBarItem ("BorderStyle", new MenuItem [] {
					new MenuItem ("Single", "", () => _win.BorderStyle = LineStyle.Single),
					new MenuItem ("None", "", () => _win.BorderStyle = LineStyle.None)
				}),
				new MenuBarItem ("Runes length", new MenuItem [] {
					new MenuItem ("Wide", "", WideRunes),
					new MenuItem ("Narrow", "", NarrowRunes),
					new MenuItem ("Mixed", "", MixedRunes)
				})
			});

			_label = new Label () {
				X = Pos.Center (),
				Y = 1,
				ColorScheme = new ColorScheme () {
					Normal = Colors.Base.Focus
				}
			};
			_text = new TextField () {
				X = Pos.Center (),
				Y = 3,
				Width = 20
			};
			_button = new Button () {
				X = Pos.Center (),
				Y = 5
			};
			_labelR = new Label () {
				X = Pos.AnchorEnd (30),
				Y = 18
			};
			_labelV = new Label () {
				TextDirection = TextDirection.TopBottom_LeftRight,
				X = Pos.AnchorEnd (30),
				Y = Pos.Bottom (_labelR)
			};
			_win = new Window () {
				X = 5,
				Y = 5,
				Width = Dim.Fill (22),
				Height = Dim.Fill (5)
			};
			_win.Add (_label, _text, _button, _labelR, _labelV);
			Application.Top.Add (menu, _win);

			WideRunes ();
			//NarrowRunes ();
			//MixedRunes ();
			Application.Run ();
		}

		private void UnsetClickedEvent ()
		{
			switch (_lastRunesUsed) {
			case "Narrow":
				_button.Clicked -= NarrowMessage;
				break;
			case "Mixed":
				_button.Clicked -= MixedMessage;
				break;
			case "Wide":
				_button.Clicked -= WideMessage;
				break;
			}
		}

		private void MixedMessage (object sender, EventArgs e)
		{
			MessageBox.Query ("Say Hello 你", $"Hello {_text.Text}", "Ok");
		}

		private void NarrowMessage (object sender, EventArgs e)
		{
			MessageBox.Query ("Say Hello", $"Hello {_text.Text}", "Ok");
		}

		private void WideMessage (object sender, EventArgs e)
		{
			MessageBox.Query ("こんにちはと言う", $"こんにちは {_text.Text}", "Ok");
		}

		private void MixedRunes ()
		{
			UnsetClickedEvent ();
			_label.Text = "Enter your name 你:";
			_text.Text = "gui.cs 你:";
			_button.Text = "Say Hello 你";
			_button.Clicked += MixedMessage;
			_labelR.X = Pos.AnchorEnd (21);
			_labelR.Y = 18;
			_labelR.Text = "This is a test text 你";
			_labelV.X = Pos.AnchorEnd (21);
			_labelV.Y = Pos.Bottom (_labelR);
			_labelV.Text = "This is a test text 你";
			_win.Title = "HACC Demo 你";
			_lastRunesUsed = "Mixed";
			Application.Refresh ();
		}

		private void NarrowRunes ()
		{
			UnsetClickedEvent ();
			_label.Text = "Enter your name:";
			_text.Text = "gui.cs";
			_button.Text = "Say Hello";
			_button.Clicked += NarrowMessage;
			_labelR.X = Pos.AnchorEnd (19);
			_labelR.Y = 18;
			_labelR.Text = "This is a test text";
			_labelV.X = Pos.AnchorEnd (19);
			_labelV.Y = Pos.Bottom (_labelR);
			_labelV.Text = "This is a test text";
			_win.Title = "HACC Demo";
			_lastRunesUsed = "Narrow";
			Application.Refresh ();
		}

		private void WideRunes ()
		{
			UnsetClickedEvent ();
			_label.Text = "あなたの名前を入力してください：";
			_text.Text = "ティラミス";
			_button.Text = "こんにちはと言う";
			_button.Clicked += WideMessage;
			_labelR.X = Pos.AnchorEnd (29);
			_labelR.Y = 18;
			_labelR.Text = "あなたの名前を入力してください";
			_labelV.X = Pos.AnchorEnd (29);
			_labelV.Y = Pos.Bottom (_labelR);
			_labelV.Text = "あなたの名前を入力してください";
			_win.Title = "デモエムポンズ";
			_lastRunesUsed = "Wide";
			Application.Refresh ();
		}

		public override void Run ()
		{
		}
	}
}