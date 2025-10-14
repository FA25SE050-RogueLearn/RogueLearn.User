namespace RogueLearn.User.Application.Features.CurriculumVersions.Commands.CreateCurriculumVersion;

public class CreateCurriculumVersionResponse
{
    public Guid Id { get; set; }
    public Guid ProgramId { get; set; }
    public string VersionCode { get; set; } = string.Empty;
    public int EffectiveYear { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}