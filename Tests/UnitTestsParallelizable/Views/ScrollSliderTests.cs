using UnitTests;

namespace ViewsTests;

public class ScrollSliderTests (ITestOutputHelper output) : TestDriverBase
{
    [Fact]
    public void Constructor_Initializes_Correctly ()
    {
        var scrollSlider = new ScrollSlider ();
        Assert.False (scrollSlider.CanFocus);
        Assert.Equal (Orientation.Vertical, scrollSlider.Orientation);
        Assert.Equal (TextDirection.TopBottom_LeftRight, scrollSlider.TextDirection);
        Assert.Equal (Alignment.Center, scrollSlider.TextAlignment);
        Assert.Equal (Alignment.Center, scrollSlider.VerticalTextAlignment);
        scrollSlider.Layout ();
        Assert.Equal (0, scrollSlider.Frame.X);
        Assert.Equal (0, scrollSlider.Frame.Y);
        Assert.Equal (1, scrollSlider.Size);
        Assert.Equal (2048, scrollSlider.VisibleContentSize);
    }

    [Fact]
    public void Add_To_SuperView_Initializes_Correctly ()
    {
        var super = new View
        {
            Id = "super",
            Width = 10,
            Height = 10,
            CanFocus = true
        };
        var scrollSlider = new ScrollSlider ();
        super.Add (scrollSlider);

        Assert.False (scrollSlider.CanFocus);
        Assert.Equal (Orientation.Vertical, scrollSlider.Orientation);
        Assert.Equal (TextDirection.TopBottom_LeftRight, scrollSlider.TextDirection);
        Assert.Equal (Alignment.Center, scrollSlider.TextAlignment);
        Assert.Equal (Alignment.Center, scrollSlider.VerticalTextAlignment);
        scrollSlider.Layout ();
        Assert.Equal (0, scrollSlider.Frame.X);
        Assert.Equal (0, scrollSlider.Frame.Y);
        Assert.Equal (1, scrollSlider.Size);
        Assert.Equal (10, scrollSlider.VisibleContentSize);
    }

    //[Fact]
    //public void OnOrientationChanged_Sets_Size_To_1 ()
    //{
    //    var scrollSlider = new ScrollSlider ();
    //    scrollSlider.Orientation = Orientation.Horizontal;
    //    Assert.Equal (1, scrollSlider.Size);
    //}

    [Fact]
    public void OnOrientationChanged_Sets_Position_To_0 ()
    {
        var super = new View
        {
            Id = "super",
            Width = 10,
            Height = 10
        };
        var scrollSlider = new ScrollSlider ();
        super.Add (scrollSlider);
        scrollSlider.Layout ();
        scrollSlider.Position = 1;
        scrollSlider.Orientation = Orientation.Horizontal;

        Assert.Equal (0, scrollSlider.Position);
    }

    [Fact]
    public void OnOrientationChanged_Updates_TextDirection_And_TextAlignment ()
    {
        var scrollSlider = new ScrollSlider ();
        scrollSlider.Orientation = Orientation.Horizontal;
        Assert.Equal (TextDirection.LeftRight_TopBottom, scrollSlider.TextDirection);
        Assert.Equal (Alignment.Center, scrollSlider.TextAlignment);
        Assert.Equal (Alignment.Center, scrollSlider.VerticalTextAlignment);
    }

    [Theory]
    [CombinatorialData]
    public void Size_Clamps_To_SuperView_Viewport ([CombinatorialRange (-1, 6, 1)] int sliderSize, Orientation orientation)
    {
        var super = new View
        {
            Id = "super",
            Width = 5,
            Height = 5
        };

        var scrollSlider = new ScrollSlider
        {
            Orientation = orientation
        };
        super.Add (scrollSlider);
        scrollSlider.Layout ();

        scrollSlider.Size = sliderSize;
        scrollSlider.Layout ();

        Assert.True (scrollSlider.Size > 0);

        Assert.True (scrollSlider.Size <= 5);
    }

