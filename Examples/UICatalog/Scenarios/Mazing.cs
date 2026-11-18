#nullable enable
using System.Text;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("A Mazing", "Illustrates how to make a basic maze game.")]
[ScenarioCategory ("Drawing")]
[ScenarioCategory ("Mouse and Keyboard")]
[ScenarioCategory ("Games")]
public class Mazing : Scenario
{
    private Window? _top;
    private MazeGenerator? _m;

    private List<Point>? _potions;
    private List<Point>? _goblins;
    private string? _message;
    private bool _dead;

    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        _top = new ();

        _m = new ();

        GenerateNpcs ();

        // Define the keys for movement
        _top.KeyBindings.Add (Key.CursorLeft, Command.Left);
        _top.KeyBindings.Add (Key.CursorRight, Command.Right);
        _top.KeyBindings.Add (Key.CursorUp, Command.Up);
        _top.KeyBindings.Add (Key.CursorDown, Command.Down);

        // Changing the key-bindings of a View is not allowed, however,
        // by default, Runnable doesn't bind any of our movement keys, so
        // we can take advantage of the CommandNotBound event to handle them
        // 
        // An alternative implementation would be to create a Runnable subclass that
        // calls AddCommand/KeyBindings.Add in the constructor. See the Snake game scenario
        // for an example.
        _top.CommandNotBound += TopCommandNotBound;

        _top.DrawingContent += (sender, _) =>
                               {
                                   if (sender is not Runnable top)
                                   {
                                       return;
                                   }

                                   // Build maze
                                   LineCanvas lc = new (_m.BuildWallLinesFromMaze ());

                                   // Print maze
                                   foreach (KeyValuePair<Point, Rune> p in lc.GetMap ())
                                   {
                                       top.Move (p.Key.X, p.Key.Y);
                                       top.AddRune (p.Value);
                                   }

                                   // Draw objects
                                   top.Move (_m.Start.X, _m.Start.Y);
                                   top.AddStr ("s");

                                   top.Move (_m.End.X, _m.End.Y);
                                   top.AddStr ("e");

                                   top.Move (_m.Player.X, _m.Player.Y);
                                   top.SetAttribute (new (Color.Cyan, top.GetAttributeForRole (VisualRole.Normal).Background));
                                   top.AddStr (_dead ? "x" : "@");

                                   // Draw goblins
                                   foreach (Point goblin in _goblins!)
                                   {
                                       top.Move (goblin.X, goblin.Y);
                                       top.SetAttribute (new (Color.Red, top.GetAttributeForRole (VisualRole.Normal).Background));
                                       top.AddStr ("G");
                                   }

                                   // Draw potions
                                   foreach (Point potion in _potions!)
                                   {
                                       top.Move (potion.X, potion.Y);
                                       top.SetAttribute (new (Color.Yellow, top.GetAttributeForRole (VisualRole.Normal).Background));
                                       top.AddStr ("p");
                                   }

                                   // Draw UI
                                   top.SetAttribute (top.GetAttributeForRole (VisualRole.Normal));

                                   Gradient g = new ([new (Color.Red), new (Color.BrightGreen)], [10]);
                                   top.Move (_m.MazeWidth + 1, 0);
                                   top.AddStr ("Name: Sir Flibble");
                                   top.Move (_m.MazeWidth + 1, 1);
                                   top.AddStr ("HP:");

                                   for (var i = 0; i < _m.PlayerHp; i++)
                                   {
                                       top.Move (_m.MazeWidth + 1 + "HP:".Length + i, 1);
                                       top.SetAttribute (new (g.GetColorAtFraction (i / 20f)));
                                       top.AddRune ('█');
                                   }

                                   top.SetAttribute (top.GetAttributeForRole (VisualRole.Normal));

                                   if (!string.IsNullOrWhiteSpace (_message))
                                   {
                                       top.Move (_m.MazeWidth + 2, 2);
                                       top.AddStr (_message);
                                   }
                               };

        app.Run (_top);
        _top.Dispose ();
    }

    private void GenerateNpcs ()
    {
        _goblins = _m?.GenerateSpawnLocations (3, []); // Generate 3 goblins
        _potions = _m?.GenerateSpawnLocations (3, _goblins!); // Generate 3 potions
    }

    private void TopCommandNotBound (object? sender, CommandEventArgs e)
    {
        if (_dead)
        {
            return;
        }

        Point newPos = _m!.Player;

        Command? command = e.Context?.Command;

        if (command == Command.Left)
        {
            newPos = _m.Player with { X = _m.Player.X - 1 };
        }

        if (command == Command.Right)
        {
            newPos = _m.Player with { X = _m.Player.X + 1 };
        }

        if (command == Command.Up)
        {
            newPos = _m.Player with { Y = _m.Player.Y - 1 };
        }

        if (command == Command.Down)
        {
            newPos = _m.Player with { Y = _m.Player.Y + 1 };
        }

        // Only move if in bounds and it's a path
        if (newPos.X >= 0 && newPos.X < _m._maze.GetLength (1) && newPos.Y >= 0 && newPos.Y < _m._maze.GetLength (0) && _m._maze [newPos.Y, newPos.X] == 0)
        {
            _m.Player = newPos;

            // Check if player is on a goblin
            if (_goblins!.Contains (_m.Player))
            {
                _message = "You fight a goblin!";
                _m.PlayerHp -= 5; // Decrease player's HP when attacked

                // Remove the goblin
                _goblins.Remove (_m.Player);

                // Check if player is dead
                if (_m.PlayerHp <= 0)
                {
                    _message = "You died!";
                    _top?.SetNeedsDraw (); // trigger redraw
                    _dead = true;

                    return; // Stop further action if dead
                }
            }
            else if (_potions!.Contains (_m.Player))
            {
                _message = "You drink a health potion!";
                _m.PlayerHp = Math.Min (20, _m.PlayerHp + 5); // increase player's HP when drinking potion

                // Remove the potion
                _potions.Remove (_m.Player);
            }
            else
            {
                _message = string.Empty;
            }

            _top?.SetNeedsDraw (); // trigger redraw
        }

        // Optional win condition:
        if (_m.Player == _m.End)
        {
            int hp = _m.PlayerHp;
            _m = new (); // Generate a new maze
            _m.PlayerHp = hp;
            GenerateNpcs ();
            _top?.SetNeedsDraw (); // trigger redraw
        }
    }
}

