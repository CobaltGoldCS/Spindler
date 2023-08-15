using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Web;

namespace Spindler.Models;

/// <summary>
/// Dataclass representing a selector path, such as an xpath or csspath
/// </summary>
public class Path
{
    public string PathString;
    public Type type { get; private set; }

    private IHTMLSelector InternalSelector;

    public enum Type
    {
        Css,
        XPath
    }

    public Path(string pathString)
    {
        PathString = pathString;
        type = PathString.StartsWith("/") ? Type.XPath : Type.Css;

        InternalSelector = type switch
        {
            Type.Css => new CSSPathSelector(),
            Type.XPath => new XPathSelector(),
            _ => throw new NotImplementedException("This selectortype is not implemented")
        };
    }

    public string Select(string path, SelectorType type)
    {
        string? selectedItems = InternalSelector.Select(path, this, type);
        return HttpUtility.HtmlDecode(selectedItems) ?? string.Empty;
    }
    public string Select(HtmlDocument nav, SelectorType type)
    {
        string? selectedItems = InternalSelector.Select(nav, this, type);
        return HttpUtility.HtmlDecode(selectedItems) ?? string.Empty;
    }
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
    /// <summary>
    /// Denotes HTML Selection
    /// </summary>
    Html,
}

public interface IHTMLSelector
{
    public string? Select(HtmlDocument nav, Path path, SelectorType type);
    public string? Select(string html, Path path, SelectorType type)
    {
        HtmlDocument doc = new();
        doc.LoadHtml(html);
        return Select(doc, path, type);
    }
}

public partial class XPathSelector : IHTMLSelector
{
    public string? Select(HtmlDocument nav, Path path, SelectorType type)
    {
        // Custom $ Syntax
        MatchCollection attributes = Regex.Matches(path.PathString, "(.+) \\$(.+)");
        if (attributes.Any())
        {
            var cleanpath = attributes[0].Groups[1].Value;
            var modifier = attributes[0].Groups[2].Value;

            var node = nav.DocumentNode.SelectSingleNode(cleanpath);
            if (node is null) return null;
            return node.GetAttributeValue(modifier, null);
        }

        HtmlNode? targetNode = nav.DocumentNode.SelectSingleNode(path.PathString);
        if (targetNode is null)
            return null;

        // Select Proper Attributes
        if (XPathSelectsValue().Matches(path.PathString).Any() && type is SelectorType.Text)
        {
            return targetNode.CreateNavigator().Value;
        }

        return type switch
        {
            SelectorType.Text => targetNode.CreateNavigator().Value,
            SelectorType.Link => targetNode.OriginalName switch
            {
                "a" => targetNode.GetAttributeValue("href", null),
                "img" => targetNode.GetAttributeValue("src", null),
                _ => targetNode.CreateNavigator().Value,
            },
            SelectorType.Html => targetNode.InnerHtml,
            _ => throw new NotImplementedException($"This selector type: {type} is not implemented (CssElementHandler)"),
        };
    }

    [GeneratedRegex("^.+/@.+$")]
    private static partial Regex XPathSelectsValue();
}


public partial class CSSPathSelector : IHTMLSelector
{
    public string? Select(HtmlDocument nav, Path path, SelectorType type)
    {
        // Custom $ Syntax
        MatchCollection attributes = Regex.Matches(path.PathString, "(.+) \\$(.+)");
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

        HtmlNode? targetNode = nav.QuerySelector(path.PathString);
        if (targetNode is null)
            return null;

        return type switch
        {
            SelectorType.Text => targetNode.CreateNavigator().Value,
            SelectorType.Link => targetNode.OriginalName switch
            {
                "a" => targetNode.GetAttributeValue("href", null),
                "img" => targetNode.GetAttributeValue("src", null),
                _ => targetNode.CreateNavigator().Value,
            },
            SelectorType.Html => targetNode.InnerHtml,
            _ => throw new NotImplementedException($"This selector type: {type} is not implemented (CssElementHandler)"),
        };
    }

    [GeneratedRegex("((?:https?:/)?/[-a-zA-Z0-9+&@#/%?=~_|!:, .;]*[-a-zA-Z0-9+&@#/%=~_|])")]
    private static partial Regex CleanLinkRegex();
}

public static class PathExtensions
{
    public static Path AsPath(this string str) => new Path(str);
}