﻿using Spindler.Utilities;

namespace Spindler.Services.Web;

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
                client.DefaultRequestHeaders.Add("User-Agent", GetUserAgent());
            }
            return client;
        }
        set => client = value;
    }

    #endregion

    public StandardWebService(HttpClient? client = null)
    {
        if (client is null)
            return;
        if (!client.DefaultRequestHeaders.TryGetValues("User-Agent", out _))
        {
            client.DefaultRequestHeaders.Add("User-Agent", GetUserAgent());
        }
        this.client = client;
    }



    /// <summary>
    /// Attempt to obtain html from a url
    /// </summary>
    /// <param name="url">The absolute url to attempt to scrape</param>
    /// <returns>Returns an ErrorOr object either containing the html or an error value string</returns>
    public async Task<Result<string>> GetHtmlFromUrl(string url, CancellationToken? token = null)
    {
        var message = new HttpRequestMessage(HttpMethod.Get, url);
        token ??= new CancellationToken();
        var result = await Client.SendAsync(message, token.Value);
        if (result.IsSuccessStatusCode)
        {
            return Result.Success(await result.Content.ReadAsStringAsync());
        }
        return Result.Error<string>($"Response {result.StatusCode} : {Environment.NewLine}{await result.Content.ReadAsStringAsync()}");
    }
    private static readonly string[] possibleUserAgents = {
            "Mozilla/5.0 (Linux; Android 10; SM-G980F Build/QP1A.190711.020; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/78.0.3904.96 Mobile Safari/537.36",
            "Mozilla/5.0 (Linux; Android 10; SM-G973U Build/PPR1.180610.011) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3497.100 Mobile Safari/537.36",
            "Mozilla/5.0 (Linux; Android 11; Pixel 6 Build/SD1A.210817.023; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/94.0.4606.71 Mobile Safari/537.36",
            "Mozilla/5.0 (Linux; Android 11; Pixel 5 Build/RQ3A.210805.001.A1; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/92.0.4515.159 Mobile Safari/537.36"
    };
    private string GetUserAgent() => possibleUserAgents[new Random().Next(possibleUserAgents.Length)];
}
