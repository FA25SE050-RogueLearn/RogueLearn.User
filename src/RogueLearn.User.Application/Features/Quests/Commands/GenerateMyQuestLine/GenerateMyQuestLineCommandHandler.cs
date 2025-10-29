// src/RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/GenerateMyQuestLine/GenerateMyQuestLineCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateCurriculum;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Quests.Commands.GenerateMyQuestLine;

public class GenerateMyQuestLineCommandHandler : IRequestHandler<GenerateMyQuestLineCommand, GenerateMyQuestLineResponse>
{
    private readonly IFlmExtractionPlugin _flmPlugin;
    private readonly CurriculumImportDataValidator _validator;
    private readonly ILearningPathRepository _learningPathRepository;
    private readonly IQuestChapterRepository _questChapterRepository;
    private readonly IQuestRepository _questRepository;
    private readonly ILogger<GenerateMyQuestLineCommandHandler> _logger;

    public GenerateMyQuestLineCommandHandler(
        IFlmExtractionPlugin flmPlugin,
        CurriculumImportDataValidator validator,
        ILearningPathRepository learningPathRepository,
        IQuestChapterRepository questChapterRepository,
        IQuestRepository questRepository,
        ILogger<GenerateMyQuestLineCommandHandler> logger)
    {
        _flmPlugin = flmPlugin;
        _validator = validator;
        _learningPathRepository = learningPathRepository;
        _questChapterRepository = questChapterRepository;
        _questRepository = questRepository;
        _logger = logger;
    }

    public async Task<GenerateMyQuestLineResponse> Handle(GenerateMyQuestLineCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GenerateMyQuestLineCommand for AuthUserId: {AuthUserId}", request.AuthUserId);

        // 1. Extract structured data from raw text using AI
        var extractedJson = await _flmPlugin.ExtractCurriculumJsonAsync(request.RawCurriculumText, cancellationToken);
        if (string.IsNullOrEmpty(extractedJson))
        {
            throw new BadRequestException("Failed to extract curriculum data from the provided text.");
        }

        CurriculumImportData? curriculumData;
        try
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
            curriculumData = JsonSerializer.Deserialize<CurriculumImportData>(extractedJson, jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI-extracted JSON for user-generated questline.");
            throw new BadRequestException("Could not understand the provided curriculum structure.");
        }

        if (curriculumData == null)
        {
            throw new BadRequestException("No valid curriculum data was extracted from the text.");
        }

        // 2. Validate the extracted data (in-memory)
        var validationResult = await _validator.ValidateAsync(curriculumData, cancellationToken);
        if (!validationResult.IsValid)
        {
            // For simplicity, we throw a generic error. In a real app, you might return detailed validation errors.
            _logger.LogWarning("Validation failed for user-provided curriculum. Errors: {Errors}", validationResult.Errors);
            throw new BadRequestException("The provided curriculum has validation errors.");
        }

        // 3. Create a custom, user-specific Learning Path (NOT linked to a global curriculum version)
        var learningPath = new LearningPath
        {
            Name = curriculumData.Program.ProgramName ?? "My Custom QuestLine",
            Description = curriculumData.Program.Description ?? "A personalized learning journey.",
            PathType = PathType.Custom, // This indicates it's user-generated
            IsPublished = true,
            CreatedBy = request.AuthUserId
        };
        await _learningPathRepository.AddAsync(learningPath, cancellationToken);

        // 4. Group by semester to create chapters
        var semesters = curriculumData.Structure.GroupBy(s => s.TermNumber).OrderBy(g => g.Key);
        int chaptersCreated = 0;
        int questsCreated = 0;

        foreach (var semesterGroup in semesters)
        {
            var chapter = new QuestChapter
            {
                LearningPathId = learningPath.Id,
                Title = $"Semester {semesterGroup.Key}",
                Sequence = semesterGroup.Key,
                Status = semesterGroup.Key == 1 ? PathProgressStatus.InProgress : PathProgressStatus.NotStarted
            };
            await _questChapterRepository.AddAsync(chapter, cancellationToken);
            chaptersCreated++;

            // 5. Create a Quest for each subject in the semester
            foreach (var structure in semesterGroup)
            {
                var subjectData = curriculumData.Subjects.FirstOrDefault(s => s.SubjectCode == structure.SubjectCode);
                if (subjectData != null)
                {
                    var quest = new Quest
                    {
                        Title = subjectData.SubjectName,
                        Description = subjectData.Description ?? $"Complete the objectives for {subjectData.SubjectCode}.",
                        QuestType = QuestType.Practice,
                        DifficultyLevel = DifficultyLevel.Beginner,
                        ExperiencePointsReward = subjectData.Credits * 50,
                        // Not linking to a global subjectId as this is a custom path
                        IsActive = true,
                        CreatedBy = request.AuthUserId
                    };
                    await _questRepository.AddAsync(quest, cancellationToken);

                    var learningPathQuest = new LearningPathQuest
                    {
                        LearningPathId = learningPath.Id,
                        QuestId = quest.Id,
                        DifficultyLevel = quest.DifficultyLevel,
                        SequenceOrder = questsCreated + 1,
                        IsMandatory = structure.IsMandatory
                    };
                    // await _learningPathQuestRepository.AddAsync(learningPathQuest, cancellationToken);
                    questsCreated++;
                }
            }
        }

        _logger.LogInformation("Successfully generated custom QuestLine {LearningPathId} for user {AuthUserId}", learningPath.Id, request.AuthUserId);

        return new GenerateMyQuestLineResponse
        {
            LearningPathId = learningPath.Id,
            LearningPathName = learningPath.Name,
            ChaptersCreated = chaptersCreated,
            QuestsCreated = questsCreated
        };
    }
}