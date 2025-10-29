// src/RogueLearn.User/src/RogueLearn.User.Application/Features/LearningPaths/Queries/GetMyLearningPath/GetMyLearningPathQuery.cs
using MediatR;

namespace RogueLearn.User.Application.Features.LearningPaths.Queries.GetMyLearningPath;

public class GetMyLearningPathQuery : IRequest<LearningPathDto?>
{
    public Guid AuthUserId { get; set; }
}