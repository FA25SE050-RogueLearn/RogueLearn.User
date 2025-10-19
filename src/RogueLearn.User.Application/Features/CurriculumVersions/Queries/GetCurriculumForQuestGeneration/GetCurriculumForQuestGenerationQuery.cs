// RogueLearn.User/src/RogueLearn.User.Application/Features/CurriculumVersions/Queries/GetCurriculumForQuestGeneration/GetCurriculumForQuestGenerationQuery.cs
using MediatR;
using RogueLearn.User.Application.DTOs.Internal;

namespace RogueLearn.User.Application.Features.CurriculumVersions.Queries.GetCurriculumForQuestGeneration;

public class GetCurriculumForQuestGenerationQuery : IRequest<CurriculumForQuestGenerationDto>
{
    public Guid CurriculumVersionId { get; set; }
}