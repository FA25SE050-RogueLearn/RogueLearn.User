namespace RogueLearn.User.Application.Features.SyllabusVersions.Commands.UpdateSyllabusVersion;

public class UpdateSyllabusVersionResponse
{
    public Guid Id { get; set; }
    public Guid SubjectId { get; set; }
    public int VersionNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateOnly EffectiveDate { get; set; }
    public bool IsActive { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}