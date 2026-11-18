using UnitTests;
// ReSharper disable AccessToDisposedClosure

namespace ViewsTests;

public partial class DialogTests
{
    // Claude - Opus 4.5

    /// <summary>
    ///     Test dialog that returns a <see cref="Color"/> result.
    /// </summary>
    private class TestColorDialog : Dialog<Color>
    {
        public TestColorDialog ()
        {
            Title = "Select Color";
            AddButton (new Button { Title = "Cancel" });
            AddButton (new Button { Title = "OK" });
        }

        public Color SelectedColor { get; init; } = Color.Blue;

        protected override bool OnAccepting (CommandEventArgs args)
        {
            if (base.OnAccepting (args))
            {
                return true;
            }
            Result = SelectedColor;

            return false;
        }
    }

    /// <summary>
    ///     Test dialog that returns a <see cref="string"/> result.
    /// </summary>
    private class TestStringDialog : Dialog<string>
    {
        public TestStringDialog ()
        {
            Title = "Enter Text";
            AddButton (new Button { Title = "Cancel" });
            AddButton (new Button { Title = "OK" });
        }

        public string InputText { get; set; } = string.Empty;

        /// <inheritdoc/>
        protected override bool OnAccepting (CommandEventArgs args)
        {
            if (base.OnAccepting (args))
            {
                return true;
            }
            Result = InputText;

            return false;
        }
    }

    /// <summary>
    ///     Test dialog that returns a <see cref="DateTime"/> result.
    /// </summary>
    private class TestDateDialog : Dialog<DateTime?>
    {
        private readonly DatePicker _datePicker = new () { Value = new DateTime (1966, 9, 10) };

        public TestDateDialog ()
        {
            Title = "Select Date";
            AddButton (new Button { Title = "Cancel" });
            AddButton (new Button { Title = "OK" });

            Add (_datePicker);
        }

        protected override void OnAccepted (ICommandContext? ctx)
        {
            base.OnAccepted (ctx);
            Result = SelectedDate;
        }

        public DateTime SelectedDate { get => _datePicker.Value; set => _datePicker.Value = value; }
    }

    [Fact]
    public void Generic_DialogButton_Accept_BubblesUp ()
    {
        TestDateDialog dialog = new () { Title = "Test" };

        DateTime selectedDate = new (1966, 9, 10);

        var dialogAcceptedFired = false;

        dialog.Accepted += (_, _) => { dialogAcceptedFired = true; };

        dialog.Buttons [1].InvokeCommand (Command.Accept);

        Assert.True (dialogAcceptedFired);

        Assert.Equal (selectedDate, dialog.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void Generic_Modal_DialogButton_Accept_BubblesUp ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using TestDateDialog dialog = new ();
        dialog.Title = "Test";
        DateTime newDateTime = dialog.SelectedDate.AddYears (1);

        dialog.SelectedDate = newDateTime;

        Button cancelButton = new () { Text = "Cancel" };
        dialog.AddButton (cancelButton);
        Button okButton = new () { Text = "OK" };
        dialog.AddButton (okButton);

        var dialogAcceptedFired = 0;

        dialog.Accepted += (_, _) => { dialogAcceptedFired++; };

        var cancelAcceptingFired = 0;
        cancelButton.Accepting += (_, _) => { cancelAcceptingFired++; };

        var cancelAcceptedFired = 0;
        cancelButton.Accepted += (_, _) => { cancelAcceptedFired++; };

        var okAcceptingFired = 0;
        okButton.Accepting += (_, _) => { okAcceptingFired++; };

        var okAcceptedFired = 0;
        okButton.Accepted += (_, _) => { okAcceptedFired++; };

        app.Iteration += AppOnIteration;
        app.Run (dialog);
        app.Iteration -= AppOnIteration;

        Assert.Equal (1, dialogAcceptedFired);
        Assert.Equal (0, cancelAcceptingFired);
        Assert.Equal (0, cancelAcceptedFired);
        Assert.Equal (1, okAcceptingFired);
        Assert.Equal (0, okAcceptedFired);
        Assert.Equal (newDateTime, dialog.Result);

        return;

        void AppOnIteration (object? sender, EventArgs<IApplication?> e)
        {
            okButton.InvokeCommand (Command.Accept);

            // Just in case
            app.Iteration -= AppOnIteration;
            Assert.True (dialog.StopRequested);
        }
    }

    [Fact]
    public void Generic_Modal_Dialog_Command_Accept_BubblesUp_TestStringDialog ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using TestStringDialog dialog = new ();
        dialog.Title = "Test";
        var newString = "new";

        dialog.InputText = newString;

        Button cancelButton = new () { Text = "Cancel" };
        dialog.AddButton (cancelButton);
        Button okButton = new () { Text = "OK" };
        dialog.AddButton (okButton);

        var dialogAcceptedFired = 0;

        dialog.Accepted += (_, _) => { dialogAcceptedFired++; };

        var cancelAcceptingFired = 0;
        cancelButton.Accepting += (_, _) => { cancelAcceptingFired++; };

        var cancelAcceptedFired = 0;
        cancelButton.Accepted += (_, _) => { cancelAcceptedFired++; };

        var okAcceptingFired = 0;
        okButton.Accepting += (_, _) => { okAcceptingFired++; };

        var okAcceptedFired = 0;
        okButton.Accepted += (_, _) => { okAcceptedFired++; };

        app.Iteration += AppOnIteration;
        app.Run (dialog);
        app.Iteration -= AppOnIteration;

        Assert.Equal (1, dialogAcceptedFired);
        Assert.Equal (0, cancelAcceptingFired);
        Assert.Equal (0, cancelAcceptedFired);
        Assert.Equal (1, okAcceptingFired);
        Assert.Equal (1, okAcceptedFired);
        Assert.Equal (newString, dialog.Result);

        return;

        void AppOnIteration (object? sender, EventArgs<IApplication?> e)
        {
            dialog.InvokeCommand (Command.Accept);

            // Just in case
            app.Iteration -= AppOnIteration;
            Assert.True (dialog.StopRequested);
        }
    }

