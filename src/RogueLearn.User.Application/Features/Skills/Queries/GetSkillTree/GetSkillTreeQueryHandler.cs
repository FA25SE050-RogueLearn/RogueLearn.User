// RogueLearn.User/src/RogueLearn.User.Application/Features/Skills/Queries/GetSkillTree/GetSkillTreeQueryHandler.cs
using MediatR;
using RogueLearn.User.Domain.Interfaces;
using System.Linq;

namespace RogueLearn.User.Application.Features.Skills.Queries.GetSkillTree;

public class GetSkillTreeQueryHandler : IRequestHandler<GetSkillTreeQuery, SkillTreeDto>
{
    private readonly ISkillRepository _skillRepository;
    private readonly IUserSkillRepository _userSkillRepository;
    private readonly ISkillDependencyRepository _skillDependencyRepository;

    public GetSkillTreeQueryHandler(
        ISkillRepository skillRepository,
        IUserSkillRepository userSkillRepository,
        ISkillDependencyRepository skillDependencyRepository)
    {
        _skillRepository = skillRepository;
        _userSkillRepository = userSkillRepository;
        _skillDependencyRepository = skillDependencyRepository;
    }

    public async Task<SkillTreeDto> Handle(GetSkillTreeQuery request, CancellationToken cancellationToken)
    {
        var allSkills = await _skillRepository.GetAllAsync(cancellationToken);
        var userSkills = await _userSkillRepository.GetSkillsByAuthIdAsync(request.AuthUserId, cancellationToken);
        var allDependencies = await _skillDependencyRepository.GetAllAsync(cancellationToken);

        var userSkillsDict = userSkills.ToDictionary(us => us.SkillId);

        var skillNodes = allSkills.Select(skill => {
            var userSkill = userSkillsDict.GetValueOrDefault(skill.Id);
            return new SkillNodeDto
            {
                SkillId = skill.Id,
                Name = skill.Name,
                Domain = skill.Domain,
                Description = skill.Description,
                Tier = (int)skill.Tier,
                UserLevel = userSkill?.Level ?? 0, // 0 if not unlocked
                UserExperiencePoints = userSkill?.ExperiencePoints ?? 0
            };
        }).ToList();

        var skillDependencies = allDependencies.Select(dep => new SkillDependencyDto
        {
            SkillId = dep.SkillId,
            PrerequisiteSkillId = dep.PrerequisiteSkillId,
            RelationshipType = dep.RelationshipType
        }).ToList();

        return new SkillTreeDto
        {
            Nodes = skillNodes,
            Dependencies = skillDependencies
        };
    }
}