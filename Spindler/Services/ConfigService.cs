﻿using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Spindler.Models;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.XPath;

namespace Spindler.Services
{
    using Path = Models.Path;
    public class ConfigService
    {
        
        public Path titlepath;
        public Path contentpath;
        public Path nextpath;
        public Path previouspath;

        public ConfigService(Config config)
        {
            titlepath = new Path(config.TitlePath);
            contentpath = new Path(config.ContentPath);
            nextpath = new Path(config.NextUrlPath);
            previouspath = new Path(config.PrevUrlPath);
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

        /// <summary>
        /// Naively Checks if a selector is of valid syntax
        /// </summary>
        /// <param name="path">The selector to test (string)</param>
        /// <returns>If the selector is valid or not</returns>
        public static bool IsValidSelector(string path)
        {
            HtmlDocument nav = new HtmlDocument();
            var temppath = new Path(path);
            try
            {
                bool _ = temppath.type switch
                {
                    Path.Type.Css => CssElementHandler(nav, path, SelectorType.Link) != null,
                    Path.Type.XPath => XPathExpression.Compile(path) != null,
                    _ => throw new NotImplementedException("This path type has not been implemented")
                };
                return true;
            }
            catch (Exception e) when (
            e is XPathException ||
            e is ArgumentException ||
            e is ArgumentNullException ||
            e is InvalidOperationException)
            {
                return false;
            }
        }

        #region Selectors using Paths
        /// <summary>
        /// Attempt to get text from element pointed to by xpath
        /// </summary>
        /// <param name="nav">The HtmlDocument to get the text from</param>
        /// <param name="path">A string representation of the target's xpath</param>
        /// <returns cref="string">A string containing the target text, or an empty string if nothing is found</returns>
        /// <exception cref="XPathException">If there is any error in the xpath</exception>
        public static string PrettyWrapSelector(HtmlDocument nav, Path path, SelectorType type)
        {
            try
            {
                string value = path.type switch
                {
                    Path.Type.XPath => nav.DocumentNode.SelectSingleNode(path.path)?.CreateNavigator().Value,
                    Path.Type.Css => CssElementHandler(nav, path.path, type),
                    _ => throw new NotImplementedException("This type is not implemented"),
                };
                return HttpUtility.HtmlDecode(value);
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
        public static string CssElementHandler(HtmlDocument nav, string path, SelectorType type)
        {

            MatchCollection attributes = Regex.Matches(path, "(.+) \\$(.+)");
            if (attributes.Any())
            {
                var cleanpath = attributes[0].Groups[1].Value;
                var modifier = attributes[0].Groups[2].Value;
                HtmlNode node = nav.QuerySelector(cleanpath);
                if (modifier == "text")
                {
                    return node.InnerText;
                }
                return node?.GetAttributeValue(modifier, null);
            }
            return type switch
            {
                SelectorType.Text => nav.QuerySelector(path)?.CreateNavigator().Value,
                SelectorType.Link => nav.QuerySelector(path)?.GetAttributeValue("href", null),
                _ => throw new NotImplementedException($"This type: {type} is not implemented"),
            };
        }

        /// <summary>
        /// Smart Get Content that matches given content path using xpath
        /// </summary>
        /// <param name="nav">The HtmlDocument to evaluate for matches</param>
        /// <returns>A String containing the text of the content matched by contentpath</returns>
        public string GetContent(HtmlDocument nav)
        {
            HtmlNode node = contentpath.type switch
            {
                Path.Type.Css => nav.QuerySelector(contentpath.path),
                Path.Type.XPath => nav.DocumentNode.SelectSingleNode(contentpath.path),
                _ => throw new NotImplementedException("This type is not implemented"),
            };

            if (node == null) return string.Empty;
            if (!node.HasChildNodes)
            {
                return HttpUtility.HtmlDecode(node.InnerText);
            }

            StringWriter stringWriter = new();
            foreach (HtmlNode child in node.ChildNodes)
            {
                if (child.OriginalName == "br")
                {
                    stringWriter.Write("\n");
                    continue;
                }
                stringWriter.WriteLine($"\t\t{HttpUtility.HtmlDecode(child.InnerText)}\n");
            }
            return stringWriter.ToString();
        }

        /// <summary>
        /// Get title from <paramref name="nav"/>
        /// </summary>
        /// <param name="nav">The document to get the title from</param>
        /// <returns>The title</returns>
        public string GetTitle(HtmlDocument nav)
        {
            if (string.IsNullOrWhiteSpace(titlepath.path))
                titlepath = new Path("//title");
            return HttpUtility.HtmlDecode(PrettyWrapSelector(nav, titlepath, type: SelectorType.Text));
        }
        #endregion
    }
}