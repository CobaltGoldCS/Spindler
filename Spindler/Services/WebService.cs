using HtmlOrError = Spindler.Utilities.Result<string, string>;

namespace Spindler.Services;

public class WebService
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

    public WebService() { }

    public WebService(Uri baseUrl)
    {
        SetBaseAddress(baseUrl);
    }

    public bool HasBaseAddress() => Client.BaseAddress is not null;
    public void SetBaseAddress(Uri baseUrl)
    {
        if (!baseUrl.IsAbsoluteUri)
        {
            throw new ArgumentException("baseUrl must be null - WebService.SetBaseAddress");
        }
        Client.BaseAddress = baseUrl;
    }


    /// <summary>
    /// Check if <paramref name="url"/> is in a valid http or https format
    /// </summary>
    /// <param name="url">The url to test validity of</param>
    /// <returns>if <paramref name="url"/> is valid</returns>
    public static bool IsUrl(string? url)
    {
        bool created = Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri _);
        return created && (url!.StartsWith("http") || url.StartsWith('/'));
    }

    public Uri MakeAbsoluteUrl(Uri url)
    {
        if (url.IsAbsoluteUri) return url;
        if (Client.BaseAddress is null) throw new NullReferenceException("Please set Base Address before making this call - WebService.MakeAbsoluteUrl");
        return new Uri(Client.BaseAddress, url!);
    }

    /// <summary>
    /// Attempt to obtain html from a url
    /// </summary>
    /// <param name="url">The url to attempt to scrape</param>
    /// <returns>Returns an ErrorOr object either containing the html or an error value string</returns>
    public async Task<HtmlOrError> HtmlOrError(string url)
    {
        try
        {
            var message = new HttpRequestMessage(HttpMethod.Get, url);
            var result = await Client.SendAsync(message);
            result = result.EnsureSuccessStatusCode();
            return new HtmlOrError.Ok(await result.Content.ReadAsStringAsync());
        }
        catch (HttpRequestException e)
        {
            return new HtmlOrError.Error($"Request Exception {e.StatusCode}: {e.Message}");
        }
    }
}
