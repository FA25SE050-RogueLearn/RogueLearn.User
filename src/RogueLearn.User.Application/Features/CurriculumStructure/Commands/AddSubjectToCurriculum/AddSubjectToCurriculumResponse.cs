namespace RogueLearn.User.Application.Features.CurriculumStructure.Commands.AddSubjectToCurriculum;

public class AddSubjectToCurriculumResponse
{
    public Guid Id { get; set; }
    public Guid CurriculumVersionId { get; set; }
    public Guid SubjectId { get; set; }
    public int TermNumber { get; set; }
    public bool IsMandatory { get; set; }
    public Guid[]? PrerequisiteSubjectIds { get; set; }
    public string? PrerequisitesText { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}