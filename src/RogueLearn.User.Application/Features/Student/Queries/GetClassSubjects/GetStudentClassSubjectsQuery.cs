using MediatR;
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;

namespace RogueLearn.User.Application.Features.Student.Queries.GetClassSubjects;

public class GetStudentClassSubjectsQuery : IRequest<List<SubjectDto>>
{
    public Guid ClassId { get; set; }
}