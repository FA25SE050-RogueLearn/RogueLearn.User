// RogueLearn.User/src/RogueLearn.User.Application/Features/LearningPaths/Commands/ForgeLearningPath/ForgeLearningPathCommand.cs
using MediatR;
using RogueLearn.User.Application.Models;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.LearningPaths.Commands.ForgeLearningPath;

public class ForgeLearningPathCommand : IRequest<ForgedLearningPath>
{
    [JsonIgnore]
    public Guid AuthUserId { get; set; }
    public ForgingPayload ForgingPayload { get; set; } = new();
}