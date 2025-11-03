// RogueLearn.User/src/RogueLearn.User.Infrastructure/Persistence/UserSkillRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using static Supabase.Postgrest.Constants; // ADDED: To use the Operator enum for direct filtering.

namespace RogueLearn.User.Infrastructure.Persistence;

public class UserSkillRepository : GenericRepository<UserSkill>, IUserSkillRepository
{
    public UserSkillRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    // ADDED: Implementation of the new specialized method.
    /// <summary>
    /// Fetches all skills for a specific user using a direct and reliable filter.
    /// </summary>
    public async Task<IEnumerable<UserSkill>> GetSkillsByAuthIdAsync(Guid authUserId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<UserSkill>()
            .Filter("auth_user_id", Operator.Equals, authUserId.ToString())
            .Get(cancellationToken);

        return response.Models;
    }
}