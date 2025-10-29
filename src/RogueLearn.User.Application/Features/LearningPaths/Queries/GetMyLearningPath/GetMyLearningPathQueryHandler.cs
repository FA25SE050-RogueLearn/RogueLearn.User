// src/RogueLearn.User/src/RogueLearn.User.Application/Features/LearningPaths/Queries/GetMyLearningPath/GetMyLearningPathQueryHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.LearningPaths.Queries.GetMyLearningPath;

public class GetMyLearningPathQueryHandler : IRequestHandler<GetMyLearningPathQuery, LearningPathDto?>
{
    private readonly ILearningPathRepository _learningPathRepository;
    private readonly IQuestChapterRepository _questChapterRepository;
    private readonly ILearningPathQuestRepository _learningPathQuestRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IUserQuestProgressRepository _userQuestProgressRepository;
    private readonly ILogger<GetMyLearningPathQueryHandler> _logger;

    public GetMyLearningPathQueryHandler(
        ILearningPathRepository learningPathRepository,
        IQuestChapterRepository questChapterRepository,
        ILearningPathQuestRepository learningPathQuestRepository,
        IQuestRepository questRepository,
        IUserQuestProgressRepository userQuestProgressRepository,
        ILogger<GetMyLearningPathQueryHandler> logger)
    {
        _learningPathRepository = learningPathRepository;
        _questChapterRepository = questChapterRepository;
        _learningPathQuestRepository = learningPathQuestRepository;
        _questRepository = questRepository;
        _userQuestProgressRepository = userQuestProgressRepository;
        _logger = logger;
    }

    public async Task<LearningPathDto?> Handle(GetMyLearningPathQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching primary learning path for user {AuthUserId}", request.AuthUserId);

        // For now, we fetch the most recently created custom learning path for the user.
        // In the future, this logic would select the "primary" or "active" path.
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
        var userProgress = (await _userQuestProgressRepository.FindAsync(p => p.AuthUserId == request.AuthUserId && questIds.Contains(p.QuestId), cancellationToken))
            .ToDictionary(p => p.QuestId);

        var chapterDtos = new List<QuestChapterDto>();
        foreach (var chapter in chapters)
        {
            var chapterDto = new QuestChapterDto
            {
                Id = chapter.Id,
                Title = chapter.Title,
                Sequence = chapter.Sequence,
                Status = chapter.Status.ToString(),
            };

            // This logic to associate quests with chapters needs to be refined.
            // For now, we will add all quests to all chapters for demonstration.
            foreach (var lpQuest in learningPathQuests)
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
                        SequenceOrder = lpQuest.SequenceOrder
                    });
                }
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