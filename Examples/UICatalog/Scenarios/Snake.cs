using System.Diagnostics;
using System.Text;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Snake", "The game of apple eating.")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Drawing")]
[ScenarioCategory ("Games")]
public class Snake : Scenario
{
    private bool _isDisposed;

    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();

        using Window win = new () { Title = GetQuitKeyAndName () };

        var state = new SnakeState ();

        state.Reset (60, 20);

        var snakeView = new SnakeView (state) { Width = state.Width, Height = state.Height };

        win.Add (snakeView);

        var sw = new Stopwatch ();

        Task.Run (
                  () =>
                  {
                      while (!_isDisposed)
                      {
                          sw.Restart ();

                          if (state.AdvanceState ())
                          {
                              // When updating from a Thread/Task always use Invoke
                              snakeView.App?.Invoke (() => { snakeView.SetNeedsDraw (); });
                          }

                          long wait = state.SleepAfterAdvancingState - sw.ElapsedMilliseconds;

                          if (wait > 0)
                          {
                              Task.Delay ((int)wait).Wait ();
                          }
                      }
                  }
                 );

        app.Run (win);
    }

    protected override void Dispose (bool disposing)
    {
        _isDisposed = true;
        base.Dispose (disposing);
    }

    private enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    private class SnakeState
    {
        private const int APPLE_GROW_RATE = 5;
        private const int MAX_SPEED = 20;
        private const int STARTING_LENGTH = 10;
        private const int STARTING_SPEED = 50;
        private int _step;

        /// <summary>Current position of the Apple that the snake has to eat.</summary>
        public Point Apple { get; private set; }

        private Direction CurrentDirection { get; set; }

        /// <summary>Position of the snakes head</summary>
        private Point Head => Snake.Last ();

        public int Height { get; private set; }
        public Direction PlannedDirection { get; set; }
        public int SleepAfterAdvancingState { get; private set; } = STARTING_SPEED;
        public List<Point> Snake { get; private set; }
        public int Width { get; private set; }

        private void GrowSnake ()
        {
            Point tail = Snake.First ();
            Snake.Insert (0, tail);
        }

        public void GrowSnake (int amount)
        {
            for (var i = 0; i < amount; i++)
            {
                GrowSnake ();
            }
        }

        internal bool AdvanceState ()
        {
            _step++;

            if (_step < GetStepVelocity ())
            {
                return false;
            }

            _step = 0;

            UpdateDirection ();

            Point newHead = GetNewHeadPoint ();

            Snake.RemoveAt (0);
            Snake.Add (newHead);

            if (IsDeath (newHead))
            {
                GameOver ();
            }

            if (newHead == Apple)
            {
                GrowSnake (APPLE_GROW_RATE);
                Apple = GetNewRandomApplePoint ();

                var delta = 5;

                if (SleepAfterAdvancingState < 40)
                {
                    delta = 3;
                }

                if (SleepAfterAdvancingState < 30)
                {
                    delta = 2;
                }

                SleepAfterAdvancingState = Math.Max (MAX_SPEED, SleepAfterAdvancingState - delta);
            }

            return true;
        }

        /// <summary>Restarts the game with the given canvas size</summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        internal void Reset (int width, int height)
        {
            if (width < 5 || height < 5)
            {
                return;
            }

            Width = width;
            Height = height;

            var middle = new Point (width / 2, height / 2);

            // Start snake with a length of 2
            Snake = new () { middle, middle };
            Apple = GetNewRandomApplePoint ();

            SleepAfterAdvancingState = STARTING_SPEED;

            GrowSnake (STARTING_LENGTH);
        }

        private bool AreOpposites (Direction a, Direction b)
        {
            switch (a)
            {
                case Direction.Left: return b == Direction.Right;
                case Direction.Right: return b == Direction.Left;
                case Direction.Up: return b == Direction.Down;
                case Direction.Down: return b == Direction.Up;
            }

            return false;
        }

        private void GameOver () { Reset (Width, Height); }

        private Point GetNewHeadPoint ()
        {
            switch (CurrentDirection)
            {
                case Direction.Left:
                    return new (Head.X - 1, Head.Y);

                case Direction.Right:
                    return new (Head.X + 1, Head.Y);

                case Direction.Up:
                    return new (Head.X, Head.Y - 1);

                case Direction.Down:
                    return new (Head.X, Head.Y + 1);
            }

            throw new ("Unknown direction");
        }

        private Point GetNewRandomApplePoint ()
        {
            var r = new Random ();

            for (var i = 0; i < 1000; i++)
            {
                int x = r.Next (0, Width);
                int y = r.Next (0, Height);

                var p = new Point (x, y);

                if (p == Head)
                {
                    continue;
                }

                if (IsDeath (p))
                {
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

        private int GetStepVelocity ()
        {
            if (CurrentDirection == Direction.Left || CurrentDirection == Direction.Right)
            {
                return 1;
            }

            return 2;
        }

        private bool IsDeath (Point p)
        {
            if (p.X <= 0 || p.X >= Width - 1)
            {
                return true;
            }

            if (p.Y <= 0 || p.Y >= Height - 1)
            {
                return true;
            }

            if (Snake.Take (Snake.Count - 1).Contains (p))
            {
                return true;
            }

            return false;
        }

        private void UpdateDirection ()
        {
            if (!AreOpposites (CurrentDirection, PlannedDirection))
            {
                CurrentDirection = PlannedDirection;
            }
        }
    }

    private class SnakeView : View
    {
        private Rune _appleRune = Glyphs.Apple;
        private readonly Attribute _red = new (Color.Red, Color.Black);
        private readonly Attribute _white = new (Color.White, Color.Black);

        public SnakeView (SnakeState state)
        {
            State = state;
            CanFocus = true;

            base.SetScheme (new (_white));

            Initialized += (_, _) =>
                           {
                               if (App?.Driver is { } driver && !driver.IsRuneSupported (_appleRune))
                               {
                                   _appleRune = Glyphs.AppleBMP;
                               }
                           };

            KeyBindings.Add (Key.CursorLeft, Command.Left);
            KeyBindings.Add (Key.CursorRight, Command.Right);
            KeyBindings.Add (Key.CursorUp, Command.Up);
            KeyBindings.Add (Key.CursorDown, Command.Down);

            AddCommand (Command.Left, () => SetDirection (Direction.Left));
            AddCommand (Command.Right, () => SetDirection (Direction.Right));
            AddCommand (Command.Up, () => SetDirection (Direction.Up));
            AddCommand (Command.Down, () => SetDirection (Direction.Down));

            return;

            bool? SetDirection (Direction direction)
            {
                State.PlannedDirection = direction;

                return true;
            }
        }

        private SnakeState State { get; }

        protected override bool OnDrawingContent (DrawContext context)
        {
            SetAttribute (_white);
            ClearViewport ();

            var canvas = new LineCanvas ();

            canvas.AddLine (Point.Empty, State.Width, Orientation.Horizontal, LineStyle.Double);
            canvas.AddLine (Point.Empty, State.Height, Orientation.Vertical, LineStyle.Double);
            canvas.AddLine (new (0, State.Height - 1), State.Width, Orientation.Horizontal, LineStyle.Double);
            canvas.AddLine (new (State.Width - 1, 0), State.Height, Orientation.Vertical, LineStyle.Double);

            for (var i = 1; i < State.Snake.Count; i++)
            {
                Point pt1 = State.Snake [i - 1];
                Point pt2 = State.Snake [i];

                Orientation orientation = pt1.X == pt2.X ? Orientation.Vertical : Orientation.Horizontal;

                int length = orientation == Orientation.Horizontal ? pt1.X > pt2.X ? 2 : -2 :
                             pt1.Y > pt2.Y ? 2 : -2;

                canvas.AddLine (
                                pt2,
                                length,
                                orientation,
                                LineStyle.Single
                               );
            }

            foreach (KeyValuePair<Point, Rune> p in canvas.GetMap (Viewport))
            {
                AddRune (p.Key.X, p.Key.Y, p.Value);
            }

            SetAttribute (_red);
            AddRune (State.Apple.X, State.Apple.Y, _appleRune);
            SetAttribute (_white);

            return true;
        }
    }
}
