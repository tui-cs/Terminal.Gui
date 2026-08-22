namespace Terminal.Gui.Views;

/// <summary>
///     Displays a modal message box with a title, message, and buttons. Returns the index of the selected button,
///     or <see langword="null"/> if the user cancels with <see cref="Application.GetDefaultKey"/>.
/// </summary>
/// <remarks>
///     <para>
///         MessageBox provides static methods for displaying modal dialogs with customizable buttons and messages.
///         All methods return <see langword="int?"/> where the value is the 0-based index of the button pressed,
///         or <see langword="null"/> if the user pressed <see cref="Application.GetDefaultKey"/> (typically Esc).
///     </para>
///     <para>
///         <see cref="Query(IApplication, string, string, string[])"/> uses the default Dialog color scheme.
///         <see cref="ErrorQuery(IApplication, string, string, string[])"/> uses the Error color scheme.
///     </para>
///     <para>
///         The last button provided is always the default button.
///     </para>
///     <para>
///         <b>Important:</b> All MessageBox methods require an <see cref="IApplication"/> instance to be passed.
///         This enables proper modal dialog management and respects the application's lifecycle. Pass your
///         application instance (from <see cref="Application.Create"/>) or use the legacy
///         <see cref="Application.Instance"/> if using the static Application pattern.
///     </para>
///     <para>
///         Example using instance-based pattern:
///         <code>
///     IApplication app = Application.Create();
///     app.Init();
///     
///     int? result = MessageBox.Query(app, "Quit Demo", "Are you sure you want to quit?", Strings.btnNo, Strings.btnYes);
///     if (result == 1) // User clicked "Yes"
///         app.RequestStop();
///     else if (result == null) // User pressed Esc
///         // Handle cancellation
///         
///     app.Shutdown();
///     </code>
///     </para>
///     <para>
///         Example using legacy static pattern:
///         <code>
///     Application.Init();
///     
///     int? result = MessageBox.Query(ApplicationImpl.Instance, "Quit Demo", "Are you sure?", Strings.btnNo, Strings.btnYes);
///     if (result == 1) // User clicked "Yes"
///         Application.RequestStop();
///     
///     Application.Shutdown();
///     </code>
///     </para>
/// </remarks>
public static class MessageBox
{
    /// <summary>
    ///     Defines the default border styling for <see cref="MessageBox"/>. Can be configured via
    ///     <see cref="TuiConfigurationBuilder"/>.
    /// </summary>
    public static LineStyle DefaultBorderStyle => MessageBoxSettings.Current.DefaultBorderStyle;

    /// <summary>The default <see cref="Alignment"/> for <see cref="Dialog"/>.</summary>
    /// <remarks>This property can be set in a Theme.</remarks>
    public static Alignment DefaultButtonAlignment => MessageBoxSettings.Current.DefaultButtonAlignment;

    /// <summary>
    ///     Displays an auto-sized error <see cref="MessageBox"/>.
    /// </summary>
    /// <param name="app">The application instance. If <see langword="null"/>, uses <see cref="IApplication.TopRunnableView"/>.</param>
    /// <param name="title">Title for the MessageBox.</param>
    /// <param name="message">Message to display. May contain multiple lines and will be word-wrapped.</param>
    /// <param name="buttons">Array of button labels.</param>
    /// <returns>
    ///     The index of the selected button, or <see langword="null"/> if the user pressed
    ///     <see cref="Application.GetDefaultKey"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="app"/> is <see langword="null"/>.</exception>
    /// <remarks>
    ///     The MessageBox is centered and auto-sized based on title, message, and buttons. The last button is the
    ///     default button.
    /// </remarks>
    public static int? ErrorQuery (IApplication app, string title, string message, params string [] buttons)
    {
        return QueryFull (
                           app,
                           true,
                           title,
                           message,
                           true,
                           buttons);
    }

    /// <summary>
    ///     Displays an auto-sized error <see cref="MessageBox"/> with word-wrap control.
    /// </summary>
    /// <param name="app">The application instance. If <see langword="null"/>, uses <see cref="IApplication.TopRunnableView"/>.</param>
    /// <param name="title">Title for the MessageBox.</param>
    /// <param name="message">Message to display. May contain multiple lines.</param>
    /// <param name="wrapMessage">
    ///     If <see langword="true"/>, word-wraps the message; otherwise displays as-is with multi-line
    ///     support.
    /// </param>
    /// <param name="buttons">Array of button labels.</param>
    /// <returns>
    ///     The index of the selected button, or <see langword="null"/> if the user pressed
    ///     <see cref="Application.GetDefaultKey"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="app"/> is <see langword="null"/>.</exception>
    /// <remarks>
    ///     The MessageBox is centered and auto-sized based on title, message, and buttons. The last button is the
    ///     default button.
    /// </remarks>
    public static int? ErrorQuery (
        IApplication app,
        string title,
        string message,
        bool wrapMessage = true,
        params string [] buttons
    )
    {
        return QueryFull (
                           app,
                           true,
                           title,
                           message,
                           wrapMessage,
                           buttons);
    }

