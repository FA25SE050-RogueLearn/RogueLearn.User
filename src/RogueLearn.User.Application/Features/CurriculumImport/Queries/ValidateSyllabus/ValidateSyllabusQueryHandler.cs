// RogueLearn.User/src/RogueLearn.User.Application/Features/CurriculumImport/Queries/ValidateSyllabus/ValidateSyllabusQueryHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using HtmlAgilityPack;

namespace RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateSyllabus;

public class ValidateSyllabusQueryHandler : IRequestHandler<ValidateSyllabusQuery, ValidateSyllabusResponse>
{
    private readonly ICurriculumImportStorage _storage;
    private readonly FluentValidation.IValidator<SyllabusData> _validator;
    private readonly ILogger<ValidateSyllabusQueryHandler> _logger;
    // MODIFIED: Dependency changed from the obsolete IFlmExtractionPlugin to the new, specific plugin.
    private readonly ISyllabusExtractionPlugin _flmPlugin;

    public ValidateSyllabusQueryHandler(
        ICurriculumImportStorage storage,
        FluentValidation.IValidator<SyllabusData> validator,
        ILogger<ValidateSyllabusQueryHandler> logger,
        // MODIFIED: Constructor now requires the correct interface.
        ISyllabusExtractionPlugin flmPlugin)
    {
        _storage = storage;
        _validator = validator;
        _logger = logger;
        _flmPlugin = flmPlugin;
    }

    public async Task<ValidateSyllabusResponse> Handle(ValidateSyllabusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting syllabus validation from text");

            // Step 1: Check cache first
            var inputHash = ComputeSha256Hash(request.RawText);
            var cachedData = await TryGetCachedDataAsync(inputHash, cancellationToken);

            string extractedJson;
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.LogInformation("Using cached syllabus data for validation");
                extractedJson = cachedData;
            }
            else
            {
                // Step 2: Extract structured data using AI
                extractedJson = await ExtractSyllabusData(request.RawText, cancellationToken);
                if (string.IsNullOrEmpty(extractedJson))
                {
                    return new ValidateSyllabusResponse
                    {
                        IsValid = false,
                        Message = "Failed to extract syllabus data from the provided text",
                        ValidationErrors = new List<string> { "Failed to extract syllabus data from the provided text" }
                    };
                }
            }

