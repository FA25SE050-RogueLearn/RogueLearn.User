using MediatR;

namespace RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;

public class CreateSubjectCommand : IRequest<CreateSubjectResponse>
{
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int Credits { get; set; }
    public string? Description { get; set; }
}