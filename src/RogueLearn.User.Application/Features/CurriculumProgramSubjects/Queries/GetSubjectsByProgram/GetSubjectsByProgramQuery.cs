
using MediatR;
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;

namespace RogueLearn.User.Application.Features.CurriculumProgramSubjects.Queries.GetSubjectsByProgram;

public class GetSubjectsByProgramQuery : IRequest<List<SubjectDto>>
{
    public Guid ProgramId { get; set; }
}