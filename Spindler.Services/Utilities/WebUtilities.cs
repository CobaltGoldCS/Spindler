namespace Spindler.Utilities;

public class WebUtilities
{
    private Uri? baseUrl;
    private Uri BaseUrl
    {
        get
        {
            ArgumentNullException.ThrowIfNull(baseUrl);
            return baseUrl;
        }
        set
        {
            baseUrl ??= value;
        }
    }
    public WebUtilities(Uri baseUrl)
    {
        BaseUrl = baseUrl;
    }

    public WebUtilities()
    {
    }

#pragma warning  disable CS8601
    /// <summary>
    /// Check if <paramref name="url"/> is in a valid http or https format
    /// </summary>
    /// <param name="url">The url to test validity of</param>
    /// <returns>if <paramref name="url"/> is valid</returns>
    public static bool IsUrl(string? url)
    {
        if (url == "#")
            return false;
        bool created = Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri _);
        return created && (url!.StartsWith("http") || url.StartsWith('/'));
    }
#pragma warning restore CS8601

    public Uri MakeAbsoluteUrl(Uri url)
    {
        if (url.IsAbsoluteUri) return url;
        return new Uri(BaseUrl, url!);
    }

    public Uri MakeAbsoluteUrl(string url)
    {
        Uri modifierUrl = new Uri(url, UriKind.RelativeOrAbsolute);
        return MakeAbsoluteUrl(modifierUrl);
    }

    public void SetBaseUrl(Uri baseUrl)
    {
        if (HasBaseUrl())
        {
            return;
        }
        BaseUrl = baseUrl;
    }

    public Result<string> SetBaseUrlSafe(string baseUrl)
    {
        if (HasBaseUrl())
        {
            return Result.Success("Already has a base url");
        }
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? uri))
            return Result.Error<string>($"'{baseUrl}' is not a valid url");

        SetBaseUrl(new Uri(uri.GetLeftPart(UriPartial.Authority) + "/", UriKind.Absolute));
        return Result.Success("Base url set");
    }

    public bool HasBaseUrl() => IsUrl(baseUrl?.ToString());
}