    [Theory]
    [CombinatorialData]
    public void Size_Clamps_To_VisibleContentSizes (
        [CombinatorialRange (1, 6, 1)] int dimension,
        [CombinatorialRange (-1, 6, 1)] int sliderSize,
        Orientation orientation
    )
    {
        var scrollSlider = new ScrollSlider
        {
            Orientation = orientation,
            VisibleContentSize = dimension,
            Size = sliderSize
        };
        scrollSlider.Layout ();

        Assert.True (scrollSlider.Size > 0);

        Assert.True (scrollSlider.Size <= dimension);
    }

    [Theory]
    [CombinatorialData]
    public void CalculateSize_ScrollBounds_0_Returns_1 (
        [CombinatorialRange (-1, 5, 1)] int visibleContentSize,
        [CombinatorialRange (-1, 5, 1)] int scrollableContentSize
    )
    {
        // Arrange

        // Act
        int sliderSize = ScrollSlider.CalculateSize (scrollableContentSize, visibleContentSize, 0);

        // Assert
        Assert.Equal (1, sliderSize);
    }

    [Theory]
    [CombinatorialData]
    public void CalculateSize_ScrollableContentSize_0_Returns_1 (
        [CombinatorialRange (-1, 5, 1)] int visibleContentSize,
        [CombinatorialRange (-1, 5, 1)] int sliderBounds
    )
    {
        // Arrange

        // Act
        int sliderSize = ScrollSlider.CalculateSize (0, visibleContentSize, sliderBounds);

        // Assert
        Assert.Equal (1, sliderSize);
    }

    //[Theory]
    //[CombinatorialData]

    //public void CalculateSize_VisibleContentSize_0_Returns_0 ([CombinatorialRange (-1, 5, 1)] int scrollableContentSize, [CombinatorialRange (-1, 5, 1)] int sliderBounds)
    //{
    //    // Arrange

    //    // Act
    //    var sliderSize = ScrollSlider.CalculateSize (scrollableContentSize, 0, sliderBounds);

    //    // Assert
    //    Assert.Equal (0, sliderSize);
    //}

    [Theory]
    [InlineData (0, 1, 1, 1)]
    [InlineData (1, 1, 1, 1)]
    [InlineData (1, 2, 1, 1)]
    [InlineData (0, 5, 5, 5)]
    [InlineData (1, 5, 5, 1)]
    [InlineData (2, 5, 5, 2)]
    [InlineData (3, 5, 5, 3)]
    [InlineData (4, 5, 5, 4)]
    [InlineData (5, 5, 5, 5)]
    [InlineData (6, 5, 5, 5)]
    public void CalculateSize_Calculates_Correctly (int visibleContentSize, int scrollableContentSize, int scrollBounds, int expectedSliderSize)
    {
        // Arrange

        // Act
        int sliderSize = ScrollSlider.CalculateSize (scrollableContentSize, visibleContentSize, scrollBounds);

        // Assert
        Assert.Equal (expectedSliderSize, sliderSize);
    }

    [Fact]
    public void VisibleContentSize_Not_Set_Uses_SuperView ()
    {
        View super = new ()
        {
            Id = "super",
            Height = 5,
            Width = 5
        };
        var scrollSlider = new ScrollSlider ();

        super.Add (scrollSlider);
        super.Layout ();
        Assert.Equal (5, scrollSlider.VisibleContentSize);
    }

    [Fact]
    public void VisibleContentSize_Set_Overrides_SuperView ()
    {
        View super = new ()
        {
            Id = "super",
            Height = 5,
            Width = 5
        };

        var scrollSlider = new ScrollSlider
        {
            VisibleContentSize = 10
        };

        super.Add (scrollSlider);
        super.Layout ();
        Assert.Equal (10, scrollSlider.VisibleContentSize);

        super.Height = 3;
        super.Layout ();
        Assert.Equal (10, scrollSlider.VisibleContentSize);

        super.Height = 7;
        super.Layout ();
        Assert.Equal (10, scrollSlider.VisibleContentSize);
    }

