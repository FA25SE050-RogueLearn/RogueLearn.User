// RogueLearn.User/src/RogueLearn.User.Application/Features/LearningPaths/Commands/AnalyzeLearningGap/AnalyzeLearningGapCommand.cs
using MediatR;
using RogueLearn.User.Application.Models;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.LearningPaths.Commands.AnalyzeLearningGap;

public class AnalyzeLearningGapCommand : IRequest<GapAnalysisResponse>
{
    [JsonIgnore]
    public Guid AuthUserId { get; set; }
    public FapRecordData VerifiedRecord { get; set; } = new();
}