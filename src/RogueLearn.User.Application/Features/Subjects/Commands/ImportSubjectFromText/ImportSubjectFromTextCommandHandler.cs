// RogueLearn.User/src/RogueLearn.User.Application/Features/Subjects/Commands/ImportSubjectFromText/ImportSubjectFromTextCommandHandler.cs
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Common;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Subjects.Commands.ImportSubjectFromText;

/// <summary>
/// Imports syllabus text and enriches it with validated URLs for each session/topic.
/// URLs are stored in subject.content so quest generation can use them directly.
/// </summary>
public class ImportSubjectFromTextCommandHandler : IRequestHandler<ImportSubjectFromTextCommand, CreateSubjectResponse>
{
    private readonly ISyllabusExtractionPlugin _syllabusExtractionPlugin;
    private readonly IConstructiveQuestionGenerationPlugin _questionGenerationPlugin;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ICurriculumProgramSubjectRepository _programSubjectRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ImportSubjectFromTextCommandHandler> _logger;
    private readonly IHtmlCleaningService _htmlCleaningService;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ICurriculumImportStorage _storage;
    private readonly IReadingUrlService _readingUrlService;

    public ImportSubjectFromTextCommandHandler(
        ISyllabusExtractionPlugin syllabusExtractionPlugin,
        IConstructiveQuestionGenerationPlugin questionGenerationPlugin,
        ISubjectRepository subjectRepository,
        ICurriculumProgramSubjectRepository programSubjectRepository,
        IMapper mapper,
        ILogger<ImportSubjectFromTextCommandHandler> logger,
        IHtmlCleaningService htmlCleaningService,
        IUserProfileRepository userProfileRepository,
        ICurriculumImportStorage storage,
        IReadingUrlService readingUrlService)
    {
        _syllabusExtractionPlugin = syllabusExtractionPlugin;
        _questionGenerationPlugin = questionGenerationPlugin;
        _subjectRepository = subjectRepository;
        _programSubjectRepository = programSubjectRepository;
        _mapper = mapper;
        _logger = logger;
        _htmlCleaningService = htmlCleaningService;
        _userProfileRepository = userProfileRepository;
        _storage = storage;
        _readingUrlService = readingUrlService;
    }

    public async Task<CreateSubjectResponse> Handle(ImportSubjectFromTextCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting single subject syllabus import for user {AuthUserId}.", request.AuthUserId);

        var cleanText = _htmlCleaningService.ExtractCleanTextFromHtml(request.RawText);
        if (string.IsNullOrWhiteSpace(cleanText))
        {
            throw new BadRequestException("Failed to extract meaningful text content from the provided HTML.");
        }

        var rawTextHash = ComputeSha256Hash(cleanText);

        string? extractedJson = await _storage.TryGetCachedSyllabusDataAsync(rawTextHash, cancellationToken);
        bool isCacheHit = !string.IsNullOrWhiteSpace(extractedJson);

        if (isCacheHit)
        {
            _logger.LogInformation("Cache HIT for syllabus hash {Hash}. Skipping AI extraction.", rawTextHash);
        }
        else
        {
            _logger.LogInformation("Cache MISS for syllabus hash {Hash}. Proceeding with AI extraction.", rawTextHash);
            extractedJson = await _syllabusExtractionPlugin.ExtractSyllabusJsonAsync(cleanText, cancellationToken);
            if (string.IsNullOrWhiteSpace(extractedJson))
            {
                throw new BadRequestException("AI extraction failed to produce valid JSON from the provided syllabus text.");
            }
        }

        SyllabusData? syllabusData;
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
            syllabusData = JsonSerializer.Deserialize<SyllabusData>(extractedJson!, options);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize extracted syllabus JSON: {Json}", extractedJson);
            throw new BadRequestException("Failed to deserialize the extracted syllabus data.");
        }

        if (syllabusData == null || string.IsNullOrWhiteSpace(syllabusData.SubjectCode))
        {
            throw new BadRequestException("Extracted syllabus data is missing a valid SubjectCode.");
        }