    [Theory]
    [CombinatorialData]
    public void VisibleContentSizes_Clamps_0_To_Dimension ([CombinatorialRange (0, 10, 1)] int dimension, Orientation orientation)
    {
        var scrollSlider = new ScrollSlider
        {
            Orientation = orientation,
            VisibleContentSize = dimension
        };

        Assert.InRange (scrollSlider.VisibleContentSize, 1, 10);

        View super = new ()
        {
            Id = "super",
            Height = dimension,
            Width = dimension
        };

        scrollSlider = new()
        {
            Orientation = orientation
        };
        super.Add (scrollSlider);
        super.Layout ();

        Assert.InRange (scrollSlider.VisibleContentSize, 1, 10);

        scrollSlider.VisibleContentSize = dimension;

        Assert.InRange (scrollSlider.VisibleContentSize, 1, 10);
    }

    [Theory]
    //// 0123456789
    ////  ---------
    //// ◄█►
    //[InlineData (3, 3, 0, 1, 0)]
    //[InlineData (3, 3, 1, 1, 0)]
    //[InlineData (3, 3, 2, 1, 0)]

    //// 0123456789
    ////  ---------
    //// ◄██►
    //[InlineData (4, 4, 0, 2, 0)]
    //[InlineData (4, 4, 1, 2, 0)]
    //[InlineData (4, 4, 2, 2, 0)]
    //[InlineData (4, 4, 3, 2, 0)]
    //[InlineData (4, 4, 4, 2, 0)]

    // 012345
    // ^----
    // █░
    [InlineData (2, 5, 0, 0)]

    // -^---
    // █░
    [InlineData (2, 5, 1, 0)]

    // --^--
    // ░█
    [InlineData (2, 5, 2, 1)]

    // ---^-
    // ░█
    [InlineData (2, 5, 3, 1)]

    // ----^
    // ░█
    [InlineData (2, 5, 4, 1)]

    // 012345
    // ^----
    // █░░
    [InlineData (3, 5, 0, 0)]

    // -^---
    // ░█░
    [InlineData (3, 5, 1, 1)]

    // --^--
    // ░░█
    [InlineData (3, 5, 2, 2)]

    // ---^-
    // ░░█
    [InlineData (3, 5, 3, 2)]

    // ----^
    // ░░█
    [InlineData (3, 5, 4, 2)]

    // 0123456789
    // ^-----
    // █░░
    [InlineData (3, 6, 0, 0)]

    // -^----
    // █░░
    [InlineData (3, 6, 1, 1)]

    // --^---
    // ░█░
    [InlineData (3, 6, 2, 1)]

    // ---^--
    // ░░█
    [InlineData (3, 6, 3, 2)]

    // ----^-
    // ░░█
    [InlineData (3, 6, 4, 2)]

    // -----^
    // ░░█
    [InlineData (3, 6, 5, 2)]

    // 012345
    // ^----
    // ███░
    [InlineData (4, 5, 0, 0)]

    // -^---
    // ░███
    [InlineData (4, 5, 1, 1)]

    // --^--
    // ░███
    [InlineData (4, 5, 2, 1)]

    // ---^-
    // ░███
    [InlineData (4, 5, 3, 1)]

    // ----^
    // ░███
    [InlineData (4, 5, 4, 1)]

    //// 01234
    //// ^---------
    //// ◄█░░►
    //[InlineData (5, 10, 0, 1, 0)]
    //// -^--------
    //// ◄█░░►
    //[InlineData (5, 10, 1, 1, 0)]
    //// --^-------
    //// ◄█░░►
    //[InlineData (5, 10, 2, 1, 0)]
    //// ---^------
    //// ◄█░░►
    //[InlineData (5, 10, 3, 1, 0)]
    //// ----^----
    //// ◄░█░►
    //[InlineData (5, 10, 4, 1, 1)]
    //// -----^---
    //// ◄░█░►
    //[InlineData (5, 10, 5, 1, 1)]
    //// ------^--
    //// ◄░░█►
    //[InlineData (5, 10, 6, 1, 2)]
    //// ------^--
    //// ◄░░█►
    //[InlineData (5, 10, 7, 1, 2)]
    //// -------^-
    //// ◄░░█►
    //[InlineData (5, 10, 8, 1, 2)]
    //// --------^
    //// ◄░░█►
    //[InlineData (5, 10, 9, 1, 2)]

