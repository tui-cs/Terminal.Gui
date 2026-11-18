using System.Collections;
using System.Collections.ObjectModel;

namespace Terminal.Gui.Views;

/// <summary>
///     A dropdown/combo-box control that combines a <see cref="TextField"/> with a popover <see cref="ListView"/>
///     for selecting from a list of items.
/// </summary>
/// <remarks>
/// <img src="../images/views/DropDownList.gif" alt="DropDownList demo"/>
///     <para>
///         <see cref="DropDownList"/> provides a modern dropdown control that can operate in two modes:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <b>ReadOnly mode</b> (<see cref="TextField.ReadOnly"/> = <see langword="true"/>): Acts like a
///                 traditional
///                 dropdown where clicking anywhere opens the list. The text field is not editable.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Editable mode</b> (<see cref="TextField.ReadOnly"/> = <see langword="false"/>): Acts like a combo
///                 box
///                 where the user can type text or select from the list.
///             </description>
///         </item>
///     </list>
///     <para>
///         <b>Key Features:</b>
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Toggle dropdown with button click, F4, or Alt+Down</description>
///         </item>
///         <item>
///             <description>Pre-selects matching item in list when opening</description>
///         </item>
///         <item>
///             <description>Returns focus to text field when closed</description>
///         </item>
///         <item>
///             <description>Supports <see cref="IValue{T}"/> interface for data binding</description>
///         </item>
///         <item>
///             <description>Auto-registers popover on first use</description>
///         </item>
///     </list>
///     <para>
///         <b>Usage Example:</b>
///     </para>
///     <code>
///         var dropdown = new DropDownList
///         {
///             Source = new ListWrapper&lt;string&gt; (["Option 1", "Option 2", "Option 3"]),
///             ReadOnly = true,
///             Text = "Option 1"
///         };
///         dropdown.ValueChanged += (s, e) => MessageBox.Query ("Selected", dropdown.Text, "Ok");
///     </code>
///     <para>
///         Default key bindings are defined in <see cref="DefaultKeyBindings"/> (in addition to
///         <see cref="TextField"/> bindings):
///     </para>
///     <list type="table">
///         <listheader>
///             <term>Key</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>F4</term> <description>Toggles the dropdown list open or closed.</description>
///         </item>
///         <item>
///             <term>Alt+Down</term> <description>Toggles the dropdown list open or closed.</description>
///         </item>
///         <item>
///             <term>Space</term> <description>Toggles the dropdown list open or closed.</description>
///         </item>
///         <item>
///             <term>Up</term> <description>Selects the previous item in the list (when closed).</description>
///         </item>
///         <item>
///             <term>Down</term> <description>Selects the next item in the list (when closed).</description>
///         </item>
///     </list>
///     <para>Default mouse bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Mouse Event</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Click</term> <description>Activates the dropdown (<see cref="Command.Activate"/>).</description>
///         </item>
///     </list>
/// </remarks>
public class DropDownList : TextField
{
    /// <summary>
    ///     Gets or sets the view-specific default key bindings for <see cref="DropDownList"/>. Contains only bindings
    ///     unique to this view; shared bindings come from <see cref="View.DefaultKeyBindings"/>.
    ///     <para>
    ///         <b>IMPORTANT:</b> This is a process-wide static property. Change with care.
    ///         Do not set in parallelizable unit tests.
    ///     </para>
    /// </summary>
    public new static Dictionary<Command, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ()
    {
        [Command.Toggle] = Bind.All (Key.F4, Key.CursorDown.WithAlt, Key.Space),
        [Command.Up] = Bind.All (Key.CursorUp),
        [Command.Down] = Bind.All (Key.CursorDown)
    };

