// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/GenerateQuestSteps/GenerateQuestStepsCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;
using AutoMapper;
using System.Text.Json.Serialization;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;

/// <summary>
/// Handler for generating AI-driven quest steps with URL enrichment and validation.
/// This class orchestrates the entire quest step generation lifecycle:
/// 1. Validates prerequisites (user, quest, subject, skills)
/// 2. Unlocks relevant skills for the user
/// 3. Calls AI to generate quest steps
/// 4. Enriches Reading steps with URLs from syllabus
/// 5. Validates generated content
/// 6. Persists valid steps to database
/// </summary>
public class GenerateQuestStepsCommandHandler : IRequestHandler<GenerateQuestStepsCommand, List<GeneratedQuestStepDto>>
{
    #region Private Types

    /// <summary>
    /// This private record defines the expected structure of each step from the AI's JSON output.
    /// Matches the JSON schema returned by the AI generation plugin.
    /// </summary>
    private record AiQuestStep(
        [property: JsonPropertyName("stepNumber")] int StepNumber,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("stepType")] string StepType,
        [property: JsonPropertyName("experiencePoints")] int ExperiencePoints,
        [property: JsonPropertyName("content")] JsonElement Content
    );

    #endregion

    #region Dependencies

    private readonly IQuestRepository _questRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILogger<GenerateQuestStepsCommandHandler> _logger;
    private readonly IQuestGenerationPlugin _questGenerationPlugin;
    private readonly IMapper _mapper;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IClassRepository _classRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly ISubjectSkillMappingRepository _subjectSkillMappingRepository;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IUserSkillRepository _userSkillRepository;
    private readonly IUrlValidationService _urlValidationService;

    #endregion

    #region Constructor

    public GenerateQuestStepsCommandHandler(
        IQuestRepository questRepository,
        IQuestStepRepository questStepRepository,
        ISubjectRepository subjectRepository,
        ILogger<GenerateQuestStepsCommandHandler> logger,
        IQuestGenerationPlugin questGenerationPlugin,
        IMapper mapper,
        IUserProfileRepository userProfileRepository,
        IClassRepository classRepository,
        ISkillRepository skillRepository,
        ISubjectSkillMappingRepository subjectSkillMappingRepository,
        IPromptBuilder promptBuilder,
        IUserSkillRepository userSkillRepository,
        IUrlValidationService urlValidationService)
    {
        _questRepository = questRepository;
        _questStepRepository = questStepRepository;
        _subjectRepository = subjectRepository;
        _logger = logger;
        _questGenerationPlugin = questGenerationPlugin;
        _mapper = mapper;
        _userProfileRepository = userProfileRepository;
        _classRepository = classRepository;
        _skillRepository = skillRepository;
        _subjectSkillMappingRepository = subjectSkillMappingRepository;
        _promptBuilder = promptBuilder;
        _userSkillRepository = userSkillRepository;
        _urlValidationService = urlValidationService;
    }

    #endregion

    #region Main Handler

    /// <summary>
    /// Main entry point for quest step generation.
    /// Orchestrates the entire workflow from validation to AI generation to persistence.
    /// </summary>
    public async Task<List<GeneratedQuestStepDto>> Handle(GenerateQuestStepsCommand request, CancellationToken cancellationToken)
    {
        // ==================================================================================
        // SECTION 1: PRE-CONDITION CHECKS
        // Validate that the request is valid and necessary before proceeding.
        // ==================================================================================

        var questHasSteps = await _questStepRepository.QuestContainsSteps(request.QuestId, cancellationToken);
        if (questHasSteps)
        {
            throw new BadRequestException("Quest Steps already created.");
        }

        var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken)
            ?? throw new NotFoundException("User Profile not found.");

        if (!userProfile.ClassId.HasValue)
        {
            throw new BadRequestException("Please choose a Class first");
        }

        var userClass = await _classRepository.GetByIdAsync(userProfile.ClassId.Value, cancellationToken)
            ?? throw new BadRequestException("Class not found");

        var quest = await _questRepository.GetByIdAsync(request.QuestId, cancellationToken)
            ?? throw new NotFoundException("Quest", request.QuestId);

        if (quest.SubjectId is null)
        {
            throw new BadRequestException("Quest is not associated with a subject.");
        }

        var subject = await _subjectRepository.GetByIdAsync(quest.SubjectId.Value, cancellationToken)
            ?? throw new NotFoundException("Subject", quest.SubjectId.Value);

        if (subject.Content is null || !subject.Content.Any())
        {
            throw new BadRequestException("No syllabus content available for this quest's subject.");
        }

        // ==================================================================================
        // SECTION 2: SKILL UNLOCKING LOGIC
        // Determine which skills this quest teaches and unlock them for the user.
        // This ensures users can progress in skill trees as they complete quests.
        // ==================================================================================

        var skillMappings = await _subjectSkillMappingRepository.GetMappingsBySubjectIdsAsync(new[] { quest.SubjectId.Value }, cancellationToken);
        var relevantSkillIds = skillMappings.Select(m => m.SkillId).ToHashSet();
        if (!relevantSkillIds.Any())
        {
            _logger.LogWarning("No skills are mapped to Subject {SubjectId}. Cannot generate quest steps with skill context.", quest.SubjectId.Value);
            throw new BadRequestException("No skills are linked to this subject. Skill mapping may be incomplete.");
        }

        var relevantSkills = (await _skillRepository.GetAllAsync(cancellationToken))
            .Where(s => relevantSkillIds.Contains(s.Id))
            .ToList();

        // Unlock skills for the user if they don't already have them
        var existingUserSkills = await _userSkillRepository.GetSkillsByAuthIdAsync(request.AuthUserId, cancellationToken);
        var existingSkillIds = existingUserSkills.Select(us => us.SkillId).ToHashSet();

        int unlockedCount = 0;
        foreach (var skill in relevantSkills)
        {
            if (!existingSkillIds.Contains(skill.Id))
            {
                var newUserSkill = new UserSkill
                {
                    AuthUserId = request.AuthUserId,
                    SkillId = skill.Id,
                    SkillName = skill.Name,
                    ExperiencePoints = 0,
                    Level = 1
                };
                await _userSkillRepository.AddAsync(newUserSkill, cancellationToken);
                unlockedCount++;
            }
        }

        if (unlockedCount > 0)
        {
            _logger.LogInformation("Unlocked {Count} new skills for User {AuthUserId} upon starting Quest {QuestId}",
                unlockedCount, request.AuthUserId, request.QuestId);
        }

        // ==================================================================================
        // SECTION 3: DATA PREPARATION
        // Load user context and prepare syllabus data for AI consumption.
        // CRITICAL: Use Newtonsoft.Json to serialize subject.Content since it contains JArray objects.
        // ==================================================================================

        var userContext = await _promptBuilder.GenerateAsync(userProfile, userClass, cancellationToken);

        // Serialize using Newtonsoft.Json to properly handle JArray
        var syllabusJson = Newtonsoft.Json.JsonConvert.SerializeObject(
            subject.Content,
            Newtonsoft.Json.Formatting.Indented,
            new Newtonsoft.Json.JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
            }
        );

        // CRITICAL FIX: Deserialize using Newtonsoft.Json to match serialization
        SyllabusData? syllabusDataForDesc = null;
        try
        {
            syllabusDataForDesc = Newtonsoft.Json.JsonConvert.DeserializeObject<SyllabusData>(syllabusJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize syllabus data for enrichment. URL enrichment may be limited.");
        }

        // ==================================================================================
        // SECTION 3.5: DIAGNOSTIC LOGGING (COMPLETE VERSION)
        // Log syllabus content to verify URL presence before AI processing.
        // This helps debug issues with URL extraction and enrichment.
        // ==================================================================================

        _logger.LogWarning("=== DIAGNOSTIC: Checking syllabus content for URLs ===");
        _logger.LogWarning("Subject ID: {SubjectId}, Subject Name: {SubjectName}", subject.Id, subject.SubjectName);

        if (subject.Content != null && subject.Content.TryGetValue("SessionSchedule", out var sessionScheduleObj))
        {
            try
            {
                // The sessionScheduleObj is likely a JArray from Newtonsoft.Json
                string sessionScheduleJson;

                if (sessionScheduleObj is Newtonsoft.Json.Linq.JArray jArray)
                {
                    // Use Newtonsoft.Json serializer for JArray
                    _logger.LogInformation("✓ Detected Newtonsoft.Json JArray - using correct serializer");
                    sessionScheduleJson = Newtonsoft.Json.JsonConvert.SerializeObject(jArray, Newtonsoft.Json.Formatting.Indented);
                }
                else
                {
                    // Fallback to System.Text.Json for other types
                    sessionScheduleJson = JsonSerializer.Serialize(sessionScheduleObj, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        WriteIndented = true
                    });
                }

                _logger.LogWarning("SessionSchedule raw JSON (first 500 chars): {Json}",
                    sessionScheduleJson.Length > 500 ? sessionScheduleJson.Substring(0, 500) : sessionScheduleJson);

                // Deserialize to typed structure for validation
                var sessions = JsonSerializer.Deserialize<List<SyllabusSessionDto>>(
                    sessionScheduleJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (sessions != null && sessions.Any())
                {
                    _logger.LogWarning("✅ Successfully deserialized {Count} sessions", sessions.Count);

                    // Log first 3 sessions with detailed URL information
                    foreach (var session in sessions.Take(3))
                    {
                        _logger.LogWarning(
                            "Session {Num}: Topic='{Topic}', SuggestedUrl='{Url}', HasUrl={HasUrl}",
                            session.SessionNumber,
                            session.Topic ?? "NULL",
                            session.SuggestedUrl ?? "NULL",
                            !string.IsNullOrWhiteSpace(session.SuggestedUrl)
                        );
                    }
                }
                else
                {
                    _logger.LogError("❌ Deserialized sessions list is null or empty");
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "❌ JSON deserialization failed. Schema mismatch detected.");
                _logger.LogError("This likely means SessionSchedule structure doesn't match SyllabusSessionDto class definition.");
            }
        }
        else
        {
            _logger.LogError("❌ subject.Content is null or has no SessionSchedule key!");
        }

        // Log what's actually being sent to AI
        _logger.LogWarning("=== Syllabus JSON being sent to AI (first 1000 chars) ===");
        _logger.LogWarning(syllabusJson.Length > 1000 ? syllabusJson.Substring(0, 1000) : syllabusJson);
        _logger.LogWarning("=== End diagnostic ===");

        // ==================================================================================
        // SECTION 4: AI PROMPT AND INVOCATION
        // Generate the detailed prompt and call the AI plugin to create quest steps.
        // ==================================================================================

        var subjectName = subject.SubjectName;
        var courseDescription = syllabusDataForDesc?.Content?.CourseDescription
            ?? syllabusDataForDesc?.Description
            ?? subject.Description
            ?? "";

        var generatedStepsJson = await _questGenerationPlugin.GenerateQuestStepsJsonAsync(
            syllabusJson,
            userContext,
            relevantSkills,
            subjectName,
            courseDescription,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(generatedStepsJson))
        {
            _logger.LogError("AI plugin returned null or empty JSON for Quest {QuestId}.", request.QuestId);
            throw new InvalidOperationException("AI failed to generate valid quest steps.");
        }

        // ==================================================================================
        // SECTION 5: DESERIALIZE AND VALIDATE AI OUTPUT
        // Treat the AI's response as untrusted data and validate its structure.
        // ==================================================================================

        _logger.LogInformation("Attempting to deserialize AI-generated JSON for Quest {QuestId}: {JsonContent}",
            request.QuestId, generatedStepsJson);

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        List<AiQuestStep>? aiGeneratedSteps;
        try
        {
            aiGeneratedSteps = JsonSerializer.Deserialize<List<AiQuestStep>>(generatedStepsJson, serializerOptions);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON deserialization failed for Quest {QuestId}. Raw response was: {JsonContent}",
                request.QuestId, generatedStepsJson);
            throw new InvalidOperationException("AI failed to generate valid quest steps. The response was not in the correct format.", jsonEx);
        }

        if (aiGeneratedSteps is null || !aiGeneratedSteps.Any())
        {
            _logger.LogError("AI failed to generate valid quest steps from syllabus content for Quest {QuestId}.", request.QuestId);
            throw new InvalidOperationException("AI failed to generate valid quest steps.");
        }

        // ==================================================================================
        // SECTION 5.5: ENRICH READING STEPS WITH URLS
        // Post-process AI output to add missing URLs from syllabus using fuzzy matching.
        // This compensates for cases where the AI fails to extract URLs from the syllabus.
        // ==================================================================================

        if (syllabusDataForDesc != null)
        {
            EnrichReadingStepsWithUrls(aiGeneratedSteps, syllabusDataForDesc, request.QuestId);
        }

        // ==================================================================================
        // SECTION 6: PERSIST VALIDATED STEPS
        // Iterate through AI-generated steps, validate each one against business rules,
        // and only save the valid steps to the database.
        // ==================================================================================

        var generatedSteps = new List<QuestStep>();
        var subjectKeywords = ExtractKeywords(subjectName, courseDescription);

        _logger.LogInformation("Validating steps against subject keywords: {Keywords}",
            string.Join(", ", subjectKeywords));

        foreach (var aiStep in aiGeneratedSteps)
        {
            // Validate step type
            if (!Enum.TryParse<StepType>(aiStep.StepType, true, out var stepType))
            {
                _logger.LogWarning("AI generated a step with an unknown StepType '{StepType}'. Skipping step.", aiStep.StepType);
                continue;
            }

            // Validate skill ID
            if (!aiStep.Content.TryGetProperty("skillId", out var skillIdElement) ||
                !Guid.TryParse(skillIdElement.GetString(), out var skillId) ||
                !relevantSkillIds.Contains(skillId))
            {
                _logger.LogWarning("AI generated a step with an invalid or missing skillId for Quest {QuestId}. Skipping step.",
                    request.QuestId);
                continue;
            }

            // Special validation for Reading steps
            if (stepType == StepType.Reading)
            {
                // Check if this is a technical subject
                var isTechSubject = subjectKeywords.Any(k => new[]
                {
        "Android", "iOS", "ASP.NET", "React", "Vue", "Angular", "Java", "Kotlin",
        "Python", "C#", ".NET", "JavaScript", "TypeScript", "Swift", "Mobile", "Web"
    }.Contains(k, StringComparer.OrdinalIgnoreCase));

                // For tech subjects, validate topic relevance
                if (isTechSubject)
                {
                    var articleTitle = "";
                    if (aiStep.Content.TryGetProperty("articleTitle", out var titleElement))
                    {
                        articleTitle = titleElement.GetString() ?? "";
                    }

                    // CRITICAL FIX: Include articleTitle + description for broader matching
                    var stepText = $"{aiStep.Title} {aiStep.Description} {articleTitle}".ToLowerInvariant();

                    // CRITICAL FIX: Use more lenient matching - accept if ANY keyword appears OR if it's a common UI/tech term
                    var commonTechTerms = new[]
                    {
            "ui", "layout", "widget", "component", "view", "activity", "fragment",
            "theme", "style", "design", "interface", "app", "application", "build",
            "studio", "development", "programming", "constraint", "linear", "responsive"
        };

                    var hasRelevantKeyword = subjectKeywords.Any(keyword =>
                        stepText.Contains(keyword.ToLowerInvariant()));

                    var hasTechTerm = commonTechTerms.Any(term =>
                        stepText.Contains(term));

                    // Accept if it has either a subject keyword OR common tech term
                    if (!hasRelevantKeyword && !hasTechTerm)
                    {
                        _logger.LogWarning(
                            "AI generated off-topic Reading step '{Title}' for tech subject '{SubjectName}'. Expected keywords: {Keywords} or tech terms. Skipping step.",
                            aiStep.Title, subjectName, string.Join(", ", subjectKeywords));
                        continue;
                    }
                }

                // Validate URL if present
                if (aiStep.Content.TryGetProperty("url", out var urlElement) &&
                    urlElement.ValueKind == JsonValueKind.String)
                {
                    var url = urlElement.GetString() ?? "";

                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        // Check URL format
                        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                        {
                            _logger.LogWarning(
                                "AI generated a Reading step with malformed URL '{Url}' for Quest {QuestId}. Skipping step.",
                                url, request.QuestId);
                            continue;
                        }

                        // Check URL accessibility
                        if (!await _urlValidationService.IsUrlAccessibleAsync(url, cancellationToken))
                        {
                            _logger.LogWarning(
                                "AI generated a Reading step with inaccessible URL '{Url}' (404/error/soft 404) for Quest {QuestId}. Skipping step.",
                                url, request.QuestId);
                            continue;
                        }

                        _logger.LogInformation("Validated Reading step URL '{Url}' for Quest {QuestId}.",
                            url, request.QuestId);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Reading step '{Title}' has no URL (none found during enrichment). Step will be saved without URL.",
                            aiStep.Title);
                    }
                }
                else
                {
                    _logger.LogInformation("Reading step '{Title}' has no URL property. Step will be saved without URL.",
                        aiStep.Title);
                }
            }

            // Create and save valid step
            var newStep = new QuestStep
            {
                QuestId = request.QuestId,
                SkillId = skillId,
                StepNumber = aiStep.StepNumber,
                Title = aiStep.Title,
                Description = aiStep.Description,
                StepType = stepType,
                ExperiencePoints = aiStep.ExperiencePoints,
                Content = aiStep.Content.GetRawText()
            };

            generatedSteps.Add(newStep);
            await _questStepRepository.AddAsync(newStep, cancellationToken);
        }

        // Ensure at least some steps were valid
        if (!generatedSteps.Any())
        {
            _logger.LogError(
                "No valid quest steps were generated after validation for Quest {QuestId}. All AI-generated steps were rejected.",
                request.QuestId);
            throw new InvalidOperationException("AI failed to generate any valid quest steps after validation.");
        }

        _logger.LogInformation(
            "Successfully generated and saved {StepCount} valid steps (out of {TotalSteps} AI-generated) for Quest {QuestId}",
            generatedSteps.Count, aiGeneratedSteps.Count, request.QuestId);

        return _mapper.Map<List<GeneratedQuestStepDto>>(generatedSteps);
    }

    #endregion

    #region Helper Methods - Keyword Extraction

    /// <summary>
    /// Extracts relevant keywords from subject name and description.
    /// Used for validating topic relevance of AI-generated steps.
    /// Prioritizes technical keywords for tech subjects.
    /// </summary>
    /// <param name="subjectName">Name of the subject</param>
    /// <param name="courseDescription">Description of the course</param>
    /// <returns>List of extracted keywords for validation</returns>
    private List<string> ExtractKeywords(string subjectName, string courseDescription)
    {
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Predefined technical keywords to look for
        var techKeywords = new[]
        {
            "Android", "iOS", "Swift", "Kotlin", "Java", "Flutter", "React Native",
            "ASP.NET", "Core", ".NET", "C#", "MVC", "Blazor", "Razor",
            "React", "Vue", "Angular", "JavaScript", "TypeScript", "Node",
            "Python", "Django", "Flask", "FastAPI",
            "Spring", "Spring Boot", "Hibernate",
            "Mobile", "Web", "Desktop", "Cloud"
        };

        var textToScan = $"{subjectName} {courseDescription}".ToLowerInvariant();

        // Extract matching tech keywords
        foreach (var keyword in techKeywords)
        {
            if (textToScan.Contains(keyword.ToLowerInvariant()))
            {
                keywords.Add(keyword);
            }
        }

        // Extract significant words from subject name
        var subjectWords = subjectName
            .Split(new[] { ' ', '-', '_', '(', ')', '[', ']', ',', '.' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length > 3 && !IsCommonStopWord(word))
            .ToList();

        foreach (var word in subjectWords)
        {
            keywords.Add(word);
        }

        // Prioritize technical keywords if found
        if (keywords.Any(k => techKeywords.Contains(k, StringComparer.OrdinalIgnoreCase)))
        {
            return keywords.Where(k => techKeywords.Contains(k, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        // Fallback to generic keywords if no technical matches
        if (!keywords.Any())
        {
            return new List<string> { "education", "learning", "knowledge" };
        }

        return keywords.ToList();
    }

    /// <summary>
    /// Checks if a word is a common stop word that should be excluded from keywords.
    /// Stop words are common words that don't carry significant meaning for matching.
    /// </summary>
    private bool IsCommonStopWord(string word)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "and", "for", "with", "from", "into", "about", "this", "that",
            "which", "what", "when", "where", "who", "why", "how", "can", "will"
        };
        return stopWords.Contains(word);
    }

    #endregion

    #region Helper Methods - URL Enrichment

    /// <summary>
    /// Enriches Reading steps with URLs from the syllabus after AI generation.
    /// This ensures URLs are correctly populated even if the AI fails to extract them.
    /// Uses fuzzy matching to find the best matching session for each reading step.
    /// 
    /// Process:
    /// 1. Identify Reading steps with empty URLs
    /// 2. Extract article title from step content
    /// 3. Find best matching session from syllabus using fuzzy matching
    /// 4. Update step content with URL from matching session
    /// </summary>
    /// <param name="aiGeneratedSteps">List of AI-generated steps to enrich</param>
    /// <param name="syllabusData">Syllabus data containing session schedule with URLs</param>
    /// <param name="questId">ID of the quest for logging purposes</param>
    private void EnrichReadingStepsWithUrls(
        List<AiQuestStep> aiGeneratedSteps,
        SyllabusData syllabusData,
        Guid questId)
    {
        if (syllabusData?.Content?.SessionSchedule == null)
        {
            _logger.LogWarning("Cannot enrich URLs: syllabusData or SessionSchedule is null for Quest {QuestId}", questId);
            return;
        }

        var sessionSchedule = syllabusData.Content.SessionSchedule;

        _logger.LogInformation("Starting URL enrichment for {StepCount} steps using {SessionCount} syllabus sessions",
            aiGeneratedSteps.Count, sessionSchedule.Count);

        for (int i = 0; i < aiGeneratedSteps.Count; i++)
        {
            var step = aiGeneratedSteps[i];

            // Only process Reading steps
            if (!step.StepType.Equals("Reading", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Only enrich if the URL is currently empty
            if (step.Content.TryGetProperty("url", out var urlElement) &&
                !string.IsNullOrWhiteSpace(urlElement.GetString()))
            {
                _logger.LogInformation("Step '{Title}' already has a URL, skipping enrichment", step.Title);
                continue;
            }

            // Extract article title for matching
            if (!step.Content.TryGetProperty("articleTitle", out var articleTitleElement))
            {
                _logger.LogWarning("Reading step '{Title}' has no articleTitle property, cannot enrich", step.Title);
                continue;
            }

            var articleTitle = articleTitleElement.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(articleTitle))
            {
                _logger.LogWarning("Reading step '{Title}' has empty articleTitle, cannot enrich", step.Title);
                continue;
            }

            // Try to find matching session by topic similarity
            var matchingSession = FindBestMatchingSession(articleTitle, sessionSchedule);

            if (matchingSession != null && !string.IsNullOrWhiteSpace(matchingSession.SuggestedUrl))
            {
                // Reconstruct the content JSON with the correct URL
                var contentDict = new Dictionary<string, object?>();

                // Copy existing properties
                foreach (var prop in step.Content.EnumerateObject())
                {
                    // Use a robust method to convert JsonElement to a basic type
                    contentDict[prop.Name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString(),
                        JsonValueKind.Number => prop.Value.GetDecimal(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => JsonSerializer.Deserialize<object>(prop.Value.GetRawText())
                    };
                }

                // Overwrite or add the correct URL
                contentDict["url"] = matchingSession.SuggestedUrl;

                // Create a new AiQuestStep record with the updated content
                var updatedContentJson = JsonSerializer.Serialize(contentDict);
                var newContentElement = JsonDocument.Parse(updatedContentJson).RootElement;

                var newStep = step with { Content = newContentElement };
                aiGeneratedSteps[i] = newStep;

                _logger.LogInformation(
                    "✅ Enriched Reading step '{Title}' with URL '{Url}' from session #{SessionNumber} ('{Topic}') for Quest {QuestId}",
                    step.Title, matchingSession.SuggestedUrl, matchingSession.SessionNumber, matchingSession.Topic, questId);
            }
            else
            {
                _logger.LogWarning(
                    "❌ Could not find matching session with a URL for Reading step '{Title}' (articleTitle: '{ArticleTitle}') in Quest {QuestId}",
                    step.Title, articleTitle, questId);
            }
        }
    }

    /// <summary>
    /// Finds the best matching session for a given article title using three fuzzy matching strategies:
    /// 1. Exact match (normalized)
    /// 2. Contains match (bidirectional)
    /// 3. Keyword overlap using Jaccard similarity (60% threshold)
    /// 
    /// Returns the first match found in priority order, or null if no good match exists.
    /// </summary>
    /// <param name="articleTitle">Title of the article to match</param>
    /// <param name="sessions">List of syllabus sessions to search</param>
    /// <returns>Best matching session or null if no good match found</returns>
    private SyllabusSessionDto? FindBestMatchingSession(string articleTitle, List<SyllabusSessionDto> sessions)
    {
        var normalizedTitle = NormalizeTopicString(articleTitle);

        // Strategy 1: Exact match (after normalization)
        var exactMatch = sessions.FirstOrDefault(s =>
            NormalizeTopicString(s.Topic).Equals(normalizedTitle, StringComparison.OrdinalIgnoreCase));

        if (exactMatch != null)
        {
            _logger.LogInformation("Found exact match for '{Title}' → Session '{Topic}'", articleTitle, exactMatch.Topic);
            return exactMatch;
        }

        // Strategy 2: Contains match (bidirectional - either contains the other)
        var containsMatch = sessions.FirstOrDefault(s =>
        {
            var normalizedTopic = NormalizeTopicString(s.Topic);
            return normalizedTopic.Contains(normalizedTitle, StringComparison.OrdinalIgnoreCase) ||
                   normalizedTitle.Contains(normalizedTopic, StringComparison.OrdinalIgnoreCase);
        });

        if (containsMatch != null)
        {
            _logger.LogInformation("Found contains match for '{Title}' → Session '{Topic}'", articleTitle, containsMatch.Topic);
            return containsMatch;
        }

        // Strategy 3: Keyword overlap using Jaccard similarity (at least 60% similarity)
        var titleWords = GetSignificantWords(normalizedTitle);
        if (titleWords.Count == 0)
        {
            _logger.LogWarning("No significant words found in article title '{Title}'", articleTitle);
            return null;
        }

        var bestMatch = sessions
            .Select(s => new
            {
                Session = s,
                Score = CalculateWordOverlapScore(titleWords, GetSignificantWords(NormalizeTopicString(s.Topic)))
            })
            .Where(x => x.Score >= 0.6) // Require 60% similarity threshold
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        if (bestMatch != null)
        {
            _logger.LogInformation(
                "Found fuzzy match for '{Title}' → Session '{Topic}' (score: {Score:P0})",
                articleTitle, bestMatch.Session.Topic, bestMatch.Score);
        }

        return bestMatch?.Session;
    }

    /// <summary>
    /// Normalizes a topic string for comparison by:
    /// - Converting to lowercase
    /// - Replacing separators (-, _) with spaces
    /// - Collapsing multiple spaces
    /// - Trimming whitespace
    /// </summary>
    private string NormalizeTopicString(string topic)
    {
        return topic
            .Replace("-", " ")
            .Replace("_", " ")
            .Replace("  ", " ")
            .Trim()
            .ToLowerInvariant();
    }

    /// <summary>
    /// Extracts significant words from text by:
    /// - Splitting on common separators
    /// - Filtering out short words (< 3 chars)
    /// - Removing common stop words
    /// - Converting to lowercase
    /// 
    /// Used for fuzzy matching of topics.
    /// </summary>
    private HashSet<string> GetSignificantWords(string text)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "and", "or", "but", "in", "on", "at", "to", "for",
            "of", "with", "by", "from", "as", "is", "are", "was", "were", "be",
            "been", "being", "have", "has", "had", "do", "does", "did", "will",
            "would", "should", "could", "may", "might", "must", "can", "intro",
            "introduction", "overview", "part", "lesson"
        };

        return text
            .Split(new[] { ' ', ',', '.', ';', ':', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length > 2 && !stopWords.Contains(word))
            .Select(word => word.ToLowerInvariant())
            .ToHashSet();
    }

    /// <summary>
    /// Calculates word overlap score using Jaccard similarity index:
    /// Score = |intersection| / |union|
    /// 
    /// Returns a value between 0 (no overlap) and 1 (identical sets).
    /// This provides a normalized measure of text similarity based on shared keywords.
    /// </summary>
    private double CalculateWordOverlapScore(HashSet<string> words1, HashSet<string> words2)
    {
        if (words1.Count == 0 || words2.Count == 0)
        {
            return 0;
        }

        var intersection = words1.Intersect(words2).Count();
        var union = words1.Union(words2).Count();

        return (double)intersection / union; // Jaccard similarity
    }

    #endregion
}