        // Generate constructive questions if missing
        if (syllabusData.Content?.ConstructiveQuestions == null || !syllabusData.Content.ConstructiveQuestions.Any())
        {
            _logger.LogInformation("No constructive questions found in extracted syllabus. Attempting to generate them with AI.");
            if (syllabusData.Content?.SessionSchedule != null && syllabusData.Content.SessionSchedule.Any())
            {
                var generatedQuestions = await _questionGenerationPlugin.GenerateQuestionsAsync(
                    syllabusData.Content.SessionSchedule, cancellationToken);

                if (generatedQuestions.Any())
                {
                    syllabusData.Content.ConstructiveQuestions = generatedQuestions;
                    _logger.LogInformation("Successfully generated {Count} constructive questions for the syllabus.",
                        generatedQuestions.Count);
                }
                else
                {
                    _logger.LogWarning("AI question generation did not produce any questions.");
                }
            }
        }

        // CRITICAL: URL ENRICHMENT PHASE
        if (syllabusData.Content?.SessionSchedule != null)
        {
            _logger.LogInformation("🔍 Starting URL enrichment for {Count} sessions in syllabus '{SubjectCode}'",
                syllabusData.Content.SessionSchedule.Count, syllabusData.SubjectCode);

            // ⭐ BUILD SUBJECT CONTEXT AND DETECT CATEGORY
            var subjectContext = BuildSubjectContext(syllabusData);
            var subjectCategory = DetectSubjectCategory(syllabusData);

            _logger.LogInformation("📋 Subject context: '{Context}' | Category: {Category}",
                subjectContext, subjectCategory);

            int successCount = 0;
            int failureCount = 0;

            foreach (var session in syllabusData.Content.SessionSchedule)
            {
                _logger.LogDebug("Processing session {SessionNumber}: '{Topic}'",
                    session.SessionNumber, session.Topic);

                var existingReadings = session.Readings ?? new List<string>();

                try
                {
                    // ⭐ PASS SUBJECT CONTEXT AND CATEGORY (4 parameters)
                    var foundUrl = await _readingUrlService.GetValidUrlForTopicAsync(
                        session.Topic,
                        existingReadings,
                        subjectContext,
                        subjectCategory,
                        cancellationToken);

                    if (!string.IsNullOrWhiteSpace(foundUrl))
                    {
                        session.SuggestedUrl = foundUrl;
                        successCount++;
                        _logger.LogInformation("✅ Session {SessionNumber} enriched with URL: {Url}",
                            session.SessionNumber, foundUrl);
                    }
                    else
                    {
                        // ⭐ SET TO EMPTY STRING to avoid JsonElement serialization bug
                        session.SuggestedUrl = string.Empty;
                        failureCount++;
                        _logger.LogWarning("⚠️ Session {SessionNumber} ('{Topic}') - No valid URL found",
                            session.SessionNumber, session.Topic);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error enriching session {SessionNumber} with URL", session.SessionNumber);
                    // ⭐ SET TO EMPTY STRING ON ERROR
                    session.SuggestedUrl = string.Empty;
                    failureCount++;
                }
            }

            _logger.LogInformation(
                "URL enrichment complete for '{SubjectCode}': {SuccessCount} URLs found, {FailureCount} failed",
                syllabusData.SubjectCode, successCount, failureCount);

            if (failureCount > syllabusData.Content.SessionSchedule.Count / 2)
            {
                _logger.LogWarning(
                    "⚠️ More than 50% of sessions failed URL enrichment. Check web search configuration or syllabus content quality.");
            }
        }

        // Serialize the enriched data for caching
        var finalJsonToCache = JsonSerializer.Serialize(syllabusData, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
            WriteIndented = true
        });

        // Cache the enriched data if this was a fresh extraction
        if (!isCacheHit)
        {
            await _storage.SaveSyllabusDataAsync(
                syllabusData.SubjectCode,
                syllabusData.VersionNumber,
                syllabusData,
                finalJsonToCache,
                rawTextHash,
                cancellationToken);
            _logger.LogInformation("💾 Cached enriched syllabus data with hash {Hash}.", rawTextHash);
        }