internal class MazeGenerator
{
    private const int WIDTH = 20;
    private const int HEIGHT = 10;
    public int [,] _maze;
    public Random Rand { get; } = new ();
    public Point Start { get; }
    public Point End { get; }
    public Point Player { get; set; }
    public int PlayerHp { get; set; } = 20;

    // Private accessors for width and height
    public int MazeWidth => WIDTH * 2 + 1;
    public int MazeHeight => HEIGHT * 2 + 1;

    public MazeGenerator ()
    {
        int w = WIDTH * 2 + 1;
        int h = HEIGHT * 2 + 1;
        _maze = new int [h, w];

        // Fill with walls
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            _maze [y, x] = 1;
        }

        // Start carving from a random odd cell
        int startX = Rand.Next (WIDTH) * 2 + 1;
        int startY = Rand.Next (HEIGHT) * 2 + 1;
        Carve (new (startX, startY));

        // Set random entrance
        Start = GetRandomEdgePoint (w, h, true);
        _maze [Start.Y, Start.X] = 0;
        Player = Start;

        // Set random exit (ensure it's not same as entrance)
        End = GetRandomEdgePoint (w, h, false, Start.X, Start.Y);
        _maze [End.Y, End.X] = 0;
    }

    public List<StraightLine> BuildWallLinesFromMaze ()
    {
        List<StraightLine> lines = new ();

        int h = _maze.GetLength (0);
        int w = _maze.GetLength (1);

        // Horizontal lines
        for (var y = 0; y < h; y++)
        {
            var x = 0;

            while (x < w)
            {
                if (_maze [y, x] == 1)
                {
                    int startX = x;

                    while (x < w && _maze [y, x] == 1)
                    {
                        x++;
                    }

                    int length = x - startX;

                    if (length > 1)
                    {
                        lines.Add (new (new (startX, y), length, Orientation.Horizontal, LineStyle.Single));
                    }
                }
                else
                {
                    x++;
                }
            }
        }

        // Vertical lines
        for (var x = 0; x < w; x++)
        {
            var y = 0;

            while (y < h)
            {
                if (_maze [y, x] == 1)
                {
                    int startY = y;

                    while (y < h && _maze [y, x] == 1)
                    {
                        y++;
                    }

                    int length = y - startY;
                    lines.Add (new (new (x, startY), length, Orientation.Vertical, LineStyle.Single));
                }
                else
                {
                    y++;
                }
            }
        }

        return lines;
    }

    public List<Point> GenerateSpawnLocations (int count, List<Point> exclude)
    {
        // Create a new copy of the list so we can track exclusions
        exclude = exclude.ToList ();

        List<Point> locations = new ();

        for (var i = 0; i < count; i++)
        {
            Point point;

            do
            {
                point = new (Rand.Next (1, WIDTH * 2), Rand.Next (1, HEIGHT * 2));
            }

            // Ensure the spawn point is not in the exclusion list and it's an open space (not a wall)
            while (exclude.Contains (point) || _maze [point.Y, point.X] != 0);

            exclude.Add (point); // Mark this location as occupied
            locations.Add (point); // Add the location to the list
        }

        return locations;
    }

    private void Carve (Point p)
    {
        _maze [p.Y, p.X] = 0;

        int [] [] dirs =
        {
            [0, -2],
            [0, 2],
            [-2, 0],
            [2, 0]
        };

        Shuffle (dirs);

        foreach (int [] dir in dirs)
        {
            int nx = p.X + dir [0], ny = p.Y + dir [1];

            if (nx > 0 && ny > 0 && nx < WIDTH * 2 && ny < HEIGHT * 2 && _maze [ny, nx] == 1)
            {
                _maze [p.Y + dir [1] / 2, p.X + dir [0] / 2] = 0;
                Carve (new (nx, ny));
            }
        }
    }

    private void Shuffle (int [] [] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Rand.Next (i + 1);
            int [] temp = array [i];
            array [i] = array [j];
            array [j] = temp;
        }
    }

    private Point GetRandomEdgePoint (int w, int h, bool isEntrance, int avoidX = -1, int avoidY = -1)
    {
        List<Point> candidates = [];

        for (var i = 1; i < h - 1; i += 2)
        {
            candidates.Add (new (0, i)); // Left edge
            candidates.Add (new (w - 1, i)); // Right edge
        }

        for (var i = 1; i < w - 1; i += 2)
        {
            candidates.Add (new (i, 0)); // Top edge
            candidates.Add (new (i, h - 1)); // Bottom edge
        }

        // Remove one if same as entrance
        if (!isEntrance)
        {
            candidates.RemoveAll (p => p.X == avoidX && p.Y == avoidY);
        }

        return candidates [Rand.Next (candidates.Count)];
    }
}
