using MediatR;

namespace RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;

public class GetAllSubjectsQuery : IRequest<List<SubjectDto>>
{
}