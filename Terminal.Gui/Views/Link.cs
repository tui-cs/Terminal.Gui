using System.Diagnostics;

namespace Terminal.Gui.Views;

/// <summary>
///     Displays a clickable hyperlink with optional display text and a target URL.
/// </summary>
/// <remarks>
/// <img src="../images/views/Link.gif" alt="Link demo"/>
///     <para>
///         <see cref="Link"/> has three independent text-related properties:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <see cref="View.Text"/> — The display text shown to the user. When empty, <see cref="Url"/> is
///                 displayed
///                 instead.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="View.Title"/> — Controls the <see cref="View.HotKey"/>. Set this to include an underscore
///                 prefix
///                 (e.g., <c>"_Link"</c>) to define a keyboard shortcut.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="Url"/> — The hyperlink target. When the link is accepted (clicked or
///                 <see cref="Command.Accept"/>
///                 is invoked), this URL is opened in the default browser via <see cref="OpenUrl"/>.
///             </description>
///         </item>
///     </list>
///     <para>
///         The link renders using OSC 8 hyperlink escape sequences when the terminal supports them, enabling
///         clickable URLs in modern terminal emulators. If <see cref="Url"/> is not a well-formed absolute URI,
///         the link renders with the <see cref="VisualRole.Disabled"/> style and OSC 8 sequences are suppressed.
///     </para>
///     <para>
///         <see cref="Url"/> changes follow the Cancellable Workflow Pattern (CWP): the <see cref="UrlChanging"/> event
///         fires before the change (and can cancel it by setting <see cref="ValueChangingEventArgs{T}.Handled"/> to
///         <see langword="true"/>), and the <see cref="UrlChanged"/> event fires after.
///     </para>
///     <para>
///         When <see cref="View.CanFocus"/> is <see langword="false"/> and the link has a valid <see cref="View.HotKey"/>,
///         pressing the HotKey passes focus to the next peer <see cref="View"/> in the SuperView's SubView list. This
///         enables <see cref="Link"/> to act as a label-like hotkey proxy (similar to <see cref="Label"/>).
///     </para>
///     <para>
///         Both <see cref="View.Width"/> and <see cref="View.Height"/> default to <see cref="DimAutoStyle.Text"/>,
///         so the link auto-sizes to fit whichever text is displayed (<see cref="View.Text"/> or <see cref="Url"/>).
///     </para>
///     <para>Default mouse bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Mouse Event</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Click</term>
///             <description>Activate the link, opening the URL (<see cref="Command.Activate"/>).</description>
///         </item>
///     </list>
/// </remarks>
public class Link : View, IDesignable
{
    /// <inheritdoc/>
    public Link ()
    {
        Height = Dim.Auto (DimAutoStyle.Text);
        Width = Dim.Auto (DimAutoStyle.Text);

        CanFocus = true;

        // On HotKey, pass it to the next view
        AddCommand (Command.HotKey, InvokeHotKeyOnNextPeer!);
    }

    /// <summary>
    ///     Handles activation. If <see cref="View.CanFocus"/> is <see langword="false"/>, delegates to the
    ///     <see cref="Command.HotKey"/> command (which passes focus to the next peer view). Otherwise, uses the
    ///     default activation behavior.
    /// </summary>
    protected override bool OnActivating (CommandEventArgs args)
    {
        // If Link can't focus and is activated, invoke HotKey on next peer
        if (!CanFocus)
        {
            return InvokeCommand (Command.HotKey, args.Context) == true;
        }

        return base.OnActivating (args);
    }

    /// <summary>
    ///     Called when the link is accepted (e.g., clicked or <see cref="Command.Accept"/> is invoked).
    ///     Opens <see cref="Url"/> in the default browser via <see cref="OpenUrl"/>.
    /// </summary>
    protected override void OnActivated (ICommandContext? ctx)
    {
        OpenUrl (Url);

        base.OnActivated (ctx);
    }

