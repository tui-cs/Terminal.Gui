using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Tracing;

/// <summary>
///     Provides unified tracing for debugging and testing. Supports independent enable/disable
///     for different trace categories. Traces are only captured in DEBUG builds.
/// </summary>
/// <remarks>
///     <para>
///         By default, all tracing is disabled. Enable categories via <see cref="EnabledCategories"/>:
///     </para>
///     <code>
///         Trace.EnabledCategories = TraceCategory.Command | TraceCategory.Mouse;
///     </code>
///     <para>
///         All trace methods are marked with <c>[Conditional("DEBUG")]</c>,
///         so they have zero overhead in Release builds.
///     </para>
///     <para>
///         When a category is enabled, if no backend has been set, <see cref="LoggingBackend"/>
///         is automatically used so traces appear in the log output.
///     </para>
/// </remarks>
public static class Trace
{
    private static readonly AsyncLocal<ITraceBackend?> _asyncLocalBackend = new ();
    private static readonly AsyncLocal<TraceCategory> _asyncLocalEnabledCategories = new ();
    private static readonly NullBackend _nullBackend = new ();
    private static readonly LoggingBackend _loggingBackend = new ();

    /// <summary>
    ///     Gets or sets the enabled trace categories for the current async context.
    ///     This property is thread-safe and isolated per async flow.
    /// </summary>
    [JsonConverter (typeof (TraceCategoryJsonConverter))]
    public static TraceCategory EnabledCategories
    {
        get => TraceSettings.Defaults.EnabledCategories;
        set
        {
            TraceSettings.Defaults.EnabledCategories = value;
            _asyncLocalEnabledCategories.Value = value;
            EnsureBackendIfEnabled ();
        }
    }

    /// <summary>
    ///     Gets or sets the trace backend for the current async context. Default is <see cref="NullBackend"/>.
    ///     Each async flow (test, thread) has its own backend instance.
    /// </summary>
    /// <remarks>
    ///     When any trace category is enabled and no backend has been explicitly set,
    ///     <see cref="LoggingBackend"/> is automatically used.
    /// </remarks>
    public static ITraceBackend Backend { get => _asyncLocalBackend.Value ?? _nullBackend; set => _asyncLocalBackend.Value = value; }

    /// <summary>
    ///     Gets the async-local backend value (for TraceScope internal use).
    /// </summary>
    internal static ITraceBackend? GetAsyncLocalBackend () => _asyncLocalBackend.Value;

    /// <summary>
    ///     Sets the async-local backend value (for TraceScope internal use).
    /// </summary>
    internal static void SetAsyncLocalBackend (ITraceBackend? backend) => _asyncLocalBackend.Value = backend;

    private static void EnsureBackendIfEnabled ()
    {
        // Auto-switch to LoggingBackend if:
        // 1. Any category is enabled, AND
        // 2. No backend has been set (null) OR the backend is NullBackend
        if (EnabledCategories != TraceCategory.None && (_asyncLocalBackend.Value is null || _asyncLocalBackend.Value is NullBackend))
        {
            _asyncLocalBackend.Value = _loggingBackend;
        }
    }

    /// <summary>
    ///     Pushes a trace scope that enables the specified categories and optionally sets a backend.
    ///     Returns an <see cref="IDisposable"/> that restores the previous state when disposed.
    ///     This is thread-safe and works correctly with parallel tests.
    /// </summary>
    /// <param name="categories">The trace categories to enable.</param>
    /// <param name="backend">Optional backend to use. If null, uses default backend selection.</param>
    /// <returns>An <see cref="IDisposable"/> scope that restores previous tracing state.</returns>
    /// <example>
    ///     <code>
    ///     using (Trace.PushScope (TraceCategory.Command | TraceCategory.Mouse))
    ///     {
    ///         // Command and Mouse tracing enabled in this scope
    ///         CheckBox checkbox = new () { Id = "test" };
    ///         checkbox.InvokeCommand (Command.Activate);
    ///     }
    ///     // Previous tracing state restored
    ///     </code>
    /// </example>
    public static IDisposable PushScope (TraceCategory categories, ITraceBackend? backend = null) => new TraceScope (categories, backend);

    #region Lifecycle Tracing

