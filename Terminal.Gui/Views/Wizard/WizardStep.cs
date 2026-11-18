namespace Terminal.Gui.Views;

/// <summary>
///     A single step in a <see cref="Wizard"/>. Can contain arbitrary <see cref="View"/>s and display help text
///     in the right <see cref="Padding"/>.
/// </summary>
/// <remarks>
///     Do not set <see cref="Button.IsDefault"/> on added buttons (conflicts with Wizard navigation).
///     Use <see cref="View.VisibleChanged"/> or <see cref="Wizard.StepChanged"/> to detect when this step becomes active.
///     Set <see cref="View.Enabled"/> to control whether the step is shown.
/// </remarks>
public class WizardStep : View, IDesignable
{
    private readonly Markdown _helpTextView = new ()
    {
        SyntaxHighlighter = new TextMateSyntaxHighlighter (),
        ShowHeadingPrefix = false,
        ViewportSettings = ViewportSettingsFlags.HasVerticalScrollBar,
        X = Pos.AnchorEnd () + 1,
        Height = Dim.Fill (),
#if DEBUG
        Id = "WizardStep._helpTextView"
#endif
    };

    /// <summary>
    ///     Initializes a new instance of the <see cref="WizardStep"/> class.
    /// </summary>
    public WizardStep ()
    {
        TabStop = TabBehavior.TabStop;
        CanFocus = true;
        Width = Dim.Fill ();
        Height = Dim.Fill ();
        CommandsToBubbleUp = [Command.Accept];
    }

    /// <inheritdoc/>
    public override void EndInit ()
    {
        // Help text goes in the right Padding
        // TODO: Enable built-in scrollbars for the help text view once TextView supports
        //_helpTextView.VerticalScrollBar.AutoShow = true;
        //_helpTextView.HorizontalScrollBar.AutoShow = true;
        _helpTextView.Width = Dim.Func (_ => CalculateHelpPaddingWidth ());

        Padding.GetOrCreateView ().Add (_helpTextView);

        ShowHide ();
        base.EndInit ();
    }

    /// <summary>The text for the Back button. Defaults to "Back".</summary>
    public string BackButtonText { get; set; } = string.Empty;

    /// <summary>Calculates the width for the help text padding based on the current frame width.</summary>
    private int CalculateHelpPaddingWidth () => 25;

    /// <inheritdoc/>
    protected override void OnFrameChanged (in Rectangle frame)
    {
        base.OnFrameChanged (frame);

        // Update padding thickness when frame changes
        if (_helpTextView.Text.Length > 0)
        {
            Padding.Thickness = Padding.Thickness with { Right = CalculateHelpPaddingWidth () };
        }
    }

    /// <summary>
    ///     The help text displayed in the right <see cref="Padding"/>.
    ///     If empty, the right padding is hidden and content fills the entire step.
    /// </summary>
    /// <remarks>The help text is displayed using a read-only <see cref="Markdown"/> view that supports markdown formatting.</remarks>
    public string HelpText
    {
        get => _helpTextView.Text;
        set
        {
            _helpTextView.Text = value;
            ShowHide ();
        }
    }

    /// <summary>The text for the Next/Finish button. Defaults to "Next..." or "Finish" based on position.</summary>
    public string NextButtonText { get; set; } = string.Empty;

    /// <summary>Does the work to show and hide the contentView and helpView as appropriate</summary>
    internal void ShowHide ()
    {
        // Check if views are available (might be null during disposal)

        if (_helpTextView.Text.Length > 0)
        {
            // Help text goes in right Padding - set thickness based on current frame width
            Padding.Thickness = Padding.Thickness with { Right = CalculateHelpPaddingWidth () };

            _helpTextView.Visible = true;
            _helpTextView.Enabled = true;
        }
        else
        {
            // No help text - no right padding needed
            Padding.Thickness = Padding.Thickness with { Right = 0 };

            _helpTextView.Visible = false;
            _helpTextView.Enabled = false;
        }

        SetNeedsLayout ();
    }

    bool IDesignable.EnableForDesign ()
    {
        Title = "Example Step";

        Label label = new () { Title = "_Enter Text:" };

        TextField textField = new () { X = Pos.Right (label) + 1, Width = 20 };
        Add (label, textField);

        label = new Label { Title = "    _A List:", Y = Pos.Bottom (label) + 1 };

        ListView listView = new ()
        {
            BorderStyle = LineStyle.Dashed,
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Height = Dim.Auto (),
            Width = 10,
            Source = new ListWrapper<string> (["Item 1", "Item 2", "Item 3", "Item 4", "Item 5"]),
            SelectedItem = 0
        };
        Add (label, listView);

        HelpText = """
                   ## WizardStep Help

                   Provide **markdown-formatted** instructions or information to guide the user through each step of the wizard.

                   Set `WizardStep.HelpText` to update this content. If `HelpText` is empty, the right padding will be hidden and the content will fill the entire step.

                   [Learn more about markdown formatting](https://www.markdownguide.org/basic-syntax/)
                   """;

        return true;
    }
} // end of WizardStep class
