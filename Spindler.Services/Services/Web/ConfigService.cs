using Spindler.Models;
using System.Xml.XPath;

namespace Spindler.Services.Web;

using SelectorPath = Models.Path;
public partial class ConfigService
{

    private SelectorPath titlepath;
    private SelectorPath contentpath;
    private SelectorPath nextpath;
    private SelectorPath previouspath;
    private SelectorPath imageUrlPath;


    public ConfigService(Config config)
    {
        if (string.IsNullOrEmpty(config.TitlePath))
        {
            config.TitlePath = "//title";
        }
        titlepath = new(config.TitlePath);
        contentpath = new(config.ContentPath);
        nextpath = new(config.NextUrlPath);
        previouspath = new(config.PrevUrlPath);
        imageUrlPath = new(config.ImageUrlPath);
    }

    public enum Selector
    {
        /// <summary>
        /// Title Path
        /// </summary>
        Title,
        /// <summary>
        /// Content Path
        /// </summary>
        Content,
        /// <summary>
        /// Next Url Path
        /// </summary>
        NextUrl,
        /// <summary>
        /// Previous Url Path
        /// </summary>
        PrevUrl,
        /// <summary>
        /// Image Url Path
        /// </summary>
        ImageUrl,
    }

    /// <summary>
    /// Naively Checks if a selector is of valid syntax
    /// </summary>
    /// <param name="path">The selector to test (string)</param>
    /// <returns>If the selector is valid or not</returns>
    public static bool IsValidSelector(string? path)
    {
        if (path is null || path.Length == 0) return false;
        try
        {
            _ = path.AsPath().Select(path, SelectorType.Text) != null;
            return true;
        }
        catch (Exception e) when (
        e is XPathException ||
        e is ArgumentException ||
        e is ArgumentNullException ||
        e is InvalidOperationException ||
        e is NotSupportedException)
        {
            return false;
        }
    }

    public SelectorPath GetPath(Selector selector)
    => selector switch
    {
        Selector.Title => titlepath,
        Selector.Content => contentpath,
        Selector.PrevUrl => previouspath,
        Selector.NextUrl => nextpath,
        Selector.ImageUrl => imageUrlPath,
        _ => throw new NotImplementedException("Selector not implemented (ConfigService.GetPath)")
    };

    public string PrettyWrapSelector(string html, Selector selector, SelectorType type)
    {
        return GetPath(selector).Select(html, type);
    }
}
