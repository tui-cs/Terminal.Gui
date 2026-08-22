using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Configuration;

/// <summary>
///     The configured <see cref="SourceGenerationContext"/> instance used by Terminal.Gui's JSON
///     converters and serializers. Wraps <see cref="SourceGenerationContext.Default"/> with the
///     options required by Terminal.Gui (comment-handling, case-insensitivity, custom converters
///     for <see cref="System.Text.Rune"/> and <see cref="Input.Key"/>, and
///     <see cref="JavaScriptEncoder.UnsafeRelaxedJsonEscaping"/>).
/// </summary>
/// <remarks>
///     Internal: converters and serializers should reference <see cref="Instance"/>.
/// </remarks>
[SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "Internal serializer context")]
internal static class TuiSerializerContext
{
    /// <summary>
    ///     The shared <see cref="SourceGenerationContext"/> instance, pre-configured for Terminal.Gui's
    ///     JSON conventions.
    /// </summary>
    internal static readonly SourceGenerationContext Instance = new (new JsonSerializerOptions
    {
        // Be relaxed
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        AllowTrailingCommas = true,
        Converters =
        {
            // Custom Rune converter so Glyphs can be specified flexibly.
            new RuneJsonConverter (),

            // Custom Key converter so "Ctrl+Q" parses as expected.
            new KeyJsonConverter ()
        },

        // Enables Key to be "Ctrl+Q" vs "Ctrl\u002BQ"
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = SourceGenerationContext.Default
    });
}
