#nullable enable
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;

// Disable all shadows and highlights
TuiConfigurationBuilder.Shared.RuntimeConfig = """
{
  "Themes": {
    "Default": {
      "Window": { "DefaultShadow": "None" },
      "Dialog": { "DefaultShadow": "None" },
      "Button": { "DefaultShadow": "None" },
      "Menu": { "DefaultBorderStyle": "Single" }
    }
  }
}
""";
TuiConfigurationBuilder.Shared.ApplyToStaticFacades ();

// Get the view name and output file from commandline
string? viewName = null;
string? outputFile = null;

string [] commandArgs = Environment.GetCommandLineArgs ();
var ansi = false;
var addBorderFrame = false;
var live = false;
var queryKeyStrokes = false;
var cols = 80;
var rows = 20;

for (var i = 0; i < commandArgs.Length; i++)
{
    if (commandArgs [i].StartsWith ("--view=", StringComparison.OrdinalIgnoreCase))
    {
        viewName = commandArgs [i] ["--view=".Length..];
    }
    else if (commandArgs [i] == "--view" && i + 1 < commandArgs.Length)
    {
        viewName = commandArgs [i + 1];
    }
    else if (commandArgs [i].StartsWith ("--output=", StringComparison.OrdinalIgnoreCase))
    {
        outputFile = commandArgs [i] ["--output=".Length..];
    }
    else if (commandArgs [i] == "--output" && i + 1 < commandArgs.Length)
    {
        outputFile = commandArgs [i + 1];
    }
    else if (commandArgs [i] == "--frame" || commandArgs [i] == "-f")
    {
        addBorderFrame = true;
    }
    else if (commandArgs [i] == "--ansi" || commandArgs [i] == "-a")
    {
        ansi = true;
    }
    else if (commandArgs [i] == "--live" || commandArgs [i] == "-l")
    {
        live = true;
    }
    else if (commandArgs [i] == "--keystrokes" || commandArgs [i] == "-k")
    {
        queryKeyStrokes = true;
    }
    else if (commandArgs [i].StartsWith ("--cols=", StringComparison.OrdinalIgnoreCase))
    {
        cols = int.Parse (commandArgs [i] ["--cols=".Length..]);
    }
    else if (commandArgs [i] == "--cols" && i + 1 < commandArgs.Length)
    {
        cols = int.Parse (commandArgs [i + 1]);
    }
    else if (commandArgs [i].StartsWith ("--rows=", StringComparison.OrdinalIgnoreCase))
    {
        rows = int.Parse (commandArgs [i] ["--rows=".Length..]);
    }
    else if (commandArgs [i] == "--rows" && i + 1 < commandArgs.Length)
    {
        rows = int.Parse (commandArgs [i + 1]);
    }
}

if (string.IsNullOrEmpty (viewName))
{
    Console.WriteLine (@"No view name specified. Use --view=ViewName to specify a view.");

    return;
}

// If --keystrokes, just query the view's demo keystrokes and exit
if (queryKeyStrokes)
{
    Type? type = ViewDemoWindow.ResolveViewType (viewName);

    if (type is null)
    {
        Console.Error.WriteLine ($"`{viewName}` type is not a valid Terminal.Gui View type.");
        Environment.Exit (1);

        return;
    }

    View? view = ViewDemoWindow.CreateView (type);

    if (view is null)
    {
        Console.WriteLine ("");

        return;
    }

    if (view is IDesignable designable)
    {
        string? keystrokes = designable.GetDemoKeyStrokes ();
        Console.WriteLine (keystrokes ?? "");
    }
    else
    {
        Console.WriteLine ("");
    }

    return;
}

ViewDemoWindow.ViewName = viewName;
ViewDemoWindow.AddBorderFrame = addBorderFrame;
ViewDemoWindow.IsLiveMode = live;
ViewDemoWindow.Cols = cols;
ViewDemoWindow.Rows = rows;

IApplication app = Application.Create ();
app.Init (DriverRegistry.Names.ANSI);

if (live)
{
    app.Driver!.SetScreenSize (cols, rows);
    app.Run<ViewDemoWindow> ();
    app.Dispose ();
}
else
{
    // Original mode: stop after first iteration and capture output
    app.StopAfterFirstIteration = true;
    app.Driver!.Force16Colors = !ansi;
    app.Driver!.SetScreenSize (cols, rows);

    var result = app.Run<ViewDemoWindow> ().GetResult<string> ();

    if (result is { })
    {
        Console.WriteLine (result);
        app.Dispose ();

        return;
    }

    // Run it again, since it set the Screen size to just fit
    app.Run<ViewDemoWindow> ().GetResult<string> ();

    string output = ansi ? app.Driver.ToAnsi () : app.ToString ().Trim ();
    app.Dispose ();

    if (string.IsNullOrEmpty (output))
    {
        Console.WriteLine (@"No output was generated.");

        return;
    }

    if (ansi)
    {
        output = AnsiConsoleToHtml.AnsiConsole.ToHtml (output);
    }

    // Write to file or console
    if (!string.IsNullOrEmpty (outputFile))
    {
        File.WriteAllText (outputFile, output);
    }
    else
    {
        Console.WriteLine (output);
    }
}

// Defines a top-level window with border and title
internal class ViewDemoWindow : Runnable<string>
{
    public static string? ViewName { get; set; }
    public static bool IsLiveMode { get; set; }
    public static int Cols { get; set; } = 80;
    public static int Rows { get; set; } = 20;

