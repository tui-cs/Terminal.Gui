namespace Terminal.Gui.Drivers;

/// <summary>
///     Holds global driver settings and cross-driver utility methods.
/// </summary>
public sealed class Driver
{
    // NOTE: Force16Colors is a configuration property (Driver.Force16Colors).
    // NOTE: IDriver also has a Force16Colors property, which is an instance property
    // NOTE: set whenever this static property is set.
    /// <summary>
    ///     Determines if driver instances should use 16 colors instead of the default TrueColors.
    /// </summary>
    /// <seealso cref="IDriver.Force16Colors"/>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static bool Force16Colors
    {
        get => DriverSettings.Defaults.Force16Colors;
        set
        {
            bool oldValue = DriverSettings.Defaults.Force16Colors;
            DriverSettings.Defaults.Force16Colors = value;
            Force16ColorsChanged?.Invoke (null, new ValueChangedEventArgs<bool> (oldValue, value));
        }
    }

    /// <summary>Raised when <see cref="Force16Colors"/> changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<bool>>? Force16ColorsChanged;

    // NOTE: SizeDetection is a configuration property (Driver.SizeDetection).
    // It controls which strategy the ANSI driver uses to determine the terminal's size.
    /// <summary>
    ///     Controls how the ANSI driver detects the terminal's window size.
    ///     Defaults to <see cref="SizeDetectionMode.Polling"/>, which uses a synchronous
    ///     native query (<c>ioctl</c> on Unix, Console API on Windows).
    ///     Set to <see cref="SizeDetectionMode.AnsiQuery"/> for transports that require
    ///     an ANSI <c>CSI 18t</c> query instead.
    /// </summary>
    /// <seealso cref="SizeDetectionMode"/>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static SizeDetectionMode SizeDetection
    {
        get => DriverSettings.Defaults.SizeDetection;
        set => DriverSettings.Defaults.SizeDetection = value;
    }

    /// <summary>
    ///     Determines whether the process has a controlling terminal usable for TUI rendering and input.
    ///     Returns <see langword="true"/> when either:
    ///     <list type="bullet">
    ///         <item>stdin/stdout are connected to a console device, or</item>
    ///         <item>
    ///             stdin/stdout are redirected (e.g. via a shell pipeline such as
    ///             <c>result=$(myapp)</c> or <c>myapp | jq</c>) but a controlling terminal is available
    ///             via <c>/dev/tty</c> on Unix or <c>CONIN$</c>/<c>CONOUT$</c> on Windows.
    ///         </item>
    ///     </list>
    ///     Set the environment variable <c>DisableRealDriverIO=1</c> to skip real terminal detection and
    ///     force this method to return false, which is required for running in test harnesses that do not
    ///     have a real terminal attached.
    /// </summary>
    /// <param name="inputAttached">
    ///     When this method returns, <see langword="true"/> if a terminal device is available for input
    ///     (either stdin or the controlling terminal); otherwise <see langword="false"/>.
    /// </param>
    /// <param name="outputAttached">
    ///     When this method returns, <see langword="true"/> if a terminal device is available for output
    ///     (either stdout or the controlling terminal); otherwise <see langword="false"/>.
    /// </param>
    /// <returns><see langword="true"/> if both input and output are attached to a terminal; otherwise <see langword="false"/>.</returns>
    public static bool IsAttachedToTerminal (out bool inputAttached, out bool outputAttached)
    {
        inputAttached = outputAttached = false;

        // When the test harness sets DisableRealDriverIO, skip real terminal detection entirely.
        if (string.Equals (Environment.GetEnvironmentVariable ("DisableRealDriverIO"), "1", StringComparison.Ordinal))
        {
            return false;
        }

        inputAttached = TerminalDevice.IsInputAttached;
        outputAttached = TerminalDevice.IsOutputAttached;

        return inputAttached && outputAttached;
    }
}
