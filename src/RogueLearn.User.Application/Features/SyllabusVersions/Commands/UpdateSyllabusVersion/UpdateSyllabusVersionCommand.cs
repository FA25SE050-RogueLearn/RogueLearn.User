using MediatR;

namespace RogueLearn.User.Application.Features.SyllabusVersions.Commands.UpdateSyllabusVersion;

public class UpdateSyllabusVersionCommand : IRequest<UpdateSyllabusVersionResponse>
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateOnly EffectiveDate { get; set; }
    public bool IsActive { get; set; }
}