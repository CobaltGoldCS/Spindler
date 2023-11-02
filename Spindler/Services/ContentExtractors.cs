using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Spindler.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Path = Spindler.Models.Path;

namespace Spindler.Services;
public abstract partial class BaseContentExtractor
{
    /// <summary>
    /// Get Relevant Content from HtmlDocument
    /// </summary>
    /// <param name="nav">The document to analyze</param>
    /// <param name="config">Configuration information for the HTML content</param>
    /// <returns>Main Content according to Config and Document</returns>
    public abstract string GetContent(HtmlDocument nav, Config config, ConfigService service);
    protected static HashSet<string> badTags = new() { "script", "link", "meta", "style", "img", "video", "track" };

    protected string ExtractChildText(HtmlNode node, Config config)
    {
        StringWriter stringWriter = new();

        string? desiredName = null;

        foreach (HtmlNode child in node.ChildNodes)
        {
            if (badTags.Contains(child.OriginalName))
            {
                continue;
            }

            desiredName ??= child.OriginalName;
            // "Smart" Filter certain tags together.
            if (config.FilteringContentEnabled && child.OriginalName != desiredName)
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
            stringWriter.Write($"{HttpUtility.HtmlDecode(child.InnerText.Trim()).Replace("\n", config.Separator)}");
            stringWriter.Write(config.Separator);
        }
        return stringWriter.ToString().Trim();
    }

    [GeneratedRegex("^\\s+$", RegexOptions.Multiline)]
    protected static partial Regex WhitespaceOnlyRegex();
}


public class HtmlContentExtractor : BaseContentExtractor
{
    public override string GetContent(HtmlDocument nav, Config config, ConfigService service)
    {
        Path contentPath = service.GetPath(ConfigService.Selector.Content);

        HtmlNode node = contentPath.type switch
        {
            Path.Type.Css => nav.QuerySelector(contentPath.PathString),
            Path.Type.XPath => nav.DocumentNode.SelectSingleNode(contentPath.PathString),
            _ => throw new NotImplementedException("This path type has not been implemented {ConfigService.GetContent}"),
        };

        if (node == null) return string.Empty;
        if (!node.HasChildNodes)
        {
            return HttpUtility.HtmlDecode(node.InnerHtml).Replace("\n", config.Separator).Trim();
        }

        return ExtractChildText(node, config);
    }
}

public class TextContentExtractor : BaseContentExtractor
{
    public override string GetContent(HtmlDocument nav, Config config, ConfigService service)
    {
        Path contentPath = service.GetPath(ConfigService.Selector.Content);
        HtmlNode node = contentPath.type switch
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
