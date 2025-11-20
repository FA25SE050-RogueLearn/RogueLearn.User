// RogueLearn.Us    er/src/RogueLearn.User.Application/Features/Quests/Commands/GenerateQuestSteps/GenerateQuestStepsCommand.cs
using MediatR;
using RogueLearn.User.Domain.Entities;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;

// MODIFIED: The command now returns a list of DTOs.
public class GenerateQuestStepsCommand : IRequest<List<GeneratedQuestStepDto>>
{
    [JsonIgnore]
    public Guid AuthUserId { get; set; }

    [JsonIgnore]
    public Guid QuestId { get; set; }
}