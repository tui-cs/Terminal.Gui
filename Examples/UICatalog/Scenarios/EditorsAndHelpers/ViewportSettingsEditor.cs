// ReSharper disable MoveLocalFunctionAfterJumpStatement

#nullable enable
namespace UICatalog.Scenarios;

/// <summary>
///     Provides an editor UI for View.ViewportSettings.
/// </summary>
public sealed class ViewportSettingsEditor : EditorBase
{
    public ViewportSettingsEditor ()
    {
        Title = "ViewportSettingsEditor";
        TabStop = TabBehavior.TabGroup;

        Initialized += ViewportSettingsEditor_Initialized;
    }

    protected override void OnViewToEditChanged ()
    {
        base.OnViewToEditChanged ();

        foreach (View subview in SubViews)
        {
            subview.Enabled = ViewToEdit is { }; // and not AdornmentView;
        }

        if (ViewToEdit is null)
        {
            return;
        }

        UpdatingLayoutSettings = true;

        _viewportEditor?.Enabled = ViewToEdit is not AdornmentView;

        _viewportEditor?.Value = ViewToEdit?.Viewport;

        if (ViewToEdit?.ContentSizeTracksViewport is false)
        {
            _contentSizeEditor?.Value = ViewToEdit?.GetContentSize ();
        }
        else
        {
            _contentSizeEditor?.Value = ViewToEdit?.Viewport.Size;
        }

        _cbAllowNegativeX?.Value = ViewToEdit!.ViewportSettings.HasFlag (ViewportSettingsFlags.AllowNegativeX) ? CheckState.Checked : CheckState.UnChecked;

        _cbAllowNegativeY?.Value = ViewToEdit!.ViewportSettings.HasFlag (ViewportSettingsFlags.AllowNegativeY) ? CheckState.Checked : CheckState.UnChecked;

        _cbAllowXGreaterThanContentWidth?.Value = ViewToEdit!.ViewportSettings.HasFlag (ViewportSettingsFlags.AllowXGreaterThanContentWidth)
                                                      ? CheckState.Checked
                                                      : CheckState.UnChecked;

        _cbAllowYGreaterThanContentHeight?.Value = ViewToEdit!.ViewportSettings.HasFlag (ViewportSettingsFlags.AllowYGreaterThanContentHeight)
                                                       ? CheckState.Checked
                                                       : CheckState.UnChecked;

        _cbAllowXPlusWidthGreaterThanContentWidth?.Value = ViewToEdit!.ViewportSettings.HasFlag (ViewportSettingsFlags.AllowXPlusWidthGreaterThanContentWidth)
                                                               ? CheckState.Checked
                                                               : CheckState.UnChecked;

        _cbAllowYPlusHeightGreaterThanContentHeight?.Value =
            ViewToEdit!.ViewportSettings.HasFlag (ViewportSettingsFlags.AllowYPlusHeightGreaterThanContentHeight) ? CheckState.Checked : CheckState.UnChecked;

        _cbClearContentOnly?.Value = ViewToEdit!.ViewportSettings.HasFlag (ViewportSettingsFlags.ClearContentOnly) ? CheckState.Checked : CheckState.UnChecked;

        _cbClipContentOnly?.Value = ViewToEdit!.ViewportSettings.HasFlag (ViewportSettingsFlags.ClipContentOnly) ? CheckState.Checked : CheckState.UnChecked;

        _cbTransparent?.Value = ViewToEdit!.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent) ? CheckState.Checked : CheckState.UnChecked;

        _cbTransparentMouse?.Value = ViewToEdit!.ViewportSettings.HasFlag (ViewportSettingsFlags.TransparentMouse) ? CheckState.Checked : CheckState.UnChecked;

        _osVerticalScrollBar?.Value = ViewToEdit!.ViewportSettings.HasFlag (ViewportSettingsFlags.HasVerticalScrollBar)
                                          ? ScrollBarVisibilityMode.Auto
                                          : ScrollBarVisibilityMode.None;

        _osHorizontalScrollBar?.Value = ViewToEdit!.ViewportSettings.HasFlag (ViewportSettingsFlags.HasHorizontalScrollBar)
                                            ? ScrollBarVisibilityMode.Auto
                                            : ScrollBarVisibilityMode.None;

