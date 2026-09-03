// ReSharper disable AccessToDisposedClosure

#nullable enable

using Terminal.Gui.Editor.Document;
using Terminal.Gui.Editor;
using Terminal.Gui.Editor.Highlighting;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Notepad", "Multi-tab text editor using the Tabs control.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Tabs")]
[ScenarioCategory ("Editor")]
public class Notepad : Scenario
{
    private IApplication? _app;
    private Tabs? _focusedTabs;
    private string? _lastDirectory;
    private int _numNewTabs = 1;
    private Tabs? _tabs;
    private Window? _topWindow;
    public Shortcut? LenShortcut { get; private set; }

    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        // Set initial directory to docfx/docs relative to the repository root
        string? repoRoot = FindRepoRoot ();

        if (repoRoot is { })
        {
            string docsPath = Path.Combine (repoRoot, "docfx", "docs");

            if (Directory.Exists (docsPath))
            {
                _lastDirectory = docsPath;
            }
        }

        _topWindow = new Window { BorderStyle = LineStyle.None };

        // MenuBar
        MenuBar menu = new ();

        menu.Add (new MenuBarItem (Strings.menuFile,
                                   [
                                       new MenuItem { Title = Strings.cmdNew, Key = Key.N.WithCtrl.WithAlt, Action = New },
                                       new MenuItem { Title = Strings.cmdOpen, Action = Open },
                                       new MenuItem { Title = Strings.cmdSave, Action = Save },
                                       new MenuItem { Title = "Save _As", Action = () => SaveAs () },
                                       new MenuItem { Title = Strings.cmdClose, Action = Close },
                                       new MenuItem { Title = Strings.cmdQuit, Action = Quit }
                                   ]));

        menu.Add (new MenuBarItem ("_About", [new MenuItem { Title = "_About", Action = () => MessageBox.Query (app, "Notepad", "About Notepad...", "Ok") }]));

        _tabs = new Tabs { X = 0, Y = Pos.Bottom (menu), Width = Dim.Fill (), Height = Dim.Fill (1) };

        LenShortcut = new Shortcut (Key.Empty, "Len: ", null);

        // StatusBar
        StatusBar statusBar =
            new ([
                     new Shortcut (Application.GetDefaultKey (Command.Quit), "Quit", Quit),
                     new Shortcut (Key.F2, "Open", Open),
                     new Shortcut (Key.F1, "New", New),
                     new Shortcut (Key.F3, "Save", Save),
                     new Shortcut (Key.F6, "Close", Close),
                     LenShortcut
                 ]) { AlignmentModes = AlignmentModes.IgnoreFirstOrLast };

        _topWindow.Add (menu, _tabs, statusBar);

        _focusedTabs = _tabs;
        _tabs.ValueChanged += Tabs_ValueChanged;
        _tabs.HasFocusChanging += (_, _) => _focusedTabs = _tabs;

        _topWindow.IsModalChanged += (_, e) =>
                                     {
                                         if (e.Value)
                                         {
                                             // Only create the initial tab the first time the window becomes modal.
                                             // IsModalChanged fires again after every nested modal dialog closes,
                                             // so we guard against creating duplicate tabs.
                                             if (!_tabs!.TabCollection.Any ())
                                             {
                                                 New ();
                                             }

                                             LenShortcut.Title = $"Len:{GetSelectedTextLength ()}";
                                         }
                                         else
                                         {
                                             _tabs.ValueChanged -= Tabs_ValueChanged;
                                         }
                                     };

