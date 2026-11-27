namespace Terminal.Gui.Views;

/// <summary>
///     A helper class for accessing the Unicode API.
/// </summary>
internal class UcdApiClient
{
    public const string BaseUrl = "https://unicode-api.aaronluna.dev/v1/";
    private static readonly HttpClient _httpClient = new ();

    internal static string GetCodepointUrl (int codepoint) => $"{BaseUrl}codepoints/{codepoint:X}?show_props=all";

    public async Task<string> GetCodepointDec (int dec)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync (GetCodepointUrl (dec)).ConfigureAwait (false);
        response.EnsureSuccessStatusCode ();

        return await response.Content.ReadAsStringAsync ().ConfigureAwait (false);
    }

}
