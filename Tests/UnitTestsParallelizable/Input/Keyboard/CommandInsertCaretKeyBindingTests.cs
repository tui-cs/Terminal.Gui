// Claude - Opus 4.7

using System.Text.Json;

namespace InputTests;

/// <summary>
///     The acceptance criterion for #5318: the new multi-caret commands must
///     round-trip through the configuration serializer <b>by readable name</b>
///     (not as a bare number), so a consumer's <c>[ConfigurationProperty]</c>
///     default key bindings are discoverable and a user can override them in
///     <c>config.json</c> by name — exactly the tui-cs/Editor scenario that
///     forced the <c>(Command)1001/1002</c> magic-int workaround. Mirrors
///     <see cref="KeyBindingSchemaTests"/> and uses the same canonical options.
/// </summary>
public class CommandInsertCaretKeyBindingTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new () { TypeInfoResolver = SourceGenerationContext.Default };

    [Fact]
    public void InsertCaretBindings_RoundTrip_ByName ()
    {
        // Arrange — the real chords tui-cs/Editor binds (DEC-006: VS Code parity).
        Dictionary<Command, PlatformKeyBinding> original = new ()
        {
            [Command.InsertCaretAbove] = new PlatformKeyBinding { All = ["Ctrl+Alt+CursorUp"] },
            [Command.InsertCaretBelow] = new PlatformKeyBinding { All = ["Ctrl+Alt+CursorDown"] }
        };

        // Act
        string json = JsonSerializer.Serialize (original, _jsonOptions);
        Dictionary<Command, PlatformKeyBinding>? deserialized = JsonSerializer.Deserialize<Dictionary<Command, PlatformKeyBinding>> (json, _jsonOptions);

        // Assert — serialized as readable names, not numbers.
        Assert.Contains ("\"InsertCaretAbove\"", json);
        Assert.Contains ("\"InsertCaretBelow\"", json);
        Assert.DoesNotContain ("\"" + (int)Command.InsertCaretAbove + "\"", json);
        Assert.DoesNotContain ("\"" + (int)Command.InsertCaretBelow + "\"", json);

        Assert.NotNull (deserialized);
        Assert.Equal (2, deserialized.Count);
        Assert.Equal ((Key [])["Ctrl+Alt+CursorUp"], deserialized [Command.InsertCaretAbove].All!.AsEnumerable ());
        Assert.Equal ((Key [])["Ctrl+Alt+CursorDown"], deserialized [Command.InsertCaretBelow].All!.AsEnumerable ());
    }

    [Fact]
    public void InsertCaretBindings_Deserialize_FromUserConfigFormat ()
    {
        // Arrange — what a user (or tui-cs/Editor's shipped default) writes by hand.
        var json =
            """
            {
              "InsertCaretAbove": { "All": ["Ctrl+Alt+CursorUp"] },
              "InsertCaretBelow": { "All": ["Ctrl+Alt+CursorDown"], "Macos": ["Alt+CursorDown"] }
            }
            """;

        // Act
        Dictionary<Command, PlatformKeyBinding>? result = JsonSerializer.Deserialize<Dictionary<Command, PlatformKeyBinding>> (json, _jsonOptions);

        // Assert
        Assert.NotNull (result);
        Assert.Equal (2, result.Count);
        Assert.True (result.ContainsKey (Command.InsertCaretAbove));
        Assert.True (result.ContainsKey (Command.InsertCaretBelow));
        Assert.Equal ((Key [])["Ctrl+Alt+CursorUp"], result [Command.InsertCaretAbove].All!.AsEnumerable ());
        Assert.Equal ((Key [])["Alt+CursorDown"], result [Command.InsertCaretBelow].Macos!.AsEnumerable ());
        Assert.Null (result [Command.InsertCaretAbove].Macos);
    }

    [Fact]
    public void ViewKeyBindings_WithInsertCaret_RoundTrips ()
    {
        // The concrete tui-cs/Editor shape: a per-view [ConfigurationProperty]
        // Dictionary<string, Dictionary<Command, PlatformKeyBinding>> ("Editor"
        // → its default bindings). This is the path the magic-int cast broke.
        Dictionary<string, Dictionary<Command, PlatformKeyBinding>> original = new ()
        {
            ["Editor"] = new Dictionary<Command, PlatformKeyBinding>
            {
                [Command.InsertCaretAbove] = new () { All = ["Ctrl+Alt+CursorUp"] },
                [Command.InsertCaretBelow] = new () { All = ["Ctrl+Alt+CursorDown"] }
            }
        };

        // Act
        string json = JsonSerializer.Serialize (original, _jsonOptions);

        Dictionary<string, Dictionary<Command, PlatformKeyBinding>>? deserialized =
            JsonSerializer.Deserialize<Dictionary<string, Dictionary<Command, PlatformKeyBinding>>> (json, _jsonOptions);

        // Assert
        Assert.Contains ("\"InsertCaretAbove\"", json);
        Assert.NotNull (deserialized);
        Assert.Single (deserialized);
        Assert.Equal ((Key [])["Ctrl+Alt+CursorUp"], deserialized ["Editor"] [Command.InsertCaretAbove].All!.AsEnumerable ());
        Assert.Equal ((Key [])["Ctrl+Alt+CursorDown"], deserialized ["Editor"] [Command.InsertCaretBelow].All!.AsEnumerable ());
    }
}
