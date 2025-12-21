using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Infrastructure.Services;

/// <summary>
/// Implements strict HTML cleaning to reduce payload size and remove noise for AI processing.
/// Converts ASP.NET/Legacy HTML into clean, semantic HTML5.
/// </summary>
public class HtmlCleaningService : IHtmlCleaningService
{
    private readonly ILogger<HtmlCleaningService> _logger;

    // Allowed tags based on requirements
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "body", "h1", "h2", "h3", "h4", "h5", "h6", "head", "hr", "html", "i", "img",
        "li", "ol", "p", "ruby", "strong", "table", "tbody", "td", "th", "title", "tr", "ul",
        "em", "br"
    };

    // Allowed attributes per tag
    private static readonly Dictionary<string, HashSet<string>> AllowedAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "a", new HashSet<string> { "href" } },
        { "img", new HashSet<string> { "src", "width", "height", "alt" } },
        { "td", new HashSet<string> { "colspan", "rowspan" } },
        { "th", new HashSet<string> { "colspan", "rowspan" } }
    };

    public HtmlCleaningService(ILogger<HtmlCleaningService> logger)
    {
        _logger = logger;
    }

    public string ExtractCleanTextFromHtml(string rawHtml)
    {
        if (string.IsNullOrWhiteSpace(rawHtml))
        {
            return string.Empty;
        }

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(rawHtml);

            // 1. Pre-cleaning: Remove noise nodes entirely
            RemoveUnwantedNodes(doc.DocumentNode);

            // 2. Recursive Sanitization: Rename tags, strip attributes, unwrap unknown tags
            SanitizeNode(doc.DocumentNode);

            // 3. Post-processing: Formatting and cleanup
            var bodyNode = doc.DocumentNode.SelectSingleNode("//body") ?? doc.DocumentNode;

            var result = bodyNode.OuterHtml;

            result = Regex.Replace(result, @"\n\s*\n", "\n");
            result = Regex.Replace(result, @">\s+<", "><");

            return result.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during HTML cleaning. Returning empty string.");
            return string.Empty;
        }
    }

    private void RemoveUnwantedNodes(HtmlNode node)
    {
        // 1. Handle FORM tags specially: Unwrap them (keep content), don't delete them
        var formNodes = node.SelectNodes("//form");
        if (formNodes != null)
        {
            foreach (var form in formNodes)
            {
                UnwrapNode(form);
            }
        }

        // 2. Select nodes that are definitely trash to delete completely
        var nodesToRemove = node.SelectNodes("//script|//style|//link|//meta|//input|//comment()|//iframe|//svg|//noscript|//button|//select|//option");

        if (nodesToRemove != null)
        {
            foreach (var n in nodesToRemove)
            {
                n.Remove();
            }
        }

        // 3. Remove elements with class 'aspNetHidden' explicitly if they survived
        var hiddenNodes = node.SelectNodes("//*[contains(@class, 'aspNetHidden')]");
        if (hiddenNodes != null)
        {
            foreach (var n in hiddenNodes)
            {
                n.Remove();
            }
        }
    }

    private void SanitizeNode(HtmlNode node)
    {
        // Iterate backwards to allow modification of the collection
        for (int i = node.ChildNodes.Count - 1; i >= 0; i--)
        {
            SanitizeNode(node.ChildNodes[i]);
        }

        // --- Tag Logic ---

        if (node.NodeType == HtmlNodeType.Text)
        {
            if (string.IsNullOrWhiteSpace(node.InnerText))
            {
                node.Remove();
            }
            return;
        }

        if (node.NodeType == HtmlNodeType.Element)
        {
            var tagName = node.Name.ToLowerInvariant();

            if (tagName == "b")
            {
                node.Name = "strong";
                tagName = "strong";
            }
            else if (tagName == "div")
            {
                // Unwrap DIVs that only contain other block elements to flatten structure
                // Otherwise convert to P
                if (HasBlockChildren(node))
                {
                    UnwrapNode(node);
                    return;
                }

                node.Name = "p";
                tagName = "p";
            }
            else if (tagName == "span" || tagName == "font" || tagName == "center")
            {
                // Unwrap presentational tags
                UnwrapNode(node);
                return; // Node is gone, stop processing it
            }

            // Whitelist Check
            if (!AllowedTags.Contains(tagName))
            {
                // If tag is not allowed, unwrap it (keep content, remove tags)
                UnwrapNode(node);
                return;
            }

            // --- Attribute Logic ---

            if (node.HasAttributes)
            {
                // Get list of attributes to remove (cannot modify collection while iterating)
                var attributesToRemove = new List<string>();

                foreach (var attr in node.Attributes)
                {
                    bool keep = false;
                    if (AllowedAttributes.ContainsKey(tagName))
                    {
                        if (AllowedAttributes[tagName].Contains(attr.Name.ToLowerInvariant()))
                        {
                            keep = true;
                            // Special Logic: Ensure alt exists for img
                            if (tagName == "img" && attr.Name == "alt" && string.IsNullOrWhiteSpace(attr.Value))
                            {
                                attr.Value = "image";
                            }
                        }
                    }

                    if (!keep)
                    {
                        attributesToRemove.Add(attr.Name);
                    }
                }

                foreach (var attrName in attributesToRemove)
                {
                    node.Attributes.Remove(attrName);
                }

                // Add missing required attributes
                if (tagName == "img" && !node.Attributes.Contains("alt"))
                {
                    node.Attributes.Add("alt", "image");
                }
            }
        }
    }

    private void UnwrapNode(HtmlNode node)
    {
        var parent = node.ParentNode;
        if (parent == null) return;

        foreach (var child in node.ChildNodes.ToList())
        {
            parent.InsertBefore(child, node);
        }

        parent.RemoveChild(node);
    }

    private bool HasBlockChildren(HtmlNode node)
    {
        var blockTags = new HashSet<string> { "p", "div", "table", "ul", "ol", "h1", "h2", "h3", "h4", "h5", "h6" };
        return node.ChildNodes.Any(c => c.NodeType == HtmlNodeType.Element && blockTags.Contains(c.Name));
    }
}