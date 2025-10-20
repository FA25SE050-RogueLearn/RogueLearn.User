using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Skills.Queries.GetSkills;

public sealed class GetSkillsQueryHandler : IRequestHandler<GetSkillsQuery, GetSkillsResponse>
{
    private readonly ISkillRepository _skillRepository;

    public GetSkillsQueryHandler(ISkillRepository skillRepository)
    {
        _skillRepository = skillRepository;
    }

    public async Task<GetSkillsResponse> Handle(GetSkillsQuery request, CancellationToken cancellationToken)
    {
        var all = await _skillRepository.GetAllAsync(cancellationToken);
        return new GetSkillsResponse
        {
            Skills = all.Select(s => new SkillDto
            {
                Id = s.Id,
                Name = s.Name,
                Domain = s.Domain,
                Tier = s.Tier,
                Description = s.Description
            }).ToList()
        };
    }
}