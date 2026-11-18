using System.Collections.Concurrent;
using System.Text.Json;
using Terminal.Gui.Configuration;

namespace ConfigurationTests;

public class ConcurrentDictionaryJsonConverterTests
{
    // Claude - Opus 4.7
    [Fact]
    public void Read_RejectsDuplicateKeys ()
    {
        const string json = """
                            [
                              { "alpha": "first" },
                              { "alpha": "second" }
                            ]
                            """;

        JsonSerializerOptions options = new ()
        {
            Converters = { new ConcurrentDictionaryJsonConverter<string> () }
        };

        Assert.Throws<JsonException> (
                                      () => JsonSerializer.Deserialize<ConcurrentDictionary<string, string>> (json, options));
    }
}