    private readonly Button? _toggleButton;
    private Popover<ListView, string?>? _listPopover;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DropDownList"/> class.
    /// </summary>
    public DropDownList ()
    {
        ReadOnly = true; // Default to readonly mode

        // Create toggle button with down arrow
        _toggleButton = new Button
        {
            Text = Glyphs.DownArrow.ToString (),
            X = Pos.AnchorEnd (),
            CanFocus = false,
            TabStop = TabBehavior.NoStop,
            NoPadding = true,
            NoDecorations = true,
            ShadowStyle = null
        };

#if DEBUG
        _toggleButton.Id = "dropDownListToggleButton";
#endif

        _toggleButton.Accepted += (_, _) => ToggleDropDown (); // Toggle dropdown on button click

        // Create ListView for popover
        ListView listView = new ()
        {
            Width = Dim.Auto (DimAutoStyle.Content),
            Height = Dim.Auto (DimAutoStyle.Content,
                               1,
                               Dim.Func (_ =>
                                         {
                                             int screenHeight = Driver?.Screen.Height ?? 0;
                                             Rectangle frame = FrameToScreen ();
                                             int spaceBelow = screenHeight - frame.Bottom;
                                             int spaceAbove = frame.Top;

                                             // Use the larger of the two directions since GetAdjustedPosition
                                             // will flip the popover above when it doesn't fit below.
                                             int available = Math.Max (spaceBelow, spaceAbove);

                                             return Math.Min (Source?.Count ?? 0, Math.Max (1, available));
                                         })),
            ViewportSettings = ViewportSettingsFlags.HasVerticalScrollBar,

            // Use the Menu scheme so the dropped list "floats" with its own background, distinct from the
            // SuperView's. See https://github.com/gui-cs/Terminal.Gui/issues/5517.
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Menu)
        };

        // Create popover
        _listPopover = new Popover<ListView, string?> (listView) { Anchor = GetAnchor };

#if DEBUG
        _listPopover.Id = "dropDownListPopover";
#endif

        // Configure commands to bubble up
        _listPopover.CommandsToBubbleUp = [Command.Activate, Command.Accept];

        _listPopover.Anchor = GetAnchor;

        // Add toggle button to Padding
        Padding.Thickness = Padding.Thickness with { Right = 1 }; // Add some spacing on the right for the button
        Padding.GetOrCreateView ().Add (_toggleButton);

        // Adjust TextField width to account for toggle button
        Width = Dim.Auto (minimumContentDim: Dim.Func (_ => _listPopover.ContentView?.MaxItemLength ?? 0));

        // Add command handler for toggle
        AddCommand (Command.Toggle, ToggleDropDown);

        // Add command handlers for navigating items when dropdown is closed
        AddCommand (Command.Up, MoveSelectionUp);
        AddCommand (Command.Down, MoveSelectionDown);

        // Apply layered key bindings (base View layer + DropDownList-specific layer)
        ApplyKeyBindings (View.DefaultKeyBindings, DefaultKeyBindings);

