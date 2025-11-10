// RogueLearn.User/src/RogueLearn.User.Infrastructure/Services/HtmlCleaningService.cs
using System.Text;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Infrastructure.Services;

/// <summary>
/// Implements HTML cleaning using HtmlAgilityPack to prepare text for AI processing.
/// </summary>
public class HtmlCleaningService : IHtmlCleaningService
{
    private readonly ILogger<HtmlCleaningService> _logger;

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
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(rawHtml);

            // 1. Remove script and style nodes to eliminate noise.
            htmlDoc.DocumentNode.SelectNodes("//script|//style")?.ToList().ForEach(n => n.Remove());

            // 2. Find the most relevant content node to focus extraction.
            // This prioritizes common content containers but falls back to the body or whole document.
            var mainContentNode = htmlDoc.GetElementbyId("content")
                                  ?? htmlDoc.DocumentNode.SelectSingleNode("//main")
                                  ?? htmlDoc.DocumentNode.SelectSingleNode("//body")
                                  ?? htmlDoc.DocumentNode;

            if (mainContentNode == null) return rawHtml;

            var sb = new StringBuilder();

            // 3. Extract text from key structural elements to preserve some structure for the AI.
            // This is more effective than just taking the entire inner text.
            foreach (var node in mainContentNode.Descendants())
            {
                // Only process text nodes that are direct children of block-level elements
                if (node.NodeType == HtmlNodeType.Text && IsBlockLevelParent(node.ParentNode))
                {
                    var text = Sanitize(node.InnerText);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        sb.AppendLine(text);
                    }
                }
                // Handle tables by converting them to a simple text format
                else if (node.Name == "table")
                {
                    ExtractTableContent(sb, node);
                }
            }

            // As a final fallback if the above yields nothing, use the full inner text.
            if (sb.Length == 0)
            {
                _logger.LogDebug("Structured text extraction yielded no content, falling back to full InnerText.");
                return Sanitize(mainContentNode.InnerText);
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during HTML cleaning. Returning raw text as a fallback.");
            return rawHtml;
        }
    }

    private void ExtractTableContent(StringBuilder sb, HtmlNode tableNode)
    {
        var rows = tableNode.SelectNodes(".//tr");
        if (rows == null) return;

        sb.AppendLine("--- TABLE START ---");
        foreach (var row in rows)
        {
            var cells = row.SelectNodes(".//th|.//td");
            if (cells != null)
            {
                var rowText = string.Join(" | ", cells.Select(c => Sanitize(c.InnerText)));
                sb.AppendLine(rowText);
            }
        }
        sb.AppendLine("--- TABLE END ---");
    }

    private bool IsBlockLevelParent(HtmlNode node)
    {
        if (node == null) return false;
        string[] blockTags = { "p", "h1", "h2", "h3", "h4", "h5", "h6", "div", "li", "td", "th", "blockquote" };
        return blockTags.Contains(node.Name);
    }

    private string Sanitize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        // Decode HTML entities (e.g., &amp; -> &) and normalize all whitespace to a single space.
        return System.Text.RegularExpressions.Regex.Replace(HtmlEntity.DeEntitize(text).Trim(), @"\s+", " ");
    }
}