        SetNeedsDraw ();
        UpdatingLayoutSettings = false;
    }

    /// <inheritdoc/>
    protected override void OnUpdateLayoutSettings ()
    {
        base.OnUpdateLayoutSettings ();

        //Enabled = ViewToEdit is not AdornmentView;

        if (ViewToEdit is null)
        {
            return;
        }

        _viewportEditor?.Value = ViewToEdit?.Viewport;

        if (ViewToEdit?.ContentSizeTracksViewport is false)
        {
            _contentSizeEditor?.Value = ViewToEdit?.GetContentSize ();
        }
        else
        {
            _contentSizeEditor?.Value = ViewToEdit?.Viewport.Size;
        }
    }

    private CheckBox? _cbAllowNegativeX;
    private CheckBox? _cbAllowNegativeY;
    private CheckBox? _cbAllowXGreaterThanContentWidth;
    private CheckBox? _cbAllowYGreaterThanContentHeight;
    private CheckBox? _cbAllowXPlusWidthGreaterThanContentWidth;
    private CheckBox? _cbAllowYPlusHeightGreaterThanContentHeight;
    private RectangleEditor? _viewportEditor;
    private TwoIntEditor<Size>? _contentSizeEditor;
    private CheckBox? _cbClearContentOnly;
    private CheckBox? _cbClipContentOnly;
    private CheckBox? _cbTransparent;
    private CheckBox? _cbTransparentMouse;
    private OptionSelector<ScrollBarVisibilityMode>? _osVerticalScrollBar;
    private OptionSelector<ScrollBarVisibilityMode>? _osHorizontalScrollBar;

    private void ViewportSettingsEditor_Initialized (object? s, EventArgs e)
    {
        Label labelViewport = new () { Title = "Viewport:" };

        _viewportEditor = new RectangleEditor { X = Pos.Right (labelViewport) + 1 };
        _viewportEditor.ValueChanging += ViewportValueChanging;

        void ViewportValueChanging (object? sender, ValueChangingEventArgs<Rectangle?> vea)
        {
            if (vea.NewValue is null
                || vea.NewValue.Value.Width < 0
                || vea.NewValue.Value.Height < 0
                || vea.NewValue.Value.X < 0
                || vea.NewValue.Value.Y < 0
                || ViewToEdit is AdornmentView)
            {
                vea.Handled = true;

                return;
            }

            if (UpdatingLayoutSettings)
            {
                return;
            }

            ViewToEdit?.Viewport = vea.NewValue.Value;
        }

        Label labelContentSize = new () { Title = "ContentSize:", X = Pos.Right (_viewportEditor) + 1, Y = Pos.Top (labelViewport) };

        _contentSizeEditor = TwoIntEditor<Size>.ForSize ();
        _contentSizeEditor.X = Pos.Right (labelContentSize) + 1;
        _contentSizeEditor.Y = Pos.Top (labelContentSize);
        _contentSizeEditor.ValueChanging += ContentSizeValueChanging;

        void ContentSizeValueChanging (object? sender, ValueChangingEventArgs<Size?> cea)
        {
            if (cea.NewValue is null || cea.NewValue.Value.Width < 0 || cea.NewValue.Value.Height < 0)
            {
                cea.Handled = true;

                return;
            }

            if (UpdatingLayoutSettings)
            {
                return;
            }

            ViewToEdit?.SetContentSize (cea.NewValue!.Value);
        }

        _cbAllowNegativeX = new CheckBox { Y = Pos.Bottom (_contentSizeEditor), Title = "Allow X < 0", CanFocus = true };

        Add (_cbAllowNegativeX);

        _cbAllowNegativeY = new CheckBox { X = Pos.Right (_cbAllowNegativeX) + 1, Y = Pos.Top (_cbAllowNegativeX), Title = "Allow Y < 0", CanFocus = true };

        Add (_cbAllowNegativeY);

        _cbAllowXGreaterThanContentWidth = new CheckBox { Title = "Allow X > Content Width", Y = Pos.Bottom (_cbAllowNegativeY), CanFocus = true };

        _cbAllowNegativeX.ValueChanging += AllowNegativeXToggle;
        _cbAllowXGreaterThanContentWidth.ValueChanging += AllowXGreaterThanContentWidthToggle;

        Add (_cbAllowXGreaterThanContentWidth);

        void AllowNegativeXToggle (object? sender, ValueChangingEventArgs<CheckState> rea)
        {
            if (rea.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= ViewportSettingsFlags.AllowNegativeX;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~ViewportSettingsFlags.AllowNegativeX;
            }
        }

        void AllowXGreaterThanContentWidthToggle (object? sender, ValueChangingEventArgs<CheckState> rea)
        {
            if (rea.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= ViewportSettingsFlags.AllowXGreaterThanContentWidth;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~ViewportSettingsFlags.AllowXGreaterThanContentWidth;
            }
        }

        _cbAllowYGreaterThanContentHeight = new CheckBox
        {
            Title = "Allow Y > Content Height",
            X = Pos.Right (_cbAllowXGreaterThanContentWidth) + 1,
            Y = Pos.Top (_cbAllowXGreaterThanContentWidth),
            CanFocus = true
        };

        _cbAllowNegativeY.ValueChanging += AllowNegativeYToggle;

        _cbAllowYGreaterThanContentHeight.ValueChanging += AllowYGreaterThanContentHeightToggle;

        Add (_cbAllowYGreaterThanContentHeight);

        void AllowNegativeYToggle (object? sender, ValueChangingEventArgs<CheckState> rea)
        {
            if (rea.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= ViewportSettingsFlags.AllowNegativeY;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~ViewportSettingsFlags.AllowNegativeY;
            }
        }

        void AllowYGreaterThanContentHeightToggle (object? sender, ValueChangingEventArgs<CheckState> rea)
        {
            if (rea.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= ViewportSettingsFlags.AllowYGreaterThanContentHeight;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~ViewportSettingsFlags.AllowYGreaterThanContentHeight;
            }
        }

        _cbAllowXPlusWidthGreaterThanContentWidth = new CheckBox
        {
            Title = "Allow X+Width > Content Width", Y = Pos.Bottom (_cbAllowXGreaterThanContentWidth), CanFocus = true
        };
        _cbAllowXPlusWidthGreaterThanContentWidth.ValueChanging += AllowXPlusWidthGreaterThanContentWidthToggle;

        Add (_cbAllowXPlusWidthGreaterThanContentWidth);

        void AllowXPlusWidthGreaterThanContentWidthToggle (object? sender, ValueChangingEventArgs<CheckState> rea)
        {
            if (rea.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= ViewportSettingsFlags.AllowXPlusWidthGreaterThanContentWidth;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~ViewportSettingsFlags.AllowXPlusWidthGreaterThanContentWidth;
            }
        }

        _cbAllowYPlusHeightGreaterThanContentHeight = new CheckBox
        {
            Title = "Allow Y+Height > Content Height",
            X = Pos.Left (_cbAllowYGreaterThanContentHeight),
            Y = Pos.Bottom (_cbAllowYGreaterThanContentHeight),
            CanFocus = true
        };
        _cbAllowYPlusHeightGreaterThanContentHeight.ValueChanging += AllowYPlusHeightGreaterThanContentHeightToggle;

        Add (_cbAllowYPlusHeightGreaterThanContentHeight);

        void AllowYPlusHeightGreaterThanContentHeightToggle (object? sender, ValueChangingEventArgs<CheckState> rea)
        {
            if (rea.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= ViewportSettingsFlags.AllowYPlusHeightGreaterThanContentHeight;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~ViewportSettingsFlags.AllowYPlusHeightGreaterThanContentHeight;
            }
        }

        _cbClearContentOnly = new CheckBox { Title = "ClearContentOnly", Y = Pos.Bottom (_cbAllowYPlusHeightGreaterThanContentHeight), CanFocus = true };
        _cbClearContentOnly.ValueChanging += ClearContentOnlyToggle;

        void ClearContentOnlyToggle (object? sender, ValueChangingEventArgs<CheckState> rea)
        {
            if (rea.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= ViewportSettingsFlags.ClearContentOnly;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~ViewportSettingsFlags.ClearContentOnly;
            }
        }

        _cbClipContentOnly = new CheckBox
        {
            Title = "ClipContentOnly", X = Pos.Right (_cbClearContentOnly) + 1, Y = Pos.Top (_cbClearContentOnly), CanFocus = true
        };
        _cbClipContentOnly.ValueChanging += ClipContentOnlyToggle;

        void ClipContentOnlyToggle (object? sender, ValueChangingEventArgs<CheckState> rea)
        {
            if (rea.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= ViewportSettingsFlags.ClipContentOnly;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~ViewportSettingsFlags.ClipContentOnly;
            }
        }

        _cbTransparent = new CheckBox { Title = "Transparent", X = Pos.Right (_cbClipContentOnly) + 1, Y = Pos.Top (_cbClipContentOnly), CanFocus = true };
        _cbTransparent.ValueChanging += TransparentToggle;

        void TransparentToggle (object? sender, ValueChangingEventArgs<CheckState> rea)
        {
            if (rea.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= ViewportSettingsFlags.Transparent;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~ViewportSettingsFlags.Transparent;
            }
        }

        _cbTransparentMouse = new CheckBox { Title = "TransparentMouse", X = Pos.Right (_cbTransparent) + 1, Y = Pos.Top (_cbTransparent), CanFocus = true };
        _cbTransparentMouse.ValueChanging += TransparentMouseToggle;

        void TransparentMouseToggle (object? sender, ValueChangingEventArgs<CheckState> rea)
        {
            if (rea.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~ViewportSettingsFlags.TransparentMouse;
            }
        }

        Label lblVerticalScrollBar = new () { Title = "V ScrollBar:", Y = Pos.Bottom (_cbClearContentOnly) };

        _osVerticalScrollBar = new OptionSelector<ScrollBarVisibilityMode>
        {
            X = Pos.Right (lblVerticalScrollBar) + 1,
            Y = Pos.Top (lblVerticalScrollBar),
            Value = ScrollBarVisibilityMode.None,
            Orientation = Orientation.Horizontal,
            AssignHotKeys = true
        };
        _osVerticalScrollBar.ValueChanged += VerticalScrollBarChanged;

        void VerticalScrollBarChanged (object? sender, EventArgs<ScrollBarVisibilityMode?> rea)
        {
            if (ViewToEdit is null or AdornmentView)
            {
                return;
            }

            if (rea.Value == ScrollBarVisibilityMode.Auto)
            {
                ViewToEdit!.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~ViewportSettingsFlags.HasVerticalScrollBar;
                ViewToEdit!.VerticalScrollBar.VisibilityMode = rea.Value!.Value;
            }
        }

        Label lblHorizontalScrollBar = new () { Title = "H ScrollBar:", Y = Pos.Bottom (lblVerticalScrollBar) };

        _osHorizontalScrollBar = new OptionSelector<ScrollBarVisibilityMode>
        {
            X = Pos.Right (lblHorizontalScrollBar) + 1,
            Y = Pos.Top (lblHorizontalScrollBar),
            Value = ScrollBarVisibilityMode.None,
            Orientation = Orientation.Horizontal,
            AssignHotKeys = true
        };
        _osHorizontalScrollBar.ValueChanged += HorizontalScrollBarChanged;

        void HorizontalScrollBarChanged (object? sender, EventArgs<ScrollBarVisibilityMode?> rea)
        {
            if (ViewToEdit is null or AdornmentView)
            {
                return;
            }

            if (rea.Value == ScrollBarVisibilityMode.Auto)
            {
                ViewToEdit!.ViewportSettings |= ViewportSettingsFlags.HasHorizontalScrollBar;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~ViewportSettingsFlags.HasHorizontalScrollBar;
                ViewToEdit!.HorizontalScrollBar.VisibilityMode = rea.Value!.Value;
            }
        }

        Add (labelViewport,
             _viewportEditor,
             labelContentSize,
             _contentSizeEditor,
             _cbClearContentOnly,
             _cbClipContentOnly,
             _cbTransparent,
             _cbTransparentMouse,
             lblVerticalScrollBar,
             _osVerticalScrollBar,
             lblHorizontalScrollBar,
             _osHorizontalScrollBar);
    }
}
