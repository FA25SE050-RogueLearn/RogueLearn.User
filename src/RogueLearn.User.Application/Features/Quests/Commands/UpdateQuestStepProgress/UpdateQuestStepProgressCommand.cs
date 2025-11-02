// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/UpdateQuestStepProgress/UpdateQuestStepProgressCommand.cs
using MediatR;
using RogueLearn.User.Domain.Enums;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestStepProgress;

public class UpdateQuestStepProgressCommand : IRequest
{
    [JsonIgnore]
    public Guid AuthUserId { get; set; }

    [JsonIgnore]
    public Guid QuestId { get; set; }

    [JsonIgnore]
    public Guid StepId { get; set; }

    public StepCompletionStatus Status { get; set; }
}