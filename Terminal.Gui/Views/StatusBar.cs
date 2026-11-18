namespace Terminal.Gui.Views;

/// <summary>
///     A status bar is a <see cref="View"/> that snaps to the bottom of the Viewport displaying set of
///     <see cref="Shortcut"/>s. The <see cref="StatusBar"/> should be context-sensitive. This means, if the main menu
///     and an open text editor are visible, the items probably shown will be ~F1~ Help ~F2~ Save ~F3~ Load. While a dialog
///     to ask a file to load is executed, the remaining commands will probably be ~F1~ Help. So for each context must be a
///     new instance of a status bar.
/// </summary>
/// <remarks>
/// <img src="../images/views/StatusBar.gif" alt="StatusBar demo"/>
/// </remarks>
public class StatusBar : Bar, IDesignable
{
    /// <inheritdoc/>
    public StatusBar () : this ([]) { }

    /// <inheritdoc/>
    public StatusBar (IEnumerable<Shortcut> shortcuts) : base (shortcuts)
    {
        TabStop = TabBehavior.NoStop;
        Orientation = Orientation.Horizontal;
        Y = Pos.AnchorEnd ();
        Width = Dim.Fill ();
        Height = Dim.Auto (DimAutoStyle.Content, 1);

        if (Border is { })
        {
            Border.LineStyle = DefaultSeparatorLineStyle;
        }

        SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Menu);

        ThemeChanges.ThemeChanged += OnThemeChanged;
    }

    /// <inheritdoc />
    protected override void OnSuperViewChanged (ValueChangedEventArgs<View?> e)
    {
        if (SuperView is null)
        {
            // BUGBUG: This is a hack for avoiding a race condition when theme overlays re-apply.
            // BUGBUG: For some reason in some unit tests, when Top is disposed, MenuBar.Dispose does not get called.
            // BUGBUG: Yet, the MenuBar does get Removed from Top (and it's SuperView set to null).
            // BUGBUG: Related: https://github.com/tui-cs/Terminal.Gui/issues/4021
            ThemeChanges.ThemeChanged -= OnThemeChanged;
        }
    }
    private void OnThemeChanged (object? sender, App.EventArgs<string> e)
    {
        if (Border is { })
        {
            Border.LineStyle = DefaultSeparatorLineStyle;
        }
    }

    /// <summary>
    ///     Gets or sets the default Line Style for the separators between the shortcuts of the StatusBar.
    /// </summary>
    public static LineStyle DefaultSeparatorLineStyle => StatusBarSettings.Current.DefaultSeparatorLineStyle;

    /// <inheritdoc />
    protected override void OnSubViewLayout (LayoutEventArgs args)
    {
        for (int index = 0; index < SubViews.Count; index++)
        {
            View barItem = SubViews.ElementAt (index);

            barItem.BorderStyle = BorderStyle;

            if (barItem.Border is { })
            {
                barItem.Border.Thickness = index == SubViews.Count - 1 ? new Thickness (0, 0, 0, 0) : new Thickness (0, 0, 1, 0);
            }

            if (barItem is Shortcut shortcut)
            {
                shortcut.Orientation = Orientation.Horizontal;
            }
        }
        base.OnSubViewLayout (args);
    }

    /// <inheritdoc/>
    protected override void OnSubViewAdded (View subView)
    {
        subView.CanFocus = false;

        if (subView is Shortcut shortcut)
        {
            // TODO: not happy about using AlignmentModes for this. Too implied.
            // TODO: instead, add a property (a style enum?) to Shortcut to control this
            shortcut.AlignmentModes = AlignmentModes.EndToStart;
        }
    }

    /// <inheritdoc />
    bool IDesignable.EnableForDesign ()
    {
        var shortcut = new Shortcut
        {
            Text = "Quit",
            Title = "Q_uit",
            Key = Key.Z.WithCtrl,
        };

        Add (shortcut);

        shortcut = new Shortcut
        {
            Text = "Help Text",
            Title = "Help",
            Key = Key.F1,
        };

        Add (shortcut);

        shortcut = new Shortcut
        {
            Title = "_Show/Hide",
            Key = Key.F10,
            CommandView = new CheckBox
            {
                CanFocus = false,
                Text = "_Show/Hide"
            },
        };

        Add (shortcut);

        var LeftButton = new Button
        {
            Text = "I'll Hide",
            // Visible = false
        };
        LeftButton.Accepting += OnButtonClicked;
        Add (LeftButton);

#pragma warning disable TGUI001
        shortcut.Accepting += (_, e) =>
                              {
                                  LeftButton.Visible = !LeftButton.Visible;
                                  LeftButton.Enabled = LeftButton.Visible;
                                  e.Handled = false;
                              };
#pragma warning restore TGUI001

        Add (new Label
        {
            HotKeySpecifier = new Rune ('_'),
            Text = "Fo_cusLabel",
            CanFocus = true
        });

        var MiddleButton = new Button
        {
            Text = "Or me!",
        };
        MiddleButton.Accepting += (s, e) => App?.RequestStop ();

        Add (MiddleButton);

        return true;

        void OnButtonClicked (object? sender, EventArgs? e) { MessageBox.Query (App!, "Hi", $"You clicked {sender}"); }
    }

    /// <inheritdoc/>
    string? IDesignable.GetDemoKeyStrokes () => "wait:500,Tab,wait:500,Tab,wait:500,Tab,wait:800";

    /// <inheritdoc />
    protected override void Dispose (bool disposing)
    {
        base.Dispose (disposing);

        ThemeChanges.ThemeChanged -= OnThemeChanged;
    }
}