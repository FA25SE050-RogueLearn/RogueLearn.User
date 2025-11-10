// RogueLearn.User/src/RogueLearn.User.Application/Features/LearningPaths/Commands/DeleteLearningPath/DeleteLearningPathCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.LearningPaths.Commands.DeleteLearningPath;

/// <summary>
/// Handles deletion of a Learning Path.
/// - Throws a standardized NotFoundException when the learning path does not exist.
/// - Relies on database cascading deletes to remove child entities (QuestChapters, Quests),
///   aligning with the new, simplified architecture.
/// </summary>
public class DeleteLearningPathCommandHandler : IRequestHandler<DeleteLearningPathCommand>
{
    private readonly ILearningPathRepository _learningPathRepository;
    // MODIFICATION: Removed the obsolete ILearningPathQuestRepository dependency.
    private readonly ILogger<DeleteLearningPathCommandHandler> _logger;

    public DeleteLearningPathCommandHandler(
        ILearningPathRepository learningPathRepository,
        ILogger<DeleteLearningPathCommandHandler> logger)
    {
        _learningPathRepository = learningPathRepository;
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

        // MODIFICATION: Removed the manual deletion of LearningPathQuest entries.
        // The database schema's "ON DELETE CASCADE" constraint on the quest_chapters table
        // will automatically handle the deletion of all associated chapters and their quests.
        // This simplifies the logic and makes it more robust.

        // Finally, delete the LearningPath itself. The database handles the rest.
        await _learningPathRepository.DeleteAsync(request.Id, cancellationToken);

        _logger.LogInformation("Deleted LearningPath and its related entities via cascade: LearningPathId={LearningPathId}", request.Id);
    }
}