        app.Run (_topWindow);
        _topWindow.Dispose ();
    }

    public void Save ()
    {
        if (_focusedTabs?.Value is OpenedFile tab)
        {
            Save (_focusedTabs, tab);
        }
    }

    private void Save (Tabs tabsToSave, OpenedFile tabToSave)
    {
        if (tabToSave.File is null)
        {
            SaveAs ();
        }
        else
        {
            tabToSave.Save ();
        }

        tabsToSave.SetNeedsDraw ();
    }

    public bool SaveAs ()
    {
        if (_focusedTabs?.Value is not OpenedFile tab)
        {
            return false;
        }

        SaveDialog fd = new ();

        if (_lastDirectory is { })
        {
            fd.Path = _lastDirectory;
        }

        _app?.Run (fd);

        if (string.IsNullOrWhiteSpace (fd.Path) || fd.Canceled)
        {
            fd.Dispose ();

            return false;
        }

        _lastDirectory = Path.GetDirectoryName (Path.GetFullPath (fd.Path));
        tab.File = new FileInfo (fd.Path);
        tab.Title = fd.FileName ?? throw new InvalidOperationException ();
        tab.Save ();

        fd.Dispose ();

        return true;
    }

    private void Close ()
    {
        if (_focusedTabs?.Value is OpenedFile tab)
        {
            Close (_focusedTabs, tab);
        }
    }

    private void Close (Tabs tabs, OpenedFile tabToClose)
    {
        _focusedTabs = tabs;

        if (tabToClose.UnsavedChanges)
        {
            int? result = MessageBox.Query (tabs.App!, "Save Changes", $"Save changes to {tabToClose.Title.TrimEnd ('*')}", "Yes", "No", "Cancel");

            if (result is null or 2)
            {
                // user cancelled
                return;
            }

            if (result == 0)
            {
                if (tabToClose.File is null)
                {
                    SaveAs ();
                }
                else
                {
                    tabToClose.Save ();
                }
            }
        }

        // close and dispose the tab
        tabs.Remove (tabToClose);
        tabToClose.Dispose ();
        _focusedTabs = tabs;

        // If last tab is closed, open a new one
        if (!tabs.TabCollection.Any ())
        {
            New ();
        }
    }

    private void New () => Open (null!, $"new {_numNewTabs++}");

    private void Open ()
    {
        OpenDialog open = new ()
        {
            Title = "Open",
            AllowsMultipleSelection = true,
            AllowedTypes =
            [
                new AllowedType ("Markdown", ".md", ".markdown"),
                new AllowedType ("Text", ".txt", ".csv", ".tsv"),
                new AllowedType ("Code", ".c", ".h", ".js", ".cs", ".json", ".yml"),
                new AllowedTypeAny ()
            ],
            MustExist = true,
            OpenMode = OpenMode.File
        };

        if (_lastDirectory is { })
        {
            open.Path = _lastDirectory;
        }

        _app?.Run (open);

        bool canceled = open.Canceled;

        if (!canceled)
        {
            foreach (string path in open.FilePaths)
            {
                if (string.IsNullOrEmpty (path) || !File.Exists (path))
                {
                    break;
                }

                _lastDirectory = Path.GetDirectoryName (Path.GetFullPath (path));
                Open (new FileInfo (path), Path.GetFileName (path));
            }
        }

        open.Dispose ();
    }

    /// <summary>Creates a new tab with initial text, or reuses the current tab if it is virgin.</summary>
    /// <param name="fileInfo">File that was read or null if a new blank document.</param>
    /// <param name="tabName">Display name for the tab.</param>
    private void Open (FileInfo? fileInfo, string tabName)
    {
        if (_focusedTabs is null)
        {
            return;
        }

        // If the current tab is virgin (no file, no content), reuse it instead of creating a new one
        if (fileInfo is { })
        {
            if (_focusedTabs.Value is OpenedFile { IsPristine: true } currentTab)
            {
                currentTab.File = fileInfo;
                currentTab.Title = tabName;
                currentTab.LoadFile (fileInfo);

                return;
            }
        }

        OpenedFile tab = new (this) { Title = tabName, File = fileInfo };
        tab.CreateAndAddEditor (fileInfo);
        tab.RegisterEditorEvents ();

        _focusedTabs.Add (tab);
        _focusedTabs.Value = tab;
    }

    private void Quit ()
    {
        if (_tabs is { })
        {
            foreach (OpenedFile tab in _tabs.TabCollection.OfType<OpenedFile> ())
            {
                if (!tab.UnsavedChanges)
                {
                    continue;
                }
                int? result = MessageBox.Query (_app!, "Unsaved Changes", $"Save changes to {tab.Title.TrimEnd ('*')}?", "Yes", "No", "Cancel");

                if (result is null or 2)
                {
                    return;
                }

                if (result != 0)
                {
                    continue;
                }
                _focusedTabs = _tabs;
                _tabs.Value = tab;

                if (tab.File is null)
                {
                    if (!SaveAs ())
                    {
                        return;
                    }
                }
                else
                {
                    tab.Save ();
                }
            }
        }

        _topWindow?.RequestStop ();
    }

    /// <summary>
    ///     Walks up the directory tree from the current directory looking for the repository root
    ///     (identified by Terminal.slnx).
    /// </summary>
    private static string? FindRepoRoot ()
    {
        DirectoryInfo? dir = new (Environment.CurrentDirectory);

        while (dir is { })
        {
            if (File.Exists (Path.Combine (dir.FullName, "Terminal.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }

    private int GetSelectedTextLength ()
    {
        if (_focusedTabs?.Value is OpenedFile tab)
        {
            return tab.EditorControl?.Text.Length ?? 0;
        }

        return 0;
    }

    private void Tabs_ValueChanged (object? sender, ValueChangedEventArgs<View?> e)
    {
        if (LenShortcut is null)
        {
            return;
        }
        var len = 0;

        if (e.NewValue is OpenedFile tab)
        {
            len = tab.EditorControl?.Text.Length ?? 0;
        }

        LenShortcut.Title = $"Len:{len}";

        //if (e.NewValue is OpenedFile openedTab)
        //{
        //    openedTab.EditorControl?.SetFocus ();
        //}
    }

    private class OpenedFile (Notepad notepad) : View
    {
        public FileInfo? File { get; set; }

        /// <summary>Gets whether this tab is a pristine new document &#8212; never opened to a file and has no content.</summary>
        public bool IsPristine => File is null && string.IsNullOrEmpty (EditorControl?.Text);

        public Editor? EditorControl { get; private set; }

        /// <summary>The text of the tab the last time it was saved.</summary>
        private string? _savedText;

        private EventHandler<DocumentChangeEventArgs>? _documentChangedHandler;

        public bool UnsavedChanges => EditorControl is { } editor && !string.Equals (_savedText, editor.Text);

        public void CreateAndAddEditor (FileInfo? file)
        {
            var initialText = string.Empty;

            if (file is { Exists: true })
            {
                initialText = System.IO.File.ReadAllText (file.FullName);
            }

            EditorControl = new ()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill (),
                Height = Dim.Fill ()
            };

            if (file is { Extension: { Length: > 0 } ext })
            {
                EditorControl.HighlightingDefinition = HighlightingManager.Instance.GetDefinitionByExtension (ext);
            }

            EditorControl.Text = initialText;

            _savedText = initialText;

            Add (EditorControl);
        }

        /// <summary>Loads a file into an existing tab, replacing its content.</summary>
        public void LoadFile (FileInfo file)
        {
            if (EditorControl is null)
            {
                return;
            }

            var text = string.Empty;

            if (file.Exists)
            {
                text = System.IO.File.ReadAllText (file.FullName);
            }

            // Set _savedText first so the DocumentChanged handler sees matching text (not dirty).
            _savedText = text;
            EditorControl.Text = text;
        }

        public void RegisterEditorEvents ()
        {
            if (EditorControl is null)
            {
                return;
            }

            // when user makes changes rename tab to indicate unsaved
            _documentChangedHandler = (_, _) =>
                                      {
                                          // if current text doesn't match saved text
                                          bool areDiff = UnsavedChanges;

                                          if (areDiff)
                                          {
                                              if (!Title.EndsWith ('*'))
                                              {
                                                  Title = Title + "*";
                                              }
                                          }
                                          else
                                          {
                                              if (Title.EndsWith ('*'))
                                              {
                                                  Title = Title.TrimEnd ('*');
                                              }
                                          }

                                          notepad.LenShortcut?.Title = $"Len:{EditorControl.Text.Length}";
                                      };

            EditorControl.Document!.Changed += _documentChangedHandler;
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing && _documentChangedHandler is { } && EditorControl?.Document is { } doc)
            {
                doc.Changed -= _documentChangedHandler;
                _documentChangedHandler = null;
            }

            base.Dispose (disposing);
        }

        internal void Save ()
        {
            if (EditorControl is null || File is null || string.IsNullOrWhiteSpace (File.FullName))
            {
                return;
            }

            string newText = EditorControl.Text;

            System.IO.File.WriteAllText (File.FullName, newText);
            _savedText = newText;

            Title = Title.TrimEnd ('*');
        }
    }
}
