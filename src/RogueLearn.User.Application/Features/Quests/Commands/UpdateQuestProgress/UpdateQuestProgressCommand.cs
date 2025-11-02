// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/UpdateQuestProgress/UpdateQuestProgressCommand.cs
using MediatR;
using RogueLearn.User.Domain.Enums;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestProgress;

public class UpdateQuestProgressCommand : IRequest
{
    [JsonIgnore]
    public Guid AuthUserId { get; set; }

    [JsonIgnore]
    public Guid QuestId { get; set; }

    public QuestStatus Status { get; set; }
}