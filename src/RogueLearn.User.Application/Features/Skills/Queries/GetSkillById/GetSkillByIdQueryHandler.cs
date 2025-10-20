using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Skills.Queries.GetSkillById;

public sealed class GetSkillByIdQueryHandler : IRequestHandler<GetSkillByIdQuery, GetSkillByIdResponse>
{
    private readonly ISkillRepository _skillRepository;

    public GetSkillByIdQueryHandler(ISkillRepository skillRepository)
    {
        _skillRepository = skillRepository;
    }

    public async Task<GetSkillByIdResponse> Handle(GetSkillByIdQuery request, CancellationToken cancellationToken)
    {
        var skill = await _skillRepository.GetByIdAsync(request.Id, cancellationToken) ?? throw new KeyNotFoundException("Skill not found");
        return new GetSkillByIdResponse
        {
            Id = skill.Id,
            Name = skill.Name,
            Domain = skill.Domain,
            Tier = skill.Tier,
            Description = skill.Description
        };
    }
}