// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Queries/GetAdminQuestDetails/GetAdminQuestDetailsQuery.cs
using MediatR;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetAdminQuestDetails;

public class GetAdminQuestDetailsQuery : IRequest<AdminQuestDetailsDto?>
{
    public Guid QuestId { get; set; }
}