using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Quests.Commands.EnsureMasterQuests;

public class EnsureMasterQuestsCommandHandler : IRequestHandler<EnsureMasterQuestsCommand, EnsureMasterQuestsResponse>
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly IQuestRepository _questRepository;
    private readonly ILogger<EnsureMasterQuestsCommandHandler> _logger;

    public EnsureMasterQuestsCommandHandler(
        ISubjectRepository subjectRepository,
        IQuestRepository questRepository,
        ILogger<EnsureMasterQuestsCommandHandler> logger)
    {
        _subjectRepository = subjectRepository;
        _questRepository = questRepository;
        _logger = logger;
    }

    public async Task<EnsureMasterQuestsResponse> Handle(EnsureMasterQuestsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Master Quest synchronization...");

        // 1. Fetch all subjects (The source of truth for what quests should exist)
        var allSubjects = await _subjectRepository.GetAllAsync(cancellationToken);

        // 2. Fetch all existing quests to check what we already have
        var allQuests = await _questRepository.GetAllAsync(cancellationToken);
        var existingSubjectIds = allQuests
            .Where(q => q.SubjectId.HasValue)
            .Select(q => q.SubjectId!.Value)
            .ToHashSet();

        int createdCount = 0;
        int existingCount = existingSubjectIds.Count;

        // 3. Create missing shells
        foreach (var subject in allSubjects)
        {
            if (existingSubjectIds.Contains(subject.Id))
            {
                continue;
            }

            var masterQuest = new Quest
            {
                Id = Guid.NewGuid(),
                SubjectId = subject.Id,
                Title = $"{subject.SubjectCode}: {subject.SubjectName}",
                Description = subject.Description ?? $"Master the concepts of {subject.SubjectName}.",

                // Defaults for Master Shell
                QuestType = QuestType.Practice,
                DifficultyLevel = DifficultyLevel.Intermediate,
                Status = QuestStatus.Draft, // Default state
                IsActive = true,

                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,

                // Initial Difficulty Metadata
                ExpectedDifficulty = "Standard",
                DifficultyReason = "Master Template Default"
            };

            await _questRepository.AddAsync(masterQuest, cancellationToken);
            createdCount++;
            _logger.LogInformation("Created Master Quest for {Code}", subject.SubjectCode);
        }

        _logger.LogInformation("Sync complete. Created {Created} new Master Quests. Total {Total}.", createdCount, existingCount + createdCount);

        return new EnsureMasterQuestsResponse(createdCount, existingCount);
    }
}