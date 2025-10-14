using MediatR;

namespace RogueLearn.User.Application.Features.CurriculumVersions.Commands.CreateCurriculumVersion;

public class CreateCurriculumVersionCommand : IRequest<CreateCurriculumVersionResponse>
{
    public Guid ProgramId { get; set; }
    public string VersionCode { get; set; } = string.Empty;
    public int EffectiveYear { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}