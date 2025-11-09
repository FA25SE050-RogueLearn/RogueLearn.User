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

            // 1. Remove script and style nodes to reduce noise.
            htmlDoc.DocumentNode.SelectNodes("//script|//style")?.ToList().ForEach(n => n.Remove());

            // 2. Find the most relevant content node.
            var mainContentNode = htmlDoc.GetElementbyId("content")
                                  ?? htmlDoc.DocumentNode.SelectSingleNode("//body")
                                  ?? htmlDoc.DocumentNode;

            if (mainContentNode == null) return rawHtml;

            var sb = new StringBuilder();

            // 3. Extract text from key structural elements.
            ExtractAndAppendSection(sb, mainContentNode, "Curriculum Details", "//table[@id='table-detail']");
            ExtractAndAppendSection(sb, mainContentNode, "Subjects List", "//table[@id='gvSubs']");

            // Add a more generic table extractor as a fallback
            if (sb.Length == 0)
            {
                _logger.LogDebug("Specific tables not found, attempting generic extraction.");
                ExtractAndAppendSection(sb, mainContentNode, "General Information", "//*[self::h1 or self::h2 or self::h3 or self::p or self::table]");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during HTML cleaning. Returning raw text as a fallback.");
            return rawHtml;
        }
    }

    private void ExtractAndAppendSection(StringBuilder sb, HtmlNode parentNode, string sectionTitle, string xpath)
    {
        var nodes = parentNode.SelectNodes(xpath);
        if (nodes == null || !nodes.Any()) return;

        sb.AppendLine($"## {sectionTitle}");
        sb.AppendLine();

        foreach (var node in nodes)
        {
            if (node.Name == "table")
            {
                foreach (var row in node.SelectNodes(".//tr"))
                {
                    var cells = row.SelectNodes(".//th|.//td");
                    if (cells != null)
                    {
                        var rowText = string.Join(" | ", cells.Select(c => Sanitize(c.InnerText)));
                        sb.AppendLine(rowText);
                    }
                }
            }
            else
            {
                sb.AppendLine(Sanitize(node.InnerText));
            }
            sb.AppendLine();
        }
    }

    private string Sanitize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        // Decode HTML entities and normalize whitespace to a single space.
        return System.Text.RegularExpressions.Regex.Replace(HtmlEntity.DeEntitize(text).Trim(), @"\s+", " ");
    }
}