    [Fact]
    public void Generic_Modal_Dialog_Command_Accept_BubblesUp_TestDateDialog ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using TestDateDialog dialog = new ();
        dialog.Title = "Test";
        DateTime newDateTime = dialog.SelectedDate.AddYears (1);

        dialog.SelectedDate = newDateTime;

        Button cancelButton = new () { Text = "Cancel" };
        dialog.AddButton (cancelButton);
        Button okButton = new () { Text = "OK" };
        dialog.AddButton (okButton);

        var dialogAcceptedFired = 0;

        dialog.Accepted += (_, _) => { dialogAcceptedFired++; };

        var cancelAcceptingFired = 0;
        cancelButton.Accepting += (_, _) => { cancelAcceptingFired++; };

        var cancelAcceptedFired = 0;
        cancelButton.Accepted += (_, _) => { cancelAcceptedFired++; };

        var okAcceptingFired = 0;
        okButton.Accepting += (_, _) => { okAcceptingFired++; };

        var okAcceptedFired = 0;
        okButton.Accepted += (_, _) => { okAcceptedFired++; };

        app.Iteration += AppOnIteration;
        app.Run (dialog);
        app.Iteration -= AppOnIteration;

        Assert.Equal (1, dialogAcceptedFired);
        Assert.Equal (0, cancelAcceptingFired);
        Assert.Equal (0, cancelAcceptedFired);
        Assert.Equal (1, okAcceptingFired);
        Assert.Equal (1, okAcceptedFired);
        Assert.Equal (newDateTime, dialog.Result);

        return;

