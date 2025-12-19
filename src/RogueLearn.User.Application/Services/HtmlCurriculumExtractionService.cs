// RogueLearn.User/src/RogueLearn.User.Infrastructure/Services/HtmlCurriculumExtractionService.cs
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins; // Using ICurriculumExtractionPlugin interface
using RogueLearn.User.Domain.Enums;
using System.Text.Json;
using System.Web;

namespace RogueLearn.User.Infrastructure.Services;

/// <summary>
/// Extracts curriculum data directly from FLM HTML using HtmlAgilityPack and Regex.
/// Replaces the AI-based extraction for higher reliability and speed.
/// </summary>
public class HtmlCurriculumExtractionService : ICurriculumExtractionPlugin
{
    private readonly ILogger<HtmlCurriculumExtractionService> _logger;

    public HtmlCurriculumExtractionService(ILogger<HtmlCurriculumExtractionService> logger)
    {
        _logger = logger;
    }

    public Task<string> ExtractCurriculumJsonAsync(string rawHtml, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting HTML-based curriculum extraction.");

        var doc = new HtmlDocument();
        doc.LoadHtml(rawHtml);

        var data = new CurriculumImportData
        {
            Program = ExtractProgramData(doc),
            Version = ExtractVersionData(doc),
            Subjects = new List<SubjectData>(),
            Structure = new List<CurriculumStructureData>()
        };

        // Extract Subjects Table
        var subjects = ExtractSubjects(doc);
        data.Subjects = subjects.Select(s => s.Subject).ToList();
        data.Structure = subjects.Select(s => s.Structure).ToList();

        // Serialize to JSON to match the interface contract
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        return Task.FromResult(json);
    }

