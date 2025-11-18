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

    // This now represents the ID of the weekly module (the parent quest_step).
    [JsonIgnore]
    public Guid StepId { get; set; }

    // ADDED: This is the ID of the specific activity within the module that the user is updating.
    [JsonIgnore]
    public Guid ActivityId { get; set; }

    public StepCompletionStatus Status { get; set; }
}