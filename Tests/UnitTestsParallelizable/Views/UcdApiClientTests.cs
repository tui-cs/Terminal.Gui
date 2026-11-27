namespace TerminalGuiTests.Views;

public class UcdApiClientTests
{
    // Codex - GPT-6.1
    [Theory]
    [InlineData (0x327, "327")]
    [InlineData (0x1F600, "1F600")]
    public void GetCodepointUrl_UsesHexadecimalCodepoint (int codepoint, string hex)
    {
        Assert.Equal ($"https://unicode-api.aaronluna.dev/v1/codepoints/{hex}?show_props=all", UcdApiClient.GetCodepointUrl (codepoint));
    }
}
