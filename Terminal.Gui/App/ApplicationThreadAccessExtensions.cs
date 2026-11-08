using System.Runtime.CompilerServices;

namespace Terminal.Gui.App;

/// <summary>Provides UI-thread access checks and enables automatic view diagnostics.</summary>
public static class ApplicationThreadAccessExtensions
{
    /// <summary>Reports whether the calling thread is the initialized application's UI thread.</summary>
    /// <param name="app">The application to check.</param>
    /// <returns>False before initialization or after reset; otherwise, whether the current thread matches the UI thread.</returns>
    public static bool CheckAccess (this IApplication app)
    {
        ArgumentNullException.ThrowIfNull (app);

        return app.Initialized && app.MainThreadId == Environment.CurrentManagedThreadId;
    }

    /// <summary>Verifies UI-thread access, independently of the automatic diagnostics setting.</summary>
    /// <param name="app">The application to check.</param>
    /// <param name="operation">The operation being verified. Defaults to the calling member's name.</param>
    /// <exception cref="NotInitializedException">The application has no initialized UI thread.</exception>
    /// <exception cref="InvalidOperationException">The calling thread is not the application's UI thread.</exception>
    public static void VerifyAccess (this IApplication app, [CallerMemberName] string operation = "")
    {
        ArgumentNullException.ThrowIfNull (app);

        if (!app.Initialized || app.MainThreadId is not { } expectedThreadId)
        {
            throw new NotInitializedException (operation);
        }

        if (expectedThreadId == Environment.CurrentManagedThreadId)
        {
            return;
        }

        throw new InvalidOperationException ($"UI-thread access violation in {operation}: expected thread {expectedThreadId}, current thread {Environment.CurrentManagedThreadId}. Use app.InvokeAsync to update the UI.");
    }

    /// <summary>Enables or disables automatic view access checks for this application.</summary>
    /// <param name="app">The application to configure.</param>
    /// <param name="enabled">True to enable checks in both Debug and Release builds; false to disable them.</param>
    /// <returns>The application, for fluent initialization.</returns>
    /// <exception cref="NotSupportedException">The application does not implement <see cref="IApplicationThreadAccess"/>.</exception>
    /// <example><code>using IApplication app = Application.Create ().EnableThreadAccessChecks ().Init ();</code></example>
    public static IApplication EnableThreadAccessChecks (this IApplication app, bool enabled = true)
    {
        ArgumentNullException.ThrowIfNull (app);

        if (app is not IApplicationThreadAccess diagnostics)
        {
            throw new NotSupportedException ("This application does not support automatic UI-thread access checks.");
        }

        diagnostics.ThreadAccessChecksEnabled = enabled;

        return app;
    }

    internal static void VerifyAccessIfEnabled (this IApplication? app, [CallerMemberName] string operation = "")
    {
        if (app is not IApplicationThreadAccess { ThreadAccessChecksEnabled: true } || !app.Initialized)
        {
            return;
        }

        app.VerifyAccess (operation);
    }
}
