using System.Runtime.CompilerServices;
using System.Text;

namespace ViewBaseTests;

/// <summary>
///     Tests of the <see cref="View.Text"/> and <see cref="View.TextFormatter"/> properties.
/// </summary>
public class TextTests ()
{
    // TextFormatter.Size should be empty unless DimAuto is set or ContentSize is set
    [Theory]
    [InlineData ("", 0, 0)]
    [InlineData (" ", 0, 0)]
    [InlineData ("01234", 0, 0)]
    public void TextFormatter_Size_Default (string text, int expectedW, int expectedH)
    {
        var view = new View ();
        view.Text = text;
        view.Layout ();
        Assert.Equal (new (expectedW, expectedH), view.TextFormatter.ConstrainToSize);
    }

    // TextFormatter.Size should track ContentSize (without DimAuto)
    [Theory]
    [InlineData ("", 1, 1)]
    [InlineData (" ", 1, 1)]
    [InlineData ("01234", 1, 1)]
    public void TextFormatter_Size_Tracks_ContentSize (string text, int expectedW, int expectedH)
    {
        var view = new View ();
        view.SetContentSize (new (1, 1));
        view.Text = text;
        view.Layout ();
        Assert.Equal (new (expectedW, expectedH), view.TextFormatter.ConstrainToSize);
    }

    // Test that View.PreserveTrailingSpaces removes trailing spaces
    [Fact]
    public void PreserveTrailingSpaces_Removes_Trailing_Spaces ()
    {
        var view = new View { Text = "Hello World " };
        Assert.Equal ("Hello World ", view.TextFormatter.Text);

        view.TextFormatter.WordWrap = true;
        view.TextFormatter.ConstrainToSize = new (5, 3);

        view.PreserveTrailingSpaces = false;
        Assert.Equal ($"Hello{Environment.NewLine}World", view.TextFormatter.Format ());

        view.PreserveTrailingSpaces = true;
        Assert.Equal ($"Hello{Environment.NewLine} {Environment.NewLine}World", view.TextFormatter.Format ());
    }

    // View.PreserveTrailingSpaces Gets or sets whether trailing spaces at the end of word-wrapped lines are preserved
    // or not when <see cref="TextFormatter.WordWrap"/> is enabled.
    // If <see langword="true"/> trailing spaces at the end of wrapped lines will be removed when
    // <see cref = "Text" / > is formatted for display.The default is <see langword = "false" / >.
    [Fact]
    public void PreserveTrailingSpaces_Set_Get ()
    {
        var view = new View { Text = "Hello World" };

        Assert.False (view.PreserveTrailingSpaces);

        view.PreserveTrailingSpaces = true;
        Assert.True (view.PreserveTrailingSpaces);
    }

    // Setting TextFormatter DOES NOT update Text
    [Fact]
    public void SettingTextFormatterDoesNotUpdateText ()
    {
        var view = new View ();
        view.TextFormatter.Text = "Hello World";

        Assert.True (string.IsNullOrEmpty (view.Text));
    }

    // Setting Text updates TextFormatter
    [Fact]
    public void SettingTextUpdatesTextFormatter ()
    {
        var view = new View { Text = "Hello World" };

        Assert.Equal ("Hello World", view.Text);
        Assert.Equal ("Hello World", view.TextFormatter.Text);
    }

    // Setting Text does NOT set the HotKey
    [Fact]
    public void Text_Does_Not_Set_HotKey ()
    {
        var view = new View { HotKeySpecifier = (Rune)'_', Text = "_Hello World" };

        Assert.NotEqual (Key.H, view.HotKey);
    }

    // Test that TextFormatter is init only
    [Fact]
    public void TextFormatterIsInitOnly ()
    {
        var view = new View ();

        // Use reflection to ensure the TextFormatter property is `init` only
        Assert.Contains (
                         typeof (IsExternalInit),
                         typeof (View).GetMethod ("set_TextFormatter")
                                      ?.ReturnParameter.GetRequiredCustomModifiers ()!);
    }

    // Test that the Text property is set correctly.
    [Fact]
    public void TextProperty ()
    {
        var view = new View { Text = "Hello World" };

        Assert.Equal ("Hello World", view.Text);
    }

    // Test view.UpdateTextFormatterText overridden in a subclass updates TextFormatter.Text
    [Fact]
    public void UpdateTextFormatterText_Overridden ()
    {
        var view = new TestView { Text = "Hello World" };

        Assert.Equal ("Hello World", view.Text);
        Assert.Equal (">Hello World<", view.TextFormatter.Text);
    }

    private class TestView : View
    {
        protected override void UpdateTextFormatterText () { TextFormatter.Text = $">{Text}<"; }
    }

    [Fact]
    public void TextDirection_Horizontal_Dims_Correct ()
    {
        // Initializes a view with a vertical direction
        var view = new View
        {
            Text = "01234",
            TextDirection = TextDirection.LeftRight_TopBottom,
            Width = Dim.Auto (DimAutoStyle.Text),
            Height = Dim.Auto (DimAutoStyle.Text)
        };
        Assert.True (view.NeedsLayout);
        view.Layout ();
        Assert.Equal (new (0, 0, 5, 1), view.Frame);
        Assert.Equal (new (0, 0, 5, 1), view.Viewport);

        view.BeginInit ();
        view.EndInit ();
        Assert.Equal (new (0, 0, 5, 1), view.Frame);
        Assert.Equal (new (0, 0, 5, 1), view.Viewport);
    }

    // BUGBUG: this is a temporary test that helped identify #3469 - It needs to be expanded upon (and renamed)
    [Fact]
    public void TextDirection_Horizontal_Dims_Correct_WidthAbsolute ()
    {
        var view = new View
        {
            Text = "01234",
            TextDirection = TextDirection.LeftRight_TopBottom,
            TextAlignment = Alignment.Center,
            Width = 10,
            Height = Dim.Auto (DimAutoStyle.Text)
        };
        view.BeginInit ();
        view.EndInit ();
        Assert.Equal (new (0, 0, 10, 1), view.Frame);
        Assert.Equal (new (0, 0, 10, 1), view.Viewport);

        Assert.Equal (new (10, 1), view.TextFormatter.ConstrainToSize);
    }

    [Fact]
    public void TextDirection_Vertical_Dims_Correct ()
    {
        // Initializes a view with a vertical direction
        var view = new View
        {
            TextDirection = TextDirection.TopBottom_LeftRight,
            Text = "01234",
            Width = Dim.Auto (DimAutoStyle.Text),
            Height = Dim.Auto (DimAutoStyle.Text)
        };
        view.Layout ();
        Assert.Equal (new (0, 0, 1, 5), view.Frame);
        Assert.Equal (new (0, 0, 1, 5), view.Viewport);
    }
}