    public ViewDemoWindow ()
    {
        Width = Cols;
        Height = Rows;

        if (!IsLiveMode)
        {
            // Use only white on black for static HTML/ANSI capture
            SetScheme (new Scheme (new Attribute (ColorName16.White, ColorName16.Black)));
        }
        else
        {
            // In live mode, don't let child Accept bubble up and stop the app
            CommandsToBubbleUp = [];
        }

        BorderStyle = LineStyle.None;
    }

    public static bool AddBorderFrame { get; set; }

    /// <summary>
    ///     Resolves a view type name to its <see cref="Type"/>, handling generic types like "ListView`1".
    /// </summary>
    public static Type? ResolveViewType (string viewName)
    {
        string normalizedViewName = NormalizeDocfxGenericName (viewName);

        // Try direct resolution first
        Type? type = Type.GetType ($"Terminal.Gui.Views.{normalizedViewName}, Terminal.Gui", false, true);

        if (type is not null)
        {
            return type;
        }

        // Search the assembly for types matching by name (handles generics)
        System.Reflection.Assembly asm = typeof (View).Assembly;

        return asm.GetTypes ()
                  .FirstOrDefault (t => t.IsClass
                                        && !t.IsAbstract
                                        && t.IsSubclassOf (typeof (View))
                                        && string.Equals (t.Name, normalizedViewName, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeDocfxGenericName (string viewName)
    {
        int suffixSeparator = viewName.LastIndexOf ('-');

        if (suffixSeparator < 0)
        {
            return viewName;
        }

        string suffix = viewName [(suffixSeparator + 1)..];

        if (!int.TryParse (suffix, out _))
        {
            return viewName;
        }

        return $"{viewName [..suffixSeparator]}`{suffix}";
    }

    /// <inheritdoc/>
    protected override void OnIsRunningChanged (bool newIsRunning)
    {
        base.OnIsRunningChanged (newIsRunning);

        if (!newIsRunning)
        {
            return;
        }

        // Convert ViewName to type that's in the Terminal.Gui assembly:
        Type? type = ResolveViewType (ViewName!);

        if (type is null)
        {
            Result = @$"`{ViewName}` type is not a valid Terminal.Gui View type.";

            return;
        }

        // Create the view
        View? view = CreateView (type);

        if (view is null)
        {
            Result = @$"`{ViewName}` could not be created.";

            return;
        }

        // Initialize the view
        view.Initialized += ViewInitialized;

        Add (view);

        Layout ();

        if (!IsLiveMode)
        {
            App?.Driver?.SetScreenSize (view.Frame.Width, view.Frame.Height);
        }
    }

    public static View? CreateView (Type type)
    {
        if (type.IsGenericTypeDefinition && type == typeof (Prompt<,>))
        {
            type = type.MakeGenericType (typeof (TextField), typeof (string));
        }
        // If we are to create a generic Type
        else if (type.IsGenericTypeDefinition)
        {
            // For each of the <T> arguments
            List<Type> typeArguments = new ();

            // use <object> or the original type if applicable
            foreach (Type arg in type.GetGenericArguments ())
            {
                if (arg.IsValueType && Nullable.GetUnderlyingType (arg) == null)
                {
                    typeArguments.Add (arg);
                }
                else
                {
                    typeArguments.Add (typeof (object));
                }
            }

            // And change what type we are instantiating from MyClass<T> to MyClass<object> or MyClass<T>
            type = type.MakeGenericType (typeArguments.ToArray ());
        }

        // Ensure the type does not contain any generic parameters
        if (type.ContainsGenericParameters)
        {
            Console.WriteLine (@$"Cannot create an instance of {type} because it contains generic parameters.");

            return null;
        }

        // Instantiate view
        View view = (View)Activator.CreateInstance (type)!;

        if (view.GetType ().IsGenericType && view.GetType ().GetGenericTypeDefinition () == typeof (Prompt<,>))
        {
            ConfigurePrompt (view);
        }
        else if (view is IDesignable designable)
        {
            string demoText = "This is some demo text.";
            designable.EnableForDesign (ref demoText);
        }
        else
        {
            view.Text = "This is some demo text.";
        }

        if (view is FileDialog)
        {
            view.Width = Dim.Fill ();
            view.Height = Dim.Fill ();
        }

        //view.Title = $"View: {type.Name}";

        return view;
    }

    private static void ConfigurePrompt (View view)
    {
        view.Title = "Prompt";

        System.Reflection.MethodInfo? getWrappedView = view.GetType ().GetMethod ("GetWrappedView");
        View? wrappedView = getWrappedView?.Invoke (view, null) as View;

        if (wrappedView is null)
        {
            return;
        }

        wrappedView.Text = "What is your name?";
        wrappedView.Width = 32;
    }

    private static void ViewInitialized (object? sender, EventArgs e)
    {
        if (sender is not View view)
        {
            return;
        }

        if (view.Width == Dim.Absolute (0))
        {
            view.Width = Dim.Fill ();
        }

        if (view.Height == Dim.Absolute (0))
        {
            view.Height = Dim.Fill ();
        }

        view.X = 0;
        view.Y = 0;

        if (AddBorderFrame && view.BorderStyle == LineStyle.None)
        {
            view.BorderStyle = LineStyle.Dotted;
        }
    }
}
