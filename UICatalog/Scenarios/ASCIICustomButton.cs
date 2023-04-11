using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "ASCIICustomButtonTest", Description: "ASCIICustomButton sample")]
	[ScenarioCategory ("Controls")]
	public class ASCIICustomButtonTest : Scenario {
		private static bool smallerWindow;
		private ScrollViewTestWindow scrollViewTestWindow;
		private MenuItem miSmallerWindow;

		public override void Init ()
		{
			Application.Init ();
			scrollViewTestWindow = new ScrollViewTestWindow ();
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem("Window Size", new MenuItem [] {
					miSmallerWindow = new MenuItem ("Smaller Window", "", ChangeWindowSize) {
						CheckType = MenuItemCheckStyle.Checked
					},
					null,
					new MenuItem("Quit", "",() => Application.RequestStop(),null,null, Application.QuitKey)
				})
			});
			Application.Top.Add (menu, scrollViewTestWindow);
			Application.Run ();
		}

		private void ChangeWindowSize ()
		{
			smallerWindow = (bool)(miSmallerWindow.Checked = !miSmallerWindow.Checked);
			scrollViewTestWindow.Dispose ();
			Application.Top.Remove (scrollViewTestWindow);
			scrollViewTestWindow = new ScrollViewTestWindow ();
			Application.Top.Add (scrollViewTestWindow);
		}

		public override void Run ()
		{
		}

		public class ASCIICustomButton : Button {
			public string Description => $"Description of: {id}";

			public event Action<ASCIICustomButton> PointerEnter;

			private Label fill;
			private FrameView border;
			private string id;

			public ASCIICustomButton (string text, Pos x, Pos y, int width, int height) : base (text)
			{
				CustomInitialize ("", text, x, y, width, height);
			}

			public ASCIICustomButton (string id, string text, Pos x, Pos y, int width, int height) : base (text)
			{
				CustomInitialize (id, text, x, y, width, height);
			}

			private void CustomInitialize (string id, string text, Pos x, Pos y, int width, int height)
			{
				this.id = id;
				X = x;
				Y = y;

				Frame = new Rect {
					Width = width,
					Height = height
				};

				border = new FrameView () {
					Width = width,
					Height = height
				};

				AutoSize = false;

				var fillText = new System.Text.StringBuilder ();
				for (int i = 0; i < Bounds.Height; i++) {
					if (i > 0) {
						fillText.AppendLine ("");
					}
					for (int j = 0; j < Bounds.Width; j++) {
						fillText.Append ("█");
					}
				}

				fill = new Label (fillText.ToString ()) {
					Visible = false,
					CanFocus = false
				};

				var title = new Label (text) {
					X = Pos.Center (),
					Y = Pos.Center (),
				};

				border.MouseClick += This_MouseClick;
				// BUGBUG: v2 This uses internal knowledge of FrameView an breaks in v2 where FrameView does not have a ContentView
				//border.Subviews [0].MouseClick += This_MouseClick;
				fill.MouseClick += This_MouseClick;
				title.MouseClick += This_MouseClick;

				Add (border, fill, title);
			}

			private void This_MouseClick (object sender, MouseEventEventArgs obj)
			{
				OnMouseEvent (obj.MouseEvent);
			}

			public override bool OnMouseEvent (MouseEvent mouseEvent)
			{
				Debug.WriteLine ($"{mouseEvent.Flags}");
				if (mouseEvent.Flags == MouseFlags.Button1Clicked) {
					if (!HasFocus && SuperView != null) {
						if (!SuperView.HasFocus) {
							SuperView.SetFocus ();
						}
						SetFocus ();
						SetNeedsDisplay ();
					}

					OnClicked ();
					return true;
				}
				return base.OnMouseEvent (mouseEvent);
			}

			public override bool OnEnter (View view)
			{
				border.Visible = false;
				fill.Visible = true;
				PointerEnter.Invoke (this);
				view = this;
				return base.OnEnter (view);
			}

			public override bool OnLeave (View view)
			{
				border.Visible = true;
				fill.Visible = false;
				if (view == null)
					view = this;
				return base.OnLeave (view);
			}
		}

		public class ScrollViewTestWindow : Window {
			private List<Button> buttons;
			private const int BUTTONS_ON_PAGE = 7;
			private const int BUTTON_HEIGHT = 3;

			private ScrollView scrollView;
			private ASCIICustomButton selected;

			public ScrollViewTestWindow ()
			{
				Title = "ScrollViewTestWindow";

				Label titleLabel = null;
				if (smallerWindow) {
					Width = 80;
					Height = 25;

					scrollView = new ScrollView () {
						X = 3,
						Y = 1,
						Width = 24,
						Height = BUTTONS_ON_PAGE * BUTTON_HEIGHT,
						ShowVerticalScrollIndicator = true,
						ShowHorizontalScrollIndicator = false
					};
				} else {
					Width = Dim.Fill ();
					Height = Dim.Fill ();

					titleLabel = new Label ("DOCUMENTS") {
						X = 0,
						Y = 0
					};

					scrollView = new ScrollView () {
						X = 0,
						Y = 1,
						Width = 27,
						Height = BUTTONS_ON_PAGE * BUTTON_HEIGHT,
						ShowVerticalScrollIndicator = true,
						ShowHorizontalScrollIndicator = false
					};
				}

				scrollView.ClearKeybindings ();

				buttons = new List<Button> ();
				Button prevButton = null;
				int count = 20;
				for (int j = 0; j < count; j++) {
					Pos yPos = prevButton == null ? 0 : Pos.Bottom (prevButton);
					var button = new ASCIICustomButton (j.ToString (), $"section {j}", 0, yPos, 25, BUTTON_HEIGHT);
					button.Id = $"button{j}";
					button.Clicked += Button_Clicked;
					button.PointerEnter += Button_PointerEnter;
					button.MouseClick += Button_MouseClick;
					button.KeyPress += Button_KeyPress;
					scrollView.Add (button);
					buttons.Add (button);
					prevButton = button;
				}

				var closeButton = new ASCIICustomButton ("close", "Close", 0, Pos.Bottom (prevButton), 25, BUTTON_HEIGHT);
				closeButton.Clicked += Button_Clicked;
				closeButton.PointerEnter += Button_PointerEnter;
				closeButton.MouseClick += Button_MouseClick;
				closeButton.KeyPress += Button_KeyPress;
				scrollView.Add (closeButton);
				buttons.Add (closeButton);

				var pages = buttons.Count / BUTTONS_ON_PAGE;
				if (buttons.Count % BUTTONS_ON_PAGE > 0)
					pages++;

				scrollView.ContentSize = new Size (25, pages * BUTTONS_ON_PAGE * BUTTON_HEIGHT);
				if (smallerWindow) {
					Add (scrollView);
				} else {
					Add (titleLabel, scrollView);
				}
			}

			private void Button_KeyPress (object sender, KeyEventEventArgs obj)
			{
				switch (obj.KeyEvent.Key) {
				case Key.End:
					scrollView.ContentOffset = new Point (scrollView.ContentOffset.X,
						 -(scrollView.ContentSize.Height - scrollView.Frame.Height
						 + (scrollView.ShowHorizontalScrollIndicator ? 1 : 0)));
					obj.Handled = true;
					return;
				case Key.Home:
					scrollView.ContentOffset = new Point (scrollView.ContentOffset.X, 0);
					obj.Handled = true;
					return;
				case Key.PageDown:
					scrollView.ContentOffset = new Point (scrollView.ContentOffset.X,
						 Math.Max (scrollView.ContentOffset.Y - scrollView.Frame.Height,
						 -(scrollView.ContentSize.Height - scrollView.Frame.Height
						 + (scrollView.ShowHorizontalScrollIndicator ? 1 : 0))));
					obj.Handled = true;
					return;
				case Key.PageUp:
					scrollView.ContentOffset = new Point (scrollView.ContentOffset.X,
						 Math.Min (scrollView.ContentOffset.Y + scrollView.Frame.Height, 0));
					obj.Handled = true;
					return;
				}
			}

			private void Button_MouseClick (object sender, MouseEventEventArgs obj)
			{
				if (obj.MouseEvent.Flags == MouseFlags.WheeledDown) {
					scrollView.ContentOffset = new Point (scrollView.ContentOffset.X,
						scrollView.ContentOffset.Y - BUTTON_HEIGHT);
					obj.Handled = true;
				} else if (obj.MouseEvent.Flags == MouseFlags.WheeledUp) {
					scrollView.ContentOffset = new Point (scrollView.ContentOffset.X,
						Math.Min (scrollView.ContentOffset.Y + BUTTON_HEIGHT, 0));
					obj.Handled = true;
				}
			}

			private void Button_Clicked (object sender, EventArgs e)
			{
				MessageBox.Query ("Button clicked.", $"'{selected.Text}' clicked!", "Ok");
				if (selected.Text == "Close") {
					Application.RequestStop ();
				}
			}

			private void Button_PointerEnter (ASCIICustomButton obj)
			{
				bool? moveDown;
				if (obj.Frame.Y > selected?.Frame.Y) {
					moveDown = true;
				} else if (obj.Frame.Y < selected?.Frame.Y) {
					moveDown = false;
				} else {
					moveDown = null;
				}
				var offSet = selected != null ? obj.Frame.Y - selected.Frame.Y + (-scrollView.ContentOffset.Y % BUTTON_HEIGHT) : 0;
				selected = obj;
				if (moveDown == true && selected.Frame.Y + scrollView.ContentOffset.Y + BUTTON_HEIGHT >= scrollView.Frame.Height && offSet != BUTTON_HEIGHT) {
					scrollView.ContentOffset = new Point (scrollView.ContentOffset.X,
						Math.Min (scrollView.ContentOffset.Y - BUTTON_HEIGHT, -(selected.Frame.Y - scrollView.Frame.Height + BUTTON_HEIGHT)));
				} else if (moveDown == true && selected.Frame.Y + scrollView.ContentOffset.Y >= scrollView.Frame.Height) {
					scrollView.ContentOffset = new Point (scrollView.ContentOffset.X,
						scrollView.ContentOffset.Y - BUTTON_HEIGHT);
				} else if (moveDown == true && selected.Frame.Y + scrollView.ContentOffset.Y < 0) {
					scrollView.ContentOffset = new Point (scrollView.ContentOffset.X,
						-selected.Frame.Y);
				} else if (moveDown == false && selected.Frame.Y < -scrollView.ContentOffset.Y) {
					scrollView.ContentOffset = new Point (scrollView.ContentOffset.X,
						Math.Max (scrollView.ContentOffset.Y + BUTTON_HEIGHT, selected.Frame.Y));
				} else if (moveDown == false && selected.Frame.Y + scrollView.ContentOffset.Y > scrollView.Frame.Height) {
					scrollView.ContentOffset = new Point (scrollView.ContentOffset.X,
						 -(selected.Frame.Y - scrollView.Frame.Height + BUTTON_HEIGHT));
				}
			}
		}
	}
}