using MediatR;

namespace RogueLearn.User.Application.Features.LearningPaths.Commands.DeleteLearningPath;

/// <summary>
/// Command to delete a Learning Path by ID.
/// </summary>
public class DeleteLearningPathCommand : IRequest
{
    public Guid Id { get; set; }
}