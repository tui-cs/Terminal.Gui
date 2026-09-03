namespace UICatalog.Scenarios;

[ScenarioMetadata ("Code View Demo", "Demonstrates the Code view with theme-aware syntax roles.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Colors")]
public sealed class CodeViewDemo : Scenario
{
    private readonly Dictionary<string, string> _snippets = new (StringComparer.OrdinalIgnoreCase)
    {
        ["cs"] = """
                 #nullable enable
                 using System;

                 // Code CodeComment CodeKeyword CodeString
                 // CodeNumber CodeOperator CodeType CodePreprocessor
                 // CodeIdentifier CodeConstant CodePunctuation
                 // CodeFunctionName CodeAttribute
                 [Obsolete ("CodeAttribute")]
                 public sealed class Person
                 {
                     // CodeComment
                     private const int CodeNumber = 37;
                     public string CodeString { get; init; } = "Ada";
                     public bool CodeConstant => true;
                     public int CodeFunctionName => Math.Max (CodeNumber, 21 + 16);
                 }

                 """,
        ["json"] = """
                   {
                     "roles1": "Code CodeComment CodeKeyword CodeString",
                     "roles2": "CodeNumber CodeOperator CodeType CodePreprocessor",
                     "roles3": "CodeIdentifier CodeConstant CodePunctuation",
                     "roles4": "CodeFunctionName CodeAttribute",
                     "code": "Code",
                     "comment": "CodeComment",
                     "keyword": "CodeKeyword",
                     "string": "CodeString",
                     "number": 37,
                     "operator": "CodeOperator",
                     "type": "CodeType",
                     "preprocessor": "CodePreprocessor",
                     "identifier": "CodeIdentifier",
                     "constant": true,
                     "punctuation": "CodePunctuation",
                     "function": "CodeFunctionName",
                     "attribute": "CodeAttribute"
                   }
                   """,
        ["xml"] = """
                  <?xml version="1.0"?>
                  <!-- Code CodeComment CodeKeyword CodeString -->
                  <!-- CodeNumber CodeOperator CodeType CodePreprocessor -->
                  <!-- CodeIdentifier CodeConstant CodePunctuation -->
                  <!-- CodeFunctionName CodeAttribute -->
                  <person name="Ada" active="true" CodeAttribute="CodeAttribute">
                    <age unit="years">37</age>
                    <roles keyword="CodeKeyword" type="CodeType" function="CodeFunctionName" />
                  </person>
                  """,
        ["md"] = """
                  # Code view

                  `VisualRole.CodeKeyword` follows the active theme.

                  Code CodeComment CodeKeyword CodeString
                  CodeNumber CodeOperator CodeType CodePreprocessor
                  CodeIdentifier CodeConstant CodePunctuation
                  CodeFunctionName CodeAttribute

                  ```cs
                  #nullable enable
                  [Obsolete ("CodeAttribute")]
                  public string CodeString => "Ada"; // CodeComment
                  public int CodeFunctionName => Math.Max (37, 21 + 16);
                  ```

                  """
    };

    /// <inheritdoc/>
    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();

        using Runnable window = new ();
        window.Title = GetQuitKeyAndName ();
        window.BorderStyle = LineStyle.None;

        string [] languages = ["cs", "json", "xml", "md"];

        OptionSelector languageSelector = new ()
        {
            Title = "_Language",
            Labels = languages,
            BorderStyle = LineStyle.Rounded,
            Width = 16,
            Height = Dim.Auto (),
            Value = 0
        };

        string [] themes = ThemeManager.GetThemeNames ().Select (theme => "_" + theme).ToArray ();

        OptionSelector themeSelector = new ()
        {
            Title = "_Theme",
            Labels = themes,
            BorderStyle = LineStyle.Rounded,
            X = Pos.Right (languageSelector) + 1,
            Width = 24,
            Height = Dim.Auto (),
            Value = ThemeManager.GetThemeNames ().IndexOf (ThemeManager.Theme)
        };

        Code code = new ()
        {
            X = 0,
            Y = Pos.Bottom (themeSelector) + 1,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Rounded,
            Title = "Code",
            Language = languages [0],
            SyntaxHighlighter = new CodeRoleLegendHighlighter (),
            Text = _snippets [languages [0]]
        };

        languageSelector.ValueChanged += (_, args) =>
                                         {
                                             if (args.NewValue is null)
                                             {
                                                 return;
                                             }

                                             string language = languages [(int)args.NewValue];
                                             code.Language = language;
                                             code.Text = _snippets [language];
                                         };

        themeSelector.ValueChanged += (_, args) =>
                                      {
                                          if (args.NewValue is null)
                                          {
                                              return;
                                          }

                                          string theme = themes [(int)args.NewValue] [1..];
                                          ThemeManager.Theme = theme;
                                      };

        window.Add (languageSelector, themeSelector, code);
        app.Run (window);
    }
}
