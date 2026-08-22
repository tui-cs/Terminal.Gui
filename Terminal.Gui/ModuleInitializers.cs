using System.Runtime.CompilerServices;
using Terminal.Gui.Configuration;

namespace Terminal.Gui;

/// <summary>
///     Contains module initializers that run when the Terminal.Gui assembly is loaded.
/// </summary>
internal static class ModuleInitializers
{
    /// <summary>
    ///     Applies MEC configuration to static settings facades when the Terminal.Gui assembly is loaded.
    /// </summary>
    /// <remarks>
    ///     Must never throw: an exception here kills the app (and any test host) with an opaque
    ///     module-initializer error before <c>Main</c> runs. Bad configuration is collected in
    ///     <see cref="TuiJsonErrors"/> and printed at shutdown; the app runs on defaults.
    /// </remarks>
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void InitializeConfiguration ()
    {
        try
        {
            TuiConfigurationBuilder.Shared.ApplyToStaticFacades ();
        }
        catch (Exception ex)
        {
            TuiJsonErrors.Add ($"Applying configuration failed: {ex.Message}. Running with default settings.");
        }
    }
}
