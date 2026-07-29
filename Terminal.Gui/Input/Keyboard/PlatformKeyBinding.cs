using System.Text.Json.Serialization;

namespace Terminal.Gui.Input;

/// <summary>
///     Defines the keys for a single command, optionally varying by platform.
///     Keys are additive — for example, on Linux both <see cref="All"/> and <see cref="Linux"/> keys apply.
/// </summary>
public record PlatformKeyBinding
{
    /// <summary>Gets or sets keys that apply on all platforms.</summary>
    [JsonConverter (typeof (KeyArrayJsonConverter))]
    public Key []? All { get; init; }

    /// <summary>Gets or sets additional keys for Windows only.</summary>
    [JsonConverter (typeof (KeyArrayJsonConverter))]
    public Key []? Windows { get; init; }

    /// <summary>Gets or sets additional keys for Linux only.</summary>
    [JsonConverter (typeof (KeyArrayJsonConverter))]
    public Key []? Linux { get; init; }

    /// <summary>Gets or sets additional keys for macOS only.</summary>
    [JsonConverter (typeof (KeyArrayJsonConverter))]
    public Key []? Macos { get; init; }

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
    ///     Returns the keys applicable to the current operating system.
    ///     Yields all <see cref="All"/> keys followed by the platform-specific keys.
    /// </summary>
    public IEnumerable<Key> GetCurrentPlatformKeys ()
    {
        if (All is { })
        {
            foreach (Key k in All)
            {
                yield return k;
            }
        }

        Key []? platKeys = OperatingSystem.IsWindows () ? Windows :
            OperatingSystem.IsMacOS () ? Macos : Linux;

        if (platKeys is null)
        {
            yield break;
        }

        foreach (Key k in platKeys)
        {
            yield return k;
        }
    }
}
