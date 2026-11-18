using System.Diagnostics;

namespace Terminal.Gui.ViewBase;

public partial class View
{
    private Lazy<ScrollBar> _horizontalScrollBar = null!;

    /// <summary>
    ///     Gets the horizontal <see cref="ScrollBar"/>. This property is lazy-loaded and will not be created until it is
    ///     accessed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="ViewportSettings"/> controls how scrolling behaves for the View.
    ///     </para>
    ///     <para>
    ///         See <see cref="ScrollBar"/> for more information on how to use the ScrollBar.
    ///     </para>
    ///     <para>
    ///         See the Layout Deep Dive for more information:
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/layout.html"/>
    ///     </para>
    /// </remarks>
    [DebuggerBrowsable (DebuggerBrowsableState.Never)]
    public ScrollBar HorizontalScrollBar => _horizontalScrollBar.Value;

    private Lazy<ScrollBar> _verticalScrollBar = null!;

    /// <summary>
    ///     Gets the vertical <see cref="ScrollBar"/>. This property is lazy-loaded and will not be created until it is
    ///     accessed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="ViewportSettings"/> controls how scrolling behaves for the View.
    ///     </para>
    ///     <para>
    ///         See <see cref="ScrollBar"/> for more information on how to use the ScrollBar.
    ///     </para>
    ///     <para>
    ///         See the Layout Deep Dive for more information:
    ///         <see href="https://tui-cs.github.io/Terminal.Gui/docs/layout.html"/>
    ///     </para>
    /// </remarks>
    [DebuggerBrowsable (DebuggerBrowsableState.Never)]
    public ScrollBar VerticalScrollBar => _verticalScrollBar.Value;

    /// <summary>
    ///     Initializes the ScrollBars of the View. Called by the View constructor.
    /// </summary>
    private void SetupScrollBars ()
    {
        if (this is AdornmentView)
        {
            return;
        }

        // Terminal.Gui is single-threaded (UI thread only), so None is appropriate
        _verticalScrollBar = new Lazy<ScrollBar> (() => CreateScrollBar (Orientation.Vertical), LazyThreadSafetyMode.None);
        _horizontalScrollBar = new Lazy<ScrollBar> (() => CreateScrollBar (Orientation.Horizontal), LazyThreadSafetyMode.None);
    }

    private ScrollBar CreateScrollBar (Orientation orientation)
    {
        // Ensure padding can support SubViews
        Padding.GetOrCreateView ();

        ScrollBar scrollBar = new ()
        {
            Orientation = orientation, Visible = false // Initially hidden until needed
        };

        if (orientation == Orientation.Vertical)
        {
            ConfigureVerticalScrollBar (scrollBar);
            ConfigureVerticalScrollBarEvents (scrollBar);
        }
        else
        {
            ConfigureHorizontalScrollBar (scrollBar);
            ConfigureHorizontalScrollBarEvents (scrollBar);
        }

        scrollBar.Initialized += OnScrollBarInitialized;

        // Add after setting Initialized event!
        Padding.View!.Add (scrollBar);

        scrollBar.Layout ();

        return scrollBar;
    }

    private void ConfigureVerticalScrollBar (ScrollBar scrollBar)
    {
        // Use dynamic Func to set the location & size. Note the -1 for X is to subtract
        // the Padding we add for the scrollbar. The Func is only called if the ScrollBar is
        // visible.
        scrollBar.X = Pos.AnchorEnd () - Pos.Func (_ => Padding.Thickness.Right - 1);
        scrollBar.Y = Pos.Func (_ => Padding.Thickness.Top);

        scrollBar.Height = Dim.Fill (Dim.Func (_ => Padding.Thickness.Bottom));
        scrollBar.ScrollableContentSize = GetContentHeight ();

        ViewportChanged += (_, _) => { scrollBar.Value = Viewport.Y; };

        ContentSizeChanged += (_, _) => { scrollBar.ScrollableContentSize = GetContentHeight (); };

        FrameChanged += (_, _) => { scrollBar.ScrollableContentSize = GetContentHeight (); };
    }

    private void ConfigureHorizontalScrollBar (ScrollBar scrollBar)
    {
        // Use dynamic Func to set the location & size. Note the -1 for Y is to subtract
        // the Padding we add for the scrollbar. The Func is only called if the ScrollBar is
        // visible.
        scrollBar.X = Pos.Func (_ => Padding.Thickness.Left);
        scrollBar.Y = Pos.AnchorEnd () - Pos.Func (_ => Padding.Thickness.Bottom - 1);

        scrollBar.Width = Dim.Fill (Dim.Func (_ => Padding.Thickness.Right));
        scrollBar.ScrollableContentSize = GetContentWidth ();

        ViewportChanged += (_, _) => { scrollBar.Value = Viewport.X; };

        ContentSizeChanged += (_, _) => { scrollBar.ScrollableContentSize = GetContentWidth (); };

        FrameChanged += (_, _) => { scrollBar.ScrollableContentSize = GetContentWidth (); };
    }

