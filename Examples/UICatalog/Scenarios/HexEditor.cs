#nullable enable

using System.Text;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("HexEditor", "A binary (hex) editor using the HexView control.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Dialogs")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Navigation")]
[ScenarioCategory ("Files and IO")]
public class HexEditor : Scenario
{
    private IApplication? _app;
    private string? _fileName;
    private HexView? _hexView;
    private MenuItem? _miReadOnly;
    private bool _saved = true;
    private Shortcut? _scAddress;
    private Shortcut? _scInfo;
    private Shortcut? _scPosition;
    private StatusBar? _statusBar;
    private Window? _win;

    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        _win = new () { BorderStyle = LineStyle.None };

        _fileName = "demo.bin";
        CreateDemoFile (_fileName);

        _hexView = new (new MemoryStream ("Demo text."u8.ToArray ()))
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill (1),
            Title = _fileName ?? "Untitled",
            BorderStyle = LineStyle.Rounded
        };
        _hexView.Arrangement = ViewArrangement.Resizable;
        _hexView.Edited += _hexView_Edited;
        _hexView.PositionChanged += _hexView_PositionChanged;

        // Scrollbars are disabled by default (VisibilityMode.Manual)

        _win.Add (_hexView);

        MenuBar menu = new ()
        {
            Menus =
            [
                new (Strings.menuFile,
                     new MenuItem []
                     {
                         new (Strings.cmdNew, "", New),
                         new (Strings.cmdOpen, "", Open),
                         new (Strings.cmdSave, "", Save),
                         null!, // Passing null automatically creates a separator (a Line object).
                         new (Strings.cmdQuit, "", Quit)
                     }),
                new ("_Edit", new MenuItem [] { new (Strings.cmdCopy, "", Copy), new (Strings.cmdCut, "", Cut), new (Strings.cmdPaste, "", Paste) }),
                new ("_Options", [_miReadOnly = new ("_Read Only", "", ToggleReadOnly)])
            ]
        };

        CheckBox cb = new () { Title = _miReadOnly.Title, Value = _hexView.ReadOnly ? CheckState.Checked : CheckState.None };
        _miReadOnly.CommandView = cb;
        _win.Add (menu);

        NumericUpDown addressWidthUpDown = new () { Value = _hexView.AddressWidth };

        NumericUpDown<long> addressUpDown = new () { Value = _hexView.Address, Format = $"0x{{0:X{_hexView.AddressWidth}}}" };

        addressWidthUpDown.ValueChanging += (_, args) =>
                                            {
                                                args.Handled = args.NewValue is < 0 or > 8;

                                                if (args.Handled)
                                                {
                                                    return;
                                                }
                                                _hexView.AddressWidth = args.NewValue;

                                                // ReSharper disable once AccessToDisposedClosure
                                                addressUpDown.Format = $"0x{{0:X{_hexView.AddressWidth}}}";
                                            };

        addressUpDown.ValueChanging += (_, args) =>
                                       {
                                           args.Handled = args.NewValue < 0;

                                           if (!args.Handled)
                                           {
                                               _hexView.Address = args.NewValue;
                                           }
                                       };

        _statusBar = new ([
                              new (Key.F2, "Open", Open),
                              new (Key.F3, "Save", Save),
                              new () { CommandView = addressWidthUpDown, HelpText = "Address Width" },
                              _scAddress = new () { CommandView = addressUpDown, HelpText = "Address:" },
                              _scInfo = new (Key.Empty, string.Empty, () => { }),
                              _scPosition = new (Key.Empty, string.Empty, () => { })
                          ]) { AlignmentModes = AlignmentModes.IgnoreFirstOrLast };
        _win.Add (_statusBar);

        _hexView.ViewportSettings |= ViewportSettingsFlags.HasScrollBars;

        _hexView.Source = LoadFile ();

        app.Run (_win);
        addressUpDown.Dispose ();
        addressWidthUpDown.Dispose ();
        _win.Dispose ();
    }

    private void _hexView_Edited (object? sender, HexViewEditEventArgs e) => _saved = false;

    private void _hexView_PositionChanged (object? sender, HexViewEventArgs obj)
    {
        _scInfo!.Title = $"Bytes: {_hexView!.Source!.Length}";
        _scPosition!.Title = $"L: {obj.Position.Y} C: {obj.Position.X} Per Line: {obj.BytesPerLine}";

        if (_scAddress!.CommandView is NumericUpDown<long> addrNumericUpDown)
        {
            addrNumericUpDown.Value = obj.Address;
        }
    }

    private void Copy () => MessageBox.ErrorQuery (_hexView!.App!, "Not Implemented", "Functionality not yet implemented.", "Ok");

    private void CreateDemoFile (string fileName)
    {
        StringBuilder sb = new ();
        sb.Append ("Hello world.\n");
        sb.Append ("This is a test of the Emergency Broadcast System.\n");

        StreamWriter sw = File.CreateText (fileName);
        sw.Write (sb.ToString ());
        sw.Close ();
    }

    private void Cut () => MessageBox.ErrorQuery (_hexView!.App!, "Not Implemented", "Functionality not yet implemented.", "Ok");

    private Stream LoadFile ()
    {
        MemoryStream stream = new ();

        if (!_saved && _hexView!.Edits.Count > 0 && _hexView.Source is { })
        {
            if (MessageBox.ErrorQuery (_hexView!.App!, "Save", "The changes were not saved. Want to open without saving?", Strings.btnNo, Strings.btnYes) == 0)
            {
                return _hexView.Source;
            }

            _hexView.DiscardEdits ();
            _saved = true;
        }

        if (_fileName is { })
        {
            byte [] bin = File.ReadAllBytes (_fileName);
            stream.Write (bin);
            _hexView!.Title = _fileName;
            _saved = true;
        }
        else
        {
            _hexView!.Title = _fileName ?? "Untitled";
        }

        return stream;
    }

    private void New ()
    {
        _fileName = null;
        _hexView!.Source = LoadFile ();
    }

    private void Open ()
    {
        OpenDialog d = new () { Title = "Open", AllowsMultipleSelection = false };
        _app?.Run (d);

        if (!d.Canceled)
        {
            _fileName = d.FilePaths [0];
            _hexView!.Source = LoadFile ();

            //_hexView.DisplayStart = 0;
        }

        d.Dispose ();
    }

    private void Paste () => MessageBox.ErrorQuery (_hexView!.App!, "Not Implemented", "Functionality not yet implemented.", Strings.btnOk);
    private void Quit () => _win?.RequestStop ();

    private void Save ()
    {
        if (_fileName != null)
        {
            using (FileStream fs = new (_fileName, FileMode.OpenOrCreate))
            {
                _hexView?.ApplyEdits (fs);

                //_hexView.Source.Position = 0;
                //_hexView.Source.CopyTo (fs);
                //fs.Flush ();
            }

            _saved = true;
        }
        else
        {
            _hexView!.ApplyEdits ();
        }
    }

    private void ToggleReadOnly ()
    {
        if (_miReadOnly?.CommandView is not CheckBox cb)
        {
            return;
        }

        _hexView!.ReadOnly = cb.Value == CheckState.Checked;
    }
}
