using MediatR;

namespace RogueLearn.User.Application.Features.CurriculumVersions.Commands.ActivateCurriculumVersion;

public class ActivateCurriculumVersionCommand : IRequest
{
    public Guid CurriculumVersionId { get; set; }
    public int EffectiveYear { get; set; }
    public Guid? ActivatedBy { get; set; }
    public string? Notes { get; set; }
}