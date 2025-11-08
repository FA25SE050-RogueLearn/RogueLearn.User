// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/GenerateQuestSteps/GenerateQuestStepsCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;
using AutoMapper;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;

public class GenerateQuestStepsCommandHandler : IRequestHandler<GenerateQuestStepsCommand, List<GeneratedQuestStepDto>>
{
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
    // MODIFICATION: Added the mapping repository to fetch the pre-approved skills for a subject.
    private readonly ISubjectSkillMappingRepository _subjectSkillMappingRepository;

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
        ISubjectSkillMappingRepository subjectSkillMappingRepository) // MODIFICATION: Injected repository.
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
        _subjectSkillMappingRepository = subjectSkillMappingRepository; // MODIFICATION: Assigned repository.
    }

    public async Task<List<GeneratedQuestStepDto>> Handle(GenerateQuestStepsCommand request, CancellationToken cancellationToken)
    {
        var existingSteps = await _questStepRepository.FindByQuestIdAsync(request.QuestId, cancellationToken);

        if (existingSteps.Any())
        {
            _logger.LogInformation("Quest steps already exist for Quest {QuestId}. Returning existing steps.", request.QuestId);
            return _mapper.Map<List<GeneratedQuestStepDto>>(existingSteps.OrderBy(s => s.StepNumber));
        }

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

        // MODIFICATION: This is the implementation of the "Foundation" stage.
        // We fetch the admin-curated skill mappings for the current subject.
        var skillMappings = await _subjectSkillMappingRepository.GetMappingsBySubjectIdsAsync(new[] { quest.SubjectId.Value }, cancellationToken);
        var relevantSkillIds = skillMappings.Select(m => m.SkillId).ToHashSet();
        if (!relevantSkillIds.Any())
        {
            _logger.LogWarning("No skills are mapped to Subject {SubjectId}. Cannot generate quest steps with skill context.", quest.SubjectId.Value);
            throw new BadRequestException("No skills are linked to this subject. Skill mapping may be incomplete.");
        }
        var relevantSkills = (await _skillRepository.GetAllAsync(cancellationToken)).Where(s => relevantSkillIds.Contains(s.Id)).ToList();

        var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken);
        var userCareerClass = "General Learner";
        if (userProfile?.ClassId != null)
        {
            var careerClass = await _classRepository.GetByIdAsync(userProfile.ClassId.Value, cancellationToken);
            userCareerClass = careerClass?.Name ?? userCareerClass;
        }
        string userContext = $"The user's career goal is '{userCareerClass}'. Tailor the content to be relevant for this role.";

        var syllabusJson = JsonSerializer.Serialize(subject.Content);

        // MODIFICATION: The curated list of skills is now passed to the AI plugin.
        var generatedStepsJson = await _questGenerationPlugin.GenerateQuestStepsJsonAsync(syllabusJson, userContext, relevantSkills, cancellationToken);

        if (string.IsNullOrWhiteSpace(generatedStepsJson))
        {
            _logger.LogError("AI plugin returned null or empty JSON for Quest {QuestId}.", request.QuestId);
            throw new InvalidOperationException("AI failed to generate valid quest steps.");
        }

        _logger.LogInformation("Attempting to deserialize AI-generated JSON for Quest {QuestId}: {JsonContent}", request.QuestId, generatedStepsJson);

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
            _logger.LogError(jsonEx, "JSON deserialization failed for Quest {QuestId}. Raw response was: {JsonContent}", request.QuestId, generatedStepsJson);
            throw new InvalidOperationException("AI failed to generate valid quest steps. The response was not in the correct format.", jsonEx);
        }

        if (aiGeneratedSteps is null || !aiGeneratedSteps.Any())
        {
            _logger.LogError("AI failed to generate valid quest steps from syllabus content for Quest {QuestId}.", request.QuestId);
            throw new InvalidOperationException("AI failed to generate valid quest steps.");
        }

        var generatedSteps = new List<QuestStep>();
        foreach (var aiStep in aiGeneratedSteps)
        {
            Enum.TryParse<Domain.Enums.StepType>(aiStep.StepType, true, out var stepType);

            // MODIFICATION: This is a critical security and integrity check.
            // We verify that the skillId returned by the AI is one of the valid IDs we provided from our admin-curated list.
            // This prevents the AI from hallucinating invalid IDs and corrupting the data.
            if (!aiStep.Content.TryGetProperty("skillId", out var skillIdElement) || !Guid.TryParse(skillIdElement.GetString(), out var skillId) || !relevantSkillIds.Contains(skillId))
            {
                _logger.LogWarning("AI generated a step with an invalid or missing skillId for Quest {QuestId}. Skipping step.", request.QuestId);
                continue; // Skip this step as it's invalid.
            }

            var newStep = new QuestStep
            {
                QuestId = request.QuestId,
                SkillId = skillId, // Set the validated skill ID
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

        _logger.LogInformation("Successfully generated and saved {StepCount} steps for Quest {QuestId}", generatedSteps.Count, request.QuestId);

        return _mapper.Map<List<GeneratedQuestStepDto>>(generatedSteps);
    }
}