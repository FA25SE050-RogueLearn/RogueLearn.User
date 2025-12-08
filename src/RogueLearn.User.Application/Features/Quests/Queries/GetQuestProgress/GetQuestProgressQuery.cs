// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Queries/GetQuestProgress/GetQuestProgressQuery.cs
using MediatR;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetQuestProgress;

public class GetQuestProgressQuery : IRequest<List<QuestStepProgressDto>>
{
    public Guid QuestId { get; set; }
    public Guid AuthUserId { get; set; }
}