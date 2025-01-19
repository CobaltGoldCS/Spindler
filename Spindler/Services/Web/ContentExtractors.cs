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
    public abstract string GetContent(HtmlDocument nav, Config config, ConfigService service);

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
    protected virtual string ExtractChildText(HtmlNode node, Config config)
    {
        StringWriter stringWriter = new();


        foreach (HtmlNode child in node.ChildNodes)
        {
            if (config.FilteringContentEnabled && BadTags.Contains(child.OriginalName))
            {
                continue;
            }

            string innerText = WhitespaceOnlyRegex().Replace(HttpUtility.HtmlDecode(child.InnerText), string.Empty);
            if (innerText.Length == 0)
            {
                if (child.OriginalName == "br" && child.NextSibling?.OriginalName != "br")
                {
                    stringWriter.Write(config.Separator);
                }
                continue;
            }
            stringWriter.Write($"{HttpUtility.HtmlDecode(child.InnerText).Replace("\n", config.Separator)}");
            stringWriter.Write(config.Separator);
        }
        return stringWriter.ToString().Trim();
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
    protected override string ExtractChildText(HtmlNode node, Config config)
    {
        string htmlSeparator = config.Separator.Replace("\n", "<br>").Replace("\t", "&#9;");
        StringBuilder builder = new();

        foreach (HtmlNode child in node.ChildNodes)
        {
            if (config.FilteringContentEnabled && BadTags.Contains(child.OriginalName))
            {
                continue;
            }

            builder.Append(child.InnerHtml);
            builder.Append(htmlSeparator);
        }
        return builder.ToString();
    }
}


/// <summary>
/// Selects the html of the path
/// </summary>
public class HtmlContentExtractor : HtmlExtractor
{
    public override string GetContent(HtmlDocument nav, Config config, ConfigService service)
    {
        Path contentPath = service.GetPath(ConfigService.Selector.Content);

        HtmlNode node = contentPath.PathType switch
        {
            Path.Type.Css => nav.QuerySelector(contentPath.PathString),
            Path.Type.XPath => nav.DocumentNode.SelectSingleNode(contentPath.PathString),
            _ => throw new NotImplementedException("This path type has not been implemented {ConfigService.GetContent}"),
        };

        string htmlSeparator = config.Separator.Replace("\n", "<br>").Replace("\t", "&#9;");

        if (node == null) return string.Empty;
        if (!node.HasChildNodes)
        {
            return HttpUtility.HtmlDecode(node.InnerHtml).Replace("\n", htmlSeparator);
        }

        return ExtractChildText(node, config);
    }
}

/// <summary>
/// Selects the Text of the matched tag and sanitizes it
/// </summary>
public class TextContentExtractor : BaseContentExtractor
{
    public override string GetContent(HtmlDocument nav, Config config, ConfigService service)
    {
        Path contentPath = service.GetPath(ConfigService.Selector.Content);
        HtmlNode node = contentPath.PathType switch
        {
            Path.Type.Css => nav.QuerySelector(contentPath.PathString),
            Path.Type.XPath => nav.DocumentNode.SelectSingleNode(contentPath.PathString),
            _ => throw new NotImplementedException("This path type has not been implemented {ConfigService.GetContent}"),
        };

        if (node == null) return string.Empty;
        if (!node.HasChildNodes)
        {
            return HttpUtility.HtmlDecode(node.InnerText).Replace("\n", config.Separator).Trim();
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
    public override string GetContent(HtmlDocument nav, Config config, ConfigService service)
    {
        Path contentPath = service.GetPath(ConfigService.Selector.Content);
        IEnumerable<HtmlNode> nodes = contentPath.PathType switch
        {
            Path.Type.Css => nav.QuerySelectorAll(contentPath.PathString),
            Path.Type.XPath => nav.DocumentNode.SelectNodes(contentPath.PathString),
            _ => throw new NotImplementedException("This path type has not been implemented {ConfigService.GetContent}"),
        };

        StringBuilder builder = new();

        foreach (HtmlNode node in nodes)
        {
            if (node == null) continue;

            if (!node.HasChildNodes)
            {
                builder.Append(HttpUtility.HtmlDecode(node.InnerText).Replace("\n", config.Separator).Trim());
            }
            else
            {
                builder.Append(ExtractChildText(node, config));
            }
            builder.Append(config.Separator);
        }

        return builder.ToString();
    }
}
