using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.XPath;

namespace Spindler.Models;

/// <summary>
/// Dataclass representing a selector path, such as an xpath or csspath
/// </summary>
public partial class SelectorPath
{
    public string PathString;
    public Type PathType { get; private set; }

    private readonly IHTMLSelector InternalSelector;

    public enum Type
    {
        Css,
        XPath
    }

    public SelectorPath(string pathString)
    {
        PathString = pathString;
        PathType = PathString.StartsWith('/') ? Type.XPath : Type.Css;

        InternalSelector = PathType switch
        {
            Type.Css => new CSSPathSelector(),
            Type.XPath => new XPathSelector(),
            _ => throw new NotImplementedException("This selectortype is not implemented")
        };
    }

    public string Select(string html, SelectorType type)
    {
        HtmlDocument doc = new();
        doc.LoadHtml(html);
        string? selectedItems = InternalSelect(doc, type);
        return HttpUtility.HtmlDecode(selectedItems) ?? string.Empty;
    }
    public string Select(HtmlDocument nav, SelectorType type)
    {
        string? selectedItems = InternalSelect(nav, type);
        return HttpUtility.HtmlDecode(selectedItems) ?? string.Empty;
    }
    public bool IsValid()
    {
        if (PathString is null || PathString.Length == 0) return false;
        try
        {
            _ = Select("""<HTML></HTML>""", SelectorType.Text);
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


    protected static (string path, string modifier) SplitModifierAndPath(SelectorPath path)
    {
        MatchCollection attributes = CustomSyntax().Matches(path.PathString);
        if (attributes.Count == 0)
            return (path.PathString, string.Empty);
        var cleanpath = attributes[0].Groups[1].Value;
        var modifier = attributes[0].Groups[2].Value;
        return (cleanpath, modifier);
    }

    private string? InternalSelect(HtmlDocument nav, SelectorType type)
    {
        // Custom $ Syntax
        var attributes = SplitModifierAndPath(this);
        if (attributes.modifier != string.Empty)
        {
            var node = InternalSelector.SelectMatch(nav, this);
            if (node is null) return null;
            if (attributes.modifier == "text")
            {
                return node?.InnerText;
            }
            var value = node?.GetAttributeValue(attributes.modifier, null);
            if (type == SelectorType.Link && attributes.modifier != "href" && value != null)
            {
                value = CleanLinkRegex().Match(value).Value;
            }
            return value;
        }

        HtmlNode? targetNode = InternalSelector.SelectMatch(nav, this);
        if (targetNode is null)
            return null;

        // Select Proper Attributes
        if (PathType == Type.XPath &&
            XPathSelectsValue().Count(PathString) != 0 &&
            type is SelectorType.Text)
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

    [GeneratedRegex("(.+) \\$(.+)")]
    private static partial Regex CustomSyntax();

    [GeneratedRegex("^.+/@.+$")]
    private static partial Regex XPathSelectsValue();

    [GeneratedRegex("((?:https?:/)?/[-a-zA-Z0-9+&@#/%?=~_|!:, .;]*[-a-zA-Z0-9+&@#/%=~_|])")]
    private static partial Regex CleanLinkRegex();
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
    public HtmlNode SelectMatch(HtmlDocument nav, SelectorPath path);

    public IList<HtmlNode> SelectAllMatches(HtmlDocument nav, SelectorPath path);

}

public class XPathSelector : IHTMLSelector
{

    public HtmlNode SelectMatch(HtmlDocument nav, SelectorPath path)
    {
        return nav.DocumentNode.SelectSingleNode(path.PathString);
    }

    public IList<HtmlNode> SelectAllMatches(HtmlDocument nav, SelectorPath path)
    {
        return nav.DocumentNode.SelectNodes(path.PathString);
    }
}


public class CSSPathSelector : IHTMLSelector
{

    public HtmlNode SelectMatch(HtmlDocument nav, SelectorPath path)
    {
        return nav.QuerySelector(path.PathString);
    }

    public IList<HtmlNode> SelectAllMatches(HtmlDocument nav, SelectorPath path)
    {
        return nav.QuerySelectorAll(path.PathString);
    }

}

public static class PathExtensions
{
    public static SelectorPath AsPath(this string str) => new(str);
}