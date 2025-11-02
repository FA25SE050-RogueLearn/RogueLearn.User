// RogueLearn.User/src/RogueLearn.User.Application/Features/LearningPaths/Queries/GetMyLearningPath/GetMyLearningPathQueryHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums; // Required for QuestStatus and PathProgressStatus enums

namespace RogueLearn.User.Application.Features.LearningPaths.Queries.GetMyLearningPath;

public class GetMyLearningPathQueryHandler : IRequestHandler<GetMyLearningPathQuery, LearningPathDto?>
{
    private readonly ILearningPathRepository _learningPathRepository;
    private readonly IQuestChapterRepository _questChapterRepository;
    private readonly ILearningPathQuestRepository _learningPathQuestRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IUserQuestProgressRepository _userQuestProgressRepository;
    private readonly ILogger<GetMyLearningPathQueryHandler> _logger;
    private readonly ICurriculumStructureRepository _curriculumStructureRepository;
    private readonly ISubjectRepository _subjectRepository;


    public GetMyLearningPathQueryHandler(
        ILearningPathRepository learningPathRepository,
        IQuestChapterRepository questChapterRepository,
        ILearningPathQuestRepository learningPathQuestRepository,
        IQuestRepository questRepository,
        IUserQuestProgressRepository userQuestProgressRepository,
        ILogger<GetMyLearningPathQueryHandler> logger,
        ICurriculumStructureRepository curriculumStructureRepository,
        ISubjectRepository subjectRepository)
    {
        _learningPathRepository = learningPathRepository;
        _questChapterRepository = questChapterRepository;
        _learningPathQuestRepository = learningPathQuestRepository;
        _questRepository = questRepository;
        _userQuestProgressRepository = userQuestProgressRepository;
        _logger = logger;
        _curriculumStructureRepository = curriculumStructureRepository;
        _subjectRepository = subjectRepository;
    }

    public async Task<LearningPathDto?> Handle(GetMyLearningPathQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching primary learning path for user {AuthUserId}", request.AuthUserId);

        var learningPath = (await _learningPathRepository.FindAsync(lp => lp.CreatedBy == request.AuthUserId, cancellationToken))
            .OrderByDescending(lp => lp.CreatedAt)
            .FirstOrDefault();

        if (learningPath == null)
        {
            _logger.LogWarning("No learning path found for user {AuthUserId}", request.AuthUserId);
            return null;
        }

        var chapters = (await _questChapterRepository.FindAsync(qc => qc.LearningPathId == learningPath.Id, cancellationToken))
            .OrderBy(c => c.Sequence)
            .ToList();

        var learningPathQuests = (await _learningPathQuestRepository.FindAsync(lpq => lpq.LearningPathId == learningPath.Id, cancellationToken)).ToList();
        var questIds = learningPathQuests.Select(lpq => lpq.QuestId).ToList();
        var quests = (await _questRepository.GetAllAsync(cancellationToken)).Where(q => questIds.Contains(q.Id)).ToDictionary(q => q.Id);

        var userProgress = (await _userQuestProgressRepository.GetUserProgressForQuestsAsync(request.AuthUserId, questIds, cancellationToken))
            .ToDictionary(p => p.QuestId);

        var structures = (await _curriculumStructureRepository.FindAsync(cs => cs.CurriculumVersionId == learningPath.CurriculumVersionId, cancellationToken)).ToList();
        var subjectToSemesterMap = structures.ToDictionary(s => s.SubjectId, s => s.Semester);

        var questToSemesterMap = new Dictionary<Guid, int>();
        foreach (var quest in quests.Values)
        {
            if (quest.SubjectId.HasValue && subjectToSemesterMap.TryGetValue(quest.SubjectId.Value, out var semester))
            {
                questToSemesterMap[quest.Id] = semester;
            }
        }

        var chapterDtos = new List<QuestChapterDto>();
        foreach (var chapter in chapters)
        {
            var chapterDto = new QuestChapterDto
            {
                Id = chapter.Id,
                Title = chapter.Title,
                Sequence = chapter.Sequence,
                // Status is now calculated dynamically below
            };

            foreach (var lpQuest in learningPathQuests)
            {
                if (questToSemesterMap.TryGetValue(lpQuest.QuestId, out var semester) && semester == chapter.Sequence)
                {
                    if (quests.TryGetValue(lpQuest.QuestId, out var quest))
                    {
                        var status = userProgress.TryGetValue(quest.Id, out var progress)
                            ? progress.Status.ToString()
                            : "NotStarted";

                        chapterDto.Quests.Add(new QuestSummaryDto
                        {
                            Id = quest.Id,
                            Title = quest.Title,
                            Status = status,
                            SequenceOrder = lpQuest.SequenceOrder,
                            LearningPathId = learningPath.Id,
                            ChapterId = chapter.Id
                        });
                    }
                }
            }
            chapterDto.Quests = chapterDto.Quests.OrderBy(q => q.SequenceOrder).ToList();

            // MODIFICATION: This is the robust, corrected logic for dynamic chapter status.
            // It correctly handles all combinations of quest statuses within a chapter.
            if (chapterDto.Quests.Any())
            {
                bool allCompleted = chapterDto.Quests.All(q => q.Status == QuestStatus.Completed.ToString());
                bool anyInProgress = chapterDto.Quests.Any(q => q.Status == QuestStatus.InProgress.ToString());
                bool anyCompleted = chapterDto.Quests.Any(q => q.Status == QuestStatus.Completed.ToString());

                if (allCompleted)
                {
                    chapterDto.Status = PathProgressStatus.Completed.ToString();
                }
                else if (anyInProgress || anyCompleted) // If any are in progress, OR if some are completed (but not all), the chapter is in progress.
                {
                    chapterDto.Status = PathProgressStatus.InProgress.ToString();
                }
                else // The only remaining case is that all quests are "NotStarted"
                {
                    chapterDto.Status = PathProgressStatus.NotStarted.ToString();
                }
            }
            else
            {
                // If a chapter has no quests, default to its stored status.
                chapterDto.Status = chapter.Status.ToString();
            }

            chapterDtos.Add(chapterDto);
        }

        var totalQuests = learningPathQuests.Count;
        var completedQuests = userProgress.Values.Count(p => p.Status == Domain.Enums.QuestStatus.Completed);
        var completionPercentage = totalQuests > 0 ? (double)completedQuests / totalQuests * 100 : 0;

        return new LearningPathDto
        {
            Id = learningPath.Id,
            Name = learningPath.Name,
            Description = learningPath.Description,
            Chapters = chapterDtos,
            CompletionPercentage = completionPercentage
        };
    }
}