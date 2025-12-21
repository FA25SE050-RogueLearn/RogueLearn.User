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

    public async Task<IEnumerable<UserSkillReward>> GetBySourceAllAsync(Guid authUserId, string sourceService, Guid sourceId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<UserSkillReward>()
            .Filter("auth_user_id", Operator.Equals, authUserId.ToString())
            .Filter("source_service", Operator.Equals, sourceService)
            .Filter("source_id", Operator.Equals, sourceId.ToString())
            .Get(cancellationToken);

        return response.Models;
    }
}
