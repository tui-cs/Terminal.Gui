namespace ViewsTests;


public class WizardStepTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Initializes_Properties ()
    {
        // Arrange & Act
        WizardStep step = new ();

        // Assert
        Assert.NotNull (step);
        Assert.True (step.CanFocus);
        Assert.Equal (TabBehavior.TabStop, step.TabStop);
        Assert.Null (step.BorderStyle);
        Assert.IsType<DimFill> (step.Width);
        Assert.IsType<DimFill> (step.Height);
    }

    [Fact]
    public void Constructor_Sets_Default_Button_Text ()
    {
        // Arrange & Act
        WizardStep step = new ();

        // Assert
        Assert.Equal (string.Empty, step.BackButtonText);
        Assert.Equal (string.Empty, step.NextButtonText);
    }

    #endregion Constructor Tests

    #region Title Tests

    [Fact]
    public void Title_Can_Be_Set ()
    {
        // Arrange
        WizardStep step = new ();
        string title = "Test Step";

        // Act
        step.Title = title;

        // Assert
        Assert.Equal (title, step.Title);
    }

    [Fact]
    public void Title_Change_Raises_TitleChanged_Event ()
    {
        // Arrange
        WizardStep step = new ();
        var eventRaised = false;
        string? newTitle = null;

        step.TitleChanged += (sender, args) =>
                             {
                                 eventRaised = true;
                                 newTitle = args.Value;
                             };

        // Act
        step.Title = "New Title";

        // Assert
        Assert.True (eventRaised);
        Assert.Equal ("New Title", newTitle);
    }

    #endregion Title Tests

    #region HelpText Tests

    [Fact]
    public void HelpText_Can_Be_Set ()
    {
        // Arrange
        WizardStep step = new ();
        string helpText = "This is help text";

        // Act
        step.HelpText = helpText;

        // Assert
        Assert.Equal (helpText, step.HelpText);
    }

    [Fact]
    public void HelpText_Empty_By_Default ()
    {
        // Arrange & Act
        WizardStep step = new ();

        // Assert
        Assert.Equal (string.Empty, step.HelpText);
    }

    [Fact]
    public void HelpText_Setting_Calls_ShowHide ()
    {
        // Arrange
        WizardStep step = new ();
        step.BeginInit ();
        step.EndInit ();

        // Act - Setting help text should adjust padding
        step.HelpText = "Help text content";

        // Assert - Padding should have right thickness when help text is present
        Assert.True (step.Padding.Thickness.Right > 0);
    }

    [Fact]
    public void HelpText_Clearing_Removes_Padding ()
    {
        // Arrange
        WizardStep step = new ();
        step.BeginInit ();
        step.EndInit ();
        step.HelpText = "Help text content";

        // Act - Clear help text
        step.HelpText = string.Empty;

        // Assert - Padding right should be 0 when help text is empty
        Assert.Equal (0, step.Padding.Thickness.Right);
    }

    #endregion HelpText Tests

    #region BackButtonText Tests

    [Fact]
    public void BackButtonText_Can_Be_Set ()
    {
        // Arrange
        WizardStep step = new ();
        string buttonText = "Previous";

        // Act
        step.BackButtonText = buttonText;

        // Assert
        Assert.Equal (buttonText, step.BackButtonText);
    }

    [Fact]
    public void BackButtonText_Empty_By_Default ()
    {
        // Arrange & Act
        WizardStep step = new ();

        // Assert
        Assert.Equal (string.Empty, step.BackButtonText);
    }

    #endregion BackButtonText Tests

    #region NextButtonText Tests

    [Fact]
    public void NextButtonText_Can_Be_Set ()
    {
        // Arrange
        WizardStep step = new ();
        string buttonText = "Continue";

        // Act
        step.NextButtonText = buttonText;

        // Assert
        Assert.Equal (buttonText, step.NextButtonText);
    }

    [Fact]
    public void NextButtonText_Empty_By_Default ()
    {
        // Arrange & Act
        WizardStep step = new ();

        // Assert
        Assert.Equal (string.Empty, step.NextButtonText);
    }

    #endregion NextButtonText Tests

    #region SubView Tests

    [Fact]
    public void Can_Add_SubViews ()
    {
        // Arrange
        WizardStep step = new ();
        Label label = new () { Text = "Test Label" };

        // Act
        step.Add (label);

        // Assert
        Assert.Contains (label, step.SubViews);
    }

    [Fact]
    public void Can_Add_Multiple_SubViews ()
    {
        // Arrange
        WizardStep step = new ();
        Label label1 = new () { Text = "Label 1" };
        Label label2 = new () { Text = "Label 2" };
        TextField textField = new () { Width = 10 };

        // Act
        step.Add (label1, label2, textField);

        // Assert
        Assert.Equal (3, step.SubViews.Count);
        Assert.Contains (label1, step.SubViews);
        Assert.Contains (label2, step.SubViews);
        Assert.Contains (textField, step.SubViews);
    }

    #endregion SubView Tests

    #region Enabled Tests

    [Fact]
    public void Enabled_True_By_Default ()
    {
        // Arrange & Act
        WizardStep step = new ();

        // Assert
        Assert.True (step.Enabled);
    }

    [Fact]
    public void Enabled_Can_Be_Set_To_False ()
    {
        // Arrange
        WizardStep step = new ();

        // Act
        step.Enabled = false;

        // Assert
        Assert.False (step.Enabled);
    }

    [Fact]
    public void Enabled_Change_Raises_EnabledChanged_Event ()
    {
        // Arrange
        WizardStep step = new ();
        var eventRaised = false;

        step.EnabledChanged += (sender, args) => { eventRaised = true; };

        // Act
        step.Enabled = false;

        // Assert
        Assert.True (eventRaised);
    }

    #endregion Enabled Tests

    #region Visible Tests

    [Fact]
    public void Visible_True_By_Default ()
    {
        // Arrange & Act
        WizardStep step = new ();

        // Assert
        Assert.True (step.Visible);
    }

    [Fact]
    public void Visible_Can_Be_Changed ()
    {
        // Arrange
        WizardStep step = new ();

        // Act
        step.Visible = false;

        // Assert
        Assert.False (step.Visible);
    }

    #endregion Visible Tests

    #region HelpTextView Tests

    [Fact]
    public void HelpTextView_Added_To_Padding ()
    {
        // Arrange
        WizardStep step = new ();
        step.BeginInit ();
        step.EndInit ();

        // Act
        step.HelpText = "Help content";

        // Assert
        // The help text view should be in the Padding
        Assert.True (step.Padding.View!.SubViews.Count > 0);
    }

    [Fact]
    public void HelpTextView_Visible_When_HelpText_Set ()
    {
        // Arrange
        WizardStep step = new ();
        step.BeginInit ();
        step.EndInit ();

        // Act
        step.HelpText = "Help content";

        // Assert
        // When help text is set, padding right should be non-zero
        Assert.True (step.Padding.Thickness.Right > 0);
    }

    [Fact]
    public void HelpTextView_Hidden_When_HelpText_Empty ()
    {
        // Arrange
        WizardStep step = new ();
        step.BeginInit ();
        step.EndInit ();
        step.HelpText = "Help content";

        // Act
        step.HelpText = string.Empty;

        // Assert
        // When help text is cleared, padding right should be 0
        Assert.Equal (0, step.Padding.Thickness.Right);
    }

    #endregion HelpTextView Tests

    #region Layout Tests

    [Fact]
    public void Width_Is_Fill_After_Construction ()
    {
        // Arrange & Act
        WizardStep step = new ();

        // Assert
        Assert.IsType<DimFill> (step.Width);
    }

    [Fact]
    public void Height_Is_Fill_After_Construction ()
    {
        // Arrange & Act
        WizardStep step = new ();

        // Assert
        Assert.IsType<DimFill> (step.Height);
    }

    #endregion Layout Tests

    #region Focus Tests

    [Fact]
    public void CanFocus_True_By_Default ()
    {
        // Arrange & Act
        WizardStep step = new ();

        // Assert
        Assert.True (step.CanFocus);
    }

    [Fact]
    public void TabStop_Is_TabStop_By_Default ()
    {
        // Arrange & Act
        WizardStep step = new ();

        // Assert
        Assert.Equal (TabBehavior.TabStop, step.TabStop);
    }

    #endregion Focus Tests

    #region BorderStyle Tests

    [Fact]
    public void BorderStyle_Can_Be_Changed ()
    {
        // Arrange
        WizardStep step = new ();

        // Act
        step.BorderStyle = LineStyle.Single;

        // Assert
        Assert.Equal (LineStyle.Single, step.BorderStyle);
    }

    #endregion BorderStyle Tests

    #region Integration Tests

    [Fact]
    public void Step_With_HelpText_And_SubViews ()
    {
        // Arrange
        WizardStep step = new ()
        {
            Title = "User Information",
            HelpText = "Please enter your details"
        };

        Label nameLabel = new () { Text = "Name:" };
        TextField nameField = new () { X = Pos.Right (nameLabel) + 1, Width = 20 };

        step.Add (nameLabel, nameField);
        step.BeginInit ();
        step.EndInit ();

        // Assert
        Assert.Equal ("User Information", step.Title);
        Assert.Equal ("Please enter your details", step.HelpText);
        // SubViews includes the views we added
        Assert.Contains (nameLabel, step.SubViews);
        Assert.Contains (nameField, step.SubViews);
        Assert.True (step.Padding.Thickness.Right > 0);
    }

    [Fact]
    public void Step_With_Custom_Button_Text ()
    {
        // Arrange & Act
        WizardStep step = new ()
        {
            Title = "Confirmation",
            BackButtonText = "Go Back",
            NextButtonText = "Accept"
        };

        // Assert
        Assert.Equal ("Confirmation", step.Title);
        Assert.Equal ("Go Back", step.BackButtonText);
        Assert.Equal ("Accept", step.NextButtonText);
    }

    [Fact]
    public void Disabled_Step_Maintains_Properties ()
    {
        // Arrange
        WizardStep step = new ()
        {
            Title = "Optional Step",
            HelpText = "This step is optional",
            Enabled = false
        };

        // Assert
        Assert.Equal ("Optional Step", step.Title);
        Assert.Equal ("This step is optional", step.HelpText);
        Assert.False (step.Enabled);
    }

    #endregion Integration Tests
}
