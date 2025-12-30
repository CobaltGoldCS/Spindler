using Spindler.Models;
using System.Xml.XPath;

namespace Spindler.Services.Web;

using SelectorPath = Models.SelectorPath;
public partial class SelectionService
{

    private readonly SelectorPath titlepath;
    private readonly SelectorPath contentpath;
    private readonly SelectorPath nextpath;
    private readonly SelectorPath previouspath;
    private readonly SelectorPath imageUrlPath;


    public SelectionService(Config config)
    {
        if (string.IsNullOrEmpty(config.TitlePath))
        {
            config.TitlePath = "//title";
        }
        titlepath = config.TitlePath.AsPath();
        contentpath = config.ContentPath.AsPath();
        nextpath = config.NextUrlPath.AsPath();
        previouspath = config.PrevUrlPath.AsPath();
        imageUrlPath = config.ImageUrlPath.AsPath();
    }

    public enum Selector
    {
        /// <summary>
        /// Title SelectorPath
        /// </summary>
        Title,
        /// <summary>
        /// Content SelectorPath
        /// </summary>
        Content,
        /// <summary>
        /// Next Url SelectorPath
        /// </summary>
        NextUrl,
        /// <summary>
        /// Previous Url SelectorPath
        /// </summary>
        PrevUrl,
        /// <summary>
        /// Image Url SelectorPath
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
            _ = path.AsPath().Select(path, SelectorType.Text);
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

    public string Select(string html, Selector selector, SelectorType type)
    {
        return GetPath(selector).Select(html, type);
    }
}
