using MediatR;
using RogueLearn.User.Domain.Enums;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestActivityProgress;

public class UpdateQuestActivityProgressCommand : IRequest
{
    [JsonIgnore]
    public Guid AuthUserId { get; set; }

    [JsonIgnore]
    public Guid QuestId { get; set; }

    [JsonIgnore]
    public Guid StepId { get; set; }

    [JsonIgnore]
    public Guid ActivityId { get; set; }

    public StepCompletionStatus Status { get; set; }
}