    /// <summary>
    ///     Traces a command routing step.
    /// </summary>
    /// <param name="id">An identifying string for the trace. E.g. <c></c></param>
    /// <param name="phase">The phase of routing (e.g., "Init", "Run", "Iteration").</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Lifecycle (string? id, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!EnabledCategories.HasFlag (TraceCategory.Lifecycle))
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Lifecycle, id ?? string.Empty, phase, method, message, DateTime.UtcNow, null));
    }

    #endregion

    #region Command Tracing

    /// <summary>
    ///     Traces a command routing step.
    /// </summary>
    /// <param name="view">The view where routing is occurring.</param>
    /// <param name="command">The command being routed.</param>
    /// <param name="routing">The current routing mode.</param>
    /// <param name="phase">The phase of routing (e.g., "Entry", "Exit", "Handler", "Event", "Routing").</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Command (View view, Command command, CommandRouting routing, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!EnabledCategories.HasFlag (TraceCategory.Command))
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Command, view.ToIdentifyingString (), phase, method, message, DateTime.UtcNow, (command, routing)));
    }

    /// <summary>
    ///     Traces a command routing step from a context.
    /// </summary>
    /// <param name="view">The view where routing is occurring.</param>
    /// <param name="ctx">The command context (provides command and routing info).</param>
    /// <param name="phase">The phase of routing (e.g., "Entry", "Exit", "Handler", "Event", "Routing").</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Command (View view, ICommandContext? ctx, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!EnabledCategories.HasFlag (TraceCategory.Command))
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Command, view.ToIdentifyingString (), phase, method, message, DateTime.UtcNow, ctx)); //(ctx?.Command ?? Input.Command.NotBound, ctx?.Routing ?? CommandRouting.Direct)));
    }

    /// <summary>
    ///     Traces a command routing step from a context.
    /// </summary>
    /// <param name="view">The view where routing is occurring.</param>
    /// <param name="phase">The phase of routing (e.g., "Entry", "Exit", "Handler", "Event", "Routing").</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Command (View view, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!EnabledCategories.HasFlag (TraceCategory.Command))
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Command, view.ToIdentifyingString (), phase, method, message, DateTime.UtcNow, null));
    }

    #endregion

    #region Mouse Tracing

    /// <summary>
    ///     Traces a mouse event at the Driver or Application level (no view context).
    /// </summary>
    /// <param name="source">Identifying string for the source (e.g., "Driver", "Application").</param>
    /// <param name="flags">The mouse flags.</param>
    /// <param name="position">The mouse position.</param>
    /// <param name="phase">The phase of processing.</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Mouse (string source, MouseFlags flags, Point position, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!EnabledCategories.HasFlag (TraceCategory.Mouse))
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Mouse, source, phase, method, message, DateTime.UtcNow, (flags, position)));
    }

    /// <summary>
    ///     Traces a mouse event at the View level.
    /// </summary>
    /// <param name="view">The view processing the mouse event.</param>
    /// <param name="flags">The mouse flags.</param>
    /// <param name="position">The mouse position.</param>
    /// <param name="phase">The phase of processing.</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Mouse (View view, MouseFlags flags, Point position, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!EnabledCategories.HasFlag (TraceCategory.Mouse))
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Mouse, view.ToIdentifyingString (), phase, method, message, DateTime.UtcNow, (flags, position)));
    }

    /// <summary>
    ///     Traces a mouse event with a Mouse object at the View level.
    /// </summary>
    /// <param name="view">The view processing the mouse event.</param>
    /// <param name="mouse">The mouse event data.</param>
    /// <param name="phase">The phase of processing.</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Mouse (View view, Mouse mouse, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!EnabledCategories.HasFlag (TraceCategory.Mouse))
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Mouse, view.ToIdentifyingString (), phase, method, message, DateTime.UtcNow, mouse));
    }

    /// <summary>
    ///     Traces a mouse event with a Mouse object at the Driver or Application level.
    /// </summary>
    /// <param name="source">Identifying string for the source (e.g., "Driver", "Application").</param>
    /// <param name="mouse">The mouse event data.</param>
    /// <param name="phase">The phase of processing.</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Mouse (string source, Mouse mouse, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!EnabledCategories.HasFlag (TraceCategory.Mouse))
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Mouse, source, phase, method, message, DateTime.UtcNow, mouse));
    }

    #endregion

    #region Configuration Tracing

    /// <summary>
    ///     Traces a configuration management operation.
    /// </summary>
    /// <param name="id">An identifying string for the trace (e.g., property name, source path).</param>
    /// <param name="phase">The phase of the operation (e.g., "Load", "Apply", "Discover").</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Configuration (string? id, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!EnabledCategories.HasFlag (TraceCategory.Configuration))
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Configuration, id ?? string.Empty, phase, method, message, DateTime.UtcNow, null));
    }

    #endregion

    #region Draw Tracing

    /// <summary>
    ///     Traces a draw operation.
    /// </summary>
    /// <param name="id">An identifying string for the trace.</param>
    /// <param name="phase">The phase of the draw operation (e.g., "Start", "End", "Adornments", "SubViews", "Text", "Content").</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Draw (string? id, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!EnabledCategories.HasFlag (TraceCategory.Draw))
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Draw, id ?? string.Empty, phase, method, message, DateTime.UtcNow, null));
    }

    #endregion

    #region Keyboard Tracing

    /// <summary>
    ///     Traces a keyboard event at the Driver or Application level (no view context).
    /// </summary>
    /// <param name="source">Identifying string for the source (e.g., "Driver", "Application").</param>
    /// <param name="key">The key being processed.</param>
    /// <param name="phase">The phase of processing.</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Keyboard (string source, Key key, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!EnabledCategories.HasFlag (TraceCategory.Keyboard))
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Keyboard, source, phase, method, message, DateTime.UtcNow, key));
    }

    /// <summary>
    ///     Traces a keyboard event at the View level.
    /// </summary>
    /// <param name="view">The view processing the keyboard event.</param>
    /// <param name="key">The key being processed.</param>
    /// <param name="phase">The phase of processing.</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Keyboard (View view, Key key, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!EnabledCategories.HasFlag (TraceCategory.Keyboard))
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Keyboard, view.ToIdentifyingString (), phase, method, message, DateTime.UtcNow, key));
    }

    /// <summary>
    ///     Traces a keyboard binding event.
    /// </summary>
    /// <param name="binding">The keyboard binding being processed.</param>
    /// <param name="phase">The phase of processing.</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Keyboard (PlatformKeyBinding binding, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!EnabledCategories.HasFlag (TraceCategory.Keyboard))
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Keyboard, string.Join (", ", binding.GetCurrentPlatformKeys ()), phase, method, message, DateTime.UtcNow, binding));
    }

    #endregion
}