        MouseBindings.Add (MouseFlags.LeftButtonPressed, Command.Activate);
    }

    /// <inheritdoc/>
    public override void EndInit ()
    {
        // Set target for command bridging
        _listPopover?.Target = new WeakReference<View> (this);

        base.EndInit ();
    }

    /// <inheritdoc/>
    protected override bool OnMouseEvent (Mouse ev)
    {
        if (!ReadOnly || !ev.Flags.FastHasFlags (MouseFlags.LeftButtonPressed))
        {
            return base.OnMouseEvent (ev);
        }

        App?.Popovers?.Register (_listPopover);

        return InvokeCommand (Command.Activate) is true;
    }

    /// <inheritdoc/>
    protected override bool OnHasFocusChanging (bool currentHasFocus, bool newHasFocus, View? currentFocused, View? newFocused)
    {
        if (base.OnHasFocusChanging (currentHasFocus, newHasFocus, currentFocused, newFocused))
        {
            return true;
        }

        if (newHasFocus)
        {
            App?.Popovers?.Register (_listPopover);

            return false;
        }

        // Hide while the popover is still active so Hide can invalidate the covered region.
        // DeRegister after that also hides if still active; doing Hide here covers the
        // keyboard Accepting+Handled path that moves focus before OnAccepted runs (#5635).
        App?.Popovers?.Hide (_listPopover);
        App?.Popovers?.DeRegister (_listPopover);

        return false;
    }

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? ctx)
    {
        // If activation came from the ListView, update text
        if (ctx?.Source?.TryGetTarget (out View? sourceView) == true)
        {
            if (_listPopover?.ContentView is { } contentView && sourceView == contentView)

            {
                _listPopover.Visible = false; // Hide popover after selection

                if (_listPopover?.ContentView.SelectedItem is not { } selectedIndex || _listPopover?.ContentView.Source is null)
                {
                    return;
                }

                IList? items = _listPopover?.ContentView.Source.ToList ();

                if (items is null || selectedIndex < 0 || selectedIndex >= items.Count)
                {
                    return;
                }

                Text = items [selectedIndex]?.ToString () ?? string.Empty;
            }

            if (sourceView == this && ReadOnly && _listPopover is not { Visible: true })
            {
                OpenDropDown (); // Open dropdown when activating the TextField in ReadOnly mode
            }
        }

        base.OnActivated (ctx);
    }

    /// <inheritdoc/>
    protected override void OnAccepted (ICommandContext? ctx)
    {
        base.OnAccepted (ctx);

        // If accept came from the ListView, update text
        if (ctx?.Source?.TryGetTarget (out View? sourceView) != true)
        {
            return;
        }

        if (sourceView == _listPopover?.ContentView)
        {
            _listPopover?.Visible = false; // Hide popover after selection

            if (_listPopover?.ContentView?.SelectedItem is not { } selectedIndex || _listPopover?.ContentView.Source is null)
            {
                return;
            }

            IList? items = _listPopover?.ContentView?.Source.ToList ();

            if (items is null || selectedIndex < 0 || selectedIndex >= items.Count)
            {
                return;
            }

            Text = items [selectedIndex]?.ToString () ?? string.Empty;
        }

        if (sourceView == _toggleButton)
        {
            ToggleDropDown ();
        }
    }

    /// <summary>
    ///     Overrides attribute retrieval to ensure that in ReadOnly mode, the control uses the Normal or Focus attributes
    /// </summary>
    /// <param name="role"></param>
    /// <param name="currentAttribute"></param>
    /// <returns></returns>
    protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
    {
        switch (role)
        {
            case VisualRole.ReadOnly when ReadOnly:

            case VisualRole.Active when ReadOnly:
                {
                    currentAttribute = GetAttributeForRole (HasFocus ? VisualRole.Focus : VisualRole.Normal);

                    return true;
                }

            case VisualRole.Editable when ReadOnly:
                {
                    currentAttribute = GetAttributeForRole (HasFocus ? VisualRole.Focus : VisualRole.Normal);

                    break;
                }
        }

        return false;
    }

    /// <summary>
    ///     Gets or sets the data source for the dropdown list.
    /// </summary>
    /// <remarks>
    ///     This property delegates to the <see cref="ListView.Source"/> property of the internal <see cref="ListView"/>.
    /// </remarks>
    public IListDataSource? Source
    {
        get => _listPopover?.ContentView?.Source;
        set
        {
            if (_listPopover?.ContentView is { } contentView)
            {
                contentView.Source = value;
            }

            KeystrokeNavigator.Collection = value?.ToList ();
        }
    }

    /// <summary>
    ///     Provides the anchor rectangle for positioning the popover below the DropDownList.
    /// </summary>
    private Rectangle? GetAnchor () => ViewportToScreen ();

    /// <summary>
    ///     Toggles the dropdown list visibility.
    /// </summary>
    private bool? ToggleDropDown ()
    {
        if (_listPopover is null)
        {
            return false;
        }

        if (_listPopover.Visible)
        {
            App?.Popovers?.Hide (_listPopover);
        }
        else
        {
            OpenDropDown ();
        }

        return true;
    }

    /// <summary>
    ///     Opens the dropdown list and pre-selects the current value.
    /// </summary>
    private void OpenDropDown ()
    {
        if (_listPopover?.ContentView is not { } listView)
        {
            return;
        }

        // Pre-select matching item
        if (!string.IsNullOrEmpty (Text) && listView.Source is { })
        {
            IList? items = listView.Source.ToList ();

            if (items is { })
            {
                for (var i = 0; i < items.Count; i++)
                {
                    if (items [i]?.ToString () != Text)
                    {
                        continue;
                    }
                    listView.SelectedItem = i;

                    break;
                }
            }
        }

        // Register if needed
        if (App?.Popovers is { } && !App.Popovers.IsRegistered (_listPopover))
        {
            App.Popovers.Register (_listPopover);
        }

        // Show the popover
        _listPopover.MakeVisible ();
    }

    /// <summary>
    ///     Gets the <see cref="IListCollectionNavigator"/> that searches the <see cref="Source"/> collection as the
    ///     user types when the dropdown is closed.
    /// </summary>
    public IListCollectionNavigator KeystrokeNavigator { get; } = new CollectionNavigator ();

    /// <inheritdoc/>
    protected override bool OnKeyDown (Key key)
    {
        // Only handle collection navigation when dropdown is closed and in ReadOnly mode
        if (_listPopover is { Visible: true } || !ReadOnly)
        {
            return base.OnKeyDown (key);
        }

        // If the key is bound to a command, let normal processing happen
        if (KeyBindings.TryGet (key, out _))
        {
            return false;
        }

        // Enable user to find & select an item by typing text
        if (Source is null)
        {
            return false;
        }

        if (!KeystrokeNavigator.Matcher.IsCompatibleKey (key))
        {
            return false;
        }

        int currentIndex = GetCurrentSelectedIndex () ?? -1;
        int? newItem = KeystrokeNavigator.GetNextMatchingItem (currentIndex >= 0 ? currentIndex : null, (char)key);

        if (newItem is null or -1)
        {
            return false;
        }

        SelectItemAtIndex (newItem.Value);

        return true;
    }

    /// <summary>
    ///     Moves the selection to the previous item in the list. Does nothing if already at the first item.
    /// </summary>
    private bool? MoveSelectionUp ()
    {
        // If the dropdown is open, let the popover handle it
        if (_listPopover is { Visible: true })
        {
            return null;
        }

        int? currentIndex = GetCurrentSelectedIndex ();

        if (currentIndex is null or <= 0)
        {
            return true; // At start or no source — do nothing but consume the key
        }

        SelectItemAtIndex (currentIndex.Value - 1);

        return true;
    }

    /// <summary>
    ///     Moves the selection to the next item in the list. Does nothing if already at the last item.
    /// </summary>
    private bool? MoveSelectionDown ()
    {
        // If the dropdown is open, let the popover handle it
        if (_listPopover is { Visible: true })
        {
            return null;
        }

        int? currentIndex = GetCurrentSelectedIndex ();
        int count = Source?.Count ?? 0;

        if (count == 0)
        {
            return true;
        }

        int nextIndex = (currentIndex ?? -1) + 1;

        if (nextIndex >= count)
        {
            return true; // At end — do nothing but consume the key
        }

        SelectItemAtIndex (nextIndex);

        return true;
    }

    /// <summary>
    ///     Gets the index of the currently selected item based on the current <see cref="TextField.Text"/>.
    /// </summary>
    private int? GetCurrentSelectedIndex ()
    {
        IList? items = Source?.ToList ();

        if (items is null)
        {
            return null;
        }

        for (var i = 0; i < items.Count; i++)
        {
            if (string.Equals (items [i]?.ToString (), Text, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return null;
    }

    /// <summary>
    ///     Selects the item at the specified index, updating <see cref="TextField.Text"/>.
    /// </summary>
    private void SelectItemAtIndex (int index)
    {
        IList? items = Source?.ToList ();

        if (items is null)
        {
            return;
        }

        if (index < 0 || index >= items.Count)
        {
            return;
        }

        Text = items [index]?.ToString () ?? string.Empty;
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public override bool EnableForDesign ()
    {
        // Sample data
        ObservableCollection<string> countries =
        [
            "Argentina",
            "Brazil",
            "Canada",
            "Denmark",
            "Egypt",
            "France",
            "Germany",
            "Hungary",
            "India",
            "Japan"
        ];

        Source = new ListWrapper<string> (countries);

        Value = "Germany";

        return true;
    }

    /// <inheritdoc/>
    public override string? GetDemoKeyStrokes () => "wait:500,F4,wait:600,CursorDown,wait:400,CursorDown,wait:400,Enter,wait:800";

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            if (_listPopover is { })
            {
                App?.Popovers?.DeRegister (_listPopover);
                _listPopover.Dispose ();
                _listPopover = null;
            }
        }

        base.Dispose (disposing);
    }
}
