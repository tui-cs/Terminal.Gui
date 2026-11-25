using System.Globalization;

namespace ViewsTests;

// Codex - GPT-6.1
public class TextValidateFieldActiveAttributeTests
{
    [Theory]
    [InlineData ("Date", TextStyle.None)]
    [InlineData ("Date", TextStyle.Bold | TextStyle.Italic)]
    [InlineData ("Time", TextStyle.Underline)]
    [InlineData ("Mask", TextStyle.Reverse)]
    [InlineData ("Regex", TextStyle.Italic)]
    public void InsertionPoint_Uses_Entire_Active_Attribute (string kind, TextStyle style)
    {
        using IApplication app = CreateApp (kind, out TextValidateField field);
        using View runnable = app.TopRunnableView!;
        Scheme scheme = CreateScheme (style);
        field.SetScheme (scheme);

        app.LayoutAndDraw ();

        AssertCells (app, field, 0, scheme.Active);
    }

    [Theory]
    [InlineData ("Date")]
    [InlineData ("Time")]
    [InlineData ("Mask")]
    [InlineData ("Regex")]
    public void InsertionPoint_Follows_Inherited_Scheme_Changes (string kind)
    {
        using IApplication app = CreateApp (kind, out TextValidateField field);
        using View runnable = app.TopRunnableView!;
        Scheme first = CreateScheme (TextStyle.Bold);
        field.SuperView!.SetScheme (first);
        app.LayoutAndDraw ();
        AssertCells (app, field, 0, first.Active);

        Scheme second = first with { Active = new Attribute (Color.Cyan, Color.Magenta, TextStyle.None) };
        field.SuperView.SetScheme (second);
        app.LayoutAndDraw ();

        AssertCells (app, field, 0, second.Active);
    }

    [Theory]
    [InlineData ("Date")]
    [InlineData ("Time")]
    [InlineData ("Mask")]
    public void InsertionPoint_Follows_Navigation_And_Skips_Separators (string kind)
    {
        using IApplication app = CreateApp (kind, out TextValidateField field);
        using View runnable = app.TopRunnableView!;
        Scheme scheme = CreateScheme (TextStyle.Bold);
        field.SetScheme (scheme);
        string originalText = field.Text;

        field.NewKeyDownEvent (Key.CursorRight);
        app.LayoutAndDraw ();
        AssertCells (app, field, 1, scheme.Active);

        field.NewKeyDownEvent (Key.CursorRight);
        app.LayoutAndDraw ();
        AssertCells (app, field, 3, scheme.Active);

        field.NewKeyDownEvent (Key.CursorLeft);
        app.LayoutAndDraw ();
        AssertCells (app, field, 1, scheme.Active);

        field.NewKeyDownEvent (Key.End);
        app.LayoutAndDraw ();
        AssertCells (app, field, field.Provider!.CursorEnd (), scheme.Active);

        // Moving beyond the last digit must leave no highlighted cell.
        field.NewKeyDownEvent (Key.CursorRight);
        app.LayoutAndDraw ();
        AssertCells (app, field, -1, scheme.Active);

        field.NewKeyDownEvent (Key.Home);
        app.LayoutAndDraw ();
        AssertCells (app, field, 0, scheme.Active);
        Assert.Equal (originalText, field.Text);
        Assert.True (field.IsValid);
    }

    [Theory]
    [InlineData ("Date")]
    [InlineData ("Time")]
    [InlineData ("Mask")]
    [InlineData ("Regex")]
    public void InsertionPoint_Highlight_Is_Removed_When_Focus_Is_Lost (string kind)
    {
        using IApplication app = CreateApp (kind, out TextValidateField field);
        using View runnable = app.TopRunnableView!;
        Scheme scheme = CreateScheme (TextStyle.Italic);
        field.SetScheme (scheme);
        app.LayoutAndDraw ();
        AssertCells (app, field, 0, scheme.Active);

        TextField other = new () { Y = 1, Width = 10 };
        field.SuperView!.Add (other);
        other.SetFocus ();
        Assert.True (other.HasFocus);
        app.LayoutAndDraw ();

        Assert.False (field.HasFocus);
        AssertCells (app, field, -1, scheme.Active);

        Assert.True (field.SetFocus ());
        app.LayoutAndDraw ();
        AssertCells (app, field, 0, scheme.Active);
    }

    private static Scheme CreateScheme (TextStyle style) => new ()
    {
        Normal = new Attribute (Color.White, Color.Black),
        Editable = new Attribute (Color.Green, Color.Blue, TextStyle.Underline),
        Focus = new Attribute (Color.Yellow, Color.Red, TextStyle.Underline),
        Active = new Attribute (Color.Magenta, Color.Cyan, style)
    };

    private static IApplication CreateApp (string kind, out TextValidateField field)
    {
        field = kind switch
                {
                    "Date" => new DateEditor { Format = CultureInfo.GetCultureInfo ("en-US").DateTimeFormat, Value = new DateTime (2024, 3, 15) },
                    "Time" => new TimeEditor { Format = CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat, Value = new TimeSpan (12, 34, 56) },
                    "Mask" => new TextValidateField { Provider = new NetMaskedTextProvider ("00/00") { Text = "1234" } },
                    "Regex" => new TextValidateField { Provider = new TextRegexProvider ("^[0-9]+$") { Text = "1234" }, Width = 5 },
                    _ => throw new ArgumentException ("Unknown editor kind.", nameof (kind))
                };

        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (30, 2);
        Runnable runnable = new () { Width = Dim.Fill (), Height = Dim.Fill () };
        runnable.Add (field);
        app.Begin (runnable);
        field.NewKeyDownEvent (Key.Home);
        Assert.True (field.HasFocus);

        return app;
    }

    private static void AssertCells (IApplication app, TextValidateField field, int activeColumn, Attribute active)
    {
        string displayText = field.Provider!.DisplayText;
        Attribute editable = field.GetAttributeForRole (VisualRole.Editable);

        for (int column = 0; column < field.Viewport.Width; column++)
        {
            Cell cell = app.Driver!.Contents! [0, column];
            Assert.Equal (column == activeColumn ? active : editable, cell.Attribute);
            Assert.Equal (column < displayText.Length ? displayText [column].ToString () : " ", cell.Grapheme);
        }
    }
}
