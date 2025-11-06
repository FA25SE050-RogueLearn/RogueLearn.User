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
        // MODIFICATION: The AI will now return a skillId (UUID) instead of a skillTag (string).
        [property: JsonPropertyName("content")] JsonElement Content
    );

    private readonly IQuestRepository _questRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly ISyllabusVersionRepository _syllabusRepository;
    private readonly ILogger<GenerateQuestStepsCommandHandler> _logger;
    private readonly IQuestGenerationPlugin _questGenerationPlugin;
    private readonly IMapper _mapper;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IClassRepository _classRepository;
    // ADDED: We now need the skill repository to fetch skills for the prompt.
    private readonly ISkillRepository _skillRepository;

    public GenerateQuestStepsCommandHandler(
        IQuestRepository questRepository,
        IQuestStepRepository questStepRepository,
        ISyllabusVersionRepository syllabusRepository,
        ILogger<GenerateQuestStepsCommandHandler> logger,
        IQuestGenerationPlugin questGenerationPlugin,
        IMapper mapper,
        IUserProfileRepository userProfileRepository,
        IClassRepository classRepository,
        ISkillRepository skillRepository) // ADDED
    {
        _questRepository = questRepository;
        _questStepRepository = questStepRepository;
        _syllabusRepository = syllabusRepository;
        _logger = logger;
        _questGenerationPlugin = questGenerationPlugin;
        _mapper = mapper;
        _userProfileRepository = userProfileRepository;
        _classRepository = classRepository;
        _skillRepository = skillRepository; // ADDED
    }

    public async Task<List<GeneratedQuestStepDto>> Handle(GenerateQuestStepsCommand request, CancellationToken cancellationToken)
    {
        // MODIFICATION START: The unreliable FindAsync call has been replaced with the new,
        // specialized FindByQuestIdAsync method. This ensures that the check for existing
        // steps is now accurate and reliable, preventing unnecessary AI calls.
        var existingSteps = await _questStepRepository.FindByQuestIdAsync(request.QuestId, cancellationToken);
        // MODIFICATION END

        if (existingSteps.Any())
        {
            _logger.LogInformation("Quest steps already exist for Quest {QuestId}. Returning existing steps.", request.QuestId);
            return _mapper.Map<List<GeneratedQuestStepDto>>(existingSteps.OrderBy(s => s.StepNumber));
        }

        var quest = await _questRepository.GetByIdAsync(request.QuestId, cancellationToken);
        if (quest is null || quest.SubjectId is null)
        {
            throw new NotFoundException("Quest or its associated subject not found.");
        }

        var syllabus = (await _syllabusRepository.FindAsync(s => s.SubjectId == quest.SubjectId.Value && s.IsActive == true, cancellationToken))
            .OrderByDescending(s => s.VersionNumber)
            .FirstOrDefault();

        if (syllabus is null || syllabus.Content is null || !syllabus.Content.Any())
        {
            throw new BadRequestException("No active syllabus content available for this quest's subject.");
        }

        // --- MODIFICATION START: Fetch relevant skills using the new database column ---
        var relevantSkills = (await _skillRepository.FindAsync(s => s.SourceSubjectId == quest.SubjectId.Value, cancellationToken)).ToList();
        if (!relevantSkills.Any())
        {
            _logger.LogWarning("No skills are sourced from Subject {SubjectId}. Cannot generate quest steps with skill context.", quest.SubjectId.Value);
            throw new BadRequestException("No skills are linked to this subject. Skill initialization may be incomplete.");
        }
        // --- MODIFICATION END ---

        var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken);
        var userCareerClass = "General Learner";
        if (userProfile?.ClassId != null)
        {
            var careerClass = await _classRepository.GetByIdAsync(userProfile.ClassId.Value, cancellationToken);
            if (careerClass != null)
            {
                userCareerClass = careerClass.Name;
            }
        }
        string userContext = $"The user's career goal is '{userCareerClass}'. Tailor the content to be relevant for this role.";

        var syllabusJson = JsonSerializer.Serialize(syllabus.Content);

        // MODIFICATION: Pass the list of relevant skills (with their IDs) to the AI plugin.
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

        var relevantSkillIds = relevantSkills.Select(s => s.Id).ToHashSet();
        var generatedSteps = new List<QuestStep>();
        foreach (var aiStep in aiGeneratedSteps)
        {
            Enum.TryParse<Domain.Enums.StepType>(aiStep.StepType, true, out var stepType);

            // MODIFICATION: Validate that the skillId returned by the AI is one of the valid ones we provided.
            if (!aiStep.Content.TryGetProperty("skillId", out var skillIdElement) || !Guid.TryParse(skillIdElement.GetString(), out var skillId) || !relevantSkillIds.Contains(skillId))
            {
                _logger.LogWarning("AI generated a step with an invalid or missing skillId for Quest {QuestId}. Skipping step.", request.QuestId);
                continue; // Skip this invalid step
            }

            var newStep = new QuestStep
            {
                Id = Guid.NewGuid(),
                QuestId = request.QuestId,
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