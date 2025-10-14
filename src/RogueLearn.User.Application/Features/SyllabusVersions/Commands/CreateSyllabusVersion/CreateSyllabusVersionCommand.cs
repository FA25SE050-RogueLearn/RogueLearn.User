using MediatR;

namespace RogueLearn.User.Application.Features.SyllabusVersions.Commands.CreateSyllabusVersion;

public class CreateSyllabusVersionCommand : IRequest<CreateSyllabusVersionResponse>
{
    public Guid SubjectId { get; set; }
    public int VersionNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateOnly EffectiveDate { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
}