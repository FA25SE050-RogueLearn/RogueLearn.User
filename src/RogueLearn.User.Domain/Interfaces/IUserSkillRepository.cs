using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IUserSkillRepository : IGenericRepository<UserSkill>
{
    Task<IEnumerable<UserSkill>> GetSkillsByAuthIdAsync(Guid authUserId, CancellationToken cancellationToken = default);
    Task<List<UserSkill>> AddRangeAsync(List<UserSkill> userSkills, CancellationToken cancellationToken = default);
}
