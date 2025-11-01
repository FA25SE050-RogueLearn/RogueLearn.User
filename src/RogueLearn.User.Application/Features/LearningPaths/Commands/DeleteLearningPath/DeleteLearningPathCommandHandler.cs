using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.LearningPaths.Commands.DeleteLearningPath;

/// <summary>
/// Handles deletion of a Learning Path and its related entities.
/// - Throws standardized NotFoundException when the learning path does not exist.
/// - Deletes related QuestChapters and LearningPathQuest entries to avoid orphaned data.
/// </summary>
public class DeleteLearningPathCommandHandler : IRequestHandler<DeleteLearningPathCommand>
{
    private readonly ILearningPathRepository _learningPathRepository;
    private readonly ILearningPathQuestRepository _learningPathQuestRepository;
    private readonly ILogger<DeleteLearningPathCommandHandler> _logger;

    public DeleteLearningPathCommandHandler(
        ILearningPathRepository learningPathRepository,
        ILearningPathQuestRepository learningPathQuestRepository,
        ILogger<DeleteLearningPathCommandHandler> logger)
    {
        _learningPathRepository = learningPathRepository;
        _learningPathQuestRepository = learningPathQuestRepository;
        _logger = logger;
    }

    public async Task Handle(DeleteLearningPathCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling DeleteLearningPathCommand for LearningPathId={LearningPathId}", request.Id);

        var learningPath = await _learningPathRepository.GetByIdAsync(request.Id, cancellationToken);
        if (learningPath == null)
        {
            _logger.LogWarning("LearningPath not found: LearningPathId={LearningPathId}", request.Id);
            throw new NotFoundException("LearningPath", request.Id);
        }

        // Delete related LearningPathQuests
        var lpQuests = (await _learningPathQuestRepository.FindAsync(lpq => lpq.LearningPathId == request.Id, cancellationToken)).ToList();
        foreach (var lpq in lpQuests)
        {
            await _learningPathQuestRepository.DeleteAsync(lpq.Id, cancellationToken);
        }

        // Finally, delete the LearningPath itself
        await _learningPathRepository.DeleteAsync(request.Id, cancellationToken);

        _logger.LogInformation("Deleted LearningPath and related entities: LearningPathId={LearningPathId}. QuestsDeleted={QuestsCount}", request.Id, lpQuests.Count);
    }
}