        void AppOnIteration (object? sender, EventArgs<IApplication?> e)
        {
            dialog.InvokeCommand (Command.Accept);

            // Just in case
            app.Iteration -= AppOnIteration;

            Assert.True (dialog.StopRequested);
        }
    }

    [Fact]
    public void Generic_Modal_Dialog_DatePicker_Accept_BubblesUp_TestDateDialog ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using TestDateDialog dialog = new ();

        DatePicker datePicker = dialog.SubViews.OfType<DatePicker> ().FirstOrDefault ()
                                ?? throw new InvalidOperationException ("DatePicker not found in dialog.");
        dialog.Title = "Test";
        DateTime newDateTime = dialog.SelectedDate.AddYears (1);

        dialog.SelectedDate = newDateTime;

        Button cancelButton = new () { Text = "Cancel" };
        dialog.AddButton (cancelButton);
        Button okButton = new () { Text = "OK" };
        dialog.AddButton (okButton);

        var dialogAcceptedFired = 0;

        dialog.Accepted += (_, _) => { dialogAcceptedFired++; };

        var cancelAcceptingFired = 0;
        cancelButton.Accepting += (_, _) => { cancelAcceptingFired++; };

        var cancelAcceptedFired = 0;
        cancelButton.Accepted += (_, _) => { cancelAcceptedFired++; };

        var okAcceptingFired = 0;
        okButton.Accepting += (_, _) => { okAcceptingFired++; };

        var okAcceptedFired = 0;
        okButton.Accepted += (_, _) => { okAcceptedFired++; };

        app.Iteration += AppOnIteration;
        app.Run (dialog);
        app.Iteration -= AppOnIteration;

        Assert.Equal (1, dialogAcceptedFired);
        Assert.Equal (0, cancelAcceptingFired);
        Assert.Equal (0, cancelAcceptedFired);
        Assert.Equal (1, okAcceptingFired);
        Assert.Equal (1, okAcceptedFired);
        Assert.Equal (newDateTime, dialog.Result);

        return;

        void AppOnIteration (object? sender, EventArgs<IApplication?> e)
        {
            datePicker.InvokeCommand (Command.Accept);

            // Just in case
            app.Iteration -= AppOnIteration;

            Assert.True (dialog.StopRequested);
        }
    }

    [Fact]
    public void Generic_Modal_DialogButton_Cancel_BubblesUp ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using TestDateDialog dialog = new ();
        dialog.Title = "Test";

        Button cancelButton = new () { Text = "Cancel" };
        dialog.AddButton (cancelButton);
        Button okButton = new () { Text = "OK" };
        dialog.AddButton (okButton);

        var dialogAcceptingFired = 0;
        dialog.Accepting += (_, _) => { dialogAcceptingFired++; };

        var dialogAcceptedFired = 0;
        dialog.Accepted += (_, _) => { dialogAcceptedFired++; };

        var cancelAcceptingFired = 0;
        cancelButton.Accepting += (_, _) => { cancelAcceptingFired++; };

        var cancelAcceptedFired = 0;
        cancelButton.Accepted += (_, _) => { cancelAcceptedFired++; };

        var okAcceptingFired = 0;
        okButton.Accepting += (_, _) => { okAcceptingFired++; };

        var okAcceptedFired = 0;
        okButton.Accepted += (_, _) => { okAcceptedFired++; };

        app.Iteration += AppOnIteration;
        app.Run (dialog);
        app.Iteration -= AppOnIteration;

        Assert.Equal (1, cancelAcceptingFired);
        Assert.Equal (0, cancelAcceptedFired);
        Assert.Equal (0, dialogAcceptingFired);
        Assert.Equal (0, dialogAcceptedFired);
        Assert.Equal (0, okAcceptingFired);
        Assert.Equal (0, okAcceptedFired);
        Assert.Null (dialog.Result);

        return;

        void AppOnIteration (object? sender, EventArgs<IApplication?> e)
        {
            cancelButton.InvokeCommand (Command.Accept);

            // Just in case
            app.Iteration -= AppOnIteration;
            Assert.True (dialog.StopRequested);
        }
    }

    // Claude - Opus 4.6
    [Fact]
    public void Generic_Modal_Dialog_EnterKey_Accepts_Dialog ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using TestDateDialog dialog = new ();
        dialog.Title = "Test";
        DateTime newDateTime = dialog.SelectedDate.AddYears (1);

        dialog.SelectedDate = newDateTime;

        var dialogAcceptedFired = 0;

        dialog.Accepted += (_, _) => { dialogAcceptedFired++; };

        app.Iteration += AppOnIteration;
        app.Run (dialog);
        app.Iteration -= AppOnIteration;

        Assert.Equal (1, dialogAcceptedFired);
        Assert.Equal (newDateTime, dialog.Result);

        return;

        void AppOnIteration (object? sender, EventArgs<IApplication?> e)
        {
            app.Iteration -= AppOnIteration;

            // Simulate pressing Enter key via Application key processing
            app.Keyboard.RaiseKeyDownEvent (Key.Enter);

            if (dialog.StopRequested)
            {
                return;
            }

            // Enter didn't work - get debug info and force stop
            View? focused = dialog.Focused;
            View? deepFocused = dialog.MostFocused;

            dialog.RequestStop ();

            Assert.Fail ($"Enter key did not accept dialog. Focused={
                focused?.GetType ().Name ?? "null"
            } ({
                focused?.Id
            }), MostFocused={
                deepFocused?.GetType ().Name ?? "null"
            } ({
                deepFocused?.Id
            })");
        }
    }

    // Claude - Opus 4.6
    [Fact]
    public void NonGeneric_Modal_Dialog_EnterKey_Accepts_Dialog ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Dialog dialog = new ();
        dialog.Title = "Test";

        Button cancelButton = new () { Text = "Cancel" };
        dialog.AddButton (cancelButton);
        Button okButton = new () { Text = "OK" };
        dialog.AddButton (okButton);

        Label label = new () { Text = "Press Enter" };
        dialog.Add (label);

        app.Iteration += AppOnIteration;
        app.Run (dialog);
        app.Iteration -= AppOnIteration;

        // Enter on the focused button (Cancel, the first button) should stop the dialog
        Assert.True (dialog.StopRequested);

        // Cancel button (index 0) is focused by default, so pressing Enter accepts with Result=0
        Assert.Equal (0, dialog.Result);

        return;

        void AppOnIteration (object? sender, EventArgs<IApplication?> e)
        {
            app.Iteration -= AppOnIteration;

            // Simulate pressing Enter key via Application key processing
            app.Keyboard.RaiseKeyDownEvent (Key.Enter);

            if (dialog.StopRequested)
            {
                return;
            }

            dialog.RequestStop ();

            Assert.Fail ("Enter key did not accept dialog.");
        }
    }

    [Fact]
    public void GenericConstructor_Initializes_DefaultValues ()
    {
        TestColorDialog dialog = new ();

        Assert.NotNull (dialog);
        Assert.True (dialog.CanFocus);
        Assert.Equal ("Select Color", dialog.Title);
        Assert.Equal (2, dialog.Buttons.Length);
        Assert.Null (((IRunnable)dialog).Result); // Check via IRunnable for nullable object?

        dialog.Dispose ();
    }

    [Fact]
    public void GenericResult_Is_Null_Initially ()
    {
        TestColorDialog colorDialog = new ();
        TestStringDialog stringDialog = new ();
        TestDateDialog dateDialog = new ();

        // Check via IRunnable for nullable object?
        Assert.Null (((IRunnable)colorDialog).Result);
        Assert.Null (((IRunnable)stringDialog).Result);
        Assert.Null (((IRunnable)dateDialog).Result);

        colorDialog.Dispose ();
        stringDialog.Dispose ();
        dateDialog.Dispose ();
    }

    [Fact]
    public void GenericColor_Result_Can_Be_Set ()
    {
        TestColorDialog dialog = new ();

        Assert.Null (((IRunnable)dialog).Result); // Check via IRunnable for nullable object?

        dialog.Result = Color.Red;
        Assert.Equal (Color.Red, dialog.Result);

        dialog.Result = Color.Green;
        Assert.Equal (Color.Green, dialog.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericString_Input_Can_Be_Set ()
    {
        TestStringDialog dialog = new () { InputText = "Initial Text" };

        Assert.Null (dialog.Result);

        dialog.InvokeCommand (Command.Accept);

        Assert.Equal (dialog.InputText, dialog.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericString_Result_Can_Be_Set ()
    {
        TestStringDialog dialog = new ();

        Assert.Null (dialog.Result);

        dialog.Result = "Hello";
        Assert.Equal ("Hello", dialog.Result);

        dialog.Result = "World";
        Assert.Equal ("World", dialog.Result);

        dialog.Result = null;
        Assert.Null (dialog.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericString_Command_Accept_BubblesUp ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using TestStringDialog dialog = new ();
        dialog.Title = "Test";
        var newString = "new";

        dialog.InputText = newString;

        Button cancelButton = new () { Text = "Cancel" };
        dialog.AddButton (cancelButton);
        Button okButton = new () { Text = "OK" };
        dialog.AddButton (okButton);

        var dialogAcceptedFired = 0;

        dialog.Accepted += (_, _) => { dialogAcceptedFired++; };

        var cancelAcceptingFired = 0;
        cancelButton.Accepting += (_, _) => { cancelAcceptingFired++; };

        var cancelAcceptedFired = 0;
        cancelButton.Accepted += (_, _) => { cancelAcceptedFired++; };

        var okAcceptingFired = 0;
        okButton.Accepting += (_, _) => { okAcceptingFired++; };

        var okAcceptedFired = 0;
        okButton.Accepted += (_, _) => { okAcceptedFired++; };

        dialog.InvokeCommand (Command.Accept);

        Assert.Equal (newString, dialog.Result);
        Assert.Equal (1, dialogAcceptedFired);
        Assert.Equal (0, cancelAcceptingFired);
        Assert.Equal (0, cancelAcceptedFired);
        Assert.Equal (1, okAcceptingFired);
        Assert.Equal (1, okAcceptedFired);
    }

    [Fact]
    public void GenericDateTime_Result_Can_Be_Set ()
    {
        TestDateDialog dialog = new ();
        DateTime testDate = new (2024, 6, 15);

        Assert.Null (((IRunnable)dialog).Result); // Check via IRunnable for nullable object?

        dialog.Result = testDate;
        Assert.Equal (testDate, dialog.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void Generic_Ok_Command_Accept_Sets_Result ()
    {
        TestColorDialog dialog = new () { SelectedColor = Color.Magenta };

        dialog.Buttons [1].InvokeCommand (Command.Accept);

        Assert.Equal (Color.Magenta, dialog.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void Generic_Cancel_Command_Accept_Does_Not_Set_Result ()
    {
        TestColorDialog dialog = new () { SelectedColor = Color.Cyan };

        // Simulate pressing Cancel button (index 0) - Result stays null
        dialog.Buttons [0].InvokeCommand (Command.Accept);

        Assert.Null (((IRunnable)dialog).Result);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericIRunnable_Result_Returns_Object ()
    {
        TestColorDialog dialog = new ();

        IRunnable runnable = dialog;
        Assert.Null (runnable.Result);

        dialog.Result = Color.Yellow;

        // IRunnable.Result returns the boxed value
        Assert.NotNull (runnable.Result);
        Assert.IsType<Color> (runnable.Result);
        Assert.Equal (Color.Yellow, (Color)runnable.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericBaseClass_Result_Casts_Correctly ()
    {
        TestStringDialog dialog = new ();

        // Set via typed property
        dialog.Result = "Test Value";
        Assert.Equal ("Test Value", dialog.Result);

        // Access via IRunnable
        IRunnable runnable = dialog;
        Assert.Equal ("Test Value", runnable.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericInherits_Properties ()
    {
        TestColorDialog dialog = new ();

        // Should inherit all Dialog properties
        Assert.Equal (Alignment.End, dialog.ButtonAlignment);
        Assert.Equal (AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems, dialog.ButtonAlignmentModes);
        Assert.Equal (LineStyle.Heavy, dialog.BorderStyle);
        Assert.Equal (ShadowStyles.Transparent, dialog.ShadowStyle);
        Assert.Equal (ViewArrangement.Movable | ViewArrangement.Resizable | ViewArrangement.Overlapped, dialog.Arrangement);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericButtons_Can_Be_Added ()
    {
        Dialog<Color> dialog = new ();

        Assert.Empty (dialog.Buttons);

        Button button1 = new () { Title = "First" };
        Button button2 = new () { Title = "Second" };

        dialog.AddButton (button1);
        dialog.AddButton (button2);

        Assert.Equal (2, dialog.Buttons.Length);
        Assert.Equal ("First", dialog.Buttons [0].Title);
        Assert.Equal ("Second", dialog.Buttons [1].Title);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericCan_Add_SubViews ()
    {
        TestColorDialog dialog = new ();

        Label label = new () { Text = "Choose a color:" };
        dialog.Add (label);

        Assert.Contains (label, dialog.SubViews);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericTitle_Can_Be_Set ()
    {
        Dialog<string> dialog = new () { Title = "Custom Title" };

        Assert.Equal ("Custom Title", dialog.Title);

        dialog.Title = "Changed Title";
        Assert.Equal ("Changed Title", dialog.Title);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericMultiple_Result_Types ()
    {
        // Test various result types work correctly
        Dialog<int> intDialog = new ();
        Dialog<bool> boolDialog = new ();
        Dialog<double> doubleDialog = new ();
        Dialog<Guid> guidDialog = new ();

        intDialog.Result = 42;
        Assert.Equal (42, intDialog.Result);

        boolDialog.Result = true;
        Assert.True (boolDialog.Result);

        doubleDialog.Result = 3.14159;
        Assert.Equal (3.14159, doubleDialog.Result);

        var testGuid = Guid.NewGuid ();
        guidDialog.Result = testGuid;
        Assert.Equal (testGuid, guidDialog.Result);

        intDialog.Dispose ();
        boolDialog.Dispose ();
        doubleDialog.Dispose ();
        guidDialog.Dispose ();
    }

    [Fact]
    public void NonGenericIs_DialogOfInt ()
    {
        Dialog dialog = new ();
        dialog.AddButton (new Button { Title = "Cancel" });
        dialog.AddButton (new Button { Title = "OK" });

        // Verify Dialog inherits from Dialog<int>
        Assert.IsAssignableFrom<Dialog<int>> (dialog);

        // Result should work as int
        dialog.Result = 1;
        Assert.Equal (1, dialog.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void NonGenericCanceled_Works_As_Expected ()
    {
        Dialog dialog = new ();
        dialog.AddButton (new Button { Title = "Cancel" });
        dialog.AddButton (new Button { Title = "OK" });

        // Initially null result means canceled
        Assert.Null (dialog.Result);
        Assert.True (dialog.Canceled);

        // Result 0 means Cancel button pressed
        dialog.Result = 0;
        Assert.True (dialog.Canceled);

        // Result 1 means OK button pressed
        dialog.Result = 1;
        Assert.False (dialog.Canceled);

        dialog.Dispose ();
    }

    [Fact]
    public void NonGenericResult_Throws_With_Invalid_Value ()
    {
        Dialog dialog = new ();
        dialog.AddButton (new Button { Title = "Cancel" });
        dialog.AddButton (new Button { Title = "OK" });

        Assert.Throws<ArgumentOutOfRangeException> (() => dialog.Result = -1);
        Assert.Throws<ArgumentOutOfRangeException> (() => dialog.Result = 2);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericLayout_Works ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        TestColorDialog dialog = new () { Driver = driver };

        dialog.Layout ();

        // Should calculate size correctly
        Assert.True (dialog.Frame.Width > 0);
        Assert.True (dialog.Frame.Height > 0);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericDraws_Correctly ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (30, 10);

        Dialog<Color> dialog = new ()
        {
            X = 0,
            Y = 0,
            BorderStyle = LineStyle.Single,
            ShadowStyle = null,
            Driver = driver
        };

        dialog.Layout ();
        dialog.Draw ();

        var expected = """
                       ┌────┐
                       └────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, driver);

        dialog.Dispose ();
    }

    [Fact]
    public void Generic_With_Buttons_Draws_Correctly ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (30, 10);

        Dialog<Color> dialog = new ()
        {
            X = 0,
            Y = 0,
            BorderStyle = LineStyle.Single,
            ShadowStyle = null,
            Driver = driver
        };

        Button cancelButton = new ()
        {
            Title = "No",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };

        Button okButton = new ()
        {
            Title = "Yes",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (cancelButton);
        dialog.AddButton (okButton);

        dialog.Layout ();
        dialog.Draw ();

        var expected = """
                       ┌─────┐
                       │     │
                       │NoYes│
                       └─────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, driver);

        dialog.Dispose ();
    }

}
