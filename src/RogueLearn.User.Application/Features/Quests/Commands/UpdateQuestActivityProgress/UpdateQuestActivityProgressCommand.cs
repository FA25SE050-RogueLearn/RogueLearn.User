// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/UpdateQuestActivityProgress/UpdateQuestActivityProgressCommand.cs
using MediatR;
using RogueLearn.User.Domain.Enums;
using System.Text.Json.Serialization;

// MODIFIED: Renamed the namespace and command for clarity and architectural alignment.
namespace RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestActivityProgress;

public class UpdateQuestActivityProgressCommand : IRequest
{
    [JsonIgnore]
    public Guid AuthUserId { get; set; }

    [JsonIgnore]
    public Guid QuestId { get; set; }

    [JsonIgnore]
    public Guid StepId { get; set; }

    // ADDED: The specific activity within the step that is being updated.
    [JsonIgnore]
    public Guid ActivityId { get; set; }

    public StepCompletionStatus Status { get; set; }
}