// RogueLearn.User/src/RogueLearn.User.Domain/Interfaces/IUserSkillRepository.cs
using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IUserSkillRepository : IGenericRepository<UserSkill>
{
    // A new specialized method to reliably fetch skills by a user's authentication ID.
    // This avoids the problematic LINQ expression translator in the generic repository for this specific query.
    Task<IEnumerable<UserSkill>> GetSkillsByAuthIdAsync(Guid authUserId, CancellationToken cancellationToken = default);
    Task<List<UserSkill>> AddRangeAsync(List<UserSkill> userSkills, CancellationToken cancellationToken = default);
}
