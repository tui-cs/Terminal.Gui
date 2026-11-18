#nullable enable

namespace UICatalog.Scenarios;

public sealed class ThemeViewer : FrameView
{
    public ThemeViewer ()
    {
        BorderStyle = LineStyle.Rounded;
        Border.Thickness = new Thickness (0, 1, 0, 0);
        Margin.Thickness = new Thickness (0, 0, 1, 0);
        TabStop = TabBehavior.TabStop;
        CanFocus = true;
        Height = Dim.Fill ();
        Width = Dim.Auto ();
        Title = $"{ThemeManager.Theme}";

        ViewportSettings |= ViewportSettingsFlags.HasScrollBars;

        SubViewsLaidOut += (sender, _) =>
                           {
                               if (sender is View sendingView)
                               {
                                   sendingView.SetContentHeight (sendingView.GetHeightRequiredForSubViews ());
                               }
                           };

        AddCommand (Command.Up, () => ScrollVertical (-1));
        AddCommand (Command.Down, () => ScrollVertical (1));

        AddCommand (Command.PageUp, () => ScrollVertical (-SubViews.OfType<SchemeViewer> ().First ().Frame.Height));
        AddCommand (Command.PageDown, () => ScrollVertical (SubViews.OfType<SchemeViewer> ().First ().Frame.Height));

        AddCommand (Command.Start,
                    () =>
                    {
                        Viewport = Viewport with { Y = 0 };

                        return true;
                    });

        AddCommand (Command.End,
                    () =>
                    {
                        Viewport = Viewport with { Y = GetContentHeight () };

                        return true;
                    });

        AddCommand (Command.ScrollDown, () => ScrollVertical (1));
        AddCommand (Command.ScrollUp, () => ScrollVertical (-1));
        AddCommand (Command.ScrollRight, () => ScrollHorizontal (1));
        AddCommand (Command.ScrollLeft, () => ScrollHorizontal (-1));

        KeyBindings.Add (Key.CursorUp, Command.Up);
        KeyBindings.Add (Key.CursorDown, Command.Down);
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.PageUp, Command.PageUp);
        KeyBindings.Add (Key.PageDown, Command.PageDown);
        KeyBindings.Add (Key.Home, Command.Start);
        KeyBindings.Add (Key.End, Command.End);
        KeyBindings.Add (PopoverMenu.DefaultKey, Command.Context);

        MouseBindings.Add (MouseFlags.LeftButtonDoubleClicked, Command.Accept);
        MouseBindings.ReplaceCommands (MouseFlags.RightButtonClicked, Command.Context);
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked | MouseFlags.Ctrl, Command.Context);
        MouseBindings.Add (MouseFlags.WheeledDown, Command.ScrollDown);
        MouseBindings.Add (MouseFlags.WheeledUp, Command.ScrollUp);
        MouseBindings.Add (MouseFlags.WheeledLeft, Command.ScrollLeft);
        MouseBindings.Add (MouseFlags.WheeledRight, Command.ScrollRight);

        SchemeViewer? prevSchemeViewer = null;

        // Order schemes: built-in Schemes enum order first, then any custom schemes alphabetically
        string [] builtInOrder = Enum.GetNames<Schemes> ();

        IEnumerable<KeyValuePair<string, Scheme?>> orderedSchemes = SchemeManager.GetSchemesForCurrentTheme ()
                                                                                 .OrderBy (kvp =>
                                                                                           {
                                                                                               int idx = Array.FindIndex (builtInOrder,
                                                                                                   n => string.Equals (n,
                                                                                                       kvp.Key,
                                                                                                       StringComparison.OrdinalIgnoreCase));

                                                                                               return idx >= 0 ? idx : builtInOrder.Length;
                                                                                           })
                                                                                 .ThenBy (kvp => kvp.Key, StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, Scheme?> kvp in orderedSchemes)
        {
            var schemeViewer = new SchemeViewer { Id = $"schemeViewer for {kvp.Key}", SchemeName = kvp.Key };

            if (prevSchemeViewer is { })
            {
                schemeViewer.Y = Pos.Bottom (prevSchemeViewer);
            }

            prevSchemeViewer = schemeViewer;
            Add (schemeViewer);
        }

        ThemeManager.ThemeChanged += OnThemeManagerOnThemeChanged;
    }

    /// <inheritdoc/>
    protected override void OnFocusedChanged (View? previousFocused, View? focused)
    {
        base.OnFocusedChanged (previousFocused, focused);

        if (focused is { })
        {
            SchemeName = focused.Title;
        }
    }

    private void OnThemeManagerOnThemeChanged (object? _, EventArgs<string> args) => Title = args.Value;

    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            ThemeManager.ThemeChanged -= OnThemeManagerOnThemeChanged;
        }

        base.Dispose (disposing);
    }
}