            // Step 3: Parse JSON
            SyllabusData? syllabusData;
            try
            {
                syllabusData = JsonSerializer.Deserialize<SyllabusData>(extractedJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse extracted JSON");
                return new ValidateSyllabusResponse
                {
                    IsValid = false,
                    Message = "Failed to parse extracted syllabus data",
                    ValidationErrors = new List<string> { "Failed to parse extracted syllabus data" }
                };
            }

            if (syllabusData == null)
            {
                return new ValidateSyllabusResponse
                {
                    IsValid = false,
                    Message = "No syllabus data was extracted",
                    ValidationErrors = new List<string> { "No syllabus data was extracted" }
                };
            }

            // Step 4: Validate extracted data first
            var validationResult = await _validator.ValidateAsync(syllabusData, cancellationToken);

            var response = new ValidateSyllabusResponse
            {
                IsValid = validationResult.IsValid,
                ExtractedData = syllabusData,
                ValidationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
            };

            if (validationResult.IsValid)
            {
                // Only save to storage if validation passes
                if (!string.IsNullOrEmpty(syllabusData.SubjectCode))
                {
                    await SaveDataToStorageAsync(inputHash, extractedJson, syllabusData, cancellationToken);
                }
                response.Message = "Syllabus data is valid and ready for import";
            }
            else
            {
                response.Message = "Syllabus data validation failed";
            }

            _logger.LogInformation("Syllabus validation completed");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during syllabus validation");
            return new ValidateSyllabusResponse
            {
                IsValid = false,
                Message = "An error occurred during validation",
                ValidationErrors = new List<string> { "An error occurred during validation" }
            };
        }
    }

    private async Task<string> ExtractSyllabusData(string rawText, CancellationToken cancellationToken)
    {
        // Guard: if no input, signal extraction failure by returning empty string
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return string.Empty;
        }
        // MODIFIED: This now calls the specific syllabus plugin.
        return await _flmPlugin.ExtractSyllabusJsonAsync(rawText, cancellationToken);
    }

    private async Task<string?> TryGetCachedDataAsync(string inputHash, CancellationToken cancellationToken)
    {
        try
        {
            return await _storage.TryGetCachedSyllabusDataAsync(inputHash, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve cached syllabus validation data");
            return null;
        }
    }

    private async Task SaveDataToStorageAsync(string inputHash, string extractedData, SyllabusData? syllabusData, CancellationToken cancellationToken)
    {
        try
        {
            if (syllabusData != null && !string.IsNullOrEmpty(syllabusData.SubjectCode))
            {
                // Use subject code and version for organized storage
                await _storage.SaveSyllabusDataAsync(syllabusData.SubjectCode, syllabusData.VersionNumber, syllabusData, extractedData, inputHash, cancellationToken);
                _logger.LogInformation("Saved syllabus data for subject: {SubjectCode} version: {Version}",
                    syllabusData.SubjectCode, syllabusData.VersionNumber);
            }
            else
            {
                // For temporary data, we'll use the curriculum storage directly since ISyllabusImportStorage doesn't have SaveTemporaryDataAsync
                _logger.LogInformation("Syllabus data saved with input hash for caching purposes");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save syllabus validation data");
        }
    }

    private string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private string TryPreprocessSyllabusHtml(string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput)) return rawInput;
        var looksLikeHtml = rawInput.Contains("<html", StringComparison.OrdinalIgnoreCase)
            || rawInput.Contains("<table", StringComparison.OrdinalIgnoreCase)
            || rawInput.Contains("<div", StringComparison.OrdinalIgnoreCase);
        if (!looksLikeHtml) return rawInput;

        try
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(rawInput);

            var junkNodes = htmlDoc.DocumentNode.SelectNodes("//script|//style");
            if (junkNodes != null)
            {
                foreach (var node in junkNodes) node.Remove();
            }

            var contentNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@id='content']")
                               ?? htmlDoc.DocumentNode.SelectSingleNode("//body")
                               ?? htmlDoc.DocumentNode;

            var headingNodes = contentNode.SelectNodes(".//h1|.//h2|.//h3");
            var headingsText = new List<string>();
            if (headingNodes != null)
            {
                foreach (var h in headingNodes)
                {
                    var t = NormalizeWhitespace(h.InnerText);
                    if (!string.IsNullOrWhiteSpace(t)) headingsText.Add(t);
                }
            }

            var tableNodes = contentNode.SelectNodes(".//table");
            var tablesText = new List<string>();
            if (tableNodes != null && tableNodes.Count > 0)
            {
                foreach (var tbl in tableNodes)
                {
                    var t = NormalizeWhitespace(tbl.InnerText);
                    if (ContainsAny(t, new[] { "syllabus", "subject", "code", "version", "objective", "assessment", "content", "topics" }))
                    {
                        tablesText.Add(t);
                    }
                }
                if (tablesText.Count == 0)
                {
                    var largest = tableNodes.Select(n => NormalizeWhitespace(n.InnerText))
                                             .OrderByDescending(s => s?.Length ?? 0)
                                             .FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(largest)) tablesText.Add(largest);
                }
            }

            var paraNodes = contentNode.SelectNodes(".//p|.//span");
            var parasText = new List<string>();
            if (paraNodes != null)
            {
                foreach (var p in paraNodes)
                {
                    var t = NormalizeWhitespace(p.InnerText);
                    if (ContainsAny(t, new[] { "subject", "code", "version", "description", "objective", "credit" }))
                    {
                        parasText.Add(t);
                    }
                }
            }

            var builder = new StringBuilder();
            if (headingsText.Count > 0)
            {
                builder.AppendLine("== Headings ==");
                foreach (var h in headingsText.Distinct()) builder.AppendLine(h);
                builder.AppendLine();
            }

            if (parasText.Count > 0)
            {
                builder.AppendLine("== Details ==");
                foreach (var p in parasText.Distinct()) builder.AppendLine(p);
                builder.AppendLine();
            }

            if (tablesText.Count > 0)
            {
                int i = 1;
                foreach (var t in tablesText)
                {
                    builder.AppendLine($"== Table {i} ==");
                    builder.AppendLine(t);
                    builder.AppendLine();
                    i++;
                }
            }

            var result = builder.ToString();
            if (string.IsNullOrWhiteSpace(result) || result.Length < 50)
            {
                result = NormalizeWhitespace(contentNode.InnerText);
            }
            return string.IsNullOrWhiteSpace(result) ? rawInput : result;
        }
        catch
        {
            return rawInput;
        }
    }

    private static bool ContainsAny(string? text, IEnumerable<string> keywords)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var lowered = text.ToLowerInvariant();
        foreach (var k in keywords)
        {
            if (lowered.Contains(k)) return true;
        }
        return false;
    }

    private static string NormalizeWhitespace(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var sb = new StringBuilder(input.Length);
        bool inSpace = false;
        foreach (var ch in input)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!inSpace)
                {
                    sb.Append(' ');
                    inSpace = true;
                }
            }
            else
            {
                sb.Append(ch);
                inSpace = false;
            }
        }
        return sb.ToString().Trim();
    }

    private SyllabusData ParseSyllabusFromHtml(string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            throw new InvalidOperationException("Input text is empty or null");
        }

        var looksLikeHtml = rawInput.Contains("<html", StringComparison.OrdinalIgnoreCase)
            || rawInput.Contains("<table", StringComparison.OrdinalIgnoreCase)
            || rawInput.Contains("<div", StringComparison.OrdinalIgnoreCase);

        if (looksLikeHtml)
        {
            return ParseSyllabusFromHtmlContent(rawInput);
        }
        else
        {
            return ParseSyllabusFromPlainText(rawInput);
        }
    }

    private SyllabusData ParseSyllabusFromHtmlContent(string htmlContent)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(htmlContent);

        // Remove script and style nodes
        var junkNodes = htmlDoc.DocumentNode.SelectNodes("//script|//style");
        if (junkNodes != null)
        {
            foreach (var node in junkNodes) node.Remove();
        }

        var subjectCode = ParseSubjectCodeFromHtml(htmlDoc);
        var versionNumber = ParseVersionNumberFromHtml(htmlDoc);
        var syllabusContent = ParseSyllabusContentFromHtml(htmlDoc);
        var materials = ParseMaterialsFromHtml(htmlDoc);

        return new SyllabusData
        {
            SubjectCode = subjectCode,
            VersionNumber = versionNumber,
            Content = syllabusContent,
            Materials = materials
        };
    }

    private SyllabusData ParseSyllabusFromPlainText(string plainText)
    {
        var lines = plainText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                            .Select(l => l.Trim())
                            .Where(l => !string.IsNullOrEmpty(l))
                            .ToList();

        var subjectCode = ParseSubjectCodeFromLines(lines);
        var versionNumber = ParseVersionNumberFromLines(lines);
        var syllabusContent = ParseSyllabusContentFromLines(lines);
        var materials = ParseMaterialsFromLines(lines);

        return new SyllabusData
        {
            SubjectCode = subjectCode,
            VersionNumber = versionNumber,
            Content = syllabusContent,
            Materials = materials
        };
    }

    private string ParseSubjectCodeFromHtml(HtmlDocument htmlDoc)
    {
        // Look for subject code in tables
        var tables = htmlDoc.DocumentNode.SelectNodes("//table");
        if (tables != null)
        {
            foreach (var table in tables)
            {
                var subjectCode = TryParseSubjectCodeFromTable(table);
                if (!string.IsNullOrEmpty(subjectCode)) return subjectCode;
            }
        }

        // Fallback to text parsing
        var allText = htmlDoc.DocumentNode.InnerText;
        return ParseSubjectCodeFromLines(allText.Split('\n').Select(l => l.Trim()).ToList());
    }

    private int ParseVersionNumberFromHtml(HtmlDocument htmlDoc)
    {
        // Look for version in tables
        var tables = htmlDoc.DocumentNode.SelectNodes("//table");
        if (tables != null)
        {
            foreach (var table in tables)
            {
                var version = TryParseVersionFromTable(table);
                if (version.HasValue) return version.Value;
            }
        }

        // Fallback to text parsing
        var allText = htmlDoc.DocumentNode.InnerText;
        return ParseVersionNumberFromLines(allText.Split('\n').Select(l => l.Trim()).ToList());
    }

    private SyllabusContent ParseSyllabusContentFromHtml(HtmlDocument htmlDoc)
    {
        var weeklySchedule = ParseWeeklyScheduleFromHtml(htmlDoc);
        var assessments = ParseAssessmentsFromHtml(htmlDoc);

        return new SyllabusContent
        {
            WeeklySchedule = weeklySchedule,
            Assessments = assessments
        };
    }

    private List<SyllabusMaterial> ParseMaterialsFromHtml(HtmlDocument htmlDoc)
    {
        var materials = new List<SyllabusMaterial>();
        var tables = htmlDoc.DocumentNode.SelectNodes("//table");

        if (tables != null)
        {
            foreach (var table in tables)
            {
                var tableMaterials = TryParseMaterialsFromTable(table);
                materials.AddRange(tableMaterials);
            }
        }

        if (materials.Count == 0)
        {
            // Fallback to text parsing
            var allText = htmlDoc.DocumentNode.InnerText;
            materials = ParseMaterialsFromLines(allText.Split('\n').Select(l => l.Trim()).ToList());
        }

        return materials;
    }

    private List<SyllabusWeek> ParseWeeklyScheduleFromHtml(HtmlDocument htmlDoc)
    {
        var schedule = new List<SyllabusWeek>();

        // Look for schedule table (id="gvSchedule" or similar patterns)
        var scheduleTable = htmlDoc.DocumentNode.SelectSingleNode("//table[@id='gvSchedule']")
                           ?? htmlDoc.DocumentNode.SelectNodes("//table")?.FirstOrDefault(t =>
                               t.InnerText.ToLowerInvariant().Contains("session") &&
                               t.InnerText.ToLowerInvariant().Contains("topic"));

        if (scheduleTable != null)
        {
            schedule = TryParseScheduleFromTable(scheduleTable);
        }

        if (schedule.Count == 0)
        {
            // Fallback: create default schedule
            schedule.Add(new SyllabusWeek
            {
                WeekNumber = 1,
                Topic = "Introduction",
                Activities = new List<string> { "Lecture and discussion" },
                Readings = new List<string> { "Course overview materials" }
            });
        }

        return schedule;
    }

    private List<AssessmentItem> ParseAssessmentsFromHtml(HtmlDocument htmlDoc)
    {
        var assessments = new List<AssessmentItem>();

        // Look for assessment table (id="gvAssessment" or similar patterns)
        var assessmentTable = htmlDoc.DocumentNode.SelectSingleNode("//table[@id='gvAssessment']")
                             ?? htmlDoc.DocumentNode.SelectNodes("//table")?.FirstOrDefault(t =>
                                 t.InnerText.ToLowerInvariant().Contains("assessment") ||
                                 t.InnerText.ToLowerInvariant().Contains("weight"));

        if (assessmentTable != null)
        {
            assessments = TryParseAssessmentsFromTable(assessmentTable);
        }

        if (assessments.Count == 0)
        {
            // Fallback: create default assessments
            assessments.Add(new AssessmentItem
            {
                Type = "Final Exam",
                WeightPercentage = 50,
                Description = "Comprehensive final examination"
            });
        }

        return assessments;
    }

    // Top-level constructive questions are no longer part of SyllabusContent.
    // During import, questions are grouped by week using session numbers from HTML tables.

    private string? TryParseSubjectCodeFromTable(HtmlNode table)
    {
        var rows = table.SelectNodes(".//tr");
        if (rows == null) return null;

        foreach (var row in rows)
        {
            var cells = row.SelectNodes(".//td|.//th");
            if (cells == null || cells.Count < 2) continue;

            var key = NormalizeWhitespace(cells[0].InnerText).ToLowerInvariant();
            var value = NormalizeWhitespace(cells[1].InnerText);

            if (key.Contains("subject") && key.Contains("code"))
                return value;
        }

        return null;
    }

    private int? TryParseVersionFromTable(HtmlNode table)
    {
        var rows = table.SelectNodes(".//tr");
        if (rows == null) return null;

        foreach (var row in rows)
        {
            var cells = row.SelectNodes(".//td|.//th");
            if (cells == null || cells.Count < 2) continue;

            var key = NormalizeWhitespace(cells[0].InnerText).ToLowerInvariant();
            var value = NormalizeWhitespace(cells[1].InnerText);

            if (key.Contains("version"))
            {
                // Try direct integer parse
                if (int.TryParse(value, out var v)) return v;
                // Try to extract a number from patterns like "v1", "1.0", "Version 2"
                var match = System.Text.RegularExpressions.Regex.Match(value, "(\\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var vv)) return vv;
            }
        }

        return null;
    }

    private List<SyllabusMaterial> TryParseMaterialsFromTable(HtmlNode table)
    {
        var materials = new List<SyllabusMaterial>();
        var rows = table.SelectNodes(".//tr");
        if (rows == null) return materials;

        // Check if this looks like a materials table
        var headerRow = rows.FirstOrDefault();
        if (headerRow == null) return materials;

        var headerText = headerRow.InnerText.ToLowerInvariant();
        if (!headerText.Contains("material") && !headerText.Contains("book") && !headerText.Contains("author"))
            return materials;

        var headers = headerRow.SelectNodes(".//th|.//td")?.Select(h => NormalizeWhitespace(h.InnerText).ToLowerInvariant()).ToList();
        if (headers == null) return materials;

        var descriptionIndex = FindColumnIndex(headers, new[] { "description", "title", "material" });
        var authorIndex = FindColumnIndex(headers, new[] { "author" });
        var publisherIndex = FindColumnIndex(headers, new[] { "publisher" });

        for (int i = 1; i < rows.Count; i++)
        {
            var cells = rows[i].SelectNodes(".//td");
            if (cells == null) continue;

            var description = descriptionIndex >= 0 && descriptionIndex < cells.Count ?
                NormalizeWhitespace(cells[descriptionIndex].InnerText) : "Material";
            var author = authorIndex >= 0 && authorIndex < cells.Count ?
                NormalizeWhitespace(cells[authorIndex].InnerText) : "Unknown Author";
            var publisher = publisherIndex >= 0 && publisherIndex < cells.Count ?
                NormalizeWhitespace(cells[publisherIndex].InnerText) : "Unknown Publisher";

            if (!string.IsNullOrEmpty(description))
            {
                materials.Add(new SyllabusMaterial
                {
                    MaterialDescription = description,
                    Author = author,
                    Publisher = publisher,
                    IsMainMaterial = true
                });
            }
        }

        return materials;
    }

    private List<SyllabusWeek> TryParseScheduleFromTable(HtmlNode table)
    {
        var schedule = new List<SyllabusWeek>();
        var rows = table.SelectNodes(".//tr");
        if (rows == null) return schedule;

        var headerRow = rows.FirstOrDefault();
        if (headerRow == null) return schedule;

        var headers = headerRow.SelectNodes(".//th|.//td")?.Select(h => NormalizeWhitespace(h.InnerText).ToLowerInvariant()).ToList();
        if (headers == null) return schedule;

        var sessionIndex = FindColumnIndex(headers, new[] { "session", "week", "no" });
        var topicIndex = FindColumnIndex(headers, new[] { "topic", "subject", "content" });
        var loIndex = FindColumnIndex(headers, new[] { "lo", "learning", "objective" });
        var activitiesIndex = FindColumnIndex(headers, new[] { "activity", "activities", "type" });

        for (int i = 1; i < rows.Count; i++)
        {
            var cells = rows[i].SelectNodes(".//td");
            if (cells == null) continue;

            var sessionText = sessionIndex >= 0 && sessionIndex < cells.Count ?
                NormalizeWhitespace(cells[sessionIndex].InnerText) : (i).ToString();
            var topic = topicIndex >= 0 && topicIndex < cells.Count ?
                NormalizeWhitespace(cells[topicIndex].InnerText) : "Topic";
            var learningObjectives = loIndex >= 0 && loIndex < cells.Count ?
                NormalizeWhitespace(cells[loIndex].InnerText) : "Learning objectives";
            var activities = activitiesIndex >= 0 && activitiesIndex < cells.Count ?
                NormalizeWhitespace(cells[activitiesIndex].InnerText) : "Activities";

            int.TryParse(sessionText, out var week);
            if (week <= 0) week = i;

            // Map to SyllabusWeek (Activities as list; optionally include LO)
            var activityList = new List<string>();
            if (!string.IsNullOrWhiteSpace(activities)) activityList.Add(activities);
            if (!string.IsNullOrWhiteSpace(learningObjectives)) activityList.Add("LO: " + learningObjectives);

            schedule.Add(new SyllabusWeek
            {
                WeekNumber = week,
                Topic = topic,
                Activities = activityList,
                Readings = new List<string>()
            });
        }

        return schedule;
    }

    private List<AssessmentItem> TryParseAssessmentsFromTable(HtmlNode table)
    {
        var assessments = new List<AssessmentItem>();
        var rows = table.SelectNodes(".//tr");
        if (rows == null) return assessments;

        var headerRow = rows.FirstOrDefault();
        if (headerRow == null) return assessments;

        var headers = headerRow.SelectNodes(".//th|.//td")?.Select(h => NormalizeWhitespace(h.InnerText).ToLowerInvariant()).ToList();
        if (headers == null) return assessments;

        var typeIndex = FindColumnIndex(headers, new[] { "type", "category", "assessment" });
        var weightIndex = FindColumnIndex(headers, new[] { "weight", "percentage", "%" });
        var descriptionIndex = FindColumnIndex(headers, new[] { "description", "criteria", "completion" });

        for (int i = 1; i < rows.Count; i++)
        {
            var cells = rows[i].SelectNodes(".//td");
            if (cells == null) continue;

            var type = typeIndex >= 0 && typeIndex < cells.Count ?
                NormalizeWhitespace(cells[typeIndex].InnerText) : "Assessment";
            var weightText = weightIndex >= 0 && weightIndex < cells.Count ?
                NormalizeWhitespace(cells[weightIndex].InnerText) : "0";
            var description = descriptionIndex >= 0 && descriptionIndex < cells.Count ?
                NormalizeWhitespace(cells[descriptionIndex].InnerText) : "Assessment description";

            // Parse weight (remove % if present)
            weightText = weightText.Replace("%", "").Trim();
            int.TryParse(weightText, out var weight);

            if (!string.IsNullOrEmpty(type))
            {
                assessments.Add(new AssessmentItem
                {
                    Name = type,
                    Type = type,
                    WeightPercentage = weight,
                    Description = description
                });
            }
        }

        return assessments;
    }

    private List<string> TryParseQuestionsFromTable(HtmlNode table)
    {
        var questions = new List<string>();
        var rows = table.SelectNodes(".//tr");
        if (rows == null) return questions;

        // Look for tables that might contain questions
        var headerRow = rows.FirstOrDefault();
        if (headerRow == null) return questions;

        var headerText = headerRow.InnerText.ToLowerInvariant();
        if (!headerText.Contains("question") && !headerText.Contains("constructive"))
            return questions;

        for (int i = 1; i < rows.Count; i++)
        {
            var cells = rows[i].SelectNodes(".//td");
            if (cells == null) continue;

            foreach (var cell in cells)
            {
                var text = NormalizeWhitespace(cell.InnerText);
                if (!string.IsNullOrEmpty(text) && text.Contains("?"))
                {
                    questions.Add(text);
                }
            }
        }

        return questions;
    }

    private int FindColumnIndex(List<string> headers, string[] possibleNames)
    {
        for (int i = 0; i < headers.Count; i++)
        {
            foreach (var name in possibleNames)
            {
                if (headers[i].Contains(name))
                    return i;
            }
        }
        return -1;
    }

    private string ParseSubjectCodeFromLines(List<string> lines)
    {
        foreach (var line in lines)
        {
            // Look for patterns like "Subject Code: CSD201"
            if (line.ToLowerInvariant().Contains("subject") && line.Contains(":"))
            {
                var parts = line.Split(':', 2);
                if (parts.Length == 2) return parts[1].Trim();
            }

            // Look for subject code patterns (letters followed by numbers)
            var match = System.Text.RegularExpressions.Regex.Match(line, @"\b([A-Z]{2,4}\d{2,4})\b");
            if (match.Success) return match.Groups[1].Value;
        }

        return "UNKNOWN";
    }

    private int ParseVersionNumberFromLines(List<string> lines)
    {
        foreach (var line in lines)
        {
            if (line.ToLowerInvariant().Contains("version") && line.Contains(":"))
            {
                var parts = line.Split(':', 2);
                if (parts.Length == 2)
                {
                    var text = parts[1].Trim();
                    if (int.TryParse(text, out var v)) return v;
                    var match = System.Text.RegularExpressions.Regex.Match(text, "(\\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var vv)) return vv;
                }
            }
        }

        return 1;
    }

    private SyllabusContent ParseSyllabusContentFromLines(List<string> lines)
    {
        return new SyllabusContent
        {
            WeeklySchedule = new List<SyllabusWeek>
            {
                new SyllabusWeek
                {
                    WeekNumber = 1,
                    Topic = "Introduction",
                    Activities = new List<string> { "Lecture and discussion", "LO: Course overview" },
                    Readings = new List<string>()
                }
            },
            Assessments = new List<AssessmentItem>
            {
                new AssessmentItem
                {
                    Name = "Final Exam",
                    Type = "Exam",
                    WeightPercentage = 50,
                    Description = "Comprehensive final examination"
                }
            }
        };
    }

    private List<SyllabusMaterial> ParseMaterialsFromLines(List<string> lines)
    {
        var materials = new List<SyllabusMaterial>();

        foreach (var line in lines)
        {
            if (line.ToLowerInvariant().Contains("book") || line.ToLowerInvariant().Contains("material"))
            {
                materials.Add(new SyllabusMaterial
                {
                    MaterialDescription = line,
                    Author = "Unknown Author",
                    Publisher = "Unknown Publisher",
                    IsMainMaterial = true
                });
            }
        }

        if (materials.Count == 0)
        {
            materials.Add(new SyllabusMaterial
            {
                MaterialDescription = "Course Materials",
                Author = "Unknown Author",
                Publisher = "Unknown Publisher",
                IsMainMaterial = true
            });
        }

        return materials;
    }
}