using MediatR;

namespace RogueLearn.User.Application.Features.SyllabusVersions.Queries.GetSyllabusVersionsBySubject;

public record GetSyllabusVersionsBySubjectQuery(Guid SubjectId) : IRequest<List<SyllabusVersionDto>>;