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

            // ARCHITECTURAL REFINEMENT: Instead of cleaning the whole document,
            // we now use a specific XPath to target ONLY the grade report table.
            // This is more robust and drastically reduces the text sent to the AI, preventing timeouts.
            var gradeTableNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@id='ctl00_mainContent_divGrade']//table");

            if (gradeTableNode == null)
            {
                _logger.LogWarning("Could not find the main grade report table node ('ctl00_mainContent_divGrade'). Falling back to body text.");
                // Fallback to the old method if the specific table isn't found.
                return ExtractFromGeneralNode(htmlDoc.DocumentNode.SelectSingleNode("//body") ?? htmlDoc.DocumentNode);
            }

            _logger.LogInformation("Successfully isolated the grade report table. Extracting its text content.");
            var sb = new StringBuilder();
            ExtractTableContent(sb, gradeTableNode);

            var result = sb.ToString();
            if (string.IsNullOrWhiteSpace(result))
            {
                _logger.LogWarning("Targeted table extraction yielded no content. Falling back to full inner text of the table node.");
                return Sanitize(gradeTableNode.InnerText);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during HTML cleaning. Returning raw text as a fallback.");
            return rawHtml;
        }
    }

    // This is the old, less precise method, now used as a fallback.
    private string ExtractFromGeneralNode(HtmlNode mainContentNode)
    {
        var sb = new StringBuilder();
        foreach (var node in mainContentNode.Descendants())
        {
            if (node.NodeType == HtmlNodeType.Text && IsBlockLevelParent(node.ParentNode))
            {
                var text = Sanitize(node.InnerText);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.AppendLine(text);
                }
            }
            else if (node.Name == "table")
            {
                ExtractTableContent(sb, node);
            }
        }
        if (sb.Length == 0)
        {
            return Sanitize(mainContentNode.InnerText);
        }
        return sb.ToString();
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
                // Join cell text with a clear separator for the AI to parse.
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