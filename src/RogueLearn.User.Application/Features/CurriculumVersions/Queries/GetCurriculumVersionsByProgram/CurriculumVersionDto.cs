namespace RogueLearn.User.Application.Features.CurriculumVersions.Queries.GetCurriculumVersionsByProgram;

public class CurriculumVersionDto
{
    public Guid Id { get; set; }
    public Guid ProgramId { get; set; }
    public string VersionCode { get; set; } = string.Empty;
    public int EffectiveYear { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}