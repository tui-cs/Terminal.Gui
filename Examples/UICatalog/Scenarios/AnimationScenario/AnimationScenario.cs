#nullable enable
using System.Diagnostics;
using System.Reflection;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Animation", "Demonstration of how to render animated images with threading.")]
[ScenarioCategory ("Threading")]
[ScenarioCategory ("Drawing")]
public class AnimationScenario : Scenario
{
    private ImageView? _imageView;

    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window win = new ()
        {
            Title = GetQuitKeyAndName (),
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        _imageView = new ImageView { Width = Dim.Fill (), Height = Dim.Fill ()! - 2 };

        win.Add (_imageView);

        var lbl = new Label { Y = Pos.AnchorEnd (), Text = "Image by Wikiscient" };
        win.Add (lbl);

        Link link = new ()
        {
            X = Pos.AnchorEnd (),
            Y = Pos.AnchorEnd (),
            Url = "https://commons.wikimedia.org/wiki/File:Spinning_globe.gif"
        };
        app.ToolTips!.SetToolTip (link, () => link.Url);
        win.Add (link);

        // Start the animation after the window is initialized
        win.Initialized += OnWinOnInitialized;

        app.Run (win);
    }

    private void OnWinOnInitialized (object? sender, EventArgs args)
    {
        DirectoryInfo dir;

        string assemblyLocation = Assembly.GetExecutingAssembly ().Location;

        if (!string.IsNullOrEmpty (assemblyLocation))
        {
            dir = new DirectoryInfo (Path.GetDirectoryName (assemblyLocation) ?? string.Empty);
        }
        else
        {
            dir = new DirectoryInfo (AppContext.BaseDirectory);
        }

        var f = new FileInfo (Path.Combine (dir.FullName, "Scenarios/AnimationScenario", "Spinning_globe_dark_small.gif"));

        if (!f.Exists)
        {
            Debug.WriteLine ($"Could not find {f.FullName}");
            MessageBox.ErrorQuery (_imageView?.App!, "Could not find gif", $"Could not find\n{f.FullName}", "Ok");

            return;
        }

        _imageView!.SetImage (Image.Load<Rgba32> (File.ReadAllBytes (f.FullName)));

        Task.Run (() =>
                  {
                      while (_imageView?.App?.Initialized == true)
                      {
                          // When updating from a Thread/Task always use Invoke
                          _imageView?.App?.Invoke (_ =>
                                                   {
                                                       _imageView?.NextFrame ();
                                                       _imageView?.SetNeedsDraw ();
                                                   });

                          Task.Delay (100).Wait ();
                      }
                  });
    }

    // This is a C# port of https://github.com/andraaspar/bitmap-to-braille by Andraaspar

    /// <summary>Renders an image as unicode Braille.</summary>
    public class BitmapToBraille (int widthPixels, int heightPixels, Func<int, int, bool> pixelIsLit)
    {
        public const int CHAR_HEIGHT = 4;
        public const int CHAR_WIDTH = 2;

        private const string CHARS =
            " ⠁⠂⠃⠄⠅⠆⠇⡀⡁⡂⡃⡄⡅⡆⡇⠈⠉⠊⠋⠌⠍⠎⠏⡈⡉⡊⡋⡌⡍⡎⡏⠐⠑⠒⠓⠔⠕⠖⠗⡐⡑⡒⡓⡔⡕⡖⡗⠘⠙⠚⠛⠜⠝⠞⠟⡘⡙⡚⡛⡜⡝⡞⡟⠠⠡⠢⠣⠤⠥⠦⠧⡠⡡⡢⡣⡤⡥⡦⡧⠨⠩⠪⠫⠬⠭⠮⠯⡨⡩⡪⡫⡬⡭⡮⡯⠰⠱⠲⠳⠴⠵⠶⠷⡰⡱⡲⡳⡴⡵⡶⡷⠸⠹⠺⠻⠼⠽⠾⠿⡸⡹⡺⡻⡼⡽⡾⡿⢀⢁⢂⢃⢄⢅⢆⢇⣀⣁⣂⣃⣄⣅⣆⣇⢈⢉⢊⢋⢌⢍⢎⢏⣈⣉⣊⣋⣌⣍⣎⣏⢐⢑⢒⢓⢔⢕⢖⢗⣐⣑⣒⣓⣔⣕⣖⣗⢘⢙⢚⢛⢜⢝⢞⢟⣘⣙⣚⣛⣜⣝⣞⣟⢠⢡⢢⢣⢤⢥⢦⢧⣠⣡⣢⣣⣤⣥⣦⣧⢨⢩⢪⢫⢬⢭⢮⢯⣨⣩⣪⣫⣬⣭⣮⣯⢰⢱⢲⢳⢴⢵⢶⢷⣰⣱⣲⣳⣴⣵⣶⣷⢸⢹⢺⢻⢼⢽⢾⢿⣸⣹⣺⣻⣼⣽⣾⣿";

