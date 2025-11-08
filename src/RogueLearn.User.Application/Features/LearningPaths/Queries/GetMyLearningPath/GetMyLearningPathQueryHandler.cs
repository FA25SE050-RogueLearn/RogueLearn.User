using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums; // Required for QuestStatus and PathProgressStatus enums

namespace RogueLearn.User.Application.Features.LearningPaths.Queries.GetMyLearningPath;

public class GetMyLearningPathQueryHandler : IRequestHandler<GetMyLearningPathQuery, LearningPathDto?>
{
    private readonly ILearningPathRepository _learningPathRepository;
    private readonly IQuestChapterRepository _questChapterRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IUserQuestProgressRepository _userQuestProgressRepository;
    private readonly ILogger<GetMyLearningPathQueryHandler> _logger;
    // MODIFICATION: Commented out obsolete repository. The logic needs to be updated
    // to derive semester information from the new `subjects` table.
    // private readonly ICurriculumStructureRepository _curriculumStructureRepository;
    private readonly ISubjectRepository _subjectRepository;


    public GetMyLearningPathQueryHandler(
        ILearningPathRepository learningPathRepository,
        IQuestChapterRepository questChapterRepository,
        IQuestRepository questRepository,
        IUserQuestProgressRepository userQuestProgressRepository,
        ILogger<GetMyLearningPathQueryHandler> logger,
        // ICurriculumStructureRepository curriculumStructureRepository,
        ISubjectRepository subjectRepository)
    {
        _learningPathRepository = learningPathRepository;
        _questChapterRepository = questChapterRepository;
        _questRepository = questRepository;
        _userQuestProgressRepository = userQuestProgressRepository;
        _logger = logger;
        // _curriculumStructureRepository = curriculumStructureRepository;
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

        // MODIFICATION: Logic now fetches quests directly linked to chapters.
        var chapterIds = chapters.Select(c => c.Id).ToList();
        var quests = (await _questRepository.GetAllAsync(cancellationToken))
            .Where(q => chapterIds.Contains(q.QuestChapterId))
            .ToList();
        var questIds = quests.Select(q => q.Id).ToList();

        var questsByChapterId = quests.GroupBy(q => q.QuestChapterId).ToDictionary(g => g.Key, g => g.ToList());

        var userProgress = (await _userQuestProgressRepository.GetUserProgressForQuestsAsync(request.AuthUserId, questIds, cancellationToken))
            .ToDictionary(p => p.QuestId);

        var chapterDtos = new List<QuestChapterDto>();
        foreach (var chapter in chapters)
        {
            var chapterDto = new QuestChapterDto
            {
                Id = chapter.Id,
                Title = chapter.Title,
                Sequence = chapter.Sequence,
            };

            if (questsByChapterId.TryGetValue(chapter.Id, out var chapterQuests))
            {
                foreach (var quest in chapterQuests)
                {
                    var status = userProgress.TryGetValue(quest.Id, out var progress)
                        ? progress.Status.ToString()
                        : "NotStarted";

                    chapterDto.Quests.Add(new QuestSummaryDto
                    {
                        Id = quest.Id,
                        Title = quest.Title,
                        Status = status,
                        // MODIFICATION: SequenceOrder is not on the quest entity, assuming a default or future property.
                        SequenceOrder = 0,
                        LearningPathId = learningPath.Id,
                        ChapterId = chapter.Id
                    });
                }
            }

            // chapterDto.Quests = chapterDto.Quests.OrderBy(q => q.SequenceOrder).ToList(); // MODIFICATION: SequenceOrder needs to be implemented on Quest

            if (chapterDto.Quests.Any())
            {
                bool allCompleted = chapterDto.Quests.All(q => q.Status == QuestStatus.Completed.ToString());
                bool anyInProgress = chapterDto.Quests.Any(q => q.Status == QuestStatus.InProgress.ToString());
                bool anyCompleted = chapterDto.Quests.Any(q => q.Status == QuestStatus.Completed.ToString());

                if (allCompleted)
                {
                    chapterDto.Status = PathProgressStatus.Completed.ToString();
                }
                else if (anyInProgress || anyCompleted)
                {
                    chapterDto.Status = PathProgressStatus.InProgress.ToString();
                }
                else
                {
                    chapterDto.Status = PathProgressStatus.NotStarted.ToString();
                }
            }
            else
            {
                chapterDto.Status = chapter.Status.ToString();
            }

            chapterDtos.Add(chapterDto);
        }

        var totalQuests = quests.Count;
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