    /// <summary>
    ///     The set of URI schemes that <see cref="OpenUrl"/> is permitted to open. Only <c>http</c>, <c>https</c>, and
    ///     <c>mailto</c> are allowed by default. Callers that explicitly require additional schemes may modify this set,
    ///     but doing so widens the attack surface when <see cref="Url"/> is populated from untrusted input.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <c>file://</c> URIs are intentionally excluded from the default set because they allow local filesystem
    ///         access and can be used to invoke registered shell handlers on Windows. Applications that display
    ///         user-controlled content (Markdown, RSS, log output, etc.) are therefore protected by default.
    ///     </para>
    ///     <para>
    ///         <b>Migration path for applications that need <c>file://</c> or other non-default schemes:</b>
    ///     </para>
    ///     <para>
    ///         <b>Option 1 — Per-link handling via <see cref="Markdown.LinkClicked"/>.</b> Handle the URL in the event
    ///         and set <c>e.Handled = true</c> to prevent <see cref="OpenUrl"/> from being called:
    ///         <code>
    /// markdownView.LinkClicked += (_, e) =>
    /// {
    ///     if (e.Url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
    ///     {
    ///         // Handle the file link yourself.
    ///         e.Handled = true;
    ///     }
    /// };
    ///         </code>
    ///     </para>
    ///     <para>
    ///         <b>Option 2 — Global opt-in at application startup.</b> To allow <c>file://</c> links across the entire
    ///         application, add the scheme to this set before any links are activated:
    ///         <code>
    /// Link.SafeSchemes.Add("file");
    ///         </code>
    ///         Only do this in applications where <c>file://</c> URIs originate from trusted content.
    ///     </para>
    /// </remarks>
    public static readonly HashSet<string> SafeSchemes = new (StringComparer.OrdinalIgnoreCase)
    {
        "http", "https", "mailto"
    };

    /// <summary>
    ///     Opens the specified URL in the default web browser using a platform-specific mechanism.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Only URLs whose scheme is listed in <see cref="SafeSchemes"/> (<c>http</c>, <c>https</c>, <c>mailto</c> by
    ///         default) are opened. URLs with any other scheme (e.g. <c>file</c>, <c>ftp</c>, or Windows protocol
    ///         handlers such as <c>ms-msdt</c>) are silently ignored.
    ///     </para>
    ///     <para>
    ///         On Windows, uses <see cref="ProcessStartInfo.UseShellExecute"/> so the URL is dispatched directly by the
    ///         OS shell without passing through <c>cmd.exe</c>. On macOS, uses <c>open</c>. On Linux, uses
    ///         <c>xdg-open</c>.
    ///     </para>
    ///     <para>
    ///         Any exception thrown by the underlying process launch (e.g. no default browser registered,
    ///         permission denied) is caught and logged via <see cref="Logging.Warning"/>; the method never
    ///         propagates such exceptions to the caller.
    ///     </para>
    ///     <para>
    ///         Callers that populate <see cref="Url"/> from untrusted input (markdown, RSS feeds, network data, etc.)
    ///         must ensure the value is validated before it reaches this method.
    ///     </para>
    /// </remarks>
    /// <param name="url">The URL to open. Must be a well-formed absolute URI with an allowed scheme.</param>
    public static void OpenUrl (string url)
    {
        if (!Uri.TryCreate (url, UriKind.Absolute, out Uri? parsed) || parsed is null || !SafeSchemes.Contains (parsed.Scheme))
        {
            return;
        }

        if (Environment.GetEnvironmentVariable ("DisableRealDriverIO") == "1")
        {
            return;
        }

        try
        {
            if (OperatingSystem.IsWindows ())
            {
                Process.Start (new ProcessStartInfo (url) { UseShellExecute = true });
            }
            else if (OperatingSystem.IsMacOS ())
            {
                Process.Start ("open", url);
            }
            else if (OperatingSystem.IsLinux ())
            {
                using Process process = new ();

                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = url,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                process.Start ();
            }
        }
        catch (Exception ex)
        {
            Logging.Warning ($"OpenUrl failed for '{url}': {ex.Message}");
        }
    }