    private CurriculumProgramData ExtractProgramData(HtmlDocument doc)
    {
        var programData = new CurriculumProgramData
        {
            DegreeLevel = DegreeLevel.Bachelor // Default for FLM
        };

        // Find the "Curriculum Details" table by looking for the H1 or just finding the first table with CurriculumCode
        // The provided HTML has <h1>Curriculum Details</h1> followed by a <table>
        var detailsTable = doc.DocumentNode.SelectSingleNode("//h1[normalize-space(text())='Curriculum Details']/following-sibling::table[1]");

        if (detailsTable == null)
        {
            // Fallback: look for any table containing "CurriculumCode"
            detailsTable = doc.DocumentNode.SelectSingleNode("//table[.//td[contains(text(), 'CurriculumCode')]]");
        }

        if (detailsTable == null)
        {
            _logger.LogWarning("Curriculum Details table not found in HTML.");
            return programData;
        }

        foreach (var row in detailsTable.SelectNodes(".//tr") ?? new HtmlNodeCollection(null))
        {
            var cells = row.SelectNodes("td");
            if (cells == null || cells.Count < 2) continue;

            var key = HttpUtility.HtmlDecode(cells[0].InnerText).Trim().TrimEnd(':');
            var value = HttpUtility.HtmlDecode(cells[1].InnerText).Trim();

            if (string.Equals(key, "CurriculumCode", StringComparison.OrdinalIgnoreCase))
            {
                programData.ProgramCode = value;
            }
            else if (string.Equals(key, "English Name", StringComparison.OrdinalIgnoreCase))
            {
                programData.ProgramName = value;
            }
            else if (string.Equals(key, "Name", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(programData.ProgramName))
            {
                // Fallback to mixed name if English Name hasn't been set yet
                programData.ProgramName = value;
            }
            else if (string.Equals(key, "Description", StringComparison.OrdinalIgnoreCase))
            {
                programData.Description = value;

                // Try to extract Total Credits from description if present
                // Pattern: "145 credits" or "145 tín chỉ"
                var creditMatch = Regex.Match(value, @"(\d+)\s+(?:credits|tín chỉ)", RegexOptions.IgnoreCase);
                if (creditMatch.Success && int.TryParse(creditMatch.Groups[1].Value, out int credits))
                {
                    programData.TotalCredits = credits;
                }
            }
        }

        return programData;
    }

    private CurriculumVersionData ExtractVersionData(HtmlDocument doc)
    {
        var versionData = new CurriculumVersionData
        {
            IsActive = true,
            EffectiveYear = DateTime.UtcNow.Year // Default
        };

        var detailsTable = doc.DocumentNode.SelectSingleNode("//table[.//td[contains(text(), 'CurriculumCode')]]");
        if (detailsTable != null)
        {
            // Try to find DecisionNo for date
            // Example: 1210/QĐ-ĐHFPT dated 10/30/2025
            var decisionRow = detailsTable.SelectSingleNode(".//tr[td[contains(text(), 'DecisionNo')]]");
            if (decisionRow != null)
            {
                var cells = decisionRow.SelectNodes("td");
                if (cells != null && cells.Count >= 2)
                {
                    var text = HttpUtility.HtmlDecode(cells[1].InnerText).Trim();
                    // Match MM/dd/yyyy format from FLM
                    var match = Regex.Match(text, @"dated\s+(\d{1,2}/\d{1,2}/\d{4})");
                    if (match.Success && DateTime.TryParse(match.Groups[1].Value, out var date))
                    {
                        versionData.EffectiveYear = date.Year;
                        versionData.VersionCode = $"{date:yyyy-MM-dd}";
                    }
                    else
                    {
                        versionData.VersionCode = $"{DateTime.UtcNow:yyyy-MM-dd}";
                    }
                    versionData.Description = text;
                }
            }
        }

        if (string.IsNullOrEmpty(versionData.VersionCode))
        {
            versionData.VersionCode = "Initial";
        }

        return versionData;
    }

    private List<(SubjectData Subject, CurriculumStructureData Structure)> ExtractSubjects(HtmlDocument doc)
    {
        var results = new List<(SubjectData, CurriculumStructureData)>();

        // Find the table that contains "SubjectCode" in header
        var subjectTable = doc.DocumentNode.SelectSingleNode("//table[.//th[contains(text(), 'SubjectCode')]]");

        if (subjectTable == null)
        {
            _logger.LogError("Subject list table not found in HTML.");
            return results;
        }

        var rows = subjectTable.SelectNodes(".//tr");
        if (rows == null) return results;

        // Skip the header row
        foreach (var row in rows.Skip(1))
        {
            var cells = row.SelectNodes("td");
            if (cells == null || cells.Count < 5) continue;

            var code = HttpUtility.HtmlDecode(cells[0].InnerText).Trim();

            // FILTER: Ignore placeholder subjects (SE_COM...) as requested
            if (code.StartsWith("SE_COM", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (code.StartsWith("PHE_COM", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (code.StartsWith("OTP101", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (code.StartsWith("PEN", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (code.StartsWith("TMI_ELE", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // The name is often inside an <a> tag
            var nameNode = cells[1].SelectSingleNode(".//a");
            var rawName = HttpUtility.HtmlDecode(nameNode != null ? nameNode.InnerText : cells[1].InnerText).Trim();

            // Clean name: "Subject English_Subject Vietnamese" -> "Subject English"
            var cleanName = rawName;
            if (cleanName.Contains('_'))
            {
                cleanName = cleanName.Split('_')[0];
            }

            // Parse numeric fields safely
            int.TryParse(HttpUtility.HtmlDecode(cells[2].InnerText).Trim(), out int semester);
            int.TryParse(HttpUtility.HtmlDecode(cells[3].InnerText).Trim(), out int credits);

            var prereqRaw = HttpUtility.HtmlDecode(cells[4].InnerText).Trim();

            var subject = new SubjectData
            {
                SubjectCode = code,
                SubjectName = cleanName,
                Credits = credits,
                Description = rawName // Store full bilingual name in description for reference
            };

            var structure = new CurriculumStructureData
            {
                SubjectCode = code,
                TermNumber = semester,
                IsMandatory = true, // Default to true
                PrerequisitesText = prereqRaw
            };

            // Parse Prerequisites: "PRF192" or "SWE102 or SWE201c"
            // We want to extract valid codes (3 letters + 3 digits)
            if (!string.IsNullOrWhiteSpace(prereqRaw) && !prereqRaw.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                var matches = Regex.Matches(prereqRaw, @"[A-Z]{3}\d{3}[a-z]?");
                if (matches.Count > 0)
                {
                    structure.PrerequisiteSubjectCodes = matches.Select(m => m.Value).Distinct().ToList();
                }
            }

            results.Add((subject, structure));
        }

        _logger.LogInformation("Extracted {Count} valid subjects from HTML.", results.Count);
        return results;
    }
}