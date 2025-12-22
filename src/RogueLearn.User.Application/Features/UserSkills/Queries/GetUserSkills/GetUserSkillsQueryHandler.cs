using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.UserSkills.Queries.GetUserSkills;

public sealed class GetUserSkillsQueryHandler : IRequestHandler<GetUserSkillsQuery, GetUserSkillsResponse>
{
    private readonly IUserSkillRepository _userSkillRepository;

    public GetUserSkillsQueryHandler(IUserSkillRepository userSkillRepository)
    {
        _userSkillRepository = userSkillRepository;
    }

    public async Task<GetUserSkillsResponse> Handle(GetUserSkillsQuery request, CancellationToken cancellationToken)
    {
        var skills = await _userSkillRepository.GetSkillsByAuthIdAsync(request.AuthUserId, cancellationToken);

        return new GetUserSkillsResponse
        {
            Skills = skills.Select(s => new UserSkillDto
            {
                SkillId = s.SkillId,
                SkillName = s.SkillName,
                ExperiencePoints = s.ExperiencePoints,
                Level = s.Level,
                LastUpdatedAt = s.LastUpdatedAt
            }).ToList()
        };
    }
}