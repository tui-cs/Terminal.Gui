namespace ViewsTests;

public partial class DialogTests
{
    [Fact]
    public void Layout_With_EnableForDesign_Default_Container ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Dialog dialog = new () { Driver = driver };

        IDesignable designable = dialog;
        designable.EnableForDesign ();

        dialog.Layout ();

        // Dialog should be centered with DimAuto
        Assert.True (dialog.X.Has<PosCenter> (out _));
        Assert.True (dialog.Y.Has<PosCenter> (out _));
        Assert.True (dialog.Width.Has<DimAuto> (out _));
        Assert.True (dialog.Height.Has<DimAuto> (out _));

        // Frame should be calculated based on content
        Assert.True (dialog.Frame.Width > 0);
        Assert.True (dialog.Frame.Height > 0);

        // Should fit within screen
        Assert.True (dialog.Frame.Width <= 80);
        Assert.True (dialog.Frame.Height <= 25);

        dialog.Dispose ();
    }

    [Fact]
    public void Layout_With_Small_Container ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (30, 10);

        Dialog dialog = new () { Driver = driver };

        IDesignable designable = dialog;
        designable.EnableForDesign ();

        dialog.Layout ();

        // Should calculate size based on content
        Assert.True (dialog.Frame.Width > 0);
        Assert.True (dialog.Frame.Height > 0);

        // With small container, dialog may need more space than available
        // Just verify it laid out successfully
        Assert.NotEqual (Rectangle.Empty, dialog.Frame);

        dialog.Dispose ();
    }

    [Fact]
    public void Layout_With_Large_Container ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (200, 100);

        Dialog dialog = new () { Driver = driver };

        IDesignable designable = dialog;
        designable.EnableForDesign ();

        dialog.Layout ();

        // Should calculate size based on content, not fill entire container
        Assert.True (dialog.Frame.Width > 0);
        Assert.True (dialog.Frame.Height > 0);

        // DimAuto with max 100% - 2 means won't exceed 198x98
        Assert.True (dialog.Frame.Width <= 198);
        Assert.True (dialog.Frame.Height <= 98);

        dialog.Dispose ();
    }

    [Fact]
    public void Width_Height_DimAuto_Calculates_Based_On_Content ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Dialog dialog = new () { Driver = driver, Title = "Test" };

        // Add content that requires specific size
        Label label = new () { Text = "This is a label", X = 0, Y = 0 };
        TextField textField = new () { X = Pos.Right (label) + 1, Y = 0, Width = 30, Text = "Input here" };

        dialog.Add (label, textField);

        Button okButton = new () { Title = "OK" };
        dialog.AddButton (okButton);

        dialog.Layout ();

        // Width should accommodate: label + space + textfield + padding + border
        int expectedMinWidth = label.Text.GetColumns () + 1 + 30;

        Assert.True (dialog.Frame.Width >= expectedMinWidth);

        dialog.Dispose ();
    }

    [Fact]
    public void Height_Accounts_For_Buttons ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Dialog dialog = new () { Driver = driver };

        Button button1 = new () { Title = "Cancel" };
        Button button2 = new () { Title = "OK" };

        dialog.AddButton (button1);
        dialog.AddButton (button2);

        dialog.Layout ();

        // Height should account for buttons at the bottom
        Assert.True (dialog.Frame.Height > 0);
        Assert.True (dialog.Padding.Thickness.Bottom >= button1.Frame.Height);

        dialog.Dispose ();
    }

    [Fact]
    public void Respects_Explicit_Width_Height ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Dialog dialog = new () { Driver = driver, Width = 50, Height = 15 };

        IDesignable designable = dialog;
        designable.EnableForDesign ();

        dialog.Layout ();

        // Should use explicit dimensions
        Assert.Equal (50, dialog.Frame.Width);
        Assert.Equal (15, dialog.Frame.Height);

        dialog.Dispose ();
    }

    [Fact]
    public void Padding_Affects_Content_Area ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Dialog dialog = new () { Driver = driver };

        IDesignable designable = dialog;
        designable.EnableForDesign ();

        dialog.Layout ();

        // Padding should be set up correctly
        Assert.NotNull (dialog.Padding);

        // Buttons should be in a button container within padding
        Assert.True (dialog.Padding.View!.SubViews.Count > 0);

        // Padding bottom should accommodate buttons
        Assert.True (dialog.Padding.Thickness.Bottom > 0);

        dialog.Dispose ();
    }

    [Fact]
    public void Multiple_Buttons_Layout_Correctly ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Dialog dialog = new ()
        {
            Driver = driver, ButtonAlignment = Alignment.End, ButtonAlignmentModes = AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems
        };

        Button button1 = new () { Title = "Help" };
        Button button2 = new () { Title = "Cancel" };
        Button button3 = new () { Title = "OK" };

        dialog.AddButton (button1);
        dialog.AddButton (button2);
        dialog.AddButton (button3);

        dialog.Layout ();

        // All buttons should be at same Y position (AnchorEnd)
        Assert.Equal (button1.Frame.Y, button2.Frame.Y);
        Assert.Equal (button2.Frame.Y, button3.Frame.Y);

        // Buttons should be aligned using PosAlign
        Assert.True (button1.X.Has (out PosAlign align1));
        Assert.True (button2.X.Has (out PosAlign align2));
        Assert.True (button3.X.Has (out PosAlign align3));

        // All should use same GroupId (Dialog's hash code)
        Assert.Equal (align1.GroupId, align2.GroupId);
        Assert.Equal (align2.GroupId, align3.GroupId);

        dialog.Dispose ();
    }

    [Fact]
    public void With_Text_Property_Affects_Height ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Dialog dialog1 = new () { Driver = driver };

        Button button = new () { Title = "OK" };
        dialog1.AddButton (button);

        dialog1.Layout ();
        int heightWithoutText = dialog1.Frame.Height;

        Dialog dialog2 = new () { Driver = driver, Text = "Line 1\nLine 2\nLine 3" };

        dialog2.AddButton (new Button { Title = "OK" });

        dialog2.Layout ();
        int heightWithText = dialog2.Frame.Height;

        // Dialog with text should be taller
        Assert.True (heightWithText > heightWithoutText);

        dialog1.Dispose ();
        dialog2.Dispose ();
    }

    /// <summary>
    ///     Regression test for https://github.com/tui-cs/Terminal.Gui/issues/4615
    ///     Dialog's size logic should not allow it to be taller or wider than the screen.
    /// </summary>
    [Fact]
    public void Frame_Clamped_To_Container ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        const int SCREEN_WIDTH = 20;
        const int SCREEN_HEIGHT = 20;
        app.Screen = new Rectangle (0, 0, SCREEN_WIDTH, SCREEN_HEIGHT);

        using Dialog dialog = new ();
        dialog.AddButton (new Button ());

        View view = new () { Width = 10, Height = 50 };

        dialog.ShadowStyle = ShadowStyles.Transparent;
        dialog.Add (view);

        app.Begin (dialog);
        Assert.True (app.Screen.Contains (dialog.Frame));
        dialog.Remove (view);

        view.Width = SCREEN_WIDTH;
        view.Height = SCREEN_WIDTH;
        dialog.Add (view);
        dialog.Layout ();
        Assert.True (app.Screen.Contains (dialog.Frame));

        dialog.Remove (view);
        view.Width = SCREEN_WIDTH + 5;
        view.Height = SCREEN_WIDTH + 5;
        dialog.Add (view);
        dialog.Layout ();
        Assert.True (app.Screen.Contains (dialog.Frame));

        dialog.Remove (view);
        dialog.ShadowStyle = ShadowStyles.Transparent;
        dialog.Add (view);
        dialog.Layout ();
        Assert.True (app.Screen.Contains (dialog.Frame));

        dialog.Remove (view);
        view.Height = SCREEN_HEIGHT + 10;
        dialog.Add (view);
        dialog.Layout ();
        Assert.True (app.Screen.Contains (dialog.Frame));
    }

    /// <summary>
    ///     Regression test for https://github.com/tui-cs/Terminal.Gui/issues/4615
    ///     Dialog's size should be constrained by maximumContentDim.
    /// </summary>
    [Theory]
    [InlineData (20, 10)]
    [InlineData (40, 20)]
    [InlineData (80, 25)]
    [InlineData (100, 50)]
    public void Height_Never_Exceeds_Screen_Height (int screenWidth, int screenHeight)
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (screenWidth, screenHeight);

        // Create a container view that represents the screen (simulating TopRunnableView)
        View container = new ()
        {
            Driver = driver,
            X = 0,
            Y = 0,
            Width = screenWidth,
            Height = screenHeight
        };

        Dialog dialog = new ();

        // Add many subviews to force a large content area
        for (var i = 0; i < 100; i++)
        {
            dialog.Add (new Label { Text = $"Label {i}", X = 0, Y = i });
        }

        dialog.AddButton (new Button { Title = "OK" });
        container.Add (dialog);

        // Initialize and layout the container
        container.BeginInit ();
        container.EndInit ();
        container.SetRelativeLayout (new Size (screenWidth, screenHeight));

        // Debug: Check container size before layout
        output.WriteLine ($"Container Frame before layout: {container.Frame}");
        output.WriteLine ($"Container ContentSize: {container.GetContentSize ()}");

        container.LayoutSubViews ();

        // Debug: Check dialog size after layout
        output.WriteLine ($"Dialog Frame after layout: {dialog.Frame}");
        output.WriteLine ($"Dialog SuperView: {dialog.SuperView}");
        output.WriteLine ($"Dialog SuperView ContentSize: {dialog.SuperView?.GetContentSize ()}");

        // The dialog should never exceed the screen height
        Assert.True (dialog.Frame.Height <= screenHeight, $"Dialog height {dialog.Frame.Height} exceeded screen height {screenHeight}");

        // The dialog should never exceed the screen width
        Assert.True (dialog.Frame.Width <= screenWidth, $"Dialog width {dialog.Frame.Width} exceeded screen width {screenWidth}");

        container.Dispose ();
    }

}
