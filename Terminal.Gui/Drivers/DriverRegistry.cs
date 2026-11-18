namespace Terminal.Gui.Drivers;

/// <summary>
///     Central registry of available Terminal.Gui drivers.
///     Provides type-safe driver identification and discovery without reflection.
/// </summary>
public static class DriverRegistry
{
    /// <summary>
    ///     Well-known driver names as constants for type safety and avoiding magic strings.
    /// </summary>
    public static class Names
    {
        /// <summary>Windows Console API driver name.</summary>
        public const string WINDOWS = "windows";

        /// <summary>.NET System.Console cross-platform driver name.</summary>
        public const string DOTNET = "dotnet";

        /// <summary>Pure ANSI escape sequence cross-platform driver name.</summary>
        public const string ANSI = "ansi";
    }

    /// <summary>
    ///     Descriptor for a registered driver containing metadata and factory.
    /// </summary>
    /// <param name="Name">The driver name (lowercase, e.g., "windows").</param>
    /// <param name="DisplayName">Human-readable display name (e.g., "Windows Console Driver").</param>
    /// <param name="Description">Brief description of the driver's purpose and features.</param>
    /// <param name="SupportedPlatforms">Array of platforms this driver supports.</param>
    /// <param name="CreateFactory">Factory function to create an IComponentFactory instance.</param>
    public sealed record DriverDescriptor (string Name,
                                           string DisplayName,
                                           string Description,
                                           PlatformID [] SupportedPlatforms,
                                           Func<IComponentFactory> CreateFactory);

    private static readonly Dictionary<string, DriverDescriptor> _registry = new (StringComparer.OrdinalIgnoreCase);

    static DriverRegistry ()
    {
        // Register all built-in drivers
        Register (new DriverDescriptor (Names.WINDOWS,
                                        "Windows Console Driver",
                                        "Optimized Windows Console API driver with native input handling",
                                        [PlatformID.Win32NT, PlatformID.Win32S, PlatformID.Win32Windows],
                                        () => new WindowsComponentFactory ()));

        Register (new DriverDescriptor (Names.DOTNET,
                                        ".NET Cross-Platform Driver",
                                        "Cross-platform driver using System.Console API",
                                        [PlatformID.Win32NT, PlatformID.Unix, PlatformID.MacOSX],
                                        () => new NetComponentFactory ()));

        Register (new DriverDescriptor (Names.ANSI,
                                        "Pure ANSI Driver",
                                        "Cross-platform driver that uses ANSI escape sequences for keyboard/mouse input and output",
                                        [PlatformID.Win32NT, PlatformID.Unix, PlatformID.MacOSX],
                                        () => new AnsiComponentFactory ()));
    }

    /// <summary>
    ///     Registers a driver descriptor. Can be used to add custom drivers.
    /// </summary>
    /// <param name="descriptor">The driver descriptor to register.</param>
    public static void Register (DriverDescriptor descriptor) => _registry [descriptor.Name] = descriptor;

    //Logging.Trace ($"Registered driver: {descriptor.Name} ({descriptor.DisplayName})");
    /// <summary>
    ///     Gets all registered driver names.
    /// </summary>
    /// <returns>Enumerable of driver names (lowercase).</returns>
    public static IEnumerable<string> GetDriverNames () => _registry.Keys;

    /// <summary>
    ///     Gets all registered driver descriptors.
    /// </summary>
    /// <returns>Enumerable of driver descriptors with full metadata.</returns>
    public static IEnumerable<DriverDescriptor> GetDrivers () => _registry.Values;

    /// <summary>
    ///     Gets a driver descriptor by name (case-insensitive).
    /// </summary>
    /// <param name="name">The driver name.</param>
    /// <param name="descriptor">The found descriptor, or null if not found.</param>
    /// <returns>True if found; false otherwise.</returns>
    public static bool TryGetDriver (string name, out DriverDescriptor? descriptor) => _registry.TryGetValue (name, out descriptor);

    /// <summary>
    ///     Checks if a driver name is registered (case-insensitive).
    /// </summary>
    /// <param name="name">The driver name to check.</param>
    /// <returns>True if registered; false otherwise.</returns>
    public static bool IsRegistered (string name) => _registry.ContainsKey (name);

    /// <summary>
    ///     Gets drivers supported on the current platform.
    /// </summary>
    /// <returns>Enumerable of driver descriptors that support the current platform.</returns>
    public static IEnumerable<DriverDescriptor> GetSupportedDrivers ()
    {
        PlatformID currentPlatform = Environment.OSVersion.Platform;

        return _registry.Values.Where (d => d.SupportedPlatforms.Contains (currentPlatform));
    }

    /// <summary>
    ///     Gets the default driver descriptor for the current platform.
    /// </summary>
    /// <returns>The default driver descriptor based on platform detection.</returns>
    public static DriverDescriptor GetDefaultDriver ()
    {
        PlatformID p = Environment.OSVersion.Platform;

        if (p is PlatformID.Win32NT or PlatformID.Win32S or PlatformID.Win32Windows)
        {
            return _registry [Names.ANSI];
        }

        if (p == PlatformID.Unix)
        {
            return _registry [Names.ANSI];
        }

        // Fallback to dotnet
        Logging.Information ("Using fallback driver: dotnet");

        return _registry [Names.DOTNET];
    }
}
