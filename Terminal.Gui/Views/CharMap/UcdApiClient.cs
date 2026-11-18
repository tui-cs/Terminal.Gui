namespace Terminal.Gui.Views;

/// <summary>
///     A helper class for accessing the ucdapi.org API.
/// </summary>
internal class UcdApiClient
{
    public const string BaseUrl = "https://ucdapi.org/unicode/latest/";
    private static readonly HttpClient _httpClient = new ();

    public async Task<string> GetChars (string chars)
    {
        HttpResponseMessage response = await _httpClient.GetAsync ($"{BaseUrl}chars/{Uri.EscapeDataString (chars)}").ConfigureAwait (false);
        response.EnsureSuccessStatusCode ();

        return await response.Content.ReadAsStringAsync ().ConfigureAwait (false);
    }

    public async Task<string> GetCharsName (string chars)
    {
        HttpResponseMessage response =
            await _httpClient.GetAsync ($"{BaseUrl}chars/{Uri.EscapeDataString (chars)}/name").ConfigureAwait (false);
        response.EnsureSuccessStatusCode ();

        return await response.Content.ReadAsStringAsync ().ConfigureAwait (false);
    }

    public async Task<string> GetCodepointDec (int dec)
    {
        HttpResponseMessage response = await _httpClient.GetAsync ($"{BaseUrl}codepoint/dec/{dec}").ConfigureAwait (false);
        response.EnsureSuccessStatusCode ();

        return await response.Content.ReadAsStringAsync ().ConfigureAwait (false);
    }

    public async Task<string> GetCodepointHex (string hex)
    {
        HttpResponseMessage response = await _httpClient.GetAsync ($"{BaseUrl}codepoint/hex/{hex}").ConfigureAwait (false);
        response.EnsureSuccessStatusCode ();

        return await response.Content.ReadAsStringAsync ().ConfigureAwait (false);
    }
}
