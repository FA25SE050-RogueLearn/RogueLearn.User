// RogueLearn.User/src/RogueLearn.User.Application/Features/CurriculumImport/Commands/ImportSyllabus/ImportSyllabusCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Plugins;
using HtmlAgilityPack;
using System.IO;

namespace RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportSyllabus;

public class ImportSyllabusCommandHandler : IRequestHandler<ImportSyllabusCommand, ImportSyllabusResponse>
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly ISyllabusVersionRepository _syllabusVersionRepository;
    private readonly ICurriculumImportStorage _storage;
    private readonly FluentValidation.IValidator<SyllabusData> _validator;
    private readonly ILogger<ImportSyllabusCommandHandler> _logger;
    private readonly IFlmExtractionPlugin _flmPlugin;

    public ImportSyllabusCommandHandler(
        ISubjectRepository subjectRepository,
        ISyllabusVersionRepository syllabusVersionRepository,
        ICurriculumImportStorage storage,
        FluentValidation.IValidator<SyllabusData> validator,
        ILogger<ImportSyllabusCommandHandler> logger,
        IFlmExtractionPlugin flmPlugin)
    {
        _subjectRepository = subjectRepository;
        _syllabusVersionRepository = syllabusVersionRepository;
        _storage = storage;
        _validator = validator;
        _logger = logger;
        _flmPlugin = flmPlugin;
    }

    public async Task<ImportSyllabusResponse> Handle(ImportSyllabusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting syllabus import from input");

            // Support local file path input: if RawText points to an existing file, load its contents.
            string inputText = request.RawText;
            if (!string.IsNullOrWhiteSpace(inputText) && Path.IsPathRooted(inputText) && File.Exists(inputText))
            {
                _logger.LogInformation("Loading syllabus HTML from file path: {Path}", inputText);
                inputText = await File.ReadAllTextAsync(inputText, cancellationToken);
            }

            SyllabusData? syllabusData;

            // Step 1: Extract structured data directly (no AI)
            syllabusData = await ExtractSyllabusDataAsync(inputText);
            if (syllabusData == null)
            {
                return new ImportSyllabusResponse
                {
                    IsSuccess = false,
                    // Align message with unit test expectations
                    Message = "Failed to extract syllabus data from the provided content"
                };
            }

            // Log a concise summary of the parsed syllabus to aid debugging/observability.
            LogParseSummary(syllabusData);

            // Step 2: Validate parsed data
            var validationResult = await _validator.ValidateAsync(syllabusData, cancellationToken);
            if (!validationResult.IsValid)
            {
                return new ImportSyllabusResponse
                {
                    IsSuccess = false,
                    Message = "Validation failed",
                    ValidationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
                };
            }

            // Step 3: Map and persist data
            var result = await PersistSyllabusDataAsync(syllabusData, request.CreatedBy, cancellationToken);

            _logger.LogInformation("Syllabus import completed successfully for subject {SubjectCode} v{Version}", syllabusData.SubjectCode, syllabusData.VersionNumber);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during syllabus import");
            return new ImportSyllabusResponse
            {
                IsSuccess = false,
                Message = "An error occurred during syllabus import"
            };
        }
    }

    private async Task<SyllabusData?> ExtractSyllabusDataAsync(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText)) return null;

        var looksLikeHtml = rawText.Contains("<html", StringComparison.OrdinalIgnoreCase)
            || rawText.Contains("<table", StringComparison.OrdinalIgnoreCase)
            || rawText.Contains("<div", StringComparison.OrdinalIgnoreCase);

        if (!looksLikeHtml)
        {
            return await Task.FromResult(ParseSyllabusFromPlainText(rawText));
        }

        try
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(rawText);

            var junkNodes = htmlDoc.DocumentNode.SelectNodes("//script|//style");
            if (junkNodes != null)
            {
                foreach (var node in junkNodes) node.Remove();
            }

            var contentNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@id='content']")
                               ?? htmlDoc.DocumentNode.SelectSingleNode("//body")
                               ?? htmlDoc.DocumentNode;

            var parsed = ParseSyllabusFromHtml(contentNode);
            if (parsed != null)
            {
                var weeklyQuestionCount = parsed.Content?.WeeklySchedule?.Sum(w => w.ConstructiveQuestions?.Count ?? 0) ?? 0;
                _logger.LogInformation("HTML parse succeeded: Subject={SubjectCode}, Version={Version}, Weeks={Weeks}, Assessments={Assessments}, Materials={Materials}, Questions={Questions}",
                    parsed.SubjectCode,
                    parsed.VersionNumber,
                    parsed.Content?.WeeklySchedule?.Count ?? 0,
                    parsed.Content?.Assessments?.Count ?? 0,
                    parsed.Materials?.Count ?? 0,
                    weeklyQuestionCount);
            }
            else
            {
                _logger.LogWarning("HTML parse returned null syllabus data");
            }
            return await Task.FromResult(parsed);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse syllabus HTML. Falling back to plain text parser.");
            return await Task.FromResult(ParseSyllabusFromPlainText(rawText));
        }
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

    // --- Direct parsing helpers (no AI) ---
    private class HtmlTable
    {
        public List<string> Headers { get; set; } = new();
        public List<List<string>> Rows { get; set; } = new();
    }

    private SyllabusData? ParseSyllabusFromHtml(HtmlNode contentNode)
    {
        var headings = contentNode.SelectNodes(".//h1|.//h2|.//h3")?.Select(h => NormalizeWhitespace(h.InnerText)).Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? new List<string>();
        var paras = contentNode.SelectNodes(".//p|.//span")?.Select(p => NormalizeWhitespace(p.InnerText)).Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? new List<string>();
        var tables = ParseTables(contentNode);
        var info = ExtractKeyValueInfo(contentNode);
        // Map for resolving week topics from CLO references (keys like CLO1 -> details text)
        var cloMap = ExtractCloMap(contentNode);
        // Explicit learning outcomes list where Id = CLO Details and Details = LO Details (as requested)
        var cloOutcomes = ExtractCourseLearningOutcomes(contentNode);

        // Identify sessions table and build weekly schedule (6 sessions per week)
        var sessions = new List<SyllabusSession>();
        foreach (var tbl in tables)
        {
            var headers = tbl.Headers.Select(h => h.ToLowerInvariant()).ToList();
            int idxSession = IndexOfAny(headers, new[] { "session" });
            int idxTopic = IndexOfAny(headers, new[] { "topic" });
            int idxType = IndexOfAny(headers, new[] { "learning teaching type", "learning/teaching type" });
            int idxLo = IndexOfAny(headers, new[] { "lo" });
            int idxItu = IndexOfAny(headers, new[] { "itu" });
            int idxStudentMaterials = IndexOfAny(headers, new[] { "student materials" });
            int idxStudentTasks = IndexOfAny(headers, new[] { "student's tasks", "students tasks", "student tasks" });
            int idxUrls = IndexOfAny(headers, new[] { "urls" });

            if (idxSession >= 0 && idxTopic >= 0)
            {
                foreach (var row in tbl.Rows)
                {
                    var sNo = ParseIntSafe(SafeGet(row, idxSession), 0, 0, 1000);
                    var topic = SafeGet(row, idxTopic);
                    if (sNo <= 0 || string.IsNullOrWhiteSpace(topic)) continue;
                    sessions.Add(new SyllabusSession
                    {
                        SessionNumber = sNo,
                        Topic = topic,
                        LearningTeachingType = SafeGet(row, idxType),
                        LO = SafeGet(row, idxLo),
                        ITU = SafeGet(row, idxItu),
                        StudentMaterials = SafeGet(row, idxStudentMaterials),
                        StudentTasks = SafeGet(row, idxStudentTasks),
                        URLs = SafeGet(row, idxUrls)
                    });
                }
            }
        }

        // Prepare container for constructive questions
        var questions = new List<ConstructiveQuestion>();

        // Parse Constructive Questions tables before building weekly schedule
        foreach (var tbl in tables)
        {
            // Normalize headers to lowercase for case-insensitive matching
            var headers = tbl.Headers.Select(h => h.ToLowerInvariant()).ToList();
            // Be tolerant of different header labels
            int idxSessionNo = IndexOfAny(headers, new[] { "session no", "session number", "session" });
            int idxName = IndexOfAny(headers, new[] { "name", "question" });
            int idxDetails = IndexOfAny(headers, new[] { "details", "answer", "description" });

            // Require at least the session number and one of name/details to consider it a CQ table
            if (idxSessionNo >= 0 && (idxName >= 0 || idxDetails >= 0))
            {
                foreach (var row in tbl.Rows)
                {
                    var sessionText = SafeGet(row, idxSessionNo);
                    var nameText = idxName >= 0 ? SafeGet(row, idxName) : null;
                    var detailsText = idxDetails >= 0 ? SafeGet(row, idxDetails) : null;

                    // Skip rows that have no meaningful question/answer content
                    if (string.IsNullOrWhiteSpace(nameText) && string.IsNullOrWhiteSpace(detailsText))
                        continue;

                    var sessionNum = !string.IsNullOrWhiteSpace(sessionText)
                        ? ParseIntSafe(sessionText, defaultValue: 0, min: 0, max: 1000)
                        : 0;

                    questions.Add(new ConstructiveQuestion
                    {
                        // Map correctly: Name column holds the question text; Details holds the answer/explanation
                        Name = nameText ?? string.Empty,
                        Question = detailsText ?? string.Empty,
                        SessionNumber = sessionNum
                    });
                }
            }
        }

        var weekly = new List<SyllabusWeek>();
        if (sessions.Count > 0)
        {
            int sessionsPerWeek = 6;
            int weekCount = (int)Math.Ceiling(sessions.Count / (double)sessionsPerWeek);
            for (int i = 0; i < weekCount; i++)
            {
                var chunk = sessions
                    .Where(s => s.SessionNumber >= i * sessionsPerWeek + 1 && s.SessionNumber <= (i + 1) * sessionsPerWeek)
                    .OrderBy(s => s.SessionNumber)
                    .ToList();
                if (chunk.Count == 0) continue;
                // Determine week topic from CLOs referenced in LO column
                var cloKeys = chunk.Select(c => c.LO).Where(lo => !string.IsNullOrWhiteSpace(lo))
                                   .SelectMany(lo => lo.Split(new[] { ',', ';', '|', '/', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                                   .Select(s => s.Trim())
                                   .Where(s => !string.IsNullOrWhiteSpace(s))
                                   .Distinct()
                                   .ToList();
                var cloDetails = cloKeys.Select(k => cloMap.TryGetValue(k, out var d) ? d : k).Where(d => !string.IsNullOrWhiteSpace(d)).ToList();
                var topics = string.Join("; ", chunk.Select(c => c.Topic).Where(t => !string.IsNullOrWhiteSpace(t)));
                // Activities now include original topics plus student tasks
                var activities = new List<string>();
                activities.AddRange(chunk.Select(c => c.Topic).Where(t => !string.IsNullOrWhiteSpace(t)).SelectMany(t => t.Split(';', ',')).Select(a => a.Trim()));
                activities.AddRange(chunk.Select(c => c.StudentTasks).Where(t => !string.IsNullOrWhiteSpace(t)).SelectMany(t => t.Split(';', ',')).Select(a => a.Trim()));
                activities = activities.Where(a => !string.IsNullOrWhiteSpace(a)).Distinct().ToList();
                var readings = chunk.Select(c => c.StudentMaterials).Where(t => !string.IsNullOrWhiteSpace(t)).SelectMany(t => t.Split(';', ',')).Select(a => a.Trim()).Where(a => !string.IsNullOrWhiteSpace(a)).Distinct().ToList();
                // Group constructive questions by week using session number ranges
                var weekQuestions = questions.Where(q => q.SessionNumber.HasValue && q.SessionNumber.Value >= i * sessionsPerWeek + 1 && q.SessionNumber.Value <= (i + 1) * sessionsPerWeek).ToList();
                weekly.Add(new SyllabusWeek
                {
                    WeekNumber = i + 1,
                    Topic = cloDetails.Count > 0 ? (string.Join("; ", cloDetails) is var ct && ct.Length > 500 ? ct[..500] : ct) : (topics.Length > 500 ? topics[..500] : topics),
                    Activities = activities.Count > 0 ? activities : null,
                    Readings = readings.Count > 0 ? readings : null,
                    ConstructiveQuestions = weekQuestions.Count > 0 ? weekQuestions : null
                });
            }
        }

        // Identify assessments table
        var assessments = new List<AssessmentItem>();
        string? gradingGuideAggregate = null;
        foreach (var tbl in tables)
        {
            var headers = tbl.Headers.Select(h => h.ToLowerInvariant()).ToList();
            int idxName = IndexOfAny(headers, new[] { "category", "assessment" });
            int idxType = IndexOfAny(headers, new[] { "type" });
            int idxWeight = IndexOfAny(headers, new[] { "weight" });
            int idxDesc = IndexOfAny(headers, new[] { "completion criteria", "description" });
            int idxGuide = IndexOfAny(headers, new[] { "grading guide" });
            if (idxName >= 0 && idxWeight >= 0)
            {
                foreach (var row in tbl.Rows)
                {
                    var name = SafeGet(row, idxName);
                    var type = SafeGet(row, idxType);
                    var weight = ParsePercentageAsInt(SafeGet(row, idxWeight));
                    var desc = SafeGet(row, idxDesc);
                    var guide = SafeGet(row, idxGuide);
                    if (!string.IsNullOrWhiteSpace(guide)) gradingGuideAggregate = string.IsNullOrWhiteSpace(gradingGuideAggregate) ? guide : $"{gradingGuideAggregate}; {guide}";
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    assessments.Add(new AssessmentItem { Name = name, Type = string.IsNullOrWhiteSpace(type) ? null : type, WeightPercentage = weight, Description = string.IsNullOrWhiteSpace(desc) ? null : desc });
                }
            }
        }

        // Materials table
        var materials = ExtractMaterialsFromHtmlTables(contentNode);
        if (materials.Count == 0)
        {
            // Fallback to text-only parsing if checkbox-based parsing didn't match
            foreach (var tbl in tables)
            {
                var headers = tbl.Headers.Select(h => h.ToLowerInvariant()).ToList();
                int idxTitle = IndexOfAny(headers, new[] { "materialdescription", "material/description", "material description" });
                int idxAuthor = IndexOfAny(headers, new[] { "author" });
                int idxPublisher = IndexOfAny(headers, new[] { "publisher" });
                int idxIsbn = IndexOfAny(headers, new[] { "isbn" });
                int idxIsMain = IndexOfAny(headers, new[] { "ismainmaterial" });
                int idxIsHardCopy = IndexOfAny(headers, new[] { "ishardcopy" });
                int idxIsOnline = IndexOfAny(headers, new[] { "isonline" });
                int idxNote = IndexOfAny(headers, new[] { "note", "more" });
                if (idxTitle >= 0)
                {
                    foreach (var row in tbl.Rows)
                    {
                        var title = SafeGet(row, idxTitle);
                        var author = SafeGet(row, idxAuthor);
                        var publisher = SafeGet(row, idxPublisher);
                        var isbn = SafeGet(row, idxIsbn);
                        if (string.IsNullOrWhiteSpace(title)) continue;
                        var m = new SyllabusMaterial
                        {
                            MaterialDescription = title,
                            Author = author ?? string.Empty,
                            Publisher = publisher ?? string.Empty,
                            ISBN = isbn ?? string.Empty,
                            IsMainMaterial = ParseBoolLoose(SafeGet(row, idxIsMain)),
                            IsHardCopy = ParseBoolLoose(SafeGet(row, idxIsHardCopy)),
                            IsOnline = ParseBoolLoose(SafeGet(row, idxIsOnline)),
                            Note = SafeGet(row, idxNote) ?? string.Empty
                        };
                        materials.Add(m);
                    }
                }
            }
        }

        // Subject code from any text
        var subjectCode = ExtractFirstCode(headings.Concat(paras)) ?? ExtractFirstCode(tables.SelectMany(t => t.Rows).SelectMany(r => r)) ?? "UNKNOWN";
        var versionNumber = TryParseFirstIntFromTexts(headings.Concat(paras), new[] { "version" }) ?? 1;
        // Prefer top info table's Description field for course description
        string? courseDescription = null;
        if (!info.TryGetValue("Description", out courseDescription))
        {
            info.TryGetValue("Course Description", out courseDescription);
            if (string.IsNullOrWhiteSpace(courseDescription))
            {
                courseDescription = paras.FirstOrDefault(p => p.Contains("Description", StringComparison.OrdinalIgnoreCase));
            }
        }
        string? attendancePolicy = null;
        if (info.TryGetValue("StudentTasks", out var studentTasksVal))
        {
            var lines = studentTasksVal.Split('\n', ';').Select(l => l.Trim());
            var attendLine = lines.FirstOrDefault(l => l.Contains("attend", StringComparison.OrdinalIgnoreCase) || l.Contains("attendance", StringComparison.OrdinalIgnoreCase));
            attendancePolicy = attendLine ?? studentTasksVal;
        }
        var gradingPolicy = gradingGuideAggregate;

        // Required vs Recommended texts based on IsMainMaterial
        var requiredTexts = materials.Where(m => m.IsMainMaterial)
                                     .Select(m => m.MaterialDescription)
                                     .Where(t => !string.IsNullOrWhiteSpace(t))
                                     .Distinct()
                                     .ToList();
        var recommendedTexts = materials.Where(m => !m.IsMainMaterial)
                                        .Select(m => m.MaterialDescription)
                                        .Where(t => !string.IsNullOrWhiteSpace(t))
                                        .Distinct()
                                        .ToList();

        // Use the requested mapping for course learning outcomes:
        // Id = CLO Details, Details = LO Details
        var cloList = cloOutcomes;
        _logger.LogInformation("Parsed {CloCount} course learning outcomes (CLOs)", cloList.Count);
        if (cloList.Count > 0)
        {
            foreach (var sample in cloList.Take(3))
            {
                _logger.LogDebug("CLO sample: Id='{IdSample}' (len={IdLen}), Details len={DetailsLen}", sample.Id, sample.Id?.Length ?? 0, sample.Details?.Length ?? 0);
            }
        }

        var content = new SyllabusContent
        {
            CourseDescription = courseDescription,
            WeeklySchedule = weekly.Count > 0 ? weekly : null,
            Assessments = assessments.Count > 0 ? assessments : null,
            CourseLearningOutcomes = cloList.Count > 0 ? cloList : null,
            RequiredTexts = requiredTexts.Count > 0 ? requiredTexts : null,
            RecommendedTexts = recommendedTexts.Count > 0 ? recommendedTexts : null,
            GradingPolicy = gradingPolicy,
            AttendancePolicy = attendancePolicy
        };

        return new SyllabusData
        {
            SubjectCode = subjectCode.Length > 20 ? subjectCode[..20] : subjectCode,
            VersionNumber = versionNumber,
            Content = content,
            Materials = materials,
            SyllabusName = headings.FirstOrDefault() ?? string.Empty
        };
    }

    private SyllabusData? ParseSyllabusFromPlainText(string text)
    {
        var cleaned = NormalizeWhitespace(text);
        var lines = cleaned.Split('\n', '\r').Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        var subjectCode = ExtractFirstCode(lines) ?? "UNKNOWN";
        var description = lines.FirstOrDefault(l => l.Contains("description", StringComparison.OrdinalIgnoreCase));
        var data = new SyllabusData
        {
            SubjectCode = subjectCode.Length > 20 ? subjectCode[..20] : subjectCode,
            VersionNumber = 1,
            Content = new SyllabusContent { CourseDescription = description }
        };
        _logger.LogInformation("Plain-text parse summary: Subject={SubjectCode}, Version={Version}, HasDescription={HasDescription}",
            data.SubjectCode,
            data.VersionNumber,
            string.IsNullOrWhiteSpace(data.Content?.CourseDescription) ? false : true);
        return data;
    }

    private List<HtmlTable> ParseTables(HtmlNode contentNode)
    {
        var tables = new List<HtmlTable>();
        var tableNodes = contentNode.SelectNodes(".//table");
        if (tableNodes == null) return tables;
        foreach (var t in tableNodes)
        {
            var table = new HtmlTable();
            var trNodes = t.SelectNodes(".//tr");
            if (trNodes == null || trNodes.Count == 0) continue;
            var headerRow = trNodes.FirstOrDefault(row => row.SelectNodes(".//th") != null && row.SelectNodes(".//th").Count > 0) ?? trNodes.First();
            var headerCells = headerRow.SelectNodes(".//th|.//td") ?? new HtmlNodeCollection(headerRow);
            foreach (var hc in headerCells) table.Headers.Add(NormalizeWhitespace(hc.InnerText));
            foreach (var row in trNodes.Skip(1))
            {
                var cells = row.SelectNodes(".//td") ?? new HtmlNodeCollection(row);
                if (cells.Count == 0) continue;
                var list = new List<string>();
                foreach (var c in cells) list.Add(NormalizeWhitespace(c.InnerText));
                table.Rows.Add(list);
            }
            tables.Add(table);
        }
        return tables;
    }

    private int IndexOfAny(List<string> headers, IEnumerable<string> keywords)
    {
        // headers are expected to be lowercase already; compare with lowercase keywords for case-insensitive matching
        var lowered = keywords.Select(k => (k ?? string.Empty).ToLowerInvariant()).ToList();
        int i = 0;
        foreach (var h in headers)
        {
            foreach (var k in lowered)
            {
                if (h.Contains(k)) return i;
            }
            i++;
        }
        return -1;
    }

    // Prefer exact equality when matching headers to avoid false positives like
    // matching "lo details" inside "clo details". Falls back to Contains if no exact match.
    private int IndexOfAnyExactFirst(List<string> headers, IEnumerable<string> keywords)
    {
        var lowered = keywords.Select(k => (k ?? string.Empty).ToLowerInvariant()).ToList();
        // Try exact match first
        for (int i = 0; i < headers.Count; i++)
        {
            var h = headers[i];
            foreach (var k in lowered)
            {
                if (string.Equals(h, k, StringComparison.Ordinal))
                    return i;
            }
        }
        // Fallback to contains
        return IndexOfAny(headers, lowered);
    }

    private bool IsCheckboxChecked(HtmlNode cell)
    {
        if (cell == null) return false;
        var input = cell.SelectSingleNode(".//input[@type='checkbox']");
        if (input == null)
        {
            // fallback to heuristics if no input element: look for text markers
            var text = NormalizeWhitespace(cell.InnerText);
            return ParseBoolLoose(text);
        }
        var checkedAttr = input.GetAttributeValue("checked", string.Empty);
        if (!string.IsNullOrEmpty(checkedAttr)) return true;
        var ariaChecked = input.GetAttributeValue("aria-checked", string.Empty);
        if (string.Equals(ariaChecked, "true", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private List<SyllabusMaterial> ExtractMaterialsFromHtmlTables(HtmlNode contentNode)
    {
        var materials = new List<SyllabusMaterial>();
        var tableNodes = contentNode.SelectNodes(".//table");
        if (tableNodes == null) return materials;
        foreach (var t in tableNodes)
        {
            var trNodes = t.SelectNodes(".//tr");
            if (trNodes == null || trNodes.Count == 0) continue;
            var headerRow = trNodes.FirstOrDefault(row => row.SelectNodes(".//th") != null && row.SelectNodes(".//th").Count > 0) ?? trNodes.First();
            var headerCells = headerRow.SelectNodes(".//th|.//td") ?? new HtmlNodeCollection(headerRow);
            var headersLower = headerCells.Select(hc => NormalizeWhitespace(hc.InnerText).ToLowerInvariant()).ToList();

            int idxTitle = IndexOfAny(headersLower, new[] { "materialdescription", "material/description", "material description" });
            int idxAuthor = IndexOfAny(headersLower, new[] { "author" });
            int idxPublisher = IndexOfAny(headersLower, new[] { "publisher" });
            int idxIsbn = IndexOfAny(headersLower, new[] { "isbn" });
            int idxIsMain = IndexOfAny(headersLower, new[] { "ismainmaterial" });
            int idxIsHardCopy = IndexOfAny(headersLower, new[] { "ishardcopy" });
            int idxIsOnline = IndexOfAny(headersLower, new[] { "isonline" });
            int idxNote = IndexOfAny(headersLower, new[] { "note", "more" });

            if (idxTitle < 0) continue;
            foreach (var row in trNodes.Skip(1))
            {
                var cells = row.SelectNodes(".//td");
                if (cells == null) continue;
                if (idxTitle >= cells.Count) continue;
                var title = NormalizeWhitespace(cells[idxTitle].InnerText);
                if (string.IsNullOrWhiteSpace(title)) continue;
                var author = (idxAuthor >= 0 && idxAuthor < cells.Count) ? NormalizeWhitespace(cells[idxAuthor].InnerText) : string.Empty;
                var publisher = (idxPublisher >= 0 && idxPublisher < cells.Count) ? NormalizeWhitespace(cells[idxPublisher].InnerText) : string.Empty;
                var isbn = (idxIsbn >= 0 && idxIsbn < cells.Count) ? NormalizeWhitespace(cells[idxIsbn].InnerText) : string.Empty;
                var isMain = (idxIsMain >= 0 && idxIsMain < cells.Count) ? IsCheckboxChecked(cells[idxIsMain]) : false;
                var isHard = (idxIsHardCopy >= 0 && idxIsHardCopy < cells.Count) ? IsCheckboxChecked(cells[idxIsHardCopy]) : false;
                var isOnline = (idxIsOnline >= 0 && idxIsOnline < cells.Count) ? IsCheckboxChecked(cells[idxIsOnline]) : false;
                var note = (idxNote >= 0 && idxNote < cells.Count) ? NormalizeWhitespace(cells[idxNote].InnerText) : string.Empty;

                materials.Add(new SyllabusMaterial
                {
                    MaterialDescription = title,
                    Author = author,
                    Publisher = publisher,
                    ISBN = isbn,
                    IsMainMaterial = isMain,
                    IsHardCopy = isHard,
                    IsOnline = isOnline,
                    Note = note
                });
            }
        }
        return materials;
    }

    private string SafeGet(List<string> row, int idx)
    {
        if (idx < 0 || idx >= row.Count) return string.Empty;
        return row[idx];
    }

    private bool ParseBoolLoose(string? input)
    {
        var s = (input ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(s)) return false;
        return s == "true" || s == "1" || s == "yes" || s == "y" || s == "✓" || s == "check" || s == "checked" || s == "x";
    }

    private int ParseIntSafe(string input, int defaultValue, int min, int max)
    {
        if (int.TryParse(new string((input ?? string.Empty).Where(char.IsDigit).ToArray()), out var val))
        {
            return Math.Clamp(val, min, max);
        }
        return defaultValue;
    }

    private int ParsePercentageAsInt(string input)
    {
        // Extract integer or decimal percentage, e.g., "3.5%" -> 4
        var s = input ?? string.Empty;
        var sb = new StringBuilder();
        bool seenDot = false;
        foreach (var ch in s)
        {
            if (char.IsDigit(ch)) sb.Append(ch);
            else if (ch == '.' && !seenDot) { sb.Append(ch); seenDot = true; }
        }
        if (double.TryParse(sb.ToString(), System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out var d))
        {
            var rounded = (int)Math.Round(d, MidpointRounding.AwayFromZero);
            return Math.Clamp(rounded, 0, 100);
        }
        return 0;
    }

    private Dictionary<string, string> ExtractKeyValueInfo(HtmlNode contentNode)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var tables = contentNode.SelectNodes(".//table");
        if (tables == null) return dict;
        foreach (var t in tables)
        {
            var trNodes = t.SelectNodes(".//tr");
            if (trNodes == null) continue;
            foreach (var tr in trNodes)
            {
                var tds = tr.SelectNodes(".//td");
                if (tds == null || tds.Count != 2) continue;
                var key = NormalizeWhitespace(tds[0].InnerText);
                var val = NormalizeWhitespace(tds[1].InnerText);
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(val))
                {
                    var keyClean = key.Trim().TrimEnd(':');
                    dict[keyClean] = val.Length > 2000 ? val[..2000] : val;
                }
            }
        }
        return dict;
    }

    private Dictionary<string, string> ExtractCloMap(HtmlNode contentNode)
    {
        // Build a mapping like: CLO1 -> "Description of CLO1" (CLO Details)
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var tableNodes = contentNode.SelectNodes(".//table");
        if (tableNodes == null) return map;
        foreach (var t in tableNodes)
        {
            var trNodes = t.SelectNodes(".//tr");
            if (trNodes == null || trNodes.Count == 0) continue;
            var headerRow = trNodes.FirstOrDefault(row => row.SelectNodes(".//th") != null && row.SelectNodes(".//th").Count > 0) ?? trNodes.First();
            var headerCells = headerRow.SelectNodes(".//th|.//td") ?? new HtmlNodeCollection(headerRow);
            var headersLower = headerCells.Select(hc => NormalizeWhitespace(hc.InnerText).ToLowerInvariant()).ToList();
            _logger.LogDebug("CLO map: headers detected -> {Headers}", string.Join(" | ", headersLower));
            // Find the identifier column (e.g., "CLO Name" or "CLO No") and the details column ("CLO Details")
            int idxCloId = IndexOfAnyExactFirst(headersLower, new[] { "clo name", "clo no", "clo id", "clo" });
            int idxCloDetails = IndexOfAnyExactFirst(headersLower, new[] { "clo details", "clo detail", "clo description", "clo desc" });
            _logger.LogDebug("CLO map: idxCloId={IdxCloId}, idxCloDetails={IdxCloDetails}", idxCloId, idxCloDetails);
            if (idxCloId < 0 || idxCloDetails < 0) continue;
            foreach (var row in trNodes.Skip(1))
            {
                var cells = row.SelectNodes(".//td");
                if (cells == null) continue;
                if (idxCloId >= cells.Count || idxCloDetails >= cells.Count) continue;
                var key = NormalizeWhitespace(cells[idxCloId].InnerText);
                var val = NormalizeWhitespace(cells[idxCloDetails].InnerText);
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(val)) continue;
                // Normalize to forms like CLO1
                var normalizedKey = key.Trim();
                if (!normalizedKey.StartsWith("CLO", StringComparison.OrdinalIgnoreCase))
                {
                    // Try to extract token that looks like CLO + digits
                    var rx = new System.Text.RegularExpressions.Regex(@"CLO\s*\d+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    var m = rx.Match(normalizedKey);
                    if (m.Success) normalizedKey = m.Value.Replace(" ", string.Empty);
                }
                map[normalizedKey] = val.Length > 500 ? val[..500] : val;
            }
        }
        return map;
    }

    private List<CourseLearningOutcome> ExtractCourseLearningOutcomes(HtmlNode contentNode)
    {
        // Build learning outcomes list where:
        // Id = CLO Details (truncated to 50 to satisfy validator)
        // Details = LO Details (truncated to 2000)
        var results = new List<CourseLearningOutcome>();
        var tableNodes = contentNode.SelectNodes(".//table");
        if (tableNodes == null) return results;
        foreach (var t in tableNodes)
        {
            var trNodes = t.SelectNodes(".//tr");
            if (trNodes == null || trNodes.Count == 0) continue;
            var headerRow = trNodes.FirstOrDefault(row => row.SelectNodes(".//th") != null && row.SelectNodes(".//th").Count > 0) ?? trNodes.First();
            var headerCells = headerRow.SelectNodes(".//th|.//td") ?? new HtmlNodeCollection(headerRow);
            var headersLower = headerCells.Select(hc => NormalizeWhitespace(hc.InnerText).ToLowerInvariant()).ToList();
            _logger.LogDebug("CLO extract: headers detected -> {Headers}", string.Join(" | ", headersLower));

            // Be tolerant to variants of header naming, but prefer exact matches to avoid "clo details" being mistaken for "lo details".
            int idxCloDetails = IndexOfAnyExactFirst(headersLower, new[] { "clo details", "clo detail", "clo description", "clo desc" });
            int idxLoDetails = IndexOfAnyExactFirst(headersLower, new[] { "lo details", "lo detail", "lo description", "learning outcome details", "learning outcomes details", "lo desc" });
            // If both indexes resolved to the same column (likely due to substring match fallback), try to locate a distinct LO Details column.
            if (idxLoDetails == idxCloDetails)
            {
                // Attempt strict exact equality for LO headers
                var loExactIdx = headersLower.FindIndex(h => h == "lo details" || h == "lo detail" || h == "lo description" || h == "learning outcome details" || h == "learning outcomes details" || h == "lo desc");
                if (loExactIdx >= 0) idxLoDetails = loExactIdx;
                else
                {
                    // Attempt contains match only on headers that are not the CLO details column
                    for (int i = 0; i < headersLower.Count; i++)
                    {
                        if (i == idxCloDetails) continue;
                        var h = headersLower[i];
                        if (h.Contains("lo details") || h.Contains("lo detail") || h.Contains("lo description") || h.Contains("learning outcome details") || h.Contains("learning outcomes details") || h.Contains("lo desc"))
                        {
                            idxLoDetails = i;
                            break;
                        }
                    }
                }
            }
            _logger.LogDebug("CLO extract: idxCloDetails={IdxCloDetails}, idxLoDetails={IdxLoDetails}", idxCloDetails, idxLoDetails);
            if (idxCloDetails < 0 || idxLoDetails < 0) continue;

            foreach (var row in trNodes.Skip(1))
            {
                var cells = row.SelectNodes(".//td");
                if (cells == null) continue;
                if (idxCloDetails >= cells.Count || idxLoDetails >= cells.Count) continue;
                var cloDetails = NormalizeWhitespace(cells[idxCloDetails].InnerText);
                var loDetails = NormalizeWhitespace(cells[idxLoDetails].InnerText);
                if (string.IsNullOrWhiteSpace(cloDetails) && string.IsNullOrWhiteSpace(loDetails)) continue;

                var id = cloDetails ?? string.Empty;
                if (id.Length > 50) id = id[..50]; // satisfy validator constraint
                var details = loDetails ?? string.Empty;
                if (details.Length > 2000) details = details[..2000];

                results.Add(new CourseLearningOutcome
                {
                    Id = id,
                    Details = details
                });
            }
        }
        return results;
    }

    private string? ExtractFirstCode(IEnumerable<string> texts)
    {
        var rx = new System.Text.RegularExpressions.Regex("\\b[A-Z]{2,}[0-9]{2,}\\b");
        foreach (var t in texts)
        {
            var m = rx.Match(t);
            if (m.Success) return m.Value;
        }
        return null;
    }

    private int? TryParseFirstIntFromTexts(IEnumerable<string> texts, IEnumerable<string> labels)
    {
        foreach (var t in texts)
        {
            foreach (var l in labels)
            {
                if (t.IndexOf(l, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (int.TryParse(new string(t.Where(char.IsDigit).ToArray()), out var val)) return val;
                }
            }
        }
        return null;
    }

    private async Task<ImportSyllabusResponse> PersistSyllabusDataAsync(
        SyllabusData data,
        Guid? createdBy,
        CancellationToken cancellationToken)
    {
        var response = new ImportSyllabusResponse { IsSuccess = true };

        // Find or create subject
        var subject = await _subjectRepository
            .FirstOrDefaultAsync(s => s.SubjectCode == data.SubjectCode, cancellationToken);

        if (subject == null)
        {
            throw new NotFoundException($"Subject with code '{data.SubjectCode}' not found. Please import curriculum first.");
        }

        response.SubjectCode = subject.SubjectCode;
        response.SubjectId = subject.Id;

        // Check if syllabus version already exists
        var existingVersion = await _syllabusVersionRepository.FirstOrDefaultAsync(
            v => v.SubjectId == subject.Id && v.VersionNumber == data.VersionNumber,
            cancellationToken);

        if (existingVersion != null)
        {
            _logger.LogWarning("Syllabus version {VersionNumber} for subject {SubjectCode} already exists. Skipping import.",
                data.VersionNumber, data.SubjectCode);

            return new ImportSyllabusResponse
            {
                IsSuccess = false,
                Message = $"Syllabus version '{data.VersionNumber}' for subject '{data.SubjectCode}' already exists. Import skipped to prevent duplicates."
            };
        }

        // Create syllabus version
        var syllabusVersion = new SyllabusVersion
        {
            SubjectId = subject.Id,
            VersionNumber = data.VersionNumber,
            // MODIFIED: This is the key change. Instead of serializing the content object to a string,
            // we deserialize it into a Dictionary<string, object> to match the updated entity property.
            Content = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(
                data.Content,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                })),
            EffectiveDate = data.EffectiveDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            IsActive = true,
            CreatedBy = createdBy
        };

        await _syllabusVersionRepository.AddAsync(syllabusVersion, cancellationToken);
        response.SyllabusVersionId = syllabusVersion.Id;

        response.Message = "Syllabus imported successfully";
        return response;
    }

    private void LogParseSummary(SyllabusData data)
    {
        try
        {
            var weeklyQuestionCount = data.Content?.WeeklySchedule?.Sum(w => w.ConstructiveQuestions?.Count ?? 0) ?? 0;
            _logger.LogInformation("Parsed syllabus summary: Subject={Subject}, Version={Version}, Name={Name}, Weeks={Weeks}, Assessments={Assessments}, Materials={Materials}, Questions={Questions}",
                data.SubjectCode,
                data.VersionNumber,
                string.IsNullOrWhiteSpace(data.SyllabusName) ? "(none)" : data.SyllabusName,
                data.Content?.WeeklySchedule?.Count ?? 0,
                data.Content?.Assessments?.Count ?? 0,
                data.Materials?.Count ?? 0,
                weeklyQuestionCount);

            // Log a few sample entries to help debug mapping
            var weekSamples = data.Content?.WeeklySchedule?.Take(3).Select(w => $"W{w.WeekNumber}:{w.Topic}") ?? Enumerable.Empty<string>();
            var assessSamples = data.Content?.Assessments?.Take(3).Select(a => $"{a.Name}:{a.WeightPercentage}%") ?? Enumerable.Empty<string>();
            var materialSamples = data.Materials?.Take(3).Select(m => m.MaterialDescription) ?? Enumerable.Empty<string>();

            if (weekSamples.Any())
                _logger.LogDebug("Weekly sample: {Weeks}", string.Join(" | ", weekSamples));
            if (assessSamples.Any())
                _logger.LogDebug("Assessment sample: {Assessments}", string.Join(" | ", assessSamples));
            if (materialSamples.Any())
                _logger.LogDebug("Material sample: {Materials}", string.Join(" | ", materialSamples));
        }
        catch
        {
            // Swallow logging exceptions to avoid impacting import flow
        }
    }
}