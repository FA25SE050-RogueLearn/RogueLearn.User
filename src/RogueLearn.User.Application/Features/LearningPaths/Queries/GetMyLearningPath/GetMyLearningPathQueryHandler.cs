// RogueLearn.User/src/RogueLearn.User.Application/Features/LearningPaths/Queries/GetMyLearningPath/GetMyLearningPathQueryHandler.cs
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.LearningPaths.Queries.GetMyLearningPath;

public class GetMyLearningPathQueryHandler : IRequestHandler<GetMyLearningPathQuery, LearningPathDto?>
{
    private readonly ILearningPathRepository _learningPathRepository;
    private readonly IQuestChapterRepository _questChapterRepository;
    private readonly IQuestRepository _questRepository;
    private readonly ILogger<GetMyLearningPathQueryHandler> _logger;

    public GetMyLearningPathQueryHandler(
        ILearningPathRepository learningPathRepository,
        IQuestChapterRepository questChapterRepository,
        IQuestRepository questRepository,
        ILogger<GetMyLearningPathQueryHandler> logger)
    {
        _learningPathRepository = learningPathRepository;
        _questChapterRepository = questChapterRepository;
        _questRepository = questRepository;
        _logger = logger;
    }

    public async Task<LearningPathDto?> Handle(GetMyLearningPathQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching primary learning path for user {AuthUserId}", request.AuthUserId);

        // Optimized: Use specialized repository method that orders at DB level
        var learningPath = await _learningPathRepository.GetLatestByUserAsync(request.AuthUserId, cancellationToken);

        if (learningPath == null)
        {
            _logger.LogWarning("No learning path found for user {AuthUserId}", request.AuthUserId);
            return null;
        }

        // Optimized: Use specialized repository method that orders at DB level
        var chapters = (await _questChapterRepository.GetChaptersByLearningPathIdOrderedAsync(learningPath.Id, cancellationToken))
            .ToList();

        // Early exit if no chapters - no need to query quests
        if (chapters.Count == 0)
        {
            return new LearningPathDto
            {
                Id = learningPath.Id,
                Name = learningPath.Name,
                Description = learningPath.Description,
                Chapters = new List<QuestChapterDto>(),
                CompletionPercentage = 0
            };
        }

        var chapterIds = chapters.Select(c => c.Id).ToList();
        var quests = (await _questRepository.GetQuestsByChapterIdsAsync(chapterIds, cancellationToken))
            .ToList();

        // Group quests by chapter for efficient lookup
        var questsByChapterId = quests
            .GroupBy(q => q.QuestChapterId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(q => q.Sequence ?? 0).ToList()); // Pre-sort quests by sequence

        // Pre-calculate completion stats to avoid multiple enumerations
        var totalQuests = quests.Count;
        var completedQuests = quests.Count(q => q.Status == QuestStatus.Completed);
        var completionPercentage = totalQuests > 0 ? Math.Round((double)completedQuests / totalQuests * 100, 2) : 0;

        // Build chapter DTOs
        var chapterDtos = new List<QuestChapterDto>(chapters.Count); // Pre-allocate capacity
        var completedStatusString = QuestStatus.Completed.ToString();
        var inProgressStatusString = QuestStatus.InProgress.ToString();

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
                // Quests are already sorted from the dictionary
                chapterDto.Quests = chapterQuests.Select(quest => new QuestSummaryDto
                {
                    Id = quest.Id,
                    Title = quest.Title,
                    Status = quest.Status.ToString(),
                    SequenceOrder = quest.Sequence ?? 0,
                    LearningPathId = learningPath.Id,
                    ChapterId = chapter.Id,
                    SubjectId = quest.SubjectId,
                    IsRecommended = quest.IsRecommended,
                    RecommendationReason = quest.RecommendationReason,
                    // Difficulty fields based on user's academic performance
                    ExpectedDifficulty = quest.ExpectedDifficulty,
                    DifficultyReason = quest.DifficultyReason,
                    SubjectGrade = quest.SubjectGrade,
                    SubjectStatus = quest.SubjectStatus
                }).ToList();

                // Optimized: Use enum comparison instead of string comparison
                var questStatuses = chapterQuests.Select(q => q.Status).ToList();
                var hasCompleted = questStatuses.Contains(QuestStatus.Completed);
                var hasInProgress = questStatuses.Contains(QuestStatus.InProgress);
                var allCompleted = questStatuses.All(s => s == QuestStatus.Completed);

                if (allCompleted)
                    chapterDto.Status = PathProgressStatus.Completed.ToString();
                else if (hasInProgress || hasCompleted)
                    chapterDto.Status = PathProgressStatus.InProgress.ToString();
                else
                    chapterDto.Status = PathProgressStatus.NotStarted.ToString();
            }
            else
            {
                chapterDto.Status = chapter.Status.ToString();
            }

            chapterDtos.Add(chapterDto);
        }

        var learningPathDto = new LearningPathDto
        {
            Id = learningPath.Id,
            Name = learningPath.Name,
            Description = learningPath.Description,
            Chapters = chapterDtos,
            CompletionPercentage = completionPercentage
        };

        return learningPathDto;
    }
}