    /// <summary>
    ///     Updates the text displayed by the <see cref="View.TextFormatter"/> based on the current values of
    ///     <see cref="View.Text"/> and <see cref="Url"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If <see cref="View.Text"/> is <see langword="null"/> or empty, the formatter displays <see cref="Url"/>
    ///         instead, ensuring the link auto-sizes correctly via <see cref="DimAutoStyle.Text"/>. Otherwise, the
    ///         base implementation is used.
    ///     </para>
    /// </remarks>
    protected override void UpdateTextFormatterText ()
    {
        if (string.IsNullOrEmpty (base.Text))
        {
            TextFormatter.Text = Url;
            TextFormatter.ConstrainToWidth = null;
            TextFormatter.ConstrainToHeight = null;
        }
        else
        {
            base.UpdateTextFormatterText ();
        }
    }

    private string _url = "";
    private bool _isUrlValid;

    /// <summary>
    ///     Gets or sets the URL (hyperlink target) associated with this <see cref="Link"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Any string value is accepted. URL validation is performed once when this property is set and the result is
    ///         cached: if the value is not a well-formed absolute URI (per <see cref="Uri.IsWellFormedUriString"/>), the
    ///         link renders with the <see cref="VisualRole.Disabled"/> style and no OSC 8 hyperlink sequence is emitted.
    ///     </para>
    ///     <para>
    ///         When <see cref="View.Text"/> is empty, <see cref="Url"/> is used as the display text.
    ///     </para>
    ///     <para>
    ///         Setting this property follows the Cancellable Workflow Pattern: <see cref="OnUrlChanging"/> and
    ///         <see cref="UrlChanging"/> fire before the change and can cancel it; <see cref="OnUrlChanged"/> and
    ///         <see cref="UrlChanged"/> fire after. Setting the same value is a no-op.
    ///     </para>
    /// </remarks>
    public string Url { get => _url; set => SetUrl (value); }

    /// <summary>
    ///     Raised when <see cref="Url"/> is about to change. Set <see cref="ValueChangingEventArgs{T}.Handled"/> to
    ///     <see langword="true"/> to cancel the change and keep the current value.
    /// </summary>
    public event EventHandler<ValueChangingEventArgs<string>>? UrlChanging;

    /// <summary>
    ///     Raised after <see cref="Url"/> has changed. The <see cref="ValueChangedEventArgs{T}.OldValue"/> and
    ///     <see cref="ValueChangedEventArgs{T}.NewValue"/> properties contain the previous and current values.
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<string>>? UrlChanged;

    /// <summary>
    ///     Called before <see cref="Url"/> changes. Override in subclasses to implement validation or cancel the change.
    /// </summary>
    /// <param name="args">
    ///     Contains the current and proposed new values. Set <see cref="ValueChangingEventArgs{T}.Handled"/> to
    ///     <see langword="true"/> to cancel the change.
    /// </param>
    /// <returns><see langword="true"/> to cancel the change; <see langword="false"/> to allow it.</returns>
    protected virtual bool OnUrlChanging (ValueChangingEventArgs<string> args) => false;

    /// <summary>
    ///     Called after <see cref="Url"/> has changed. Override in subclasses to react to URL changes.
    /// </summary>
    /// <param name="args">Contains the old and new URL values.</param>
    protected virtual void OnUrlChanged (ValueChangedEventArgs<string> args) { }

    private bool? InvokeHotKeyOnNextPeer (ICommandContext commandContext)
    {
        if (RaiseHandlingHotKey (commandContext) == true)
        {
            return true;
        }

        if (CanFocus)
        {
            SetFocus ();

            // Always return true on hotkey, even if SetFocus fails because
            // hotkeys are always handled by the View (unless RaiseHandlingHotKey cancels).
            // This is the same behavior as the base (View).
            return true;
        }

        if (!HotKey.IsValid)
        {
            return false;
        }

        // If the Link has a hotkey, we need to find the next view in the subview list
        int me = SuperView?.SubViews.IndexOf (this) ?? -1;

        if (me != -1 && me < SuperView?.SubViews.Count - 1)
        {
            return SuperView?.SubViews.ElementAt (me + 1).InvokeCommand (Command.HotKey) == true;
        }

        return false;
    }

    /// <summary>
    ///     Copies the current <see cref="Url"/> to the system clipboard.
    /// </summary>
    /// <returns><see langword="true"/> if the copy operation was initiated.</returns>
    public bool Copy ()
    {
        SetClipboard (Url);

        return true;
    }

