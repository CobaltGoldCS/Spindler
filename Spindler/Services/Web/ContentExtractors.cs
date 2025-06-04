using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Spindler.Models;
using Spindler.Services.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Path = Spindler.Models.Path;

namespace Spindler.Services;

public enum TargetType
{
    Text,
    Html,
    All_Tags_Matching_Path
}
public abstract partial class BaseContentExtractor
{
    /// <summary>
    /// Get Relevant Content from HtmlDocument
    /// </summary>
    /// <param name="nav">The document to analyze</param>
    /// <param name="config">Configuration information for the HTML content</param>
    /// <returns>Main Content according to Config and Document</returns>
    public abstract IEnumerable<string> GetContent(HtmlDocument nav, Config config, ConfigService service);

    /// <summary>
    /// Tags that should not be included (Generally)
    /// </summary>
    protected static readonly HashSet<string> BadTags = ["script", "link", "meta", "style", "img", "video", "track"];

    /// <summary>
    /// Extract the child text from every child node of <see cref="HtmlNode"/>
    /// </summary>
    /// <param name="node">The parent node to extract child text from</param>
    /// <param name="config">The configuration options to use for extracting text</param>
    /// <returns>A string containing the sanitized, formatted, and extracted child text</returns>
    protected virtual IEnumerable<string> ExtractChildText(HtmlNode node, Config config)
    {
        List<string> stringWriter = [];


        foreach (HtmlNode child in node.ChildNodes)
        {
            if (config.FilteringContentEnabled && BadTags.Contains(child.OriginalName))
            {
                continue;
            }

            string innerText = WhitespaceOnlyRegex().Replace(HttpUtility.HtmlDecode(child.InnerText), string.Empty);
            if (innerText.Length == 0)
            {
                continue;
            }
            stringWriter.AddRange(HttpUtility.HtmlDecode(child.InnerText).Split("\n").Where(str => str.Length > 0));
        }
        return stringWriter;
    }

    [GeneratedRegex("^\\s+$", RegexOptions.Multiline)]
    protected static partial Regex WhitespaceOnlyRegex();
}

public abstract class HtmlExtractor : BaseContentExtractor
{
    /// <summary>
    /// Extract the child text in html format of each child of <paramref name="node"/>
    /// </summary>
    /// <param name="node">The parent node to extract text from</param>
    /// <param name="config">Configuration information for targetting relevant text</param>
    /// <returns>Sanitized / formatted child text in HTML format</returns>
    protected override IEnumerable<string> ExtractChildText(HtmlNode node, Config config)
    {
        List<string> builder = [];

        foreach (HtmlNode child in node.ChildNodes)
        {
            if (config.FilteringContentEnabled && BadTags.Contains(child.OriginalName))
            {
                continue;
            }

            builder.AddRange(child.InnerHtml.Split("\n").Where(str => str.Length > 0));
        }
        return builder;
    }
}


/// <summary>
/// Selects the html of the path
/// </summary>
public class HtmlContentExtractor : HtmlExtractor
{
    public override IEnumerable<string> GetContent(HtmlDocument nav, Config config, ConfigService service)
    {
        Path contentPath = service.GetPath(ConfigService.Selector.Content);

        HtmlNode node = contentPath.PathType switch
        {
            Path.Type.Css => nav.QuerySelector(contentPath.PathString),
            Path.Type.XPath => nav.DocumentNode.SelectSingleNode(contentPath.PathString),
            _ => throw new NotImplementedException("This path type has not been implemented {ConfigService.GetContent}"),
        };

        if (node == null) return [];
        if (!node.HasChildNodes)
        {
            return HttpUtility.HtmlDecode(node.InnerHtml).Split("\n").Where(str =>  str.Length > 0);
        }

        return ExtractChildText(node, config);
    }
}

/// <summary>
/// Selects the Text of the matched tag and sanitizes it
/// </summary>
public class TextContentExtractor : BaseContentExtractor
{
    public override IEnumerable<string> GetContent(HtmlDocument nav, Config config, ConfigService service)
    {
        Path contentPath = service.GetPath(ConfigService.Selector.Content);
        HtmlNode node = contentPath.PathType switch
        {
            Path.Type.Css => nav.QuerySelector(contentPath.PathString),
            Path.Type.XPath => nav.DocumentNode.SelectSingleNode(contentPath.PathString),
            _ => throw new NotImplementedException("This path type has not been implemented {ConfigService.GetContent}"),
        };

        if (node == null) return [];
        if (!node.HasChildNodes)
        {
            return HttpUtility.HtmlDecode(node.InnerHtml).Split("\n").Where(str => str.Length > 0);
        }

        return ExtractChildText(node, config);
    }
}

/// <summary>
/// This selector selects all tags that match the path, and concatenates
/// their string content into the full content.
/// </summary>
public class AllTagsContentExtractor : BaseContentExtractor
{
    public override IEnumerable<string> GetContent(HtmlDocument nav, Config config, ConfigService service)
    {
        Path contentPath = service.GetPath(ConfigService.Selector.Content);
        IEnumerable<HtmlNode> nodes = contentPath.PathType switch
        {
            Path.Type.Css => nav.QuerySelectorAll(contentPath.PathString),
            Path.Type.XPath => nav.DocumentNode.SelectNodes(contentPath.PathString),
            _ => throw new NotImplementedException("This path type has not been implemented {ConfigService.GetContent}"),
        };

        List<string> builder = [];

        foreach (HtmlNode node in nodes)
        {
            if (node == null) continue;

            if (!node.HasChildNodes)
            {
                builder.AddRange(HttpUtility.HtmlDecode(node.InnerHtml).Split("\n").Where(str => str.Length > 0));
            }
            else
            {
                builder.AddRange(ExtractChildText(node, config).Where(str => str.Length > 0));
            }
        }

        return builder;
    }
}
