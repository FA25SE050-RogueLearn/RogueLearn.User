// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Queries/GetAllQuests/GetAllQuestsQuery.cs
using MediatR;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetAllQuests;

public class GetAllQuestsQuery : IRequest<PaginatedQuestsResponse>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? Status { get; set; } // Optional filter by status
}