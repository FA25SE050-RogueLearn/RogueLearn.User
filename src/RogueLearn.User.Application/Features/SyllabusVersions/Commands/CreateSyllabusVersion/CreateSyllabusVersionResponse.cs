namespace RogueLearn.User.Application.Features.SyllabusVersions.Commands.CreateSyllabusVersion;

public class CreateSyllabusVersionResponse
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