    // 0123456789
    // ████░░░░
    // ^-----------------
    // 012345678901234567890123456789
    // ░████░░░
    // ----^-------------
    // 012345678901234567890123456789
    // ░░████░░
    // --------^---------
    // 012345678901234567890123456789
    // ░░░████░
    // ------------^-----
    // 012345678901234567890123456789
    // ░░░░████
    // ----------------^--

    // 0123456789
    // ███░░░░░
    // ^-----------------

    // 012345678901234567890123456789
    // ░░███░░░
    // --------^---------
    // 012345678901234567890123456789
    // ░░░███░░
    // ------------^-----
    // 012345678901234567890123456789
    // ░░░░███░
    // ----------------^--
    // 012345678901234567890123456789
    // ░░░░░███
    // ----------------^--
    [InlineData (8, 18, 0, 0)]
    [InlineData (8, 18, 1, 0)]

    // 012345678901234567890123456789
    // ░███░░░░
    // --^---------------
    [InlineData (8, 18, 2, 1)]
    [InlineData (8, 18, 3, 2)]
    [InlineData (8, 18, 4, 2)]
    [InlineData (8, 18, 5, 2)]
    [InlineData (8, 18, 6, 3)]
    [InlineData (8, 18, 7, 4)]
    [InlineData (8, 18, 8, 4)]
    [InlineData (8, 18, 9, 4)]

    // 012345678901234567890123456789
    // ░░░░░███
    // ----------^--------
    [InlineData (8, 18, 10, 5)]
    [InlineData (8, 18, 11, 5)]
    [InlineData (8, 18, 12, 5)]
    [InlineData (8, 18, 13, 5)]
    [InlineData (8, 18, 14, 5)]
    [InlineData (8, 18, 15, 5)]
    [InlineData (8, 18, 16, 5)]
    [InlineData (8, 18, 17, 5)]
    [InlineData (8, 18, 18, 5)]
    [InlineData (8, 18, 19, 5)]
    [InlineData (8, 18, 20, 5)]
    [InlineData (8, 18, 21, 5)]
    [InlineData (8, 18, 22, 5)]
    [InlineData (8, 18, 23, 5)]

    // ------------------   ^
    [InlineData (8, 18, 24, 5)]
    [InlineData (8, 18, 25, 5)]

    //// 0123456789
    //// ◄████░░░░►
    //// ^-----------------
    //[InlineData (10, 20, 0, 5, 0)]
    //[InlineData (10, 20, 1, 5, 0)]
    //[InlineData (10, 20, 2, 5, 0)]
    //[InlineData (10, 20, 3, 5, 0)]
    //[InlineData (10, 20, 4, 5, 1)]
    //[InlineData (10, 20, 5, 5, 1)]
    //[InlineData (10, 20, 6, 5, 1)]
    //[InlineData (10, 20, 7, 5, 2)]
    //[InlineData (10, 20, 8, 5, 2)]
    //[InlineData (10, 20, 9, 5, 2)]
    //[InlineData (10, 20, 10, 5, 3)]
    //[InlineData (10, 20, 11, 5, 3)]
    //[InlineData (10, 20, 12, 5, 3)]
    //[InlineData (10, 20, 13, 5, 3)]
    //[InlineData (10, 20, 14, 5, 4)]
    //[InlineData (10, 20, 15, 5, 4)]
    //[InlineData (10, 20, 16, 5, 4)]
    //[InlineData (10, 20, 17, 5, 5)]
    //[InlineData (10, 20, 18, 5, 5)]
    //[InlineData (10, 20, 19, 5, 5)]
    //[InlineData (10, 20, 20, 5, 6)]
    //[InlineData (10, 20, 21, 5, 6)]
    //[InlineData (10, 20, 22, 5, 6)]
    //[InlineData (10, 20, 23, 5, 6)]
    //[InlineData (10, 20, 24, 5, 6)]
    //[InlineData (10, 20, 25, 5, 6)]
    public void CalculatePosition_Calculates_Correctly (int visibleContentSize, int scrollableContentSize, int contentPosition, int expectedSliderPosition)
    {
        // Arrange

        // Act
        int sliderPosition = ScrollSlider.CalculatePosition (
                                                             scrollableContentSize,
                                                             visibleContentSize,
                                                             contentPosition,
                                                             visibleContentSize,
                                                             NavigationDirection.Forward);

        // Assert
        Assert.Equal (expectedSliderPosition, sliderPosition);
    }

