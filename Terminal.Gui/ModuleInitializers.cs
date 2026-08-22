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
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void InitializeConfiguration ()
    {
        TuiConfigurationBuilder.Shared.ApplyToStaticFacades ();
    }
}
