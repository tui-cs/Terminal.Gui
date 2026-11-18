#nullable enable

using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using TextMateSharp.Grammars;

// ReSharper disable AccessToDisposedClosure

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Deepdives", "Use MarkDownView to provide a TG Deep Dive browser.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Text and Formatting")]
public class Deepdives : Scenario
{
    private static readonly HttpClient _httpClient = new ();

    private const string DOCS_API_URL = "https://api.github.com/repos/tui-cs/Terminal.Gui/contents/docfx/docs?ref=develop";
    private const string INCLUDES_API_URL = "https://api.github.com/repos/tui-cs/Terminal.Gui/contents/docfx/includes?ref=develop";

    private IApplication? _app;
    private ListView? _docList;
    private Markdown? _markdownView;
    private FrameView? _viewerFrame;
    private Shortcut? _statusShortcut;
    private SpinnerView? _spinner;
    private NumericUpDown? _contentWidthUpDown;
    private bool _updatingContentWidth;

    private List<DocEntry> _docs = [];
    private readonly Dictionary<string, string> _includes = new (StringComparer.OrdinalIgnoreCase);

    public override void Main ()
    {
        _app = Application.Create ();
        _app.Init ();

        Window window = new () { Title = GetName (), Width = Dim.Fill (), Height = Dim.Fill () };

        FrameView listFrame = new ()
        {
            Title = "_Docs",
            X = 0,
            Y = 0,
            Width = 30,
            Height = Dim.Fill (1),
            TabStop = TabBehavior.TabStop
        };

        _docList = new ListView { Width = Dim.Fill (), Height = Dim.Fill () };

        _docList.ValueChanged += OnDocListValueChanged;
        listFrame.Add (_docList);

        _viewerFrame = new FrameView
        {
            Title = "MarkdownView",
            X = Pos.Right (listFrame),
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (1),
            TabStop = TabBehavior.TabStop
        };

        _markdownView = new Markdown { Width = Dim.Fill (), Height = Dim.Fill (), SyntaxHighlighter = new TextMateSyntaxHighlighter () };

        _markdownView.ViewportSettings |= ViewportSettingsFlags.HasHorizontalScrollBar;

        _markdownView.LinkClicked += (_, e) =>
                                     {
                                         _statusShortcut?.Title = e.Url;
                                         SelectDocFromLink (e.Url);
                                         e.Handled = true;
                                     };

        // Reset the content width control only when viewport SIZE changes (not scroll position)
        _markdownView.ViewportChanged += (_, e) =>
                                         {
                                             if (e.NewViewport.Size == e.OldViewport.Size)
                                             {
                                                 return;
                                             }

                                             SyncContentWidthToViewport ();
                                         };

        _viewerFrame.Add (_markdownView);

        _spinner = new SpinnerView { Style = new SpinnerStyle.Aesthetic (), Width = 8, AutoSpin = false, Visible = false };

        _statusShortcut = new Shortcut (Key.Empty, "Ready", null);

        Shortcut spinnerShortcut = new () { CommandView = _spinner, Title = "" };

        _contentWidthUpDown = new NumericUpDown { Value = 80 };

        _contentWidthUpDown.ValueChanging += (_, args) =>
                                             {
                                                 if (_markdownView is null || _updatingContentWidth)
                                                 {
                                                     return;
                                                 }

                                                 int newWidth = args.NewValue;

                                                 if (newWidth < 1)
                                                 {
                                                     args.Handled = true;

                                                     return;
                                                 }

                                                 _markdownView.SetContentWidth (newWidth);
                                             };

        Shortcut contentWidthShortcut = new () { CommandView = _contentWidthUpDown, Text = "Content Width" };

        DropDownList<ThemeName> themeDropDown = new ()
        {
            ReadOnly = true,
            CanFocus = false,
            Value = !Enum.TryParse (_markdownView.SyntaxHighlighter.ThemeName, out ThemeName theme) ? ThemeName.DarkPlus : theme,
            Autocomplete = null
        };

        themeDropDown.ValueChanged += (_, e) =>
                                      {
                                          if (_markdownView is null || e.Value is not { } themeName)
                                          {
                                              return;
                                          }

                                          _markdownView.SyntaxHighlighter = new TextMateSyntaxHighlighter (themeName);
                                      };

        Shortcut themeShortcut = new () { Text = "_Theme:", CommandView = themeDropDown, MouseHighlightStates = MouseState.None };

        // Auto-select a light or dark syntax theme based on the terminal's actual background color.
        _app.Driver!.DefaultAttributeChanged += (_, e) =>
                                                {
                                                    if (_markdownView is null || e.NewValue is not { } attr)
                                                    {
                                                        return;
                                                    }

                                                    ThemeName autoTheme = TextMateSyntaxHighlighter.GetThemeForBackground (attr.Background);
                                                    _markdownView.SyntaxHighlighter = new TextMateSyntaxHighlighter (autoTheme);
                                                    themeDropDown.Value = autoTheme;
                                                };

        CheckBox themeBgCheckBox = new () { Text = "Theme _BG", Value = _markdownView.UseThemeBackground ? CheckState.Checked : CheckState.UnChecked };

        themeBgCheckBox.ValueChanged += (_, e) =>
                                        {
                                            if (_markdownView is null)
                                            {
                                                return;
                                            }

                                            _markdownView.UseThemeBackground = e.NewValue == CheckState.Checked;
                                        };

        Shortcut themeBgShortcut = new () { CommandView = themeBgCheckBox };

        StatusBar statusBar = new ([
                                       new Shortcut (Application.GetDefaultKey (Command.Quit), "Quit", window.RequestStop),
                                       contentWidthShortcut,
                                       themeShortcut,
                                       themeBgShortcut,
                                       _statusShortcut,
                                       spinnerShortcut
                                   ]) { AlignmentModes = AlignmentModes.IgnoreFirstOrLast };

        window.Add (listFrame, _viewerFrame, statusBar);

        // Set initial content width value after layout
        window.Initialized += (_, _) =>
                              {
                                  _ = LoadDocListAsync ();
                                  _ = LoadIncludesAsync ();
                                  SyncContentWidthToViewport ();
                              };

        _app.Run (window);

        window.Dispose ();
        _app.Dispose ();
    }

