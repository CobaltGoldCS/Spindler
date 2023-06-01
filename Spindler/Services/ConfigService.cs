using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Spindler.Models;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.XPath;

namespace Spindler.Services;

using SelectorPath = Models.Path;
public partial class ConfigService
{

    private SelectorPath titlepath;
    private SelectorPath contentpath;
    private SelectorPath nextpath;
    private SelectorPath previouspath;
    private SelectorPath imageUrlPath;

    #region Public Apis

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

    public enum SelectorType
    {
        /// <summary>
        /// Denotes a preference for links (target href if possible)
        /// </summary>
        Link,
        /// <summary>
        /// Denotes a preference for text of html elements (target text)
        /// </summary>
        Text,
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
    public static bool IsValidSelector(string path)
    {
        if (path is null) return false;
        HtmlDocument nav = new HtmlDocument();
        var temppath = new SelectorPath(path);
        try
        {
            bool _ = temppath.type switch
            {
                SelectorPath.Type.Css => CssElementHandler(nav, path, SelectorType.Text) != null,
                SelectorPath.Type.XPath => XPathHandler(nav, path, SelectorType.Text) != null,
                _ => throw new NotImplementedException("This path type has not been implemented")
            };
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
        return PrettyWrapSelector(html, GetPath(selector), type);
    }
    /// <summary>
    /// Attempt to get text from element pointed to by <paramref name="path"/>
    /// </summary>
    /// <param name="html">Html to select from</param>
    /// <param name="path">A string representation of the target's css or x path</param>
    /// <returns cref="string">A string containing the target text, or an empty string if nothing is found</returns>
    /// <exception cref="XPathException">If there is any error in the path</exception>
    public static string PrettyWrapSelector(string html, SelectorPath path, SelectorType type)
    {
        HtmlDocument doc = new();
        doc.LoadHtml(html);

        return PrettyWrapSelector(nav: doc, path, type);
    }

    /// <summary>
    /// Attempt to get text from element pointed to by <paramref name="path"/>
    /// </summary>
    /// <param name="nav">The HtmlDocument to get the text from</param>
    /// <param name="path">A string representation of the target's css or x path</param>
    /// <returns cref="string">A string containing the target text, or an empty string if nothing is found</returns>
    /// <exception cref="XPathException">If there is any error in the path</exception>
    public static string PrettyWrapSelector(HtmlDocument nav, SelectorPath path, SelectorType type)
    {
        try
        {
            string? value = path.type switch
            {
                SelectorPath.Type.XPath => XPathHandler(nav, path.path, type),
                SelectorPath.Type.Css => CssElementHandler(nav, path.path, type),
                _ => throw new NotImplementedException("This type is not implemented (PrettyWrapSelector)"),
            };
            return HttpUtility.HtmlDecode(value) ?? string.Empty;
        }
        catch (XPathException e)
        {
            throw new XPathException($"Error on path {path}: {e}");
        }
    }


    #endregion

    #region Selectors using Paths
    /// <summary>
    /// Select string from <paramref name="nav"/> using a csspath
    /// </summary>
    /// <param name="nav">The document to select string from </param>
    /// <param name="path">The csspath to use</param>
    /// <param name="type">The specific type to prioritize</param>
    /// <returns>A string based on the css syntax used</returns>
    /// <exception cref="NotImplementedException">If they selector type has not been implemented</exception>
    private static string? CssElementHandler(HtmlDocument nav, string path, SelectorType type)
    {
        // Custom $ Syntax
        MatchCollection attributes = Regex.Matches(path, "(.+) \\$(.+)");
        if (attributes.Any())
        {
            var cleanpath = attributes[0].Groups[1].Value;
            var modifier = attributes[0].Groups[2].Value;
            HtmlNode node = nav.QuerySelector(cleanpath);
            if (modifier == "text")
            {
                return node?.InnerText;
            }
            var value = node?.GetAttributeValue(modifier, null);
            if (type == SelectorType.Link && modifier != "href" && value != null)
            {
                value = CleanLinkRegex().Match(value).Value;
            }
            return value;
        }

        HtmlNode? targetNode = nav.QuerySelector(path);
        switch (type)
        {
            case SelectorType.Text:
                return targetNode?.CreateNavigator().Value;
            case SelectorType.Link:
                return targetNode.OriginalName switch
                {
                    "a" => targetNode.GetAttributeValue("href", null),
                    "img" => targetNode.GetAttributeValue("src", null),
                    _ => targetNode.CreateNavigator().Value,
                };
            default:
                throw new NotImplementedException($"This selector type: {type} is not implemented (CssElementHandler)");
        }
    }

    /// <summary>
    /// Select string from <paramref name="nav"/> using a xpath
    /// </summary>
    /// <param name="nav">The document to select string from </param>
    /// <param name="path">The xpath to use</param>
    /// <param name="type">The specific type to prioritize</param>
    /// <returns>A string based on the xpath syntax used</returns>
    private static string? XPathHandler(HtmlDocument nav, string path, SelectorType type)
    {
        // Custom $ Syntax
        MatchCollection attributes = Regex.Matches(path, "(.+) \\$(.+)");
        if (attributes.Any())
        {
            var cleanpath = attributes[0].Groups[1].Value;
            var modifier = attributes[0].Groups[2].Value;

            var node = nav.DocumentNode.SelectSingleNode(cleanpath);
            if (node is null) return null;
            return node.GetAttributeValue(modifier, null);
        }

        HtmlNode? targetNode = nav.DocumentNode.SelectSingleNode(path);
        if (targetNode is null) 
            return null;

        // Select proper attributes from links
        if (type == SelectorType.Link && !XPathSelectsValue().Matches(path).Any())
        {
            return targetNode.OriginalName switch
            {
                "a" => targetNode.GetAttributeValue("href", null),
                "img" => targetNode.GetAttributeValue("src", null),
                _ => targetNode.CreateNavigator().Value,
            };
        }
        return targetNode.CreateNavigator().Value;
    }

    #endregion

    [GeneratedRegex("((?:https?:/)?/[-a-zA-Z0-9+&@#/%?=~_|!:, .;]*[-a-zA-Z0-9+&@#/%=~_|])")]
    private static partial Regex CleanLinkRegex();

    [GeneratedRegex("^.+/@.+$")]
    private static partial Regex XPathSelectsValue();
}
