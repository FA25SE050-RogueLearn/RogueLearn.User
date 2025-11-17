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

namespace RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;

public class GenerateQuestStepsCommandHandler : IRequestHandler<GenerateQuestStepsCommand, List<GeneratedQuestStepDto>>
{
    // This private record defines the expected structure of each step from the AI's JSON output.
    private record AiQuestStep(
        [property: JsonPropertyName("stepNumber")] int StepNumber,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("stepType")] string StepType,
        [property: JsonPropertyName("experiencePoints")] int ExperiencePoints,
        [property: JsonPropertyName("content")] JsonElement Content
    );

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
    private readonly IReadingUrlService _readingUrlService;
    private readonly IUrlValidationService _urlValidationService;

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
        IReadingUrlService readingUrlService,
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
        _readingUrlService = readingUrlService;
        _urlValidationService = urlValidationService;
    }

    public async Task<List<GeneratedQuestStepDto>> Handle(GenerateQuestStepsCommand request, CancellationToken cancellationToken)
    {
        // 1. PRE-CONDITION CHECKS: Ensure the request is valid and necessary.
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

        // 2. SKILL UNLOCKING LOGIC: Determine which skills this quest teaches and unlock them for the user.
        // This is the source of truth for skills, based on admin-curated data.
        var skillMappings = await _subjectSkillMappingRepository.GetMappingsBySubjectIdsAsync(new[] { quest.SubjectId.Value }, cancellationToken);
        var relevantSkillIds = skillMappings.Select(m => m.SkillId).ToHashSet();
        if (!relevantSkillIds.Any())
        {
            _logger.LogWarning("No skills are mapped to Subject {SubjectId}. Cannot generate quest steps with skill context.", quest.SubjectId.Value);
            throw new BadRequestException("No skills are linked to this subject. Skill mapping may be incomplete.");
        }
        var relevantSkills = (await _skillRepository.GetAllAsync(cancellationToken)).Where(s => relevantSkillIds.Contains(s.Id)).ToList();

        // Check which of these skills the user already has.
        var existingUserSkills = await _userSkillRepository.GetSkillsByAuthIdAsync(request.AuthUserId, cancellationToken);
        var existingSkillIds = existingUserSkills.Select(us => us.SkillId).ToHashSet();

        // For any skill linked to the subject that the user doesn't have, create a new record.
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
            _logger.LogInformation("Unlocked {Count} new skills for User {AuthUserId} upon starting Quest {QuestId}", unlockedCount, request.AuthUserId, request.QuestId);
        }

        // 3. DATA ENRICHMENT: Pre-process the syllabus to add live, validated URLs before sending it to the AI.
        var userContext = await _promptBuilder.GenerateAsync(userProfile, userClass, cancellationToken);
        var syllabusData = JsonSerializer.Deserialize<SyllabusData>(JsonSerializer.Serialize(subject.Content), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        int urlsEnrichedCount = 0;
        int urlsNotFoundCount = 0;

        if (syllabusData?.Content?.SessionSchedule != null)
        {
            _logger.LogInformation("Enriching syllabus with validated URLs for {Count} sessions.", syllabusData.Content.SessionSchedule.Count);

            foreach (var session in syllabusData.Content.SessionSchedule)
            {
                _logger.LogInformation("Attempting to find URL for session {SessionNumber}: '{Topic}'", session.SessionNumber, session.Topic);

                // This service checks for existing valid URLs first, validates them, then falls back to a web search.
                var foundUrl = await _readingUrlService.GetValidUrlForTopicAsync(session.Topic, session.Readings ?? new List<string>(), cancellationToken);

                if (!string.IsNullOrWhiteSpace(foundUrl))
                {
                    session.SuggestedUrl = foundUrl;
                    urlsEnrichedCount++;
                    _logger.LogInformation("✓ Found valid URL for session {SessionNumber} '{Topic}': {Url}",
                        session.SessionNumber, session.Topic, foundUrl);
                }
                else
                {
                    urlsNotFoundCount++;
                    _logger.LogWarning("✗ Could not find a valid URL for session {SessionNumber} '{Topic}'. AI will generate steps without URLs.",
                        session.SessionNumber, session.Topic);
                }
            }

            _logger.LogInformation("URL enrichment complete: {SuccessCount} URLs found, {FailCount} not found",
                urlsEnrichedCount, urlsNotFoundCount);
        }
        else
        {
            _logger.LogWarning("No SessionSchedule found in syllabus data for Quest {QuestId}", request.QuestId);
        }

        // 4. AI PROMPT AND INVOCATION: Generate the detailed prompt and call the AI.
        var enrichedSyllabusJson = JsonSerializer.Serialize(syllabusData, new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

        // Extract subject/course name to guide AI
        var subjectName = subject.SubjectName ?? syllabusData?.SubjectName ?? "Unknown Subject";
        var courseDescription = syllabusData?.Content?.CourseDescription ?? syllabusData?.Description ?? "";

        var generatedStepsJson = await _questGenerationPlugin.GenerateQuestStepsJsonAsync(
            enrichedSyllabusJson,
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

        // 5. DESERIALIZE AND VALIDATE AI OUTPUT: Treat the AI's response as untrusted data.
        _logger.LogInformation("Attempting to deserialize AI-generated JSON for Quest {QuestId}: {JsonContent}", request.QuestId, generatedStepsJson);
        var serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } };
        List<AiQuestStep>? aiGeneratedSteps;
        try
        {
            aiGeneratedSteps = JsonSerializer.Deserialize<List<AiQuestStep>>(generatedStepsJson, serializerOptions);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON deserialization failed for Quest {QuestId}. Raw response was: {JsonContent}", request.QuestId, generatedStepsJson);
            throw new InvalidOperationException("AI failed to generate valid quest steps. The response was not in the correct format.", jsonEx);
        }

        if (aiGeneratedSteps is null || !aiGeneratedSteps.Any())
        {
            _logger.LogError("AI failed to generate valid quest steps from syllabus content for Quest {QuestId}.", request.QuestId);
            throw new InvalidOperationException("AI failed to generate valid quest steps.");
        }

        // 6. PERSIST VALIDATED STEPS: Iterate through the AI's suggestions, validate each one, and only save the valid steps.
        var generatedSteps = new List<QuestStep>();

        // Extract key terms from subject/course to validate topic consistency
        var subjectKeywords = ExtractKeywords(subjectName, courseDescription);
        _logger.LogInformation("Validating steps against subject keywords: {Keywords}", string.Join(", ", subjectKeywords));

        foreach (var aiStep in aiGeneratedSteps)
        {
            if (!Enum.TryParse<StepType>(aiStep.StepType, true, out var stepType))
            {
                _logger.LogWarning("AI generated a step with an unknown StepType '{StepType}'. Skipping step.", aiStep.StepType);
                continue;
            }

            // VALIDATION A: Check if the skillId is valid and from our pre-approved list.
            if (!aiStep.Content.TryGetProperty("skillId", out var skillIdElement) ||
                !Guid.TryParse(skillIdElement.GetString(), out var skillId) ||
                !relevantSkillIds.Contains(skillId))
            {
                _logger.LogWarning("AI generated a step with an invalid or missing skillId for Quest {QuestId}. Skipping step.", request.QuestId);
                continue; // Discard this invalid step.
            }

            // VALIDATION B: If it's a Reading step, validate topic consistency and URL if provided
            if (stepType == StepType.Reading)
            {
                // Topic consistency check: Only validate for tech subjects to avoid cross-technology contamination
                var isTechSubject = subjectKeywords.Any(k => new[]
                {
                    "Android", "iOS", "ASP.NET", "React", "Vue", "Angular", "Java", "Kotlin",
                    "Python", "C#", ".NET", "JavaScript", "TypeScript", "Swift", "Mobile", "Web"
                }.Contains(k, StringComparer.OrdinalIgnoreCase));

                if (isTechSubject)
                {
                    var articleTitle = "";
                    if (aiStep.Content.TryGetProperty("articleTitle", out var titleElement))
                    {
                        articleTitle = titleElement.GetString() ?? "";
                    }

                    var stepText = $"{aiStep.Title} {aiStep.Description} {articleTitle}".ToLowerInvariant();

                    // Check if step contains at least one relevant keyword
                    var hasRelevantKeyword = subjectKeywords.Any(keyword =>
                        stepText.Contains(keyword.ToLowerInvariant()));

                    if (!hasRelevantKeyword)
                    {
                        _logger.LogWarning("AI generated off-topic Reading step '{Title}' for tech subject '{SubjectName}'. Expected keywords: {Keywords}. Skipping step.",
                            aiStep.Title, subjectName, string.Join(", ", subjectKeywords));
                        continue;
                    }
                }
                // For non-tech subjects (History, Philosophy, etc.), skip topic validation
                // The AI prompt is sufficient to keep content on-topic

                // Check if URL exists and is not empty
                if (aiStep.Content.TryGetProperty("url", out var urlElement) &&
                    urlElement.ValueKind == JsonValueKind.String)
                {
                    var url = urlElement.GetString() ?? "";

                    // If URL is provided and not empty, validate it
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        // Check if it's a well-formed absolute URI
                        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                        {
                            _logger.LogWarning("AI generated a Reading step with malformed URL '{Url}' for Quest {QuestId}. Skipping step.", url, request.QuestId);
                            continue;
                        }

                        // CRITICAL: Validate the URL is actually accessible and not a soft 404
                        if (!await _urlValidationService.IsUrlAccessibleAsync(url, cancellationToken))
                        {
                            _logger.LogWarning("AI generated a Reading step with inaccessible URL '{Url}' (404/error/soft 404) for Quest {QuestId}. Skipping step.", url, request.QuestId);
                            continue;
                        }

                        _logger.LogInformation("Validated Reading step URL '{Url}' for Quest {QuestId}.", url, request.QuestId);
                    }
                    else
                    {
                        // Empty URL - this is acceptable if no valid URL could be found during enrichment
                        _logger.LogInformation("Reading step '{Title}' has no URL (none found during enrichment). Step will be saved without URL.", aiStep.Title);
                    }
                }
                else
                {
                    // No URL property at all - acceptable if enrichment couldn't find any
                    _logger.LogInformation("Reading step '{Title}' has no URL property. Step will be saved without URL.", aiStep.Title);
                }
            }

            // If all validations pass, create the entity to be saved.
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

        if (!generatedSteps.Any())
        {
            _logger.LogError("No valid quest steps were generated after validation for Quest {QuestId}. All AI-generated steps were rejected.", request.QuestId);
            throw new InvalidOperationException("AI failed to generate any valid quest steps after validation.");
        }

        _logger.LogInformation("Successfully generated and saved {StepCount} valid steps (out of {TotalSteps} AI-generated) for Quest {QuestId}",
            generatedSteps.Count, aiGeneratedSteps.Count, request.QuestId);

        return _mapper.Map<List<GeneratedQuestStepDto>>(generatedSteps);
    }

    private List<string> ExtractKeywords(string subjectName, string courseDescription)
    {
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Common technology keywords
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

        // Extract technology keywords
        foreach (var keyword in techKeywords)
        {
            if (textToScan.Contains(keyword.ToLowerInvariant()))
            {
                keywords.Add(keyword);
            }
        }

        // Extract meaningful words from subject name (length > 3 to skip articles/prepositions)
        var subjectWords = subjectName
            .Split(new[] { ' ', '-', '_', '(', ')', '[', ']', ',', '.' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length > 3 && !IsCommonStopWord(word))
            .ToList();

        foreach (var word in subjectWords)
        {
            keywords.Add(word);
        }

        // If we found tech keywords, return only those (more precise for tech subjects)
        if (keywords.Any(k => techKeywords.Contains(k, StringComparer.OrdinalIgnoreCase)))
        {
            return keywords.Where(k => techKeywords.Contains(k, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        // For non-tech subjects (History, Philosophy, etc.), return all meaningful words
        // But if we still don't have any keywords, just return a very permissive list
        if (!keywords.Any())
        {
            // Very permissive - accept anything
            return new List<string> { "education", "learning", "knowledge" };
        }

        return keywords.ToList();
    }

    private bool IsCommonStopWord(string word)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "and", "for", "with", "from", "into", "about", "this", "that",
            "which", "what", "when", "where", "who", "why", "how", "can", "will"
        };
        return stopWords.Contains(word);
    }
}