    private void OnScrollBarInitialized (object? sender, EventArgs e)
    {
        var scrollBar = (ScrollBar)sender!;

        // Do NOT adjust Padding.Thickness here — the VisibleChanged handler already does so
        // when the scrollbar becomes visible. Adjusting here would cause a double-increment.
        scrollBar.Layout ();
    }

    private void ConfigureVerticalScrollBarEvents (ScrollBar scrollBar)
    {
        scrollBar.ValueChanged += (_, args) =>
                                  {
                                      Viewport = Viewport with { Y = Math.Min (args.NewValue, scrollBar.ScrollableContentSize - scrollBar.VisibleContentSize) };
                                  };

        scrollBar.VisibleChanging += (_, args) =>
                                     {
                                         if (!args.NewValue)
                                         {
                                             return;
                                         }
                                         int width = Frame.Size.Width - GetAdornmentsThickness ().Horizontal;

                                         if (width < 1 || Viewport.Height < 2)
                                         {
                                             // Prevent scrollbar from becoming visible if it would cause negative available space for content
                                             args.Cancel = true;
                                         }
                                     };

        scrollBar.VisibleChanged += (_, _) =>
                                    {
                                        // Reset scrolling
                                        if (!scrollBar.Visible)
                                        {
                                            Viewport = Viewport with { Y = 0 };
                                        }

                                        Padding.Thickness = Padding.Thickness with
                                        {
                                            Right = scrollBar.Visible ? Padding.Thickness.Right + 1 : Padding.Thickness.Right - 1
                                        };
                                    };
    }

    private void ConfigureHorizontalScrollBarEvents (ScrollBar scrollBar)
    {
        scrollBar.ValueChanged += (_, args) =>
                                  {
                                      Viewport = Viewport with { X = Math.Min (args.NewValue, scrollBar.ScrollableContentSize - scrollBar.VisibleContentSize) };
                                  };

        scrollBar.VisibleChanging += (_, args) =>
                                     {
                                         if (!args.NewValue)
                                         {
                                             return;
                                         }
                                         int height = Frame.Size.Height - GetAdornmentsThickness ().Vertical;

                                         if (Viewport.Width < 2 || height < 1)
                                         {
                                             // Prevent scrollbar from becoming visible if it would cause negative available space for content
                                             args.Cancel = true;
                                         }
                                     };

        scrollBar.VisibleChanged += (_, _) =>
                                    {
                                        // Reset scrolling
                                        if (!scrollBar.Visible)
                                        {
                                            Viewport = Viewport with { X = 0 };
                                        }

                                        Padding.Thickness = Padding.Thickness with
                                        {
                                            Bottom = scrollBar.Visible ? Padding.Thickness.Bottom + 1 : Padding.Thickness.Bottom - 1
                                        };
                                    };
    }

    /// <summary>
    ///     Synchronizes the ScrollBar states with the ViewportSettings flags.
    ///     Called when ViewportSettings changes to enable/disable built-in scrollbars.
    /// </summary>
    private void SyncScrollBarsToSettings (ViewportSettingsFlags oldFlags, ViewportSettingsFlags newFlags)
    {
        if (this is AdornmentView)
        {
            return;
        }

        SyncOneScrollBar (oldFlags.FastHasFlags (ViewportSettingsFlags.HasVerticalScrollBar),
                          newFlags.FastHasFlags (ViewportSettingsFlags.HasVerticalScrollBar),
                          Orientation.Vertical);

        SyncOneScrollBar (oldFlags.FastHasFlags (ViewportSettingsFlags.HasHorizontalScrollBar),
                          newFlags.FastHasFlags (ViewportSettingsFlags.HasHorizontalScrollBar),
                          Orientation.Horizontal);
    }

    private void SyncOneScrollBar (bool hadFlag, bool hasFlag, Orientation orientation)
    {
        if (!hadFlag && hasFlag)
        {
            // Enabling: access triggers lazy creation, then set Auto mode
            ScrollBar scrollBar = orientation == Orientation.Vertical ? VerticalScrollBar : HorizontalScrollBar;
            scrollBar.VisibilityMode = ScrollBarVisibilityMode.Auto;
        }
        else if (hadFlag && !hasFlag)
        {
            // Disabling: only if the scrollbar was ever created
            Lazy<ScrollBar> lazy = orientation == Orientation.Vertical ? _verticalScrollBar : _horizontalScrollBar;

            if (!lazy.IsValueCreated)
            {
                return;
            }
            lazy.Value.VisibilityMode = ScrollBarVisibilityMode.None;
        }
    }

    /// <summary>
    ///     Clean up the ScrollBars of the View. Called by View.Dispose.
    /// </summary>
    private void DisposeScrollBars ()
    {
        if (this is AdornmentView)
        {
            return;
        }

        if (_horizontalScrollBar.IsValueCreated)
        {
            Padding.View?.Remove (_horizontalScrollBar.Value);
            _horizontalScrollBar.Value.Dispose ();
        }

        if (_verticalScrollBar.IsValueCreated)
        {
            Padding.View?.Remove (_verticalScrollBar.Value);
            _verticalScrollBar.Value.Dispose ();
        }
    }
}
