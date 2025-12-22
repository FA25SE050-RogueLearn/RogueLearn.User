using MediatR;
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;

namespace RogueLearn.User.Application.Features.Student.Queries.GetProgramSubjects;

public class GetStudentProgramSubjectsQuery : IRequest<List<SubjectDto>>
{
    public Guid ProgramId { get; set; }
}