    [Theory]
    [InlineData (8, 18, 0, 0)]
    public void CalculateContentPosition_Calculates_Correctly (
        int visibleContentSize,
        int scrollableContentSize,
        int sliderPosition,
        int expectedContentPosition
    )
    {
        // Arrange

        // Act
        int contentPosition = ScrollSlider.CalculateContentPosition (
                                                                     scrollableContentSize,
                                                                     visibleContentSize,
                                                                     sliderPosition,
                                                                     visibleContentSize);

        // Assert
        Assert.Equal (expectedContentPosition, contentPosition);
    }

    [Theory]
    [CombinatorialData]
    public void ClampPosition_WithSuperView_Clamps_To_ViewPort_Minus_Size_If_VisibleContentSize_Not_Set (
        [CombinatorialRange (10, 10, 1)] int dimension,
        [CombinatorialRange (1, 5, 1)] int sliderSize,
        [CombinatorialRange (-1, 10, 2)] int sliderPosition,
        Orientation orientation
    )
    {
        View super = new ()
        {
            Id = "super",
            Height = dimension,
            Width = dimension
        };

        var scrollSlider = new ScrollSlider
        {
            Orientation = orientation,
            Size = sliderSize
        };
        super.Add (scrollSlider);
        super.Layout ();

        Assert.Equal (dimension, scrollSlider.VisibleContentSize);

        int clampedPosition = scrollSlider.ClampPosition (sliderPosition);

        Assert.InRange (clampedPosition, 0, dimension - sliderSize);
    }

    [Theory]
    [CombinatorialData]
    public void ClampPosition_WithSuperView_Clamps_To_VisibleContentSize_Minus_Size (
        [CombinatorialRange (10, 10, 1)] int dimension,
        [CombinatorialRange (1, 5, 1)] int sliderSize,
        [CombinatorialRange (-1, 10, 2)] int sliderPosition,
        Orientation orientation
    )
    {
        View super = new ()
        {
            Id = "super",
            Height = dimension + 2,
            Width = dimension + 2
        };

        var scrollSlider = new ScrollSlider
        {
            Orientation = orientation,
            VisibleContentSize = dimension,
            Size = sliderSize
        };
        super.Add (scrollSlider);
        super.Layout ();

        int clampedPosition = scrollSlider.ClampPosition (sliderPosition);

        Assert.InRange (clampedPosition, 0, dimension - sliderSize);
    }

    [Theory]
    [CombinatorialData]
    public void ClampPosition_NoSuperView_Clamps_To_VisibleContentSize_Minus_Size (
        [CombinatorialRange (10, 10, 1)] int dimension,
        [CombinatorialRange (1, 5, 1)] int sliderSize,
        [CombinatorialRange (-1, 10, 2)] int sliderPosition,
        Orientation orientation
    )
    {
        var scrollSlider = new ScrollSlider
        {
            Orientation = orientation,
            VisibleContentSize = dimension,
            Size = sliderSize
        };

        int clampedPosition = scrollSlider.ClampPosition (sliderPosition);

        Assert.InRange (clampedPosition, 0, dimension - sliderSize);
    }

    [Theory]
    [CombinatorialData]
    public void Position_Clamps_To_VisibleContentSize (
        [CombinatorialRange (0, 5, 1)] int dimension,
        [CombinatorialRange (1, 5, 1)] int sliderSize,
        [CombinatorialRange (-1, 10, 2)] int sliderPosition,
        Orientation orientation
    )
    {
        var scrollSlider = new ScrollSlider
        {
            Orientation = orientation,
            VisibleContentSize = dimension,
            Size = sliderSize,
            Position = sliderPosition
        };

        Assert.True (scrollSlider.Position <= 5);
    }

    [Theory]
    [CombinatorialData]
    public void Position_Clamps_To_SuperView_Viewport (
        [CombinatorialRange (0, 5, 1)] int sliderSize,
        [CombinatorialRange (-2, 10, 2)] int sliderPosition,
        Orientation orientation
    )
    {
        var super = new View
        {
            Id = "super",
            Width = 5,
            Height = 5
        };

        var scrollSlider = new ScrollSlider
        {
            Orientation = orientation
        };
        super.Add (scrollSlider);
        scrollSlider.Size = sliderSize;
        scrollSlider.Layout ();

        scrollSlider.Position = sliderPosition;

        Assert.True (scrollSlider.Position <= 5);
    }


