namespace ViewsTests;

public class MessageBoxTests
{
    [Fact]
    public void KeyBindings_Esc_Closes ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        int? result = 999;
        var iteration = 0;

        void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
        {
            iteration++;

            switch (iteration)
            {
                case 1:
                    result = MessageBox.Query (app, string.Empty, string.Empty, false, "btn0", "btn1");
                    app.RequestStop ();

                    break;

                case 2:
                    app.Keyboard.RaiseKeyDownEvent (Key.Esc);

                    break;

                default:
                    Assert.Fail ();

                    break;
            }
        }

        try
        {
            app.Iteration += OnApplicationOnIteration;
            app.Run<Runnable<bool>> ();

            Assert.Null (result);
        }
        finally
        {
            app.Iteration -= OnApplicationOnIteration;
            app.Dispose ();
        }
    }

    [Theory]
    [MemberData (nameof (AcceptingKeys))]
    public void Enter_Or_Space_Returns_Default_Button_Index (Key key)
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        void OnApplicationOnIteration (object? o, EventArgs<IApplication?> iterationEventArgs) => Assert.True (app.Keyboard.RaiseKeyDownEvent (key));

        try
        {
            app.Iteration += OnApplicationOnIteration;
            int? res = MessageBox.Query (app, "hey", "IsDefault", Strings.btnNo, Strings.btnYes);

            Assert.Equal (1, res);
        }
        finally
        {
            app.Iteration -= OnApplicationOnIteration;
            app.Dispose ();
        }
    }

    [Fact]
    public void Esc_Returns_Null ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        void OnApplicationOnIteration (object? o, EventArgs<IApplication?> iterationEventArgs) => Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Esc));

        try
        {
            app.Iteration += OnApplicationOnIteration;
            int? res = MessageBox.Query (app, "hey", "IsDefault", "_No", "_Yes");

            Assert.Null (res);
        }
        finally
        {
            app.Iteration -= OnApplicationOnIteration;
            app.Dispose ();
        }
    }

    public static IEnumerable<object []> AcceptingKeys ()
    {
        yield return [Key.Enter];
        yield return [Key.Space];
    }

    // Copilot
    [Fact]
    public void Query_Uses_Last_Button_As_Default ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        int? result = null;
        var iteration = 0;

        void OnApplicationOnIteration (object? sender, EventArgs<IApplication?> args)
        {
            iteration++;

            if (iteration != 1)
            {
                return;
            }

            Dialog dialog = (Dialog)app.TopRunnableView!;
            Button [] buttons = dialog.Buttons;

            Assert.False (buttons [0].IsDefault);
            Assert.True (buttons [1].IsDefault);
            Assert.Same (buttons [1], dialog.DefaultAcceptView);
            Assert.True (buttons [1].HasFocus);

            app.RequestStop ();
        }

        try
        {
            app.Iteration += OnApplicationOnIteration;
            result = MessageBox.Query (app, "hey", "IsDefault", Strings.btnNo, Strings.btnYes);

            Assert.Null (result);
        }
        finally
        {
            app.Iteration -= OnApplicationOnIteration;
            app.Dispose ();
        }
    }

    // Copilot
    [Fact]
    public void ErrorQuery_Uses_Last_Button_As_Default ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        int? result = null;
        var iteration = 0;

        void OnApplicationOnIteration (object? sender, EventArgs<IApplication?> args)
        {
            iteration++;

            if (iteration != 1)
            {
                return;
            }

            Dialog dialog = (Dialog)app.TopRunnableView!;
            Button [] buttons = dialog.Buttons;

            Assert.False (buttons [0].IsDefault);
            Assert.True (buttons [1].IsDefault);
            Assert.Same (buttons [1], dialog.DefaultAcceptView);
            Assert.True (buttons [1].HasFocus);

            app.RequestStop ();
        }

        try
        {
            app.Iteration += OnApplicationOnIteration;
            result = MessageBox.ErrorQuery (app, "Error", "Message", Strings.btnNo, Strings.btnYes);

            Assert.Null (result);
        }
        finally
        {
            app.Iteration -= OnApplicationOnIteration;
            app.Dispose ();
        }
    }

    // Claude - Opus 4.5
    [Fact]
    public void Query_Sets_Dialog_SchemeName ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        string? schemeName = null;
        var iteration = 0;

        void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
        {
            iteration++;

            if (iteration == 1)
            {
                // Capture the SchemeName from the running dialog
                var dialog = app.TopRunnableView as Dialog;
                Assert.NotNull (dialog);
                schemeName = dialog.SchemeName;
                app.RequestStop ();
            }
        }

        try
        {
            app.Iteration += OnApplicationOnIteration;
            int? result = MessageBox.Query (app, "Test", "Message", "OK");

            Assert.Equal ("Dialog", schemeName);
        }
        finally
        {
            app.Iteration -= OnApplicationOnIteration;
            app.Dispose ();
        }
    }

    // Claude - Opus 4.5
    [Fact]
    public void ErrorQuery_Sets_Error_SchemeName ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        string? schemeName = null;
        var iteration = 0;

        void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
        {
            iteration++;

            if (iteration == 1)
            {
                // Capture the SchemeName from the running dialog
                var dialog = app.TopRunnableView as Dialog;
                Assert.NotNull (dialog);
                schemeName = dialog.SchemeName;
                app.RequestStop ();
            }
        }

        try
        {
            app.Iteration += OnApplicationOnIteration;
            int? result = MessageBox.ErrorQuery (app, "Error", "Error Message", "OK");

            Assert.Equal ("Error", schemeName);
        }
        finally
        {
            app.Iteration -= OnApplicationOnIteration;
            app.Dispose ();
        }
    }
}
