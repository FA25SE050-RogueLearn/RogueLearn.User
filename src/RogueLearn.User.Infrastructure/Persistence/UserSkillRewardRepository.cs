// RogueLearn.User/src/RogueLearn.User.Infrastructure/Persistence/UserSkillRewardRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace RogueLearn.User.Infrastructure.Persistence;

public class UserSkillRewardRepository : GenericRepository<UserSkillReward>, IUserSkillRewardRepository
{
    public UserSkillRewardRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    // Implementation of the new interface method.
    /// <summary>
    /// Finds a single UserSkillReward based on the combination of user, source service, and source ID.
    /// This uses chained filters to avoid LINQ translation issues with complex predicates.
    /// </summary>
    public async Task<UserSkillReward?> GetBySourceAsync(Guid authUserId, string sourceService, Guid sourceId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<UserSkillReward>()
            .Filter("auth_user_id", Operator.Equals, authUserId.ToString())
            .Filter("source_service", Operator.Equals, sourceService)
            .Filter("source_id", Operator.Equals, sourceId.ToString())
            .Single(cancellationToken);

        return response;
    }

    /// <summary>
    /// Finds a UserSkillReward by source AND skill ID.
    /// This ensures proper idempotency when a single subject maps to multiple skills.
    /// Example: PRF192 → C Programming, PRF192 → Problem Solving are tracked separately.
    /// </summary>
    public async Task<UserSkillReward?> GetBySourceAndSkillAsync(Guid authUserId, string sourceService, Guid sourceId, Guid skillId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<UserSkillReward>()
            .Filter("auth_user_id", Operator.Equals, authUserId.ToString())
            .Filter("source_service", Operator.Equals, sourceService)
            .Filter("source_id", Operator.Equals, sourceId.ToString())
            .Filter("skill_id", Operator.Equals, skillId.ToString())
            .Single(cancellationToken);

        return response;
    }
}
