using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

/// <summary>
///     Sets up the test environment for all test projects via Directory.Build.props.
///     This module initializer runs before any test code executes, ensuring the
///     <c>DisableRealDriverIO</c> environment variable is always set to <c>"1"</c>
///     regardless of test runner (CLI, IDE, CI).
/// </summary>
internal static class TestEnvironmentSetup
{
    [ModuleInitializer]
    [SuppressMessage ("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries")]
    internal static void Initialize ()
    {
        // Disable real driver I/O in all test processes.
        // This forces Driver.IsAttachedToTerminal() to return false,
        // preventing tests from accessing actual console handles.
        Environment.SetEnvironmentVariable ("DisableRealDriverIO", "1");
    }
}