        public int HeightPixels { get; } = heightPixels;
        public Func<int, int, bool> PixelIsLit { get; } = pixelIsLit;
        public int WidthPixels { get; } = widthPixels;

        public string GenerateImage ()
        {
            var imageHeightChars = (int)Math.Ceiling ((double)HeightPixels / CHAR_HEIGHT);
            var imageWidthChars = (int)Math.Ceiling ((double)WidthPixels / CHAR_WIDTH);

            var result = new StringBuilder ();

            for (var y = 0; y < imageHeightChars; y++)
            {
                for (var x = 0; x < imageWidthChars; x++)
                {
                    int baseX = x * CHAR_WIDTH;
                    int baseY = y * CHAR_HEIGHT;

                    var charIndex = 0;
                    var value = 1;

                    for (var charX = 0; charX < CHAR_WIDTH; charX++)
                    {
                        for (var charY = 0; charY < CHAR_HEIGHT; charY++)
                        {
                            int bitmapX = baseX + charX;
                            int bitmapY = baseY + charY;

                            bool pixelExists = bitmapX < WidthPixels && bitmapY < HeightPixels;

                            if (pixelExists && PixelIsLit (bitmapX, bitmapY))
                            {
                                charIndex += value;
                            }

                            value *= 2;
                        }
                    }

                    result.Append (CHARS [charIndex]);
                }

                result.Append ('\n');
            }

            return result.ToString ().TrimEnd ();
        }
    }

    private class ImageView : View
    {
        private string []? _brailleCache;
        private int _currentFrame;
        private int _frameCount;
        private Image<Rgba32> []? _fullResImages;
        private Image<Rgba32> []? _matchSizes;
        private Rectangle _oldSize = Rectangle.Empty;
        public void NextFrame () => _currentFrame = (_currentFrame + 1) % _frameCount;

        protected override bool OnDrawingContent (DrawContext? context)
        {
            if (_frameCount == 0)
            {
                return false;
            }

            if (_oldSize != Viewport)
            {
                // Invalidate cached images now size has changed
                _matchSizes = new Image<Rgba32> [_frameCount];
                _brailleCache = new string [_frameCount];
                _oldSize = Viewport;
            }

            Image<Rgba32>? imgScaled = _matchSizes? [_currentFrame];
            string? braille = _brailleCache? [_currentFrame];

            if (imgScaled == null)
            {
                Image<Rgba32>? imgFull = _fullResImages? [_currentFrame];

                // keep aspect ratio
                int newSize = Math.Min (Viewport.Width, Viewport.Height);

                // generate one
                if (_matchSizes is { } && imgFull is { })
                {
                    _matchSizes [_currentFrame] =
                        imgScaled = imgFull.Clone (x => x.Resize (newSize * BitmapToBraille.CHAR_HEIGHT, newSize * BitmapToBraille.CHAR_HEIGHT));
                }
            }

            if (braille == null && _brailleCache is { })
            {
                _brailleCache [_currentFrame] = braille = GetBraille (_matchSizes? [_currentFrame]!);
            }

            string []? lines = braille?.Split ('\n');

            for (var y = 0; y < lines!.Length; y++)
            {
                string line = lines [y];

                for (var x = 0; x < line.Length; x++)
                {
                    AddRune (x, y, (Rune)line [x]);
                }
            }

            return true;
        }

        internal void SetImage (Image<Rgba32> image)
        {
            _frameCount = image.Frames.Count;

            _fullResImages = new Image<Rgba32> [_frameCount];
            _matchSizes = new Image<Rgba32> [_frameCount];
            _brailleCache = new string [_frameCount];

            for (var i = 0; i < _frameCount - 1; i++)
            {
                _fullResImages [i] = image.Frames.ExportFrame (0);
            }

            _fullResImages [_frameCount - 1] = image;

            SetNeedsDraw ();
        }

        private string GetBraille (Image<Rgba32> img)
        {
            var braille = new BitmapToBraille (img.Width, img.Height, (x, y) => IsLit (img, x, y));

            return braille.GenerateImage ();
        }

        private bool IsLit (Image<Rgba32> img, int x, int y)
        {
            Rgba32 rgb = img [x, y];

            return rgb.R + rgb.G + rgb.B > 50;
        }
    }
}
