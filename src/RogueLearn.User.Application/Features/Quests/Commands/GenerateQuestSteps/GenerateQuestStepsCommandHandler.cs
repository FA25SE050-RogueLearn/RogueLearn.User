// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/GenerateQuestSteps/GenerateQuestStepsCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;

public class GenerateQuestStepsCommandHandler : IRequestHandler<GenerateQuestStepsCommand, List<QuestStep>>
{
    private readonly IQuestRepository _questRepository;
    private readonly IQuestStepRepository _questStepRepository; // Assuming this exists
    private readonly ISubjectRepository _subjectRepository;
    private readonly ISyllabusVersionRepository _syllabusRepository;
    private readonly ILogger<GenerateQuestStepsCommandHandler> _logger;
    // This plugin will be responsible for the AI call
    private readonly IFlmExtractionPlugin _contentGenerationPlugin;

    public GenerateQuestStepsCommandHandler(
        IQuestRepository questRepository,
        IQuestStepRepository questStepRepository,
        ISubjectRepository subjectRepository,
        ISyllabusVersionRepository syllabusRepository,
        ILogger<GenerateQuestStepsCommandHandler> logger,
        IFlmExtractionPlugin contentGenerationPlugin)
    {
        _questRepository = questRepository;
        _questStepRepository = questStepRepository;
        _subjectRepository = subjectRepository;
        _syllabusRepository = syllabusRepository;
        _logger = logger;
        _contentGenerationPlugin = contentGenerationPlugin;
    }

    public async Task<List<QuestStep>> Handle(GenerateQuestStepsCommand request, CancellationToken cancellationToken)
    {
        var quest = await _questRepository.GetByIdAsync(request.QuestId, cancellationToken);
        if (quest is null || quest.SubjectId is null)
        {
            throw new NotFoundException("Quest or its associated subject not found.");
        }

        var syllabus = (await _syllabusRepository.FindAsync(s => s.SubjectId == quest.SubjectId.Value, cancellationToken))
            .OrderByDescending(s => s.VersionNumber)
            .FirstOrDefault();

        if (syllabus is null || string.IsNullOrWhiteSpace(syllabus.Content))
        {
            throw new BadRequestException("No syllabus content available for this quest's subject.");
        }

        _logger.LogInformation("Generating quest steps for Quest {QuestId} based on syllabus {SyllabusVersionId}", request.QuestId, syllabus.Id);

        // Here we would invoke the AI with the syllabus content to get back a list of QuestStep objects.
        // For now, we will simulate this.
        // var generatedStepsJson = await _contentGenerationPlugin.GenerateQuestStepsJsonAsync(syllabus.Content);
        // var generatedSteps = JsonSerializer.Deserialize<List<QuestStep>>(generatedStepsJson);

        // Simulating the AI response for now
        var generatedSteps = new List<QuestStep>
        {
            new QuestStep { QuestId = request.QuestId, StepNumber = 1, Title = "Introduction to " + quest.Title, Description = "Learn the basics.", StepType = Domain.Enums.StepType.Reading },
            new QuestStep { QuestId = request.QuestId, StepNumber = 2, Title = "Core Concepts", Description = "A short quiz on core concepts.", StepType = Domain.Enums.StepType.Quiz },
            new QuestStep { QuestId = request.QuestId, StepNumber = 3, Title = "Practical Challenge", Description = "Apply what you've learned.", StepType = Domain.Enums.StepType.Coding }
        };

        if (generatedSteps is null || !generatedSteps.Any())
        {
            throw new InvalidOperationException("AI failed to generate valid quest steps.");
        }

        // Persist the generated steps
        foreach (var step in generatedSteps)
        {
            step.Id = Guid.NewGuid(); // Ensure a new GUID
            await _questStepRepository.AddAsync(step, cancellationToken);
        }

        _logger.LogInformation("Successfully generated and saved {StepCount} steps for Quest {QuestId}", generatedSteps.Count, request.QuestId);

        return generatedSteps;
    }
}