using Spindler.Utilities;

namespace Spindler.Services;

public class StandardWebService : IWebService
{
    #region Client
    private HttpClient? client;
    private HttpClient Client
    {
        get
        {
            if (client is null)
            {
                HttpClientHandler handler = new();
                client = new(handler) { };
                client.DefaultRequestHeaders.Add("User-Agent", App.SharedValues.userAgent);
            }
            return client;
        }
        set => client = value;
    }

    #endregion

    public StandardWebService() { }

    

    /// <summary>
    /// Attempt to obtain html from a url
    /// </summary>
    /// <param name="url">The absolute url to attempt to scrape</param>
    /// <returns>Returns an ErrorOr object either containing the html or an error value string</returns>
    public async Task<IResult<string>> GetHtmlFromUrl(string url)
    {
        try
        {
            var message = new HttpRequestMessage(HttpMethod.Get, url);
            var result = await Client.SendAsync(message);
            result = result.EnsureSuccessStatusCode();
            return new Ok<string>(await result.Content.ReadAsStringAsync());
        }
        catch (HttpRequestException e)
        {
            return new Invalid<string>(new Error($"Request Exception {e.StatusCode}: {e.Message}"));
        }
    }
}
