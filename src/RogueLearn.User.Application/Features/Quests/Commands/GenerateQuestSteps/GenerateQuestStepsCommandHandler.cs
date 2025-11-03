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
    // The internal record now includes ExperiencePoints.
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
    private readonly ISyllabusVersionRepository _syllabusRepository;
    private readonly ILogger<GenerateQuestStepsCommandHandler> _logger;
    private readonly IQuestGenerationPlugin _questGenerationPlugin;
    private readonly IMapper _mapper;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IClassRepository _classRepository;

    public GenerateQuestStepsCommandHandler(
        IQuestRepository questRepository,
        IQuestStepRepository questStepRepository,
        ISubjectRepository subjectRepository,
        ISyllabusVersionRepository syllabusRepository,
        ILogger<GenerateQuestStepsCommandHandler> logger,
        IQuestGenerationPlugin questGenerationPlugin,
        IMapper mapper,
        IUserProfileRepository userProfileRepository,
        IClassRepository classRepository)
    {
        _questRepository = questRepository;
        _questStepRepository = questStepRepository;
        _subjectRepository = subjectRepository;
        _syllabusRepository = syllabusRepository;
        _logger = logger;
        _questGenerationPlugin = questGenerationPlugin;
        _mapper = mapper;
        _userProfileRepository = userProfileRepository;
        _classRepository = classRepository;
    }

    public async Task<List<GeneratedQuestStepDto>> Handle(GenerateQuestStepsCommand request, CancellationToken cancellationToken)
    {
        var existingSteps = await _questStepRepository.FindAsync(s => s.QuestId == request.QuestId, cancellationToken);
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

        // MODIFICATION: The check is updated to handle the new Dictionary type.
        // It now verifies that the Content dictionary is not null and contains entries.
        if (syllabus is null || syllabus.Content is null || !syllabus.Content.Any())
        {
            throw new BadRequestException("No active syllabus content available for this quest's subject.");
        }

        _logger.LogInformation("Generating quest steps for Quest {QuestId} based on syllabus {SyllabusVersionId}", request.QuestId, syllabus.Id);

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

        // MODIFICATION: The syllabus.Content dictionary is serialized back into a JSON string
        // before being passed to the AI plugin, which expects string input.
        var syllabusJson = JsonSerializer.Serialize(syllabus.Content);
        var generatedStepsJson = await _questGenerationPlugin.GenerateQuestStepsJsonAsync(syllabusJson, userContext, cancellationToken);

        if (string.IsNullOrWhiteSpace(generatedStepsJson))
        {
            _logger.LogError("AI plugin returned null or empty JSON for Quest {QuestId}. The AI call may have failed or produced no output.", request.QuestId);
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
            _logger.LogError(jsonEx, "JSON deserialization failed for Quest {QuestId}. The AI returned malformed JSON. Raw response was: {JsonContent}", request.QuestId, generatedStepsJson);
            throw new InvalidOperationException("AI failed to generate valid quest steps. The response was not in the correct format.", jsonEx);
        }

        if (aiGeneratedSteps is null || !aiGeneratedSteps.Any())
        {
            _logger.LogError("AI failed to generate valid quest steps from syllabus content. Deserialized JSON was empty or invalid for Quest {QuestId}.", request.QuestId);
            throw new InvalidOperationException("AI failed to generate valid quest steps.");
        }

        var generatedSteps = new List<QuestStep>();
        foreach (var aiStep in aiGeneratedSteps)
        {
            Enum.TryParse<Domain.Enums.StepType>(aiStep.StepType, true, out var stepType);

            var newStep = new QuestStep
            {
                Id = Guid.NewGuid(),
                QuestId = request.QuestId,
                StepNumber = aiStep.StepNumber,
                Title = aiStep.Title,
                Description = aiStep.Description,
                StepType = stepType,
                // The ExperiencePoints property is now mapped from the AI response.
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