    private void SyncContentWidthToViewport ()
    {
        if (_markdownView is null || _contentWidthUpDown is null)
        {
            return;
        }

        _updatingContentWidth = true;
        _contentWidthUpDown.Value = _markdownView.Viewport.Width;
        _updatingContentWidth = false;
    }

    private async Task LoadDocListAsync ()
    {
        ShowSpinner ("Loading doc list...");

        try
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Clear ();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd ("Terminal.Gui-UICatalog/1.0");

            string json = await _httpClient.GetStringAsync (DOCS_API_URL).ConfigureAwait (false);

            using JsonDocument doc = JsonDocument.Parse (json);
            List<DocEntry> entries = [];

            foreach (JsonElement element in doc.RootElement.EnumerateArray ())
            {
                string? name = element.GetProperty ("name").GetString ();
                string? downloadUrl = element.GetProperty ("download_url").GetString ();

                if (name is { } && downloadUrl is { } && name.EndsWith (".md", StringComparison.OrdinalIgnoreCase))
                {
                    entries.Add (new DocEntry (name, downloadUrl));
                }
            }

            entries.Sort ((a, b) => string.Compare (a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            _app?.Invoke (() =>
                          {
                              _docs = entries;
                              ObservableCollection<string> names = new (_docs.Select (d => d.Name));
                              _docList?.SetSource (names);
                              _docList?.SelectedItem = 0;
                              HideSpinner ("Ready");
                          });
        }
        catch (Exception ex)
        {
            _app?.Invoke (() =>
                          {
                              HideSpinner ("Error loading doc list");

                              _markdownView?.Text = $"# Error\n\nFailed to load doc list:\n\n`{ex.Message}`";
                          });
        }
    }

    private void OnDocListValueChanged (object? sender, ValueChangedEventArgs<int?> e)
    {
        if (e.NewValue is null || e.NewValue < 0 || e.NewValue >= _docs.Count)
        {
            return;
        }

        DocEntry entry = _docs [e.NewValue.Value];
        _ = LoadDocContentAsync (entry);
    }

    private void SelectDocFromLink (string url)
    {
        if (_docList is null || _docs.Count == 0)
        {
            return;
        }

        string relativePath = url;

        if (relativePath.StartsWith ("~/docs/", StringComparison.Ordinal))
        {
            relativePath = relativePath ["~/docs/".Length..];
        }

        int queryIndex = relativePath.IndexOf ('?');

        if (queryIndex >= 0)
        {
            relativePath = relativePath [..queryIndex];
        }

        int anchorIndex = relativePath.IndexOf ('#');

        if (anchorIndex >= 0)
        {
            relativePath = relativePath [..anchorIndex];
        }

        if (string.IsNullOrWhiteSpace (relativePath))
        {
            return;
        }

        int slashIndex = relativePath.LastIndexOf ('/');
        string docName = slashIndex >= 0 ? relativePath [(slashIndex + 1)..] : relativePath;
        docName = Uri.UnescapeDataString (docName);

        int docIndex = _docs.FindIndex (d => string.Equals (d.Name, docName, StringComparison.OrdinalIgnoreCase));

        if (docIndex < 0 && !docName.EndsWith (".md", StringComparison.OrdinalIgnoreCase))
        {
            docIndex = _docs.FindIndex (d => string.Equals (d.Name, $"{docName}.md", StringComparison.OrdinalIgnoreCase));
        }

        if (docIndex < 0)
        {
            return;
        }
        _docList.SelectedItem = docIndex;
        _statusShortcut?.Title = _docs [docIndex].Name;
    }

    private async Task LoadDocContentAsync (DocEntry entry)
    {
        ShowSpinner ($"Loading {entry.Name}...");

        try
        {
            string content = await _httpClient.GetStringAsync (entry.DownloadUrl).ConfigureAwait (false);
            content = ExpandIncludes (content);

            _app?.Invoke (() =>
                          {
                              _markdownView?.Text = content;

                              _markdownView?.Viewport = _markdownView.Viewport with { X = 0, Y = 0 };

                              _viewerFrame?.Title = entry.Name;

                              HideSpinner (entry.Name);
                          });
        }
        catch (Exception ex)
        {
            _app?.Invoke (() =>
                          {
                              HideSpinner ("Error");

                              _markdownView?.Text = $"# Error\n\nFailed to load `{entry.Name}`:\n\n`{ex.Message}`";
                          });
        }
    }

    private void ShowSpinner (string message) =>
        _app?.Invoke (() =>
                      {
                          _spinner?.Visible = true;
                          _spinner?.AutoSpin = true;

                          _statusShortcut?.Title = message;
                      });

    private void HideSpinner (string message)
    {
        _spinner?.AutoSpin = false;
        _spinner?.Visible = false;

        _statusShortcut?.Title = message;
    }

    private static readonly Regex _includeRegex =
        new (@"\[!INCLUDE\s+\[.*?\]\((?:~/|\.\./)includes/(.+?)\)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private string ExpandIncludes (string content) =>
        _includeRegex.Replace (content,
                               match =>
                               {
                                   string fileName = match.Groups [1].Value;

                                   return _includes.TryGetValue (fileName, out string? includeContent) ? includeContent : match.Value;
                               });

    private async Task LoadIncludesAsync ()
    {
        try
        {
            string json = await _httpClient.GetStringAsync (INCLUDES_API_URL).ConfigureAwait (false);

            using JsonDocument doc = JsonDocument.Parse (json);

            List<Task> tasks = [];

            foreach (JsonElement element in doc.RootElement.EnumerateArray ())
            {
                string? name = element.GetProperty ("name").GetString ();
                string? downloadUrl = element.GetProperty ("download_url").GetString ();

                if (name is { } && downloadUrl is { } && name.EndsWith (".md", StringComparison.OrdinalIgnoreCase))
                {
                    tasks.Add (FetchInclude (name, downloadUrl));
                }
            }

            await Task.WhenAll (tasks).ConfigureAwait (false);
        }
        catch
        {
            // Non-critical — includes just won't expand
        }
    }

    private async Task FetchInclude (string name, string url)
    {
        try
        {
            string content = await _httpClient.GetStringAsync (url).ConfigureAwait (false);
            _includes [name] = content;
        }
        catch
        {
            // Skip failed includes
        }
    }

    private sealed record DocEntry (string Name, string DownloadUrl);
}