    private void SetClipboard (string text) => App?.Clipboard?.SetClipboardData (text);

    /// <summary>
    ///     Draws the link text with OSC 8 hyperlink sequences when the URL is valid.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If <see cref="View.Text"/> is empty, <see cref="Url"/> is drawn; otherwise <see cref="View.Text"/> is drawn.
    ///         The displayed text is determined by <see cref="UpdateTextFormatterText"/>.
    ///     </para>
    ///     <para>
    ///         If <see cref="Url"/> is a well-formed absolute URI, the driver's <c>CurrentUrl</c> is set so that
    ///         all drawn cells carry the URL (enabling OSC 8 hyperlink output in supporting terminals).
    ///         If the URL is not well-formed, the text is rendered with the <see cref="VisualRole.Disabled"/> style
    ///         and no hyperlink sequence is emitted.
    ///     </para>
    /// </remarks>
    /// <param name="context">The draw context for tracking drawn regions.</param>
    /// <returns><see langword="true"/> — drawing is always handled by <see cref="Link"/>.</returns>
    protected override bool OnDrawingText (DrawContext? context)
    {
        if (Driver is null)
        {
            return base.OnDrawingText (context);
        }

        Rectangle drawRect = new (ContentToScreen (Point.Empty), GetContentSize ());

        Region textDrawRegion = TextFormatter.GetDrawRegion (drawRect);

        // Report the drawn area to the context
        context?.AddDrawnRegion (textDrawRegion);

        Attribute normalAttr = HasFocus ? GetAttributeForRole (VisualRole.Focus) : GetAttributeForRole (VisualRole.Normal);
        Attribute hotAttr = HasFocus ? GetAttributeForRole (VisualRole.HotFocus) : GetAttributeForRole (VisualRole.HotNormal);

        string? url = Url;

        // If the URL is not valid, don't set CurrentUrl, and adjust the attributes to indicate it's not well-formed
        if (!_isUrlValid)
        {
            normalAttr = GetAttributeForRole (VisualRole.Disabled);
            normalAttr = normalAttr with { Background = HasFocus ? GetAttributeForRole (VisualRole.Focus).Background : normalAttr.Background };
            url = null;
        }

        // Set the URL in the driver so all cells drawn will have this URL
        Driver.CurrentUrl = url;

        try
        {
            // Draw the Title using TextFormatter - all cells will now have the URL
            TextFormatter.Draw (Driver, drawRect, normalAttr, hotAttr, Rectangle.Empty);
        }
        finally
        {
            // Always clear the URL after drawing
            Driver.CurrentUrl = null;
        }

        // We assume that the text has been drawn over the entire area; ensure that the SubViews are redrawn.
        SetSubViewNeedsDrawDownHierarchy ();

        return true; // We handled the drawing
    }

    private void SetUrl (string value)
    {
        if (_url == value)
        {
            return;
        }

        string oldValue = _url;

        // CWP: Fire ValueChanging (allows cancellation)
        ValueChangingEventArgs<string> changingArgs = new (oldValue, value);

        if (OnUrlChanging (changingArgs) || changingArgs.Handled)
        {
            return;
        }

        UrlChanging?.Invoke (this, changingArgs);

        if (changingArgs.Handled)
        {
            return;
        }

        // Do the work
        _url = value;
        _isUrlValid = Uri.IsWellFormedUriString (value, UriKind.Absolute);

        // CWP: Fire ValueChanged
        ValueChangedEventArgs<string> changedArgs = new (oldValue, value);
        OnUrlChanged (changedArgs);
        UrlChanged?.Invoke (this, changedArgs);

        // Indicate the formatter needs formatting, which will cause UpdateTextFormatterText to be invoked
        TextFormatter.NeedsFormat = true;
        SetNeedsLayout ();
    }

    /// <inheritdoc/>
    bool IDesignable.EnableForDesign ()
    {
        Title = "_Link";
        Url = "https://github.com/tui-cs";

        Initialized += (_, _) => { App?.ToolTips?.SetToolTip (this, "This is a Link. Click to open the URL in the default browser."); };

        return true;
    }

    /// <inheritdoc/>
    string? IDesignable.GetDemoKeyStrokes () => "wait:500,Enter,wait:1000";


}
