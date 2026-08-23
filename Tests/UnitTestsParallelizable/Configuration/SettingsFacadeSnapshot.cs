// Grok - grok-4.6
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace ConfigurationTests;

/// <summary>
///     Captures and restores every process-wide settings facade that
///     <see cref="TuiConfigurationBuilder.ApplyToStaticFacades"/> can mutate.
/// </summary>
internal sealed class SettingsFacadeSnapshot : IDisposable
{
    private readonly ApplicationSettings _application = ApplicationSettings.Defaults;
    private readonly DriverSettings _driver = DriverSettings.Defaults;
    private readonly FileDialogSettings _fileDialog = FileDialogSettings.Defaults;
    private readonly FileDialogStyleSettings _fileDialogStyle = FileDialogStyleSettings.Defaults;
    private readonly KeySettings _key = KeySettings.Defaults;
    private readonly TraceSettings _trace = TraceSettings.Defaults;
    private readonly ThemeSettings _theme = ThemeSettings.Defaults;
    private readonly ButtonSettings _button = ButtonSettings.Current;
    private readonly CheckBoxSettings _checkBox = CheckBoxSettings.Current;
    private readonly CharMapSettings _charMap = CharMapSettings.Current;
    private readonly DialogSettings _dialog = DialogSettings.Current;
    private readonly FrameViewSettings _frameView = FrameViewSettings.Current;
    private readonly HexViewSettings _hexView = HexViewSettings.Current;
    private readonly LinearRangeSettings _linearRange = LinearRangeSettings.Current;
    private readonly MenuBarSettings _menuBar = MenuBarSettings.Current;
    private readonly MenuSettings _menu = MenuSettings.Current;
    private readonly MessageBoxSettings _messageBox = MessageBoxSettings.Current;
    private readonly NerdFontsSettings _nerdFonts = NerdFontsSettings.Current;
    private readonly PopoverMenuSettings _popoverMenu = PopoverMenuSettings.Current;
    private readonly SelectorBaseSettings _selectorBase = SelectorBaseSettings.Current;
    private readonly StatusBarSettings _statusBar = StatusBarSettings.Current;
    private readonly TextFieldSettings _textField = TextFieldSettings.Current;
    private readonly TextViewSettings _textView = TextViewSettings.Current;
    private readonly WindowSettings _window = WindowSettings.Current;
    private readonly GlyphSettings _glyphs = GlyphSettings.Current;
    private readonly Dictionary<Command, PlatformKeyBinding>? _applicationKeyBindings = CloneCommandBindings (Application.DefaultKeyBindings);
    private readonly Dictionary<Command, PlatformKeyBinding>? _viewKeyBindings = CloneCommandBindings (View.DefaultKeyBindings);
    private readonly Dictionary<string, Dictionary<Command, PlatformKeyBinding>>? _viewTypeKeyBindings = CloneViewKeyBindings (View.ViewKeyBindings);

    private readonly (Dictionary<Command, PlatformKeyBinding?>?, Dictionary<Command, PlatformKeyBinding?>?,
        Dictionary<string, Dictionary<Command, PlatformKeyBinding?>>?) _keyBindingOverlayTracking = KeyBindingConfiguration.SnapshotOverlayTracking ();

    public void Dispose ()
    {
        ApplicationSettings.Defaults = _application;
        DriverSettings.Defaults = _driver;
        FileDialogSettings.Defaults = _fileDialog;
        FileDialogStyleSettings.Defaults = _fileDialogStyle;
        KeySettings.Defaults = _key;
        TraceSettings.Defaults = _trace;
        ThemeSettings.Defaults = _theme;
        ButtonSettings.Current = _button;
        CheckBoxSettings.Current = _checkBox;
        CharMapSettings.Current = _charMap;
        DialogSettings.Current = _dialog;
        FrameViewSettings.Current = _frameView;
        HexViewSettings.Current = _hexView;
        LinearRangeSettings.Current = _linearRange;
        MenuBarSettings.Current = _menuBar;
        MenuSettings.Current = _menu;
        MessageBoxSettings.Current = _messageBox;
        NerdFontsSettings.Current = _nerdFonts;
        PopoverMenuSettings.Current = _popoverMenu;
        SelectorBaseSettings.Current = _selectorBase;
        StatusBarSettings.Current = _statusBar;
        TextFieldSettings.Current = _textField;
        TextViewSettings.Current = _textView;
        WindowSettings.Current = _window;
        GlyphSettings.Current = _glyphs;
        Application.DefaultKeyBindings = CloneCommandBindings (_applicationKeyBindings);
        View.DefaultKeyBindings = CloneCommandBindings (_viewKeyBindings);
        View.ViewKeyBindings = CloneViewKeyBindings (_viewTypeKeyBindings);
        KeyBindingConfiguration.RestoreOverlayTracking (_keyBindingOverlayTracking);
    }

    private static Dictionary<Command, PlatformKeyBinding>? CloneCommandBindings (Dictionary<Command, PlatformKeyBinding>? source) =>
        source is null ? null : new (source);

    private static Dictionary<string, Dictionary<Command, PlatformKeyBinding>>? CloneViewKeyBindings (
        Dictionary<string, Dictionary<Command, PlatformKeyBinding>>? source)
    {
        if (source is null)
        {
            return null;
        }

        Dictionary<string, Dictionary<Command, PlatformKeyBinding>> clone = new (StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, Dictionary<Command, PlatformKeyBinding>> pair in source)
        {
            clone [pair.Key] = new (pair.Value);
        }

        return clone;
    }
}
