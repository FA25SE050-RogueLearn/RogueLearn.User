using MediatR;

namespace RogueLearn.User.Application.Features.Subjects.Commands.UpdateSubject;

public class UpdateSubjectCommand : IRequest<UpdateSubjectResponse>
{
    public Guid Id { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int Credits { get; set; }
    public string? Description { get; set; }
}