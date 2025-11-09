using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum;

public class GenerateQuestLineCommandHandler : IRequestHandler<GenerateQuestLine, GenerateQuestLineResponse>
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILearningPathRepository _learningPathRepository;
    private readonly IQuestChapterRepository _questChapterRepository;
    private readonly IQuestRepository _questRepository;
    private readonly ILogger<GenerateQuestLineCommandHandler> _logger;

    public GenerateQuestLineCommandHandler(
        ISubjectRepository subjectRepository,
        ILearningPathRepository learningPathRepository,
        IQuestChapterRepository questChapterRepository,
        IQuestRepository questRepository,
        ILogger<GenerateQuestLineCommandHandler> logger)
    {
        _subjectRepository = subjectRepository;
        _learningPathRepository = learningPathRepository;
        _questChapterRepository = questChapterRepository;
        _questRepository = questRepository;
        _logger = logger;
    }

    public async Task<GenerateQuestLineResponse> Handle(GenerateQuestLine request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating QuestLine for User: {userID}", request.AuthUserId);

        // 1. Create the top-level Learning Path
        var learningPath = new LearningPath
        {
            Name = $"Learning Path for ",
            Description = $"The primary learning journey based on curriculum {curriculumVersion.VersionCode}.",
            PathType = PathType.Course,
            CurriculumVersionId = curriculumVersion.Id,
            IsPublished = true, // Auto-publish for the user
            CreatedBy = request.AuthUserId
        };
        await _learningPathRepository.AddAsync(learningPath, cancellationToken);

        // 2. Fetch all subjects and structure for this version
        var structures = (await _curriculumStructureRepository.FindAsync(cs => cs.CurriculumVersionId == curriculumVersion.Id, cancellationToken)).ToList();
        var subjectIds = structures.Select(s => s.SubjectId).ToList();
        var subjects = (await _subjectRepository.GetAllAsync(cancellationToken)).Where(s => subjectIds.Contains(s.Id)).ToDictionary(s => s.Id);

        // 3. Group subjects by semester to create chapters
        var semesters = structures.GroupBy(s => s.Semester).OrderBy(g => g.Key);

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

            // 4. Create a Quest for each subject in the semester
            foreach (var structure in semesterGroup)
            {
                if (subjects.TryGetValue(structure.SubjectId, out var subject))
                {
                    var quest = new Quest
                    {
                        Title = subject.SubjectName,
                        Description = subject.Description ?? $"Complete the objectives for {subject.SubjectCode}.",
                        QuestType = QuestType.Practice, // Default type
                        DifficultyLevel = DifficultyLevel.Beginner, // Can be refined later
                        ExperiencePointsReward = subject.Credits * 50, // Example XP calculation
                        SubjectId = subject.Id,
                        IsActive = true,
                        CreatedBy = request.AuthUserId
                    };
                    // Quest is not directly linked to a chapter in the schema, but is linked via learning_path_quests
                    await _questRepository.AddAsync(quest, cancellationToken);

                    // Link quest to learning path
                    var learningPathQuest = new LearningPathQuest
                    {
                        LearningPathId = learningPath.Id,
                        QuestId = quest.Id,
                        DifficultyLevel = quest.DifficultyLevel,
                        SequenceOrder = questsCreated + 1, // Simple sequence for now
                        IsMandatory = structure.IsMandatory,
                    };
                    // Assuming you have a repository for LearningPathQuest
                    // await _learningPathQuestRepository.AddAsync(learningPathQuest, cancellationToken);

                    questsCreated++;
                }
            }
        }

        _logger.LogInformation("Successfully generated QuestLine {LearningPathId} with {Chapters} chapters and {Quests} quests.", learningPath.Id, chaptersCreated, questsCreated);

        return new GenerateQuestLineResponse
        {
            LearningPathId = learningPath.Id,
            ChaptersCreated = chaptersCreated,
            QuestsCreated = questsCreated
        };
    }
}
