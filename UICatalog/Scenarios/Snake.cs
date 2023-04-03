using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Terminal.Gui;
using Terminal.Gui.Graphs;
using Attribute = Terminal.Gui.Attribute;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Snake", Description: "The game of apple eating.")]
	[ScenarioCategory ("Colors")]
	public class Snake : Scenario {
		private bool isDisposed;

		public override void Setup ()
		{
			base.Setup ();

			var state = new SnakeState ();

			state.Reset (60, 20);

			var snakeView = new SnakeView (state) {
				Width = state.Width,
				Height = state.Height
			};


			Win.Add (snakeView);

			Stopwatch sw = new Stopwatch ();

			Task.Run (() => {
				while (!isDisposed) {

					sw.Restart ();

					if (state.AdvanceState ()) {

						// When updating from a Thread/Task always use Invoke
						Application.MainLoop?.Invoke (() => {
							snakeView.SetNeedsDisplay ();
						});
					}

					var wait = state.SleepAfterAdvancingState - sw.ElapsedMilliseconds;

					if (wait > 0) {
						Task.Delay ((int)wait).Wait ();
					}
				}
			});
		}

		protected override void Dispose (bool disposing)
		{
			isDisposed = true;
			base.Dispose (disposing);
		}

		private class SnakeView : View {

			private Attribute red = new Terminal.Gui.Attribute (Color.Red,Color.Black);
			private Attribute white = new Terminal.Gui.Attribute (Color.White, Color.Black);

			public SnakeState State { get; }

			public SnakeView (SnakeState state)
			{
				State = state;
				CanFocus = true;

				ColorScheme = new ColorScheme {
					Normal = white,
					Focus = white,
					HotNormal = white,
					HotFocus = white,
					Disabled = white
				};
			}

			public override void Redraw (Rect bounds)
			{
				base.Redraw (bounds);

				Driver.SetAttribute (white);
				Clear ();

				var canvas = new LineCanvas ();

				canvas.AddLine (new Point (0, 0), State.Width - 1, Orientation.Horizontal, BorderStyle.Double);
				canvas.AddLine (new Point (0, 0), State.Height - 1, Orientation.Vertical, BorderStyle.Double);
				canvas.AddLine (new Point (0, State.Height - 1), State.Width - 1, Orientation.Horizontal, BorderStyle.Double);
				canvas.AddLine (new Point (State.Width - 1, 0), State.Height - 1, Orientation.Vertical, BorderStyle.Double);

				for (int i = 1; i < State.Snake.Count; i++) {

					var pt1 = State.Snake [i - 1];
					var pt2 = State.Snake [i];

					var orientation = pt1.X == pt2.X ? Orientation.Vertical : Orientation.Horizontal;
					var length = orientation == Orientation.Horizontal
						? pt1.X > pt2.X ? 1 : -1
						: pt1.Y > pt2.Y ? 1 : -1;

					canvas.AddLine (
						pt2,
						length,
						orientation,
						BorderStyle.Single);

				}

				foreach(var p in canvas.GenerateImage (bounds)) {
					AddRune (p.Key.X, p.Key.Y, p.Value);
				}


				Driver.SetAttribute (red);
				AddRune (State.Apple.X, State.Apple.Y, 'A');
				Driver.SetAttribute (white);
			}
			public override bool OnKeyDown (KeyEvent keyEvent)
			{
				if (keyEvent.Key == Key.CursorUp) {
					State.PlannedDirection = Direction.Up;
					return true;
				}
				if (keyEvent.Key == Key.CursorDown) {
					State.PlannedDirection = Direction.Down;
					return true;
				}
				if (keyEvent.Key == Key.CursorLeft) {
					State.PlannedDirection = Direction.Left;
					return true;
				}
				if (keyEvent.Key == Key.CursorRight) {
					State.PlannedDirection = Direction.Right;
					return true;
				}

				return false;
			}
		}
		private class SnakeState {

			public const int StartingLength = 10;
			public const int AppleGrowRate = 5;
			public const int StartingSpeed = 50;
			public const int MaxSpeed = 20;

			public int Width { get; private set; }
			public int Height { get; private set; }

			/// <summary>
			/// Position of the snakes head
			/// </summary>
			public Point Head => Snake.Last ();

			/// <summary>
			/// Current position of the Apple that the snake has to eat.
			/// </summary>
			public Point Apple { get; private set; }

			public Direction CurrentDirection { get; private set; }
			public Direction PlannedDirection { get; set; }

			public List<Point> Snake { get; private set; }

			public int SleepAfterAdvancingState { get; private set; } = StartingSpeed;

			int step;

			internal bool AdvanceState ()
			{
				step++;

				if (step < GetStepVelocity ()) {
					return false;
				}

				step = 0;

				UpdateDirection ();

				var newHead = GetNewHeadPoint ();

				Snake.RemoveAt (0);
				Snake.Add (newHead);

				if (IsDeath (newHead)) {
					GameOver ();
				}

				if (newHead == Apple) {
					GrowSnake (AppleGrowRate);
					Apple = GetNewRandomApplePoint ();

					var delta = 5;
					if(SleepAfterAdvancingState < 40) {
						delta = 3;
					}
					if (SleepAfterAdvancingState < 30) {
						delta = 2;
					}
					SleepAfterAdvancingState = Math.Max (MaxSpeed, SleepAfterAdvancingState - delta);
				}

				return true;
			}

			private int GetStepVelocity ()
			{
				if (CurrentDirection == Direction.Left || CurrentDirection == Direction.Right) {
					return 1;
				}

				return 2;
			}

			public void GrowSnake ()
			{
				var tail = Snake.First ();
				Snake.Insert (0, tail);
			}
			public void GrowSnake (int amount)
			{
				for (int i = 0; i < amount; i++) {
					GrowSnake ();
				}
			}

			private void UpdateDirection ()
			{
				if (!AreOpposites (CurrentDirection, PlannedDirection)) {
					CurrentDirection = PlannedDirection;
				}
			}

			private bool AreOpposites (Direction a, Direction b)
			{
				switch (a) {
				case Direction.Left: return b == Direction.Right;
				case Direction.Right: return b == Direction.Left;
				case Direction.Up: return b == Direction.Down;
				case Direction.Down: return b == Direction.Up;
				}

				return false;
			}

			private void GameOver ()
			{
				Reset (Width, Height);
			}

			private Point GetNewHeadPoint ()
			{
				switch (CurrentDirection) {
				case Direction.Left:
					return new Point (Head.X - 1, Head.Y);

				case Direction.Right:
					return new Point (Head.X + 1, Head.Y);

				case Direction.Up:
					return new Point (Head.X, Head.Y - 1);

				case Direction.Down:
					return new Point (Head.X, Head.Y + 1);
				}

				throw new Exception ("Unknown direction");
			}

			/// <summary>
			/// Restarts the game with the given canvas size
			/// </summary>
			/// <param name="width"></param>
			/// <param name="height"></param>
			internal void Reset (int width, int height)
			{
				if (width < 5 || height < 5) {
					return;
				}

				Width = width;
				Height = height;

				var middle = new Point (width / 2, height / 2);

				// Start snake with a length of 2
				Snake = new List<Point> { middle, middle };
				Apple = GetNewRandomApplePoint ();

				SleepAfterAdvancingState = StartingSpeed;

				GrowSnake (StartingLength);
			}

			private Point GetNewRandomApplePoint ()
			{
				Random r = new Random ();

				for (int i = 0; i < 1000; i++) {
					var x = r.Next (0, Width);
					var y = r.Next (0, Height);

					var p = new Point (x, y);

					if (p == Head) {
						continue;
					}

					if (IsDeath (p)) {
						continue;
					}

					return p;
				}

				// Game is won or we are unable to generate a valid apple
				// point after 1000 attempts.  Maybe screen size is very small
				// or something.  Either way restart the game.
				Reset (Width, Height);
				return Apple;
			}

			private bool IsDeath (Point p)
			{
				if (p.X <= 0 || p.X >= Width - 1) {
					return true;
				}

				if (p.Y <= 0 || p.Y >= Height - 1) {
					return true;
				}

				if (Snake.Take (Snake.Count - 1).Contains (p))
					return true;

				return false;
			}
		}
		private enum Direction {
			Up,
			Down,
			Left,
			Right
		}
	}
}