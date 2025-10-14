using MediatR;
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;

namespace RogueLearn.User.Application.Features.Subjects.Queries.GetSubjectById;

public class GetSubjectByIdQuery : IRequest<SubjectDto?>
{
    public Guid Id { get; set; }
}