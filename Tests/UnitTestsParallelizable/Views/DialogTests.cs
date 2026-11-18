using UnitTests;
// ReSharper disable AccessToDisposedClosure

namespace ViewsTests;

/// <summary>
///     Pure unit tests for <see cref=\"Dialog\"/> that don't require Application static dependencies.
///     These tests can run in parallel without interference.
/// </summary>
/// <remarks>
///     CoPilot - GitHub Copilot v4
/// </remarks>
public partial class DialogTests (ITestOutputHelper output) : TestDriverBase
{
    [Fact]
    public void Add_SubView_Updates_Dialog ()
    {
        Dialog dialog = new ();

        Label label = new () { Text = "Hello World", X = 0, Y = 0 };

        dialog.Add (label);

        Assert.Contains (label, dialog.SubViews);

        dialog.Dispose ();
    }

    [Fact]
    public void AddButton_Adds_Button_To_Dialog ()
    {
        Dialog dialog = new ();
        Button button = new () { Title = "OK" };

        dialog.AddButton (button);

        Assert.Single (dialog.Buttons);
        Assert.Equal ("OK", dialog.Buttons [0].Title);
        Assert.True (button.IsDefault);

        dialog.Dispose ();
    }

    [Fact]
    public void AddButton_Multiple_Buttons_Last_IsDefault ()
    {
        Dialog dialog = new ();
        Button button1 = new () { Title = "Cancel" };
        Button button2 = new () { Title = "OK" };
        Button button3 = new () { Title = "Apply" };

        dialog.AddButton (button1);
        dialog.AddButton (button2);
        dialog.AddButton (button3);

        Assert.Equal (3, dialog.Buttons.Length);
        Assert.False (button1.IsDefault);
        Assert.False (button2.IsDefault);
        Assert.True (button3.IsDefault);

        dialog.Dispose ();
    }

    [Fact]
    public void AddButton_Sets_Button_Position ()
    {
        Dialog dialog = new ();
        Button button = new () { Title = "OK" };

        dialog.AddButton (button);

        Assert.True (button.X.Has<PosAlign> (out _));
        Assert.Equal (1, button.Y);

        dialog.Dispose ();
    }

    [Fact]
    public void ButtonAlignment_Get_Set ()
    {
        Dialog dialog = new ();

        Assert.Equal (Alignment.End, dialog.ButtonAlignment);

        dialog.ButtonAlignment = Alignment.Start;
        Assert.Equal (Alignment.Start, dialog.ButtonAlignment);

        dialog.ButtonAlignment = Alignment.Center;
        Assert.Equal (Alignment.Center, dialog.ButtonAlignment);

        dialog.Dispose ();
    }

    [Theory]
    [InlineData (Alignment.Start)]
    [InlineData (Alignment.Center)]
    [InlineData (Alignment.End)]
    [InlineData (Alignment.Fill)]
    public void ButtonAlignment_Theory (Alignment alignment)
    {
        Dialog dialog = new () { ButtonAlignment = alignment };

        Assert.Equal (alignment, dialog.ButtonAlignment);

        dialog.Dispose ();
    }

    [Fact]
    public void ButtonAlignmentModes_Get_Set ()
    {
        Dialog dialog = new ();

        Assert.Equal (AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems, dialog.ButtonAlignmentModes);

        dialog.ButtonAlignmentModes = AlignmentModes.StartToEnd;
        Assert.Equal (AlignmentModes.StartToEnd, dialog.ButtonAlignmentModes);

        dialog.ButtonAlignmentModes = AlignmentModes.IgnoreFirstOrLast;
        Assert.Equal (AlignmentModes.IgnoreFirstOrLast, dialog.ButtonAlignmentModes);

        dialog.Dispose ();
    }

    [Fact]
    public void Buttons_Property_Set_Adds_Buttons ()
    {
        Dialog dialog = new ();

        Button [] buttons = [new () { Title = "Cancel" }, new () { Title = "OK" }];

        dialog.Buttons = buttons;

        Assert.Equal (2, dialog.Buttons.Length);
        Assert.Equal ("Cancel", dialog.Buttons [0].Title);
        Assert.Equal ("OK", dialog.Buttons [1].Title);
        Assert.True (dialog.Buttons [1].IsDefault);

        dialog.Dispose ();
    }

    [Fact]
    public void Canceled_False_When_Result_Null ()
    {
        Dialog dialog = new ();

        Assert.Null (dialog.Result);
        Assert.True (dialog.Canceled);

        dialog.Dispose ();
    }

    [Fact]
    public void Constructor_Initializes_DefaultValues ()
    {
        Dialog dialog = new ();

        Assert.NotNull (dialog);
        Assert.True (dialog.CanFocus);
        Assert.Equal (Alignment.End, dialog.ButtonAlignment);
        Assert.Equal (AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems, dialog.ButtonAlignmentModes);
        Assert.Equal (LineStyle.Heavy, dialog.BorderStyle);
        Assert.Equal (ShadowStyles.Transparent, dialog.ShadowStyle);
        Assert.Empty (dialog.Buttons);
        Assert.Null (dialog.Result);
        Assert.True (dialog.Canceled); // Canceled is true when Result is null
        Assert.Equal (ViewArrangement.Movable | ViewArrangement.Resizable | ViewArrangement.Overlapped, dialog.Arrangement);

        dialog.Dispose ();
    }

    [Fact]
    public void Constructor_Sets_AutoDimensions ()
    {
        Dialog dialog = new ();

        Assert.True (dialog.Width.Has<DimAuto> (out _));
        Assert.True (dialog.Height.Has<DimAuto> (out _));

        dialog.Dispose ();
    }

    [Fact]
    public void Constructor_Sets_Position_Center ()
    {
        Dialog dialog = new ();

        Assert.True (dialog.X.Has<PosCenter> (out _));
        Assert.True (dialog.Y.Has<PosCenter> (out _));

        dialog.Dispose ();
    }

    [Fact]
    public void Arrangement_Default ()
    {
        Dialog dialog = new ();

        Assert.Equal (ViewArrangement.Movable | ViewArrangement.Resizable | ViewArrangement.Overlapped, dialog.Arrangement);

        dialog.Dispose ();
    }

    [Fact]
    public void Border_Style_Can_Be_Changed ()
    {
        Dialog dialog = new () { BorderStyle = LineStyle.Single };

        Assert.Equal (LineStyle.Single, dialog.BorderStyle);

        dialog.BorderStyle = LineStyle.Double;
        Assert.Equal (LineStyle.Double, dialog.BorderStyle);

        dialog.Dispose ();
    }

    [Fact]
    public void CanFocus_Default_True ()
    {
        Dialog dialog = new ();

        Assert.True (dialog.CanFocus);

        dialog.Dispose ();
    }

    [Fact]
    public void Command_Accept_SetsResultAndStops ()
    {
        Dialog dialog = new () { Title = "Test" };
        Button button = new () { Text = "OK" };
        dialog.AddButton (button);

        var acceptingFired = false;

        dialog.Accepting += (_, e) =>
                            {
                                acceptingFired = true;
                                e.Handled = true;
                            };

        // Accept command on dialog should propagate through default button
        dialog.InvokeCommand (Command.Accept);

        // The accepting event on dialog fires
        Assert.True (acceptingFired);

        dialog.Dispose ();
    }

    [Fact]
    public void DialogButton_Accept_BubblesUp ()
    {
        Dialog dialog = new () { Title = "Test" };
        Button button = new () { Text = "OK" };
        dialog.AddButton (button);

        Assert.Equal (dialog.DefaultAcceptView, button);

        var buttonAcceptingFired = false;

        button.Accepting += (_, _) => { buttonAcceptingFired = true; };

        var dialogAcceptedFired = false;

        dialog.Accepted += (_, _) => { dialogAcceptedFired = true; };

        // Button's Accept should fire
        button.InvokeCommand (Command.Accept);

        Assert.True (buttonAcceptingFired);
        Assert.True (dialogAcceptedFired);

        dialog.Dispose ();
    }

    [Fact]
    public void Modal_DialogButton_Accept_BubblesUp ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Dialog dialog = new ();
        dialog.Title = "Test";
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
        Assert.Equal (1, dialog.Result);

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
    public void Modal_DialogButton_Cancel_BubblesUp ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Dialog dialog = new ();
        dialog.Title = "Test";
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