    /// <summary>
    ///     Displays a <see cref="MessageBox"/> with fixed dimensions.
    /// </summary>
    /// <param name="app">The application instance. If <see langword="null"/>, uses <see cref="IApplication.TopRunnableView"/>.</param>
    /// <param name="width">Width for the MessageBox.</param>
    /// <param name="height">Height for the MessageBox.</param>
    /// <param name="title">Title for the MessageBox.</param>
    /// <param name="message">Message to display. May contain multiple lines and will be word-wrapped.</param>
    /// <param name="buttons">Array of button labels.</param>
    /// <returns>
    ///     The index of the selected button, or <see langword="null"/> if the user pressed
    ///     <see cref="Application.GetDefaultKey"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="app"/> is <see langword="null"/>.</exception>
    /// <remarks>
    ///     Consider using <see cref="Query(IApplication, string, string, string[])"/> which automatically sizes the
    ///     MessageBox. The last button is the default button.
    /// </remarks>
    public static int? Query (IApplication app, int width, int height, string title, string message, params string [] buttons)
    {
        return QueryFull (
                           app,
                           false,
                           title,
                           message,
                           true,
                           buttons);
    }

    /// <summary>
    ///     Displays an auto-sized <see cref="MessageBox"/>.
    /// </summary>
    /// <param name="app">The application instance. If <see langword="null"/>, uses <see cref="IApplication.TopRunnableView"/>.</param>
    /// <param name="title">Title for the MessageBox.</param>
    /// <param name="message">Message to display. May contain multiple lines and will be word-wrapped.</param>
    /// <param name="buttons">Array of button labels.</param>
    /// <returns>
    ///     The index of the selected button, or <see langword="null"/> if the user pressed
    ///     <see cref="Application.GetDefaultKey"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="app"/> is <see langword="null"/>.</exception>
    /// <remarks>
    ///     The MessageBox is centered and auto-sized based on title, message, and buttons. The last button is the
    ///     default button.
    /// </remarks>
    public static int? Query (IApplication app, string title, string message, params string [] buttons)
    {
        return QueryFull (
                           app,
                           false,
                           title,
                           message,
                           true,
                           buttons);
    }

    /// <summary>
    ///     Displays an auto-sized <see cref="MessageBox"/> with word-wrap control.
    /// </summary>
    /// <param name="app">The application instance. If <see langword="null"/>, uses <see cref="IApplication.TopRunnableView"/>.</param>
    /// <param name="title">Title for the MessageBox.</param>
    /// <param name="message">Message to display. May contain multiple lines.</param>
    /// <param name="wrapMessage">
    ///     If <see langword="true"/>, word-wraps the message; otherwise displays as-is with multi-line
    ///     support.
    /// </param>
    /// <param name="buttons">Array of button labels.</param>
    /// <returns>
    ///     The index of the selected button, or <see langword="null"/> if the user pressed
    ///     <see cref="Application.GetDefaultKey"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="app"/> is <see langword="null"/>.</exception>
    /// <remarks>
    ///     The MessageBox is centered and auto-sized based on title, message, and buttons. The last button is the
    ///     default button.
    /// </remarks>
    public static int? Query (
        IApplication app,
        string title,
        string message,
        bool wrapMessage = true,
        params string [] buttons
    )
    {
        return QueryFull (
                           app,
                           false,
                           title,
                           message,
                           wrapMessage,
                           buttons);
    }

    private static int? QueryFull (
        IApplication app,
        bool useErrorScheme,
        string title,
        string message,
        bool wrapMessage = true,
        params string [] buttons
    )
    {
        List<Button> buttonList = [];
        buttonList.AddRange (buttons.Select (buttonText => new Button { Text = buttonText, }));

        using Dialog dialog = new ();
        dialog.App = app;
        dialog.Title = title;
        dialog.ButtonAlignment = DefaultButtonAlignment;
        dialog.BorderStyle = DefaultBorderStyle;
        dialog.SchemeName = useErrorScheme ? SchemeManager.SchemesToSchemeName (Schemes.Error) : SchemeManager.SchemesToSchemeName (Schemes.Dialog);
        dialog.HotKeySpecifier = new Rune ('\xFFFF');
        dialog.Text = message;
        dialog.TextAlignment = Alignment.Center;
        dialog.VerticalTextAlignment = Alignment.Center;
        dialog.TextFormatter.WordWrap = wrapMessage;
        dialog.TextFormatter.MultiLine = !wrapMessage;

        dialog.Buttons = buttonList.ToArray ();
        dialog.Buttons.FirstOrDefault (b => b.IsDefault)?.SetFocus ();

        dialog.Accepting += (sender, args) =>
                            {
                                if (args.Handled || sender is not Dialog d)
                                {
                                    return;
                                }

                                args.Handled = true;
                                d.RequestStop ();
                            };

        // Run the modal
        int? result = app.Run (dialog) as int?;

        return result;
    }
}
