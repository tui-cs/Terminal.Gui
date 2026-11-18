using System.Text.Json.Serialization;

namespace Terminal.Gui.Input;

/// <summary>
///     Defines the mouse flags for a single command, optionally varying by platform.
///     Mouse flags are additive — for example, on Linux both <see cref="All"/> and <see cref="Linux"/> flags apply.
/// </summary>
public record PlatformMouseBinding
{
    /// <summary>Gets or sets mouse flags that apply on all platforms.</summary>
    [JsonConverter (typeof (MouseFlagsArrayJsonConverter))]
    public MouseFlags []? All { get; init; }

    /// <summary>Gets or sets additional mouse flags for Windows only.</summary>
    [JsonConverter (typeof (MouseFlagsArrayJsonConverter))]
    public MouseFlags []? Windows { get; init; }

    /// <summary>Gets or sets additional mouse flags for Linux only.</summary>
    [JsonConverter (typeof (MouseFlagsArrayJsonConverter))]
    public MouseFlags []? Linux { get; init; }

    /// <summary>Gets or sets additional mouse flags for macOS only.</summary>
    [JsonConverter (typeof (MouseFlagsArrayJsonConverter))]
    public MouseFlags []? Macos { get; init; }

    /// <inheritdoc/>
    public override string ToString ()
    {
        List<string> parts = [];

        if (All is { Length: > 0 })
        {
            parts.Add ($"All=[{string.Join (", ", All.Select (k => k.ToString ()))}]");
        }

        if (Windows is { Length: > 0 })
        {
            parts.Add ($"Win=[{string.Join (", ", Windows.Select (k => k.ToString ()))}]");
        }

        if (Linux is { Length: > 0 })
        {
            parts.Add ($"Linux=[{string.Join (", ", Linux.Select (k => k.ToString ()))}]");
        }

        if (Macos is { Length: > 0 })
        {
            parts.Add ($"Mac=[{string.Join (", ", Macos.Select (k => k.ToString ()))}]");
        }

        return parts.Count > 0 ? string.Join ("; ", parts) : "(none)";
    }

    /// <summary>
    ///     Returns the mouse flags applicable to the current operating system.
    ///     Yields all <see cref="All"/> flags followed by the platform-specific flags.
    /// </summary>
    public IEnumerable<MouseFlags> GetCurrentPlatformMouseFlags ()
    {
        if (All is { })
        {
            foreach (MouseFlags mouseFlags in All)
            {
                yield return mouseFlags;
            }
        }

        MouseFlags []? platformFlags = OperatingSystem.IsWindows () ? Windows :
            OperatingSystem.IsMacOS () ? Macos : Linux;

        if (platformFlags is null)
        {
            yield break;
        }

        foreach (MouseFlags mouseFlags in platformFlags)
        {
            yield return mouseFlags;
        }
    }
}
