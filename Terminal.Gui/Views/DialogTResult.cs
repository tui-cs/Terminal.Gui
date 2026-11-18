namespace Terminal.Gui.Views;

/// <summary>
///     A generic modal dialog window with buttons across the bottom. Derive from this class
///     to create dialogs that return custom result types.
/// </summary>
/// <typeparam name="TResult">
///     The type of result data returned when the dialog closes.
///     Since <see cref="IRunnable{TResult}.Result"/> is <c>TResult?</c>, use non-nullable types
///     (e.g., <c>string</c> not <c>string?</c>) to allow <c>null</c> to indicate cancellation.
/// </typeparam>
/// <remarks>
///     <para>
///         By default, <see cref="Dialog{TResult}"/> is centered with <see cref="Dim.Auto"/> sizing and uses the
///         <see cref="Schemes.Dialog"/> color scheme (applied at construction time, regardless of running state).
///     </para>
///     <para>
///         To run modally, pass the dialog to <see cref="IApplication.Run(IRunnable, Func{Exception, bool})"/>.
///         The dialog executes until terminated by <see cref="Application.GetDefaultKey"/> (Esc by default),
///         a press of one of the <see cref="Buttons"/>, or if any subview receives the <see cref="Command.Accept"/>
///         command
///         and does not handle it.
///     </para>
///     <para>
///         Buttons are added via <see cref="AddButton"/> or the <see cref="Buttons"/> property. The last button added
///         becomes the default (<see cref="Button.IsDefault"/>). Button alignment is controlled by
///         <see cref="ButtonAlignment"/> and <see cref="ButtonAlignmentModes"/>.
///     </para>
///     <para>
///         Subclasses should set <see cref="IRunnable{TResult}.Result"/> before calling <see cref="Runnable.RequestStop"/>
///         to return a value. If Result is not set (remains <c>null</c>), the dialog is considered canceled.
///     </para>
///     <para>
///         The dialog is positioned at <see cref="Pos.Center"/> with <see cref="Dim.Auto"/> sizing,
///         limited to 100% of <see cref="IApplication.TopRunnableView"/> (or screen dimensions).
///     </para>
///     <para>
///         <b>NOTE </b> - Setting <see cref="View.ViewportSettings"/> to
///         <see cref="ViewportSettingsFlags.HasHorizontalScrollBar"/> or
///         <see cref="ViewportSettingsFlags.HasVerticalScrollBar"/>
///         is not supported and may cause layout issues.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     // Custom dialog returning a Color
///     public class ColorDialog : Dialog&lt;Color&gt;
///     {
///         private ColorPicker _picker;
/// 
///         public ColorDialog (Color initialColor)
///         {
///             _picker = new () { Value = initialColor };
///             Add (_picker);
///             AddButton (new () { Text = "_Cancel" });
///             AddButton (new () { Text = "_Ok" });
///         }
/// 
///         protected override bool OnAccepting (CommandEventArgs args)
///         {
///             if (base.OnAccepting (args))
///             {
///                 return true;
///             }
///             Result = _picker.Value;
///             RequestStop ();
///             return false;
///         }
///     }
///     </code>
/// </example>
public class Dialog<TResult> : Runnable<TResult>, IDesignable
{
    /// <summary>
    ///     The container view that holds the dialog buttons in the Padding area.
    /// </summary>
    protected readonly View? _buttonContainer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Dialog{TResult}"/> class with no buttons.
    /// </summary>
    public Dialog ()
    {
        X = Pos.Center ();
        Y = Pos.Center ();

        // Set automatic width and height, with minimums based on content size. Also, subtract
        // Padding thickness in case the scrollbar is visible
        Width = Dim.Auto (minimumContentDim: Dim.Func (_ => GetMinimumDialogWidth () - (VerticalScrollBar.Visible ? 1 : 0)),
                          maximumContentDim: Dim.Percent (100) - Dim.Func (_ => GetAdornmentsThickness ().Horizontal));

        Height = Dim.Auto (minimumContentDim: Dim.Func (_ => GetMinimumDialogHeight () - _minimumButtonsSize.Height - (HorizontalScrollBar.Visible ? 1 : 0)),
                           maximumContentDim: Dim.Percent (100) - Dim.Func (_ => GetAdornmentsThickness ().Vertical));
        ButtonAlignment = Dialog.DefaultButtonAlignment;
        ButtonAlignmentModes = Dialog.DefaultButtonAlignmentModes;

        BorderStyle = Dialog.DefaultBorderStyle;
        base.ShadowStyle = Dialog.DefaultShadow;

        SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Dialog);

        Arrangement |= ViewArrangement.Movable | ViewArrangement.Resizable | ViewArrangement.Overlapped;

        CommandsToBubbleUp = [Command.Accept];

        _buttonContainer = new View
        {
#if DEBUG
            Id = "Dialog.ButtonContainer",
#endif
            CanFocus = true,
            X = 0,
            Y = Pos.AnchorEnd (),
            Width = Dim.Fill (),
            Height = Dim.Auto (),
            CommandsToBubbleUp = CommandsToBubbleUp
        };

        Padding.GetOrCreateView ();
        Padding.View?.Add (_buttonContainer);
        UpdateSizes ();
    }

    /// <inheritdoc/>
    public override void EndInit ()
    {
        base.EndInit ();
        UpdateSizes ();
    }

    /// <inheritdoc/>
    protected override void OnSubViewLayout (LayoutEventArgs args)
    {
        UpdateSizes ();
        base.OnSubViewLayout (args);
    }

    /// <summary>
    ///     Because Dialog has a complex Dim.Auto setup, we override OnSubViewsLaidOut to see if another
    ///     Layout is required.
    /// </summary>
    /// <param name="args">The event data containing information about the layout event.</param>
    protected override void OnSubViewsLaidOut (LayoutEventArgs args)
    {
        base.OnSubViewsLaidOut (args);

        if (NeedsLayout)
        {
            Layout ();
        }
    }

    /// <inheritdoc/>
    protected override bool OnAccepting (CommandEventArgs args)
    {
        if (base.OnAccepting (args))
        {
            return true;
        }

        View? sourceView = null;
        args.Context?.Source?.TryGetTarget (out sourceView);

        if (sourceView is null)
        {
            return false;
        }
        RequestStop ();

        return sourceView is IAcceptTarget { IsDefault: false };
    }

    /// <inheritdoc/>
    protected override void OnViewportChanged (DrawEventArgs e)
    {
        SetContentSize (new Size (Math.Max (_minimumButtonsSize.Width, Viewport.Width), Math.Max (_minimumButtonsSize.Height, Viewport.Height)));
        base.OnViewportChanged (e);
    }

    private void UpdateSizes ()
    {
        if (SubViews.Count == 0)
        {
            // This is primarily to support MessageBox where there are no subviews but
            // Text is used.
            return;
        }

        // Always floor at Viewport size — the content area should never be smaller
        // than what's visible.
        SetContentSize (new Size (Math.Max (_minimumButtonsSize.Width, Viewport.Width), Math.Max (_minimumButtonsSize.Height, Viewport.Height)));
    }

    /// <summary>
    ///     INTERNAL: Gets the minimum width required for the <see cref="Dialog{TResult}"/>. This takes into account
    ///     the width required for the title, the buttons, and any subviews.
    /// </summary>
    /// <returns></returns>
    private int GetMinimumDialogWidth ()
    {
        int minSize = Math.Max (

                                // Ensure space for title + borders
                                Title.GetColumns () + 4,
                                _minimumButtonsSize.Width);

        return minSize;
    }

    /// <summary>
    ///     INTERNAL: Gets the minimum height required for the <see cref="Dialog{TResult}"/>. This takes into account
    ///     the height required for the buttons.
    /// </summary>
    /// <returns></returns>
    private int GetMinimumDialogHeight ()
    {
        int minSize = _minimumButtonsSize.Height - Border.Thickness.Vertical - Margin.Thickness.Vertical;

        return minSize;
    }

    private readonly List<Button> _buttons = [];

    private Size _minimumButtonsSize;

    /// <summary>
    ///     Adds a <see cref="Button"/> to the bottom of the dialog and to the <see cref="Buttons"/> collection.
    /// </summary>
    /// <param name="dialogButton">The Dialog button to add. Its lifetime and layout will be managed by the Dialog.</param>
    /// <remarks>
    ///     <para>
    ///         Buttons are positioned according to <see cref="ButtonAlignment"/> and <see cref="ButtonAlignmentModes"/>.
    ///         The last button added becomes the default (<see cref="Button.IsDefault"/> = <see langword="true"/>).
    ///     </para>
    ///     <para>
    ///         When a button is pressed, the dialog's <see cref="View.Accepting"/> event is raised.
    ///     </para>
    /// </remarks>
    public void AddButton (Button dialogButton)
    {
        // Use a distinct GroupId so users can use Pos.Align for other views in the Dialog
        dialogButton.X = Pos.Align (ButtonAlignment, ButtonAlignmentModes, GetHashCode ());
        dialogButton.Y = 1;

        _buttons.Add (dialogButton);

        foreach (Button button in _buttons)
        {
            button.IsDefault = false;

            //button.Accepting -= OnDialogButtonOnAccepting;
            //button.Accepting += OnDialogButtonOnAccepting;
        }

        DefaultAcceptView = dialogButton;
        dialogButton.IsDefault = true;

        _buttonContainer?.Add (dialogButton);
        Padding.Thickness = Padding.Thickness with { Bottom = _buttonContainer!.GetHeightRequiredForSubViews () };
        _minimumButtonsSize = new Size (_buttonContainer?.GetWidthRequiredForSubViews () ?? 0, _buttonContainer?.GetHeightRequiredForSubViews () ?? 0);
    }

    /// <summary>
    ///     Determines how buttons are aligned horizontally at the bottom of the dialog.
    /// </summary>
    /// <remarks>
    ///     Default is <see cref="Dialog.DefaultButtonAlignment"/> (typically <see cref="Alignment.End"/>).
    /// </remarks>
    public Alignment ButtonAlignment
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;

            foreach (Button dialogButton in _buttons)
            {
                dialogButton.X = Pos.Align (ButtonAlignment, ButtonAlignmentModes, GetHashCode ());
            }
        }
    }

    /// <summary>
    ///     Controls button spacing and alignment behavior.
    /// </summary>
    /// <remarks>
    ///     Default is <see cref="Dialog.DefaultButtonAlignmentModes"/> (typically
    ///     <see cref="AlignmentModes.StartToEnd"/> | <see cref="AlignmentModes.AddSpaceBetweenItems"/>).
    /// </remarks>
    public AlignmentModes ButtonAlignmentModes
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;

            foreach (Button dialogButton in _buttons)
            {
                dialogButton.X = Pos.Align (ButtonAlignment, ButtonAlignmentModes, GetHashCode ());
            }

            UpdateSizes ();
        }
    }

    // BUGBUG: The setter of this property does not clear existing buttons, leading to potential duplicates.
    // BUGBUG: Also, Buttons.Clear does not remove buttons from the _buttonContainer view (or even _buttons) because
    // BUGBUG: the getter returns a copy of the internal list.
    /// <summary>
    ///     Gets or sets the buttons displayed at the bottom of the dialog.
    /// </summary>
    /// <remarks>
    ///     Setting this property calls <see cref="AddButton"/> for each button in the array.
    ///     Getting returns a copy of the internal button list.
    /// </remarks>
    public Button [] Buttons
    {
        get => _buttons.ToArray ();
        set
        {
            foreach (Button b in value)
            {
                AddButton (b);
            }
        }
    }

    // Dialogs are Modal and Focus is indicated by their Border. _drawingText ensures the
    // Text of the dialog (e.g. for a MessageBox) is always drawn using the Normal Attribute
    // instead of the Focus attribute.
    private bool _drawingText;

    /// <inheritdoc/>
    protected override bool OnDrawingText ()
    {
        _drawingText = true;

        return false;
    }

    /// <inheritdoc/>
    protected override void OnDrewText () => _drawingText = false;

    /// <inheritdoc/>
    protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
    {
        if (!IsRunning)
        {
            return false;
        }

        if (!_drawingText || role is not VisualRole.Focus || Border.Thickness == Thickness.Empty)
        {
            return false;
        }

        currentAttribute = GetScheme ().Normal;

        return true;
    }

    bool IDesignable.EnableForDesign ()
    {
        Title = "Dialog Title";

        AddButton (new Button { Id = "btnCancel", Title = Strings.btnCancel });

        AddButton (new Button
        {
            Id = "btnOk", Title = Strings.btnOk

            // Dialog will automatically set IsDefault to the last button added
        });

        // Add some example content to the dialog
        Label infoLabel = new () { Id = "infoLabel", Text = "_Example:" };

        TextField info = new ()
        {
            Id = "info",
            X = Pos.Right (infoLabel) + 1,
            Y = Pos.Top (infoLabel),
            Text = "Type and press ENTER to accept.",
            Width = 40
        };
        Add (infoLabel, info);

        Accepting += (s, e) =>
                     {
                         if (e.Handled || !IsRunning)
                         {
                             return;
                         }

                         (s as View)?.App?.RequestStop ();
                         e.Handled = true;
                     };

        return true;
    }
}