        // Check if subject already exists in user's context
        var existingSubject = await _subjectRepository.GetSubjectForUserContextAsync(
            syllabusData.SubjectCode,
            request.AuthUserId,
            cancellationToken);

        if (existingSubject != null)
        {
            _logger.LogInformation("Found existing subject {SubjectId} within user's context. Updating content.",
                existingSubject.Id);
            return await UpdateExistingSubjectContent(existingSubject, syllabusData, cancellationToken);
        }
        else
        {
            _logger.LogWarning(
                "Subject with code {SubjectCode} not found within user's context. Creating a new subject shell and linking it to the user's program.",
                syllabusData.SubjectCode);

            var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken)
                ?? throw new NotFoundException("UserProfile", request.AuthUserId);

            if (userProfile.RouteId == null)
            {
                throw new BadRequestException("Cannot create a new subject because the user has not selected a curriculum program (route).");
            }

            var newSubject = new Subject
            {
                SubjectCode = syllabusData.SubjectCode,
                Description = syllabusData.Description,
                Credits = syllabusData.Credits,
                Semester = syllabusData.Semester,
                PrerequisiteSubjectIds = await ResolvePrerequisiteIdsAsync(syllabusData.PreRequisite, cancellationToken)
            };

            // Serialize the enriched content (which now includes SuggestedUrl for each session)
            var contentJson = JsonSerializer.Serialize(syllabusData.Content);
            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new ObjectToInferredTypesConverter() }
            };
            newSubject.Content = JsonSerializer.Deserialize<Dictionary<string, object>>(contentJson, serializerOptions);

            newSubject.SubjectName = syllabusData.SubjectName;

            if (syllabusData.ApprovedDate.HasValue)
            {
                newSubject.UpdatedAt = new DateTimeOffset(syllabusData.ApprovedDate.Value.ToDateTime(TimeOnly.MinValue));
            }

            var createdSubjectEntity = await _subjectRepository.AddAsync(newSubject, cancellationToken);
            _logger.LogInformation("Successfully created new subject {SubjectId} with enriched syllabus content.",
                createdSubjectEntity.Id);

            var programSubjectLink = new CurriculumProgramSubject
            {
                ProgramId = userProfile.RouteId.Value,
                SubjectId = createdSubjectEntity.Id
            };
            await _programSubjectRepository.AddAsync(programSubjectLink, cancellationToken);
            _logger.LogInformation("Linked new subject {SubjectId} to program {ProgramId}",
                createdSubjectEntity.Id, userProfile.RouteId.Value);

            return _mapper.Map<CreateSubjectResponse>(createdSubjectEntity);
        }
    }

    /// <summary>
    /// Build subject context string from syllabus data for relevance filtering.
    /// Extracts technology stack from TechnologyStack field, subject name, and subject code.
    /// </summary>
    private string BuildSubjectContext(SyllabusData syllabusData)
    {
        var contextParts = new List<string>();

        // Priority 1: Use TechnologyStack if AI extracted it
        if (!string.IsNullOrWhiteSpace(syllabusData.TechnologyStack))
        {
            contextParts.Add(syllabusData.TechnologyStack);
            _logger.LogDebug("Using TechnologyStack from AI: '{Stack}'", syllabusData.TechnologyStack);
        }

        // Priority 2: Extract from subject name
        var subjectNameLower = syllabusData.SubjectName?.ToLowerInvariant() ?? "";

        // Mobile/Android
        if (subjectNameLower.Contains("android") || subjectNameLower.Contains("mobile"))
        {
            if (!contextParts.Any(c => c.Contains("Android", StringComparison.OrdinalIgnoreCase)))
                contextParts.Add("Android Mobile");
        }

        // .NET/ASP.NET
        if (subjectNameLower.Contains("asp.net") || subjectNameLower.Contains(".net") || subjectNameLower.Contains("c#"))
        {
            if (!contextParts.Any(c => c.Contains("ASP.NET", StringComparison.OrdinalIgnoreCase)))
                contextParts.Add("ASP.NET Core");
        }

        // JavaScript frameworks
        if (subjectNameLower.Contains("react"))
        {
            if (!contextParts.Any(c => c.Contains("React", StringComparison.OrdinalIgnoreCase)))
                contextParts.Add("React JavaScript");
        }
        if (subjectNameLower.Contains("vue"))
        {
            if (!contextParts.Any(c => c.Contains("Vue", StringComparison.OrdinalIgnoreCase)))
                contextParts.Add("Vue JavaScript");
        }
        if (subjectNameLower.Contains("angular"))
        {
            if (!contextParts.Any(c => c.Contains("Angular", StringComparison.OrdinalIgnoreCase)))
                contextParts.Add("Angular TypeScript");
        }

        // Java (not JavaScript)
        if (subjectNameLower.Contains("java") && !subjectNameLower.Contains("javascript"))
        {
            if (!contextParts.Any(c => c.Contains("Java", StringComparison.OrdinalIgnoreCase)))
                contextParts.Add("Java");
        }

        // Python
        if (subjectNameLower.Contains("python"))
        {
            if (!contextParts.Any(c => c.Contains("Python", StringComparison.OrdinalIgnoreCase)))
                contextParts.Add("Python");
        }

        // Priority 3: Extract from subject code patterns
        var subjectCodeLower = syllabusData.SubjectCode?.ToLowerInvariant() ?? "";

        // Common FPT University subject code patterns
        if (subjectCodeLower.StartsWith("prm") || subjectCodeLower.StartsWith("mad"))
        {
            if (!contextParts.Any(c => c.Contains("Android", StringComparison.OrdinalIgnoreCase)))
                contextParts.Add("Android Mobile");
        }
        else if (subjectCodeLower.StartsWith("prn") || subjectCodeLower.StartsWith("prj"))
        {
            if (!contextParts.Any(c => c.Contains("ASP.NET", StringComparison.OrdinalIgnoreCase)))
                contextParts.Add("ASP.NET Core");
        }
        else if (subjectCodeLower.StartsWith("swp") || subjectCodeLower.StartsWith("swd"))
        {
            if (!contextParts.Any(c => c.Contains("Web", StringComparison.OrdinalIgnoreCase)))
                contextParts.Add("Web Development");
        }

        // Build final context string
        var finalContext = contextParts.Any()
            ? string.Join(", ", contextParts.Distinct())
            : syllabusData.SubjectName ?? "Programming";

        return finalContext;
    }

    /// <summary>
    /// Detect the category of subject for appropriate source selection.
    /// </summary>
    private SubjectCategory DetectSubjectCategory(SyllabusData syllabusData)
    {
        var subjectNameLower = syllabusData.SubjectName?.ToLowerInvariant() ?? "";
        var subjectCodeLower = syllabusData.SubjectCode?.ToLowerInvariant() ?? "";
        var descriptionLower = syllabusData.Description?.ToLowerInvariant() ?? "";

        var combinedText = $"{subjectNameLower} {subjectCodeLower} {descriptionLower}";

        // Vietnamese Political/Ideological subjects
        if (combinedText.Contains("marxism") || combinedText.Contains("marx-lenin") ||
            combinedText.Contains("hồ chí minh") || combinedText.Contains("ho chi minh") ||
            combinedText.Contains("tư tưởng") || combinedText.Contains("chính trị") ||
            combinedText.Contains("đảng cộng sản") || subjectCodeLower.StartsWith("mln") ||
            subjectCodeLower.StartsWith("hcm"))
        {
            return SubjectCategory.VietnamesePolitics;
        }

        // History subjects
        if (combinedText.Contains("history") || combinedText.Contains("lịch sử") ||
            combinedText.Contains("historical") || subjectCodeLower.StartsWith("his"))
        {
            return SubjectCategory.History;
        }

        // Vietnamese language/literature
        if (combinedText.Contains("vietnamese") || combinedText.Contains("tiếng việt") ||
            combinedText.Contains("văn học") || combinedText.Contains("ngữ văn") ||
            subjectCodeLower.StartsWith("vie") || subjectCodeLower.StartsWith("vlt"))
        {
            return SubjectCategory.VietnameseLiterature;
        }

        // Economics/Business
        if (combinedText.Contains("economics") || combinedText.Contains("kinh tế") ||
            combinedText.Contains("business") || combinedText.Contains("quản trị") ||
            combinedText.Contains("marketing") || subjectCodeLower.StartsWith("eco") ||
            subjectCodeLower.StartsWith("bus") || subjectCodeLower.StartsWith("mkt"))
        {
            return SubjectCategory.Business;
        }

        // Math/Physics/Chemistry (theory-based science)
        if (combinedText.Contains("mathematics") || combinedText.Contains("toán") ||
            combinedText.Contains("physics") || combinedText.Contains("vật lý") ||
            combinedText.Contains("chemistry") || combinedText.Contains("hóa học") ||
            subjectCodeLower.StartsWith("mat") || subjectCodeLower.StartsWith("phy") ||
            subjectCodeLower.StartsWith("che"))
        {
            return SubjectCategory.Science;
        }

        // Programming/Technology
        if (combinedText.Contains("programming") || combinedText.Contains("lập trình") ||
            combinedText.Contains("android") || combinedText.Contains("web") ||
            combinedText.Contains("mobile") || combinedText.Contains("software") ||
            combinedText.Contains("development") || combinedText.Contains("coding") ||
            subjectCodeLower.StartsWith("prm") || subjectCodeLower.StartsWith("prn") ||
            subjectCodeLower.StartsWith("mad") || subjectCodeLower.StartsWith("swd") ||
            subjectCodeLower.StartsWith("swp"))
        {
            return SubjectCategory.Programming;
        }

        return SubjectCategory.General;
    }

    private async Task<CreateSubjectResponse> UpdateExistingSubjectContent(
        Subject subjectToUpdate,
        SyllabusData syllabusData,
        CancellationToken cancellationToken)
    {
        var contentJson = JsonSerializer.Serialize(syllabusData.Content);

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new ObjectToInferredTypesConverter() }
        };
        subjectToUpdate.Content = JsonSerializer.Deserialize<Dictionary<string, object>>(contentJson, serializerOptions);

        subjectToUpdate.SubjectName = syllabusData.SubjectName;
        subjectToUpdate.Description = syllabusData.Description;
        subjectToUpdate.Credits = syllabusData.Credits;

        // Only update semester if provided
        if (syllabusData.Semester.HasValue)
        {
            subjectToUpdate.Semester = syllabusData.Semester.Value;
        }

        // Only update prerequisites if provided
        if (!string.IsNullOrWhiteSpace(syllabusData.PreRequisite))
        {
            subjectToUpdate.PrerequisiteSubjectIds = await ResolvePrerequisiteIdsAsync(
                syllabusData.PreRequisite, cancellationToken);
        }

        if (syllabusData.ApprovedDate.HasValue)
        {
            subjectToUpdate.UpdatedAt = new DateTimeOffset(syllabusData.ApprovedDate.Value.ToDateTime(TimeOnly.MinValue));
        }
        else
        {
            subjectToUpdate.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var resultSubject = await _subjectRepository.UpdateAsync(subjectToUpdate, cancellationToken);

        _logger.LogInformation("Successfully updated subject {SubjectId} with new enriched syllabus content.",
            resultSubject.Id);

        return _mapper.Map<CreateSubjectResponse>(resultSubject);
    }

    private async Task<Guid[]> ResolvePrerequisiteIdsAsync(string? preRequisiteText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(preRequisiteText))
        {
            return Array.Empty<Guid>();
        }

        var codes = preRequisiteText.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(code => code.Trim())
                                    .ToList();

        if (!codes.Any())
        {
            return Array.Empty<Guid>();
        }

        var prereqIds = new List<Guid>();
        foreach (var code in codes)
        {
            var subject = await _subjectRepository.FirstOrDefaultAsync(s => s.SubjectCode == code, cancellationToken);
            if (subject != null)
            {
                prereqIds.Add(subject.Id);
            }
            else
            {
                _logger.LogWarning("Could not resolve prerequisite subject code '{SubjectCode}' to a valid subject ID.", code);
            }
        }

        return prereqIds.ToArray();
    }

    private static string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
