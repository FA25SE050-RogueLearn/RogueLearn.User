// RogueLearn.User/src/RogueLearn.User.Application/Features/Subjects/Queries/GetAllSubjects/PaginatedSubjectsResponse.cs
namespace RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;

public class PaginatedSubjectsResponse
{
    public List<SubjectDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}