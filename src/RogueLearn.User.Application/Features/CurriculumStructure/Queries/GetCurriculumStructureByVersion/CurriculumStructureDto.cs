namespace RogueLearn.User.Application.Features.CurriculumStructure.Queries.GetCurriculumStructureByVersion;

public class CurriculumStructureDto
{
    public Guid Id { get; set; }
    public Guid CurriculumVersionId { get; set; }
    public Guid SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int Credits { get; set; }
    public int TermNumber { get; set; }
    public bool IsMandatory { get; set; }
    public Guid[]? PrerequisiteSubjectIds { get; set; }
    public string? PrerequisitesText { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}