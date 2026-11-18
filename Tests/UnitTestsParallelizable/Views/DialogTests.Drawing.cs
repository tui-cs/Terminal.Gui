using UnitTests;

namespace ViewsTests;

public partial class DialogTests
{
    [Fact]
    public void Draws_Single_Button ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (30, 10);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = null;

        Button okButton = new ()
        {
            Title = "OK",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (okButton);

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        var expected = """
                       ┌────┐
                       │    │
                       │  OK│
                       └────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    [Fact]
    public void Draws_Two_Buttons ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (40, 10);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = null;
        dialog.ButtonAlignment = Alignment.End;

        Button cancelButton = new ()
        {
            Title = "Cancel",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };

        Button okButton = new ()
        {
            Title = "OK",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (cancelButton);
        dialog.AddButton (okButton);

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        var expected = """
                       ┌────────┐
                       │        │
                       │CancelOK│
                       └────────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    [Fact]
    public void Draws_Text_Content ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (30, 20);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = null;
        dialog.Text = "Hello World";

        Button okButton = new ()
        {
            Title = "OK",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (okButton);

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        var expected = """
                       ┌───────────┐
                       │Hello World│
                       │           │
                       │         OK│
                       └───────────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    [Fact]
    public void Draws_Multiline_Text ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (30, 12);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = null;
        dialog.Text = "Line 1\nLine 2\nLine 3";

        Button okButton = new ()
        {
            Title = "OK",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (okButton);

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        var expected = """
                       ┌──────┐
                       │Line 1│
                       │Line 2│
                       │Line 3│
                       │      │
                       │    OK│
                       └──────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    [Fact]
    public void Draws_Three_Buttons_End_Aligned ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (50, 10);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = null;
        dialog.ButtonAlignment = Alignment.End;

        Button helpButton = new ()
        {
            Title = "Help",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };

        Button cancelButton = new ()
        {
            Title = "Cancel",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };

        Button okButton = new ()
        {
            Title = "OK",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (helpButton);
        dialog.AddButton (cancelButton);
        dialog.AddButton (okButton);

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        var expected = """
                       ┌────────────┐
                       │            │
                       │HelpCancelOK│
                       └────────────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    [Fact]
    public void Draws_Buttons_Center_Aligned ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (35, 10);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = null;
        dialog.ButtonAlignment = Alignment.Center;

        Button cancelButton = new ()
        {
            Title = "Cancel",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };

        Button okButton = new ()
        {
            Title = "OK",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (cancelButton);
        dialog.AddButton (okButton);

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        var expected = """
                       ┌────────┐
                       │        │
                       │CancelOK│
                       └────────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    [Fact]
    public void Draws_Buttons_Start_Aligned ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (35, 10);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = null;
        dialog.ButtonAlignment = Alignment.Start;

        Button cancelButton = new ()
        {
            Title = "Cancel",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };

        Button okButton = new ()
        {
            Title = "OK",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (cancelButton);
        dialog.AddButton (okButton);

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        var expected = """
                       ┌────────┐
                       │        │
                       │CancelOK│
                       └────────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    [Fact]
    public void Draws_EnableForDesign ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 10);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = null;

        (dialog as IDesignable).EnableForDesign ();

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        var expected = """
                       ┌┤Dialog Title├───────────────────────────────────┐
                       │Example: Type and press ENTER to accept.         │
                       │                                                 │
                       │                            ⟦ Cancel ⟧  ⟦► OK ◄⟧ │
                       │                                                 │
                       └─────────────────────────────────────────────────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    [Theory]
    [MemberData (nameof (PosData))]
    public void Dialog_Draws_SubView_With_SubViews_WithDifferentPosTypes (Pos pos, string expected)
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (22, 9);

        using Dialog dialog = new ();

        dialog.Driver = driver;
        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = null;
        dialog.Title = "Dialog";

        var container = new View
        {
            X = pos,
            Id = "container",
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Title = "container",
            BorderStyle = LineStyle.Single
        };
        var view1 = new View { Width = Dim.Auto (), Height = Dim.Auto (), Text = "view1" };
        var view2 = new View { Y = 1, Width = Dim.Auto (), Height = Dim.Auto (), Text = "view2" };
        container.Add (view1, view2);

        dialog.Add (container);

        dialog.Layout ();
        dialog.Draw ();

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    public static TheoryData<Pos, string> PosData () =>
        new ()
        {
            {
                Pos.Absolute (0), """
                                  ┌┤Dialog├──┐
                                  │┌┤con├┐   │
                                  ││view1│   │
                                  ││view2│   │
                                  │└─────┘   │
                                  └──────────┘
                                  """
            },
            {
                Pos.Absolute (2), """
                                  ┌┤Dialog├──┐
                                  │  ┌┤con├┐ │
                                  │  │view1│ │
                                  │  │view2│ │
                                  │  └─────┘ │
                                  └──────────┘
                                  """
            },
            {
                Pos.Center (), """
                               ┌┤Dialog├──┐
                               │ ┌┤con├┐  │
                               │ │view1│  │
                               │ │view2│  │
                               │ └─────┘  │
                               └──────────┘
                               """
            },
            {
                Pos.AnchorEnd (), """
                                  ┌┤Dialog├──┐
                                  │   ┌┤con├┐│
                                  │   │view1││
                                  │   │view2││
                                  │   └─────┘│
                                  └──────────┘
                                  """
            },
            {
                Pos.Align (Alignment.Start), """
                                             ┌┤Dialog├──┐
                                             │┌┤con├┐   │
                                             ││view1│   │
                                             ││view2│   │
                                             │└─────┘   │
                                             └──────────┘
                                             """
            },
            {
                Pos.Align (Alignment.Center), """
                                              ┌┤Dialog├──┐
                                              │ ┌┤con├┐  │
                                              │ │view1│  │
                                              │ │view2│  │
                                              │ └─────┘  │
                                              └──────────┘
                                              """
            },
            {
                Pos.Align (Alignment.End), """
                                           ┌┤Dialog├──┐
                                           │   ┌┤con├┐│
                                           │   │view1││
                                           │   │view2││
                                           │   └─────┘│
                                           └──────────┘
                                           """
            },
            {
                Pos.Align (Alignment.Fill), """
                                            ┌┤Dialog├──┐
                                            │┌┤con├┐   │
                                            ││view1│   │
                                            ││view2│   │
                                            │└─────┘   │
                                            └──────────┘
                                            """
            },
            {
                Pos.Percent (50), """
                                  ┌┤Dialog├──┐
                                  │     ┌┤con│
                                  │     │view│
                                  │     │view│
                                  │     └────│
                                  └──────────┘
                                  """
            },
            {
                Pos.Func (_ => 3), """
                                   ┌┤Dialog├──┐
                                   │   ┌┤con├┐│
                                   │   │view1││
                                   │   │view2││
                                   │   └─────┘│
                                   └──────────┘
                                   """
            }
        };

    [Theory]
    [MemberData (nameof (PosViewData))]
    public void Dialog_Draws_SubView_With_SubViews_WithDifferentPosViewTypes (Func<View, Pos> posFactory, Func<View> viewFactory, string expected)
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (23, 11);

        using Dialog dialog = new ();

        dialog.Driver = driver;
        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = null;
        dialog.Title = "Dialog";

        View view = viewFactory (); // Create fresh instance
        Pos pos = posFactory (view);

        var container = new View
        {
            X = pos,
            Y = pos,
            Id = "container",
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Title = "container",
            BorderStyle = LineStyle.Single
        };
        var view1 = new View { Width = Dim.Auto (), Height = Dim.Auto (), Text = "v" };
        container.Add (view1);

        dialog.Add (container, view);

        dialog.Layout ();
        dialog.Draw ();

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    public static TheoryData<Func<View, Pos>, Func<View>, string> PosViewData () =>
        new ()
        {
            {
                Pos.Bottom, () => new View
                {
                    X = 2,
                    Y = 2,
                    Width = Dim.Auto (),
                    Height = Dim.Auto (),
                    Text = "view"
                },
                """
                ┌┤Dialog├──┐
                │          │
                │          │
                │  view    │
                │   ┌─┐    │
                │   │v│    │
                │   └─┘    │
                └──────────┘
                """
            },
            {
                Pos.Left, () => new View
                {
                    X = 2,
                    Y = 2,
                    Width = Dim.Auto (),
                    Height = Dim.Auto (),
                    Text = "view"
                },
                """
                ┌┤Dialog├──┐
                │          │
                │          │
                │  view    │
                │  │v│     │
                │  └─┘     │
                └──────────┘
                """
            },
            {
                Pos.Right, () => new View
                {
                    X = 2,
                    Y = 2,
                    Width = Dim.Auto (),
                    Height = Dim.Auto (),
                    Text = "view"
                },
                """
                ┌┤Dialog├──┐
                │          │
                │          │
                │  view    │
                │          │
                │          │
                │          │
                │      ┌─┐ │
                │      │v│ │
                │      └─┘ │
                └──────────┘
                """
            },
            {
                Pos.Top, () => new View
                {
                    X = 2,
                    Y = 2,
                    Width = Dim.Auto (),
                    Height = Dim.Auto (),
                    Text = "view"
                },
                """
                ┌┤Dialog├──┐
                │          │
                │          │
                │  view    │
                │  │v│     │
                │  └─┘     │
                └──────────┘

                """
            },
            {
                Pos.X, () => new View
                {
                    X = 2,
                    Y = 2,
                    Width = Dim.Auto (),
                    Height = Dim.Auto (),
                    Text = "view"
                },
                """
                ┌┤Dialog├──┐
                │          │
                │          │
                │  view    │
                │  │v│     │
                │  └─┘     │
                └──────────┘
                """
            },
            {
                Pos.Y, () => new View
                {
                    X = 2,
                    Y = 2,
                    Width = Dim.Auto (),
                    Height = Dim.Auto (),
                    Text = "view"
                },
                """
                ┌┤Dialog├──┐
                │          │
                │          │
                │  view    │
                │  │v│     │
                │  └─┘     │
                └──────────┘
                """
            }
        };

}
