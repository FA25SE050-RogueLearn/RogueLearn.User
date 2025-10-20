using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.UserSkills.Queries.GetUserSkill;

public sealed class GetUserSkillQueryHandler : IRequestHandler<GetUserSkillQuery, GetUserSkillResponse>
{
    private readonly IUserSkillRepository _userSkillRepository;

    public GetUserSkillQueryHandler(IUserSkillRepository userSkillRepository)
    {
        _userSkillRepository = userSkillRepository;
    }

    public async Task<GetUserSkillResponse> Handle(GetUserSkillQuery request, CancellationToken cancellationToken)
    {
        var skill = await _userSkillRepository.FirstOrDefaultAsync(
            s => s.AuthUserId == request.AuthUserId && s.SkillName == request.SkillName,
            cancellationToken);

        if (skill is null)
        {
            throw new NotFoundException("UserSkill", request.SkillName);
        }

        return new GetUserSkillResponse
        {
            SkillName = skill.SkillName,
            ExperiencePoints = skill.ExperiencePoints,
            Level = skill.Level,
            LastUpdatedAt = skill.LastUpdatedAt
        };
    }
}