using MediatR;

namespace RogueLearn.User.Application.Features.CurriculumStructure.Commands.RemoveSubjectFromCurriculum;

public record RemoveSubjectFromCurriculumCommand(Guid Id) : IRequest;