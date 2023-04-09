using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Spindler.Models;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.XPath;

namespace Spindler.Services;

using Path = Models.Path;
public class ConfigService
{

    public Path titlepath;
    public Path contentpath;
    public Path nextpath;
    public Path previouspath;
    public Path imageUrlPath;
    public Dictionary<string, object> extraconfigs;

    public int configId;

    public bool IsNull = false;

    public ConfigService(Config config)
    {
        configId = config.Id;
        if (string.IsNullOrEmpty(config.TitlePath))
        {
            config.TitlePath = "//title";
        }
        titlepath = new Path(config.TitlePath);
        contentpath = new Path(config.ContentPath);
        nextpath = new Path(config.NextUrlPath);
        previouspath = new Path(config.PrevUrlPath);
        imageUrlPath = new Path(config.ImageUrlPath);
        extraconfigs = config.ExtraConfigs;
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
        Title,
        Content,
        NextUrl,
        PrevUrl,
        ImageUrl,
    }

    /// <summary>
    /// Naively Checks if a selector is of valid syntax
    /// </summary>
    /// <param name="path">The selector to test (string)</param>
    /// <returns>If the selector is valid or not</returns>
    public static bool IsValidSelector(string path)
    {
        if (path == null) return false;
        HtmlDocument nav = new HtmlDocument();
        var temppath = new Path(path);
        try
        {
            bool _ = temppath.type switch
            {
                Path.Type.Css => CssElementHandler(nav, path, SelectorType.Text) != null,
                Path.Type.XPath => XPathHandler(nav, path, SelectorType.Text) != null,
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

    public Path GetPath(Selector selector)
    {
        switch (selector)
        {
            case Selector.Title:
                return titlepath;
            case Selector.Content:
                return contentpath;
            case Selector.PrevUrl:
                return previouspath;
            case Selector.NextUrl:
                return nextpath;
            case Selector.ImageUrl:
                return imageUrlPath;
            default:
                throw new NotImplementedException("Selector not implemented (ConfigService.GetPath)");
        }
    }

    public Dictionary<string, object> GetExtraConfigs() => extraconfigs;

    #region Selectors using Paths

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
    public static string PrettyWrapSelector(string html, Path path, SelectorType type)
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
    public static string PrettyWrapSelector(HtmlDocument nav, Path path, SelectorType type)
    {
        try
        {
            string? value = path.type switch
            {
                Path.Type.XPath => XPathHandler(nav, path.path, type),
                Path.Type.Css => CssElementHandler(nav, path.path, type),
                _ => throw new NotImplementedException("This type is not implemented (PrettyWrapSelector)"),
            };
            return HttpUtility.HtmlDecode(value) ?? string.Empty;
        }
        catch (XPathException e)
        {
            throw new XPathException($"Error on path {path}: {e}");
        }
    }

    /// <summary>
    /// Select string from <paramref name="nav"/> using a csspath
    /// </summary>
    /// <param name="nav">The document to select string from </param>
    /// <param name="path">The csspath to use</param>
    /// <param name="type">The specific type to prioritize</param>
    /// <returns>A string based on the css syntax used</returns>
    /// <exception cref="NotImplementedException">If they selector type has not been implemented</exception>
    public static string? CssElementHandler(HtmlDocument nav, string path, SelectorType type)
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
                value = Regex.Match(value, "((?:https?:/)?/[-a-zA-Z0-9+&@#/%?=~_|!:, .;]*[-a-zA-Z0-9+&@#/%=~_|])").Value;
            }
            return value;
        }
        return type switch
        {
            SelectorType.Text => nav.QuerySelector(path)?.CreateNavigator().Value,
            SelectorType.Link => nav.QuerySelector(path)?.GetAttributeValue("href", null),
            _ => throw new NotImplementedException($"This selector type: {type} is not implemented (CssElementHandler)"),
        };
    }

    public static string? XPathHandler(HtmlDocument nav, string path, SelectorType type)
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
        if (type == SelectorType.Link && !path.Contains('@'))
            path += path.EndsWith("/") ? "@href" : "/@href";
        var value = nav.DocumentNode.SelectSingleNode(path)?.CreateNavigator().Value;
        if (value is null) return value;
        if (type == SelectorType.Link)
            value = Regex.Match(value, "((?:https?:/)?/[-a-zA-Z0-9+&@#/%?=~_|!:, .;]*[-a-zA-Z0-9+&@#/%=~_|])").Value;
        return value;
    }

    /// <summary>
    /// Smart Get Content that matches given content path using xpath
    /// </summary>
    /// <param name="nav">The HtmlDocument to evaluate for matches</param>
    /// <returns>A String containing the text of the content matched by contentpath</returns>
    public string GetContent(HtmlDocument nav)
    {
        HtmlNode node = contentpath!.type switch
        {
            Path.Type.Css => nav.QuerySelector(contentpath.path),
            Path.Type.XPath => nav.DocumentNode.SelectSingleNode(contentpath.path),
            _ => throw new NotImplementedException("This path type has not been implemented {ConfigService.GetContent}"),
        };

        if (node == null) return string.Empty;
        if (!node.HasChildNodes)
        {
            return HttpUtility.HtmlDecode(node.InnerText);
        }

        // Node contains child nodes, so we must get the text of each
        StringWriter stringWriter = new();
        string separator = (string)extraconfigs!.GetValueOrDefault("separator", "\n");
        foreach (HtmlNode child in node.ChildNodes)
        {
            if (child.OriginalName == "br")
            {
                if (child.NextSibling?.OriginalName != "br")
                    stringWriter.Write("\n");
                continue;
            }
            stringWriter.WriteLine($"\t\t{HttpUtility.HtmlDecode(child.InnerText)}{separator}");
        }
        return stringWriter.ToString();
    }



    #endregion
}