        Assert.Equal (0, dialogAcceptedFired); // 0 because Cancel's OnAccepting handled it; RaiseAccepted is not called
        Assert.Equal (1, cancelAcceptingFired);
        Assert.Equal (0, cancelAcceptedFired);
        Assert.Equal (0, okAcceptingFired);
        Assert.Equal (0, okAcceptedFired);
        Assert.Equal (0, dialog.Result);

        return;

        void AppOnIteration (object? sender, EventArgs<IApplication?> e)
        {
            cancelButton.InvokeCommand (Command.Accept);

            // Just in case
            app.Iteration -= AppOnIteration;
            Assert.True (dialog.StopRequested);
        }
    }

    [Fact]
    public void Disposes_Buttons ()
    {
        Dialog dialog = new ();
        Button button1 = new () { Title = "OK" };
        Button button2 = new () { Title = "Cancel" };

        dialog.AddButton (button1);
        dialog.AddButton (button2);

        Assert.Equal (2, dialog.Buttons.Length);

        dialog.Dispose ();

#if DEBUG_IDISPOSABLE

        // After disposal, buttons should be disposed through the dialog's disposal chain
        Assert.True (dialog.WasDisposed);
        Assert.True (button1.WasDisposed);
        Assert.True (button2.WasDisposed);
#endif
    }

    [Fact]
    public void ShadowStyle_Can_Be_Changed ()
    {
        Dialog dialog = new () { ShadowStyle = null };

        Assert.Null (dialog.ShadowStyle);

        dialog.ShadowStyle = ShadowStyles.Opaque;
        Assert.Equal (ShadowStyles.Opaque, dialog.ShadowStyle);

        dialog.Dispose ();
    }

    // Copilot
    [Fact]
    public void SchemeName_IsDialog_WhenNotRunning ()
    {
        // A Dialog uses the Dialog scheme by default, set in the constructor
        Dialog dialog = new ();

        Assert.Equal (SchemeManager.SchemesToSchemeName (Schemes.Dialog), dialog.SchemeName);

        dialog.Dispose ();
    }

    // Copilot
    [Fact]
    public void SchemeName_IsDialog_WhenRunning ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Dialog dialog = new ();

        string? schemeNameWhileRunning = null;

        app.Iteration += AppOnIteration;
        app.Run (dialog);
        app.Iteration -= AppOnIteration;

        Assert.Equal (SchemeManager.SchemesToSchemeName (Schemes.Dialog), schemeNameWhileRunning);

        return;

        void AppOnIteration (object? sender, EventArgs<IApplication?> e)
        {
            schemeNameWhileRunning = dialog.SchemeName;
            app.Iteration -= AppOnIteration;
            app.RequestStop ();
        }
    }

    // Copilot
    [Fact]
    public void Custom_SchemeName_And_Arrangement_Are_Not_Overwritten_By_Run ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Dialog dialog = new ();
        string customSchemeName = SchemeManager.SchemesToSchemeName (Schemes.Base)!;
        const ViewArrangement customArrangement = ViewArrangement.Overlapped;

        dialog.SchemeName = customSchemeName;
        dialog.Arrangement = customArrangement;

        string? schemeNameWhileRunning = null;
        ViewArrangement arrangementWhileRunning = default;

        app.Iteration += AppOnIteration;
        app.Run (dialog);
        app.Iteration -= AppOnIteration;

        Assert.Equal (customSchemeName, schemeNameWhileRunning);
        Assert.Equal (customArrangement, arrangementWhileRunning);
        Assert.Equal (customSchemeName, dialog.SchemeName);
        Assert.Equal (customArrangement, dialog.Arrangement);

        return;

        void AppOnIteration (object? sender, EventArgs<IApplication?> e)
        {
            schemeNameWhileRunning = dialog.SchemeName;
            arrangementWhileRunning = dialog.Arrangement;
            app.Iteration -= AppOnIteration;
            app.RequestStop ();
        }
    }

    [Fact]
    public void Text_Property ()
    {
        Dialog dialog = new () { Text = "This is a message" };

        Assert.Equal ("This is a message", dialog.Text);

        dialog.Text = "Updated message";
        Assert.Equal ("Updated message", dialog.Text);

        dialog.Dispose ();
    }

    [Fact]
    public void With_Multiple_SubViews ()
    {
        Dialog dialog = new () { Title = "Form" };

        Label nameLabel = new () { Text = "Name:", X = 0, Y = 0 };

        TextField nameField = new () { X = Pos.Right (nameLabel) + 1, Y = 0, Width = 20 };

        Label emailLabel = new () { Text = "Email:", X = 0, Y = 1 };

        TextField emailField = new () { X = Pos.Right (emailLabel) + 1, Y = 1, Width = 20 };

        dialog.Add (nameLabel, nameField, emailLabel, emailField);

        Button okButton = new () { Title = "OK" };
        dialog.AddButton (okButton);

        Assert.Equal (4, dialog.SubViews.Count);
        Assert.Single (dialog.Buttons);
        Assert.Contains (nameLabel, dialog.SubViews);
        Assert.Contains (nameField, dialog.SubViews);
        Assert.Contains (emailLabel, dialog.SubViews);
        Assert.Contains (emailField, dialog.SubViews);

        dialog.Dispose ();
    }

    [Fact]
    public void With_Title_And_Buttons ()
    {
        Dialog dialog = new () { Title = "Confirm" };

        Button cancelButton = new () { Title = "Cancel" };
        Button okButton = new () { Title = "OK" };

        dialog.AddButton (cancelButton);
        dialog.AddButton (okButton);

        Assert.Equal ("Confirm", dialog.Title);
        Assert.Equal (2, dialog.Buttons.Length);
        Assert.Equal ("Cancel", dialog.Buttons [0].Title);
        Assert.Equal ("OK", dialog.Buttons [1].Title);
        Assert.False (cancelButton.IsDefault);
        Assert.True (okButton.IsDefault);

        dialog.Dispose ();
    }

    [Fact]
    public void With_Wide_Character_Button_Text ()
    {
        Dialog dialog = new ();
        Button button = new () { Title = "确定" };

        dialog.AddButton (button);

        Assert.Single (dialog.Buttons);
        Assert.Equal ("确定", dialog.Buttons [0].Title);

        dialog.Dispose ();
    }

    [Fact]
    public void With_Wide_Character_Title ()
    {
        Dialog dialog = new () { Title = "你好世界" };

        Assert.Equal ("你好世界", dialog.Title);

        dialog.Dispose ();
    }

    [Fact]
    public void Empty_Has_No_Buttons ()
    {
        Dialog dialog = new ();

        Assert.Empty (dialog.Buttons);
        Assert.Null (dialog.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void Result_Can_Be_Set_And_Retrieved ()
    {
        Dialog dialog = new ();
        dialog.AddButton (new Button { Title = "Cancel" });
        dialog.AddButton (new Button { Title = "OK" });

        Assert.Null (dialog.Result);

        dialog.Result = 0;
        Assert.Equal (0, dialog.Result);

        dialog.Result = 1;
        Assert.Equal (1, dialog.Result);

        dialog.Result = null;
        Assert.Null (dialog.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void Title_Get_Set ()
    {
        Dialog dialog = new ();

        Assert.Equal (string.Empty, dialog.Title);

        dialog.Title = "Test Dialog";
        Assert.Equal ("Test Dialog", dialog.Title);

        dialog.Title = "你好";
        Assert.Equal ("你好", dialog.Title);

        dialog.Dispose ();
    }

    [Fact]
    public void EnableForDesign_Initializes_With_Content ()
    {
        IDriver driver = CreateTestDriver ();
        Dialog dialog = new () { Driver = driver };

        IDesignable designable = dialog;
        bool result = designable.EnableForDesign ();

        Assert.True (result);
        Assert.Equal ("Dialog Title", dialog.Title);
        Assert.Equal (2, dialog.Buttons.Length);
        Assert.Equal (Strings.btnCancel, dialog.Buttons [0].Title);
        Assert.Equal (Strings.btnOk, dialog.Buttons [1].Title);
        Assert.True (dialog.Buttons [1].IsDefault);

        // Should have label and textfield
        Assert.True (dialog.SubViews.Count >= 2);

        dialog.Dispose ();
    }
}
