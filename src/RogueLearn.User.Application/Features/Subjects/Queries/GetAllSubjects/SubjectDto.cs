namespace RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;

public class SubjectDto
{
    public Guid Id { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int Credits { get; set; }
    public string? Description { get; set; }
    public int? Semester { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}