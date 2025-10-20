using MediatR;

namespace RogueLearn.User.Application.Features.Skills.Queries.GetSkillById;

public sealed class GetSkillByIdQuery : IRequest<GetSkillByIdResponse>
{
    public Guid Id { get; set; }
}