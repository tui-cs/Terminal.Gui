namespace Terminal.Gui.Views;

/// <summary>
///     A modal dialog window with buttons across the bottom. When a button is pressed,
///     <see cref="IRunnable{TResult}.Result"/>
///     is set to the button's index (0-based).
/// </summary>
/// <remarks>
/// <img src="../images/views/Dialog.gif" alt="Dialog demo"/>
///     <para>
///         This is the standard dialog class for simple button-index-based results. For dialogs that need to return
///         custom result types, derive from <see cref="Dialog{TResult}"/> instead.
///     </para>
///     <para>
///         By default, <see cref="Dialog"/> is centered with <see cref="Dim.Auto"/> sizing and uses the
///         <see cref="Schemes.Dialog"/> color scheme (applied at construction time, regardless of running state).
///     </para>
///     <para>
///         To run modally, pass the dialog to <see cref="IApplication.Run(IRunnable, Func{Exception, bool})"/>.
///         The dialog executes until terminated by <see cref="Application.GetDefaultKey"/> (Esc by default),
///         a press of one of the <see cref="Dialog{TResult}.Buttons"/>, or if any subview receives the
///         <see cref="Command.Accept"/>
///         command
///         and does not handle it.
///     </para>
///     <para>
///         Buttons are added via <see cref="Dialog{TResult}.AddButton"/> or the <see cref="Dialog{TResult}.Buttons"/>
///         property. The last button added
///         becomes the default (<see cref="Button.IsDefault"/>). Button alignment is controlled by
///         <see cref="Dialog{TResult}.ButtonAlignment"/> and <see cref="Dialog{TResult}.ButtonAlignmentModes"/>.
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
///     Dialog dialog = new () { Title = "Confirm" };
/// 
///     dialog.AddButton (new () { Title = "_Cancel" });
///     dialog.AddButton (new () { Title = "_Ok" });
/// 
///     Label label = new () { Text = "Are you sure?" };
///     dialog.Add (label);
/// 
///     Application.Run (dialog);
/// 
///     if (dialog.Result == 1) // OK button (second button, index 1)
///     {
///         // User clicked OK
///     }
///     </code>
/// </example>
public class Dialog : Dialog<int>
{
    /// <summary>
    ///     The default border style for new <see cref="Dialog"/> instances. Can be configured via
    ///     <see cref="TuiConfigurationBuilder"/> and theme files.
    /// </summary>
    public static LineStyle DefaultBorderStyle => DialogSettings.Current.DefaultBorderStyle;

    /// <summary>
    ///     The default button alignment for new <see cref="Dialog"/> instances. Can be configured via theme files.
    /// </summary>
    public static Alignment DefaultButtonAlignment => DialogSettings.Current.DefaultButtonAlignment;

    /// <summary>
    ///     The default button alignment modes for new <see cref="Dialog"/> instances. Can be configured via theme files.
    /// </summary>
    public static AlignmentModes DefaultButtonAlignmentModes => DialogSettings.Current.DefaultButtonAlignmentModes;

    /// <summary>
    ///     The default shadow style for new <see cref="Dialog"/> instances. Can be configured via theme files.
    /// </summary>
    public static ShadowStyles DefaultShadow => DialogSettings.Current.DefaultShadow;

    /// <summary>
    ///     Helper property that gets whether the dialog was canceled (Result is <see langword="null"/> or 1).
    /// </summary>
    /// <remarks>
    ///     Returns <see langword="true"/> if <see cref="Result"/> is <see langword="null"/>
    ///     (user pressed Escape or closed the dialog without pressing a button) or if any button
    ///     other than the last one was pressed.
    /// </remarks>
    public bool Canceled => Result is null || Result != Buttons.Length - 1;

    /// <summary>
    ///     Gets or sets the result of the dialog, indicating which button was pressed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The value is the zero-based index of the button that was pressed, or <see langword="null"/>
    ///         if the dialog was dismissed without a button press (e.g., via Escape key).
    ///     </para>
    ///     <para>
    ///         This property shadows the base <see cref="IRunnable{TResult}.Result"/> property to provide
    ///         explicit <c>int?</c> nullability for backward compatibility.
    ///     </para>
    /// </remarks>
    public new int? Result
    {
        get => ((IRunnable)this).Result is int value ? value : null;
        set
        {
            if (value >= Buttons.Length || value < 0)
            {
                throw new ArgumentOutOfRangeException (nameof (value), @"Result value must be a valid button index or null.");
            }

            ((IRunnable)this).Result = value;
        }
    }

    /// <summary>
    ///     Overrides the <see cref="Dialog{TResult}"/>  Activating behavior to handle non-Default Dialog Button presses.
    ///     The <see cref="View.DefaultAcceptView"/> button press is handled in <see cref="OnAccepting(CommandEventArgs)"/>.
    /// </summary>
    protected override bool OnActivating (CommandEventArgs args)
    {
        if (base.OnActivating (args))
        {
            return true;
        }

        if (args.Context?.Source?.TryGetTarget (out View? sourceView) is not true || !Buttons.Contains (sourceView as Button))
        {
            return false;
        }
        Result = Buttons.IndexOf (sourceView as Button);

        return false;
    }

    /// <inheritdoc/>
    protected override bool OnAccepting (CommandEventArgs args)
    {
        // For non-default button sources (e.g., Cancel), set Result before base handles it.
        // Base (Dialog<int>.OnAccepting) will call RequestStop and return true (handled) for
        // non-default IAcceptTarget sources, preventing DefaultAcceptView from being invoked.
        View? sourceView = null;
        args.Context?.Source?.TryGetTarget (out sourceView);

        if (sourceView is Button button && Buttons.Contains (button) && button is IAcceptTarget { IsDefault: false })
        {
            Result = Buttons.IndexOf (button);
        }
        else if (sourceView is { } && DefaultAcceptView is Button defaultButton && Buttons.Contains (defaultButton))
        {
            // Non-button source (e.g., CheckBox double-click): set Result to default button's index
            Result = Buttons.IndexOf (defaultButton);
        }

        return base.OnAccepting (args);
    }

    /// <summary>
    ///     Overrides the <see cref="Dialog{TResult}"/> Accepting behavior to set <see cref="Result"/> to the index of the
    ///     dialog button pressed.
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    protected override void OnAccepted (ICommandContext? ctx)
    {
        base.OnAccepted (ctx);

        if (ctx?.Source?.TryGetTarget (out View? sourceView) is not true || !Buttons.Contains (sourceView as Button))
        {
            return;
        }

        RequestStop ();

        int buttonIndex = Buttons.IndexOf (sourceView as Button);
        Result = buttonIndex;
    }
}