    [Theory]
    [InlineData (
                    3,
                    10,
                    1,
                    0,
                    Orientation.Vertical,
                    @"
┌───┐
│███│
│   │
│   │
│   │
│   │
│   │
│   │
│   │
│   │
│   │
└───┘")]
    [InlineData (
                    10,
                    1,
                    3,
                    0,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│███       │
└──────────┘")]
    [InlineData (
                    3,
                    10,
                    3,
                    0,
                    Orientation.Vertical,
                    @"
┌───┐
│███│
│███│
│███│
│   │
│   │
│   │
│   │
│   │
│   │
│   │
└───┘")]



    [InlineData (
                    3,
                    10,
                    5,
                    0,
                    Orientation.Vertical,
                    @"
┌───┐
│███│
│███│
│███│
│███│
│███│
│   │
│   │
│   │
│   │
│   │
└───┘")]

    [InlineData (
                    3,
                    10,
                    5,
                    1,
                    Orientation.Vertical,
                    @"
┌───┐
│   │
│███│
│███│
│███│
│███│
│███│
│   │
│   │
│   │
│   │
└───┘")]
    [InlineData (
                    3,
                    10,
                    5,
                    4,
                    Orientation.Vertical,
                    @"
┌───┐
│   │
│   │
│   │
│   │
│███│
│███│
│███│
│███│
│███│
│   │
└───┘")]
    [InlineData (
                    3,
                    10,
                    5,
                    5,
                    Orientation.Vertical,
                    @"
┌───┐
│   │
│   │
│   │
│   │
│   │
│███│
│███│
│███│
│███│
│███│
└───┘")]
    [InlineData (
                    3,
                    10,
                    5,
                    6,
                    Orientation.Vertical,
                    @"
┌───┐
│   │
│   │
│   │
│   │
│   │
│███│
│███│
│███│
│███│
│███│
└───┘")]

    [InlineData (
                    3,
                    10,
                    10,
                    0,
                    Orientation.Vertical,
                    @"
┌───┐
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
└───┘")]

    [InlineData (
                    3,
                    10,
                    10,
                    5,
                    Orientation.Vertical,
                    @"
┌───┐
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
└───┘")]
    [InlineData (
                    3,
                    10,
                    11,
                    0,
                    Orientation.Vertical,
                    @"
┌───┐
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
└───┘")]

    [InlineData (
                    10,
                    3,
                    5,
                    0,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│█████     │
│█████     │
│█████     │
└──────────┘")]

    [InlineData (
                    10,
                    3,
                    5,
                    1,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│ █████    │
│ █████    │
│ █████    │
└──────────┘")]
    [InlineData (
                    10,
                    3,
                    5,
                    4,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│    █████ │
│    █████ │
│    █████ │
└──────────┘")]
    [InlineData (
                    10,
                    3,
                    5,
                    5,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│     █████│
│     █████│
│     █████│
└──────────┘")]
    [InlineData (
                    10,
                    3,
                    5,
                    6,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│     █████│
│     █████│
│     █████│
└──────────┘")]

    [InlineData (
                    10,
                    3,
                    10,
                    0,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│██████████│
│██████████│
│██████████│
└──────────┘")]

    [InlineData (
                    10,
                    3,
                    10,
                    5,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│██████████│
│██████████│
│██████████│
└──────────┘")]
    [InlineData (
                    10,
                    3,
                    11,
                    0,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│██████████│
│██████████│
│██████████│
└──────────┘")]
    public void Draws_Correctly (int superViewportWidth, int superViewportHeight, int sliderSize, int position, Orientation orientation, string expected)
    {
        IDriver driver = CreateTestDriver ();
        var super = new Window
        {
            Driver = driver,
            Id = "super",
            Width = superViewportWidth + 2,
            Height = superViewportHeight + 2
        };

        var scrollSlider = new ScrollSlider
        {
            Orientation = orientation,
            Size = sliderSize,
            //Position = position,
        };
        Assert.Equal (sliderSize, scrollSlider.Size);
        super.Add (scrollSlider);
        scrollSlider.Position = position;

        super.Layout ();
        super.Draw ();

        _ = DriverAssert.AssertDriverContentsWithFrameAre (expected, output, driver);
    }

    // Copilot - IValue<int> tests

    [Fact]
    public void Value_Gets_And_Sets_Position ()
    {
        ScrollSlider slider = new () { VisibleContentSize = 100, Size = 10 };
        Assert.Equal (0, slider.Value);

        slider.Value = 5;
        Assert.Equal (5, slider.Value);
        Assert.Equal (5, slider.Position);

        slider.Position = 10;
        Assert.Equal (10, slider.Value);
        Assert.Equal (10, slider.Position);

        slider.Dispose ();
    }

    [Fact]
    public void ValueChanging_Event_Is_Raised ()
    {
        ScrollSlider slider = new () { VisibleContentSize = 100, Size = 10 };
        var eventRaised = false;
        int oldValue = -1;
        int newValue = -1;

        slider.ValueChanging += (_, e) =>
                                {
                                    eventRaised = true;
                                    oldValue = e.CurrentValue;
                                    newValue = e.NewValue;
                                };

        slider.Value = 5;

        Assert.True (eventRaised);
        Assert.Equal (0, oldValue);
        Assert.Equal (5, newValue);

        slider.Dispose ();
    }

    [Fact]
    public void ValueChanging_Can_Cancel_Change ()
    {
        ScrollSlider slider = new () { VisibleContentSize = 100, Size = 10 };

        slider.ValueChanging += (_, e) => e.Handled = true;

        slider.Value = 5;

        Assert.Equal (0, slider.Value);

        slider.Dispose ();
    }

    [Fact]
    public void ValueChanged_Event_Is_Raised ()
    {
        ScrollSlider slider = new () { VisibleContentSize = 100, Size = 10 };
        var eventRaised = false;
        int oldValue = -1;
        int newValue = -1;

        slider.ValueChanged += (_, e) =>
                               {
                                   eventRaised = true;
                                   oldValue = e.OldValue;
                                   newValue = e.NewValue;
                               };

        slider.Value = 5;

        Assert.True (eventRaised);
        Assert.Equal (0, oldValue);
        Assert.Equal (5, newValue);

        slider.Dispose ();
    }

    [Fact]
    public void ValueChangedUntyped_Event_Is_Raised ()
    {
        ScrollSlider slider = new () { VisibleContentSize = 100, Size = 10 };
        var eventRaised = false;
        object? oldValue = null;
        object? newValue = null;

        slider.ValueChangedUntyped += (_, e) =>
                                      {
                                          eventRaised = true;
                                          oldValue = e.OldValue;
                                          newValue = e.NewValue;
                                      };

        slider.Value = 5;

        Assert.True (eventRaised);
        Assert.Equal (0, oldValue);
        Assert.Equal (5, newValue);

        slider.Dispose ();
    }

    [Fact]
    public void GetValue_Returns_Boxed_Position ()
    {
        ScrollSlider slider = new () { VisibleContentSize = 100, Size = 10 };
        slider.Value = 7;

        IValue ivalue = slider;
        Assert.Equal (7, ivalue.GetValue ());

        slider.Dispose ();
    }

    [Fact]
    public void TrySetValueFromString_Sets_Position ()
    {
        ScrollSlider slider = new () { VisibleContentSize = 100, Size = 10 };

        IValue ivalue = slider;
        bool result = ivalue.TrySetValueFromString ("5");

        Assert.True (result);
        Assert.Equal (5, slider.Position);
        Assert.Equal (5, slider.Value);

        slider.Dispose ();
    }

    [Fact]
    public void TrySetValueFromString_Returns_False_For_Invalid_Input ()
    {
        ScrollSlider slider = new () { VisibleContentSize = 100, Size = 10 };

        IValue ivalue = slider;
        bool result = ivalue.TrySetValueFromString ("not-a-number");

        Assert.False (result);
        Assert.Equal (0, slider.Value);

        slider.Dispose ();
    }
}
