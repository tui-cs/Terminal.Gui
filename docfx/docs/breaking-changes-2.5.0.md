# Terminal.Gui 2.5.0 Breaking Changes

Terminal.Gui 2.5.0 is a minor bump that includes planned API breaks. Apps that compiled against 2.4.17 may need source changes.

The GitHub Release notes list for this train lives on [#5630](https://github.com/tui-cs/Terminal.Gui/issues/5630). This page covers the four breaks that belong in the conceptual docs.

## `View.Text` is no longer virtual

<xref:Terminal.Gui.ViewBase.View.Text> is a non-virtual property. To validate or cancel a change, override `OnTextChanging(string)` or subscribe to <xref:Terminal.Gui.ViewBase.View.TextChanging>. To react after a change, override `OnTextChanged` or subscribe to <xref:Terminal.Gui.ViewBase.View.TextChanged>.

`TextField` uses `View.Text` directly (no `new` hider).

See [Cancellable Work Pattern](cancellable-work-pattern.md) and [Events](events.md).

## `IAcceptTarget` moved to `Terminal.Gui.Input`

<xref:Terminal.Gui.Input.IAcceptTarget> now lives in `Terminal.Gui.Input`. To compile against 2.5.0, add `using Terminal.Gui.Input;`.

See [Command](command.md).

## `ConfigurationManager` and `[ConfigurationProperty]` removed

2.5.0 deletes the legacy `ConfigurationManager` type and the `[ConfigurationProperty]` attribute that 2.4.17 marked `[Obsolete]`.

To load themes and settings, use <xref:Terminal.Gui.Configuration.TuiConfigurationBuilder> and Settings POCOs such as `ButtonSettings.Current`.

See [Configuration](config.md) and [Migrating ConfigurationManager to TuiConfigurationBuilder](migrate-cm-to-mec.md).

## `config.json` is nested only

A pre-2.5.0 file with dotted keys (`"Button.DefaultShadow"`) or array-shaped `Themes` / `Schemes` is not applied. A `WARN` log names the file.

To convert a legacy file, run `Tools/MigrateConfig`. Nested JSON is the supported contract.

See [Configuration](config.md) and [Migrating ConfigurationManager to TuiConfigurationBuilder](migrate-cm-to-mec.md).
