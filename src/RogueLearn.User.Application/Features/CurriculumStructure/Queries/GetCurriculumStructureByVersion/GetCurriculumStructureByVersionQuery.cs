using MediatR;

namespace RogueLearn.User.Application.Features.CurriculumStructure.Queries.GetCurriculumStructureByVersion;

public record GetCurriculumStructureByVersionQuery(Guid CurriculumVersionId) : IRequest<List<CurriculumStructureDto>>;