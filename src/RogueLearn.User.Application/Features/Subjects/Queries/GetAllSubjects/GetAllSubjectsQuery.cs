// RogueLearn.User/src/RogueLearn.User.Application/Features/Subjects/Queries/GetAllSubjects/GetAllSubjectsQuery.cs
using MediatR;

namespace RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;

public class GetAllSubjectsQuery : IRequest<PaginatedSubjectsResponse>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
}