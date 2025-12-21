using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace RogueLearn.User.Infrastructure.Persistence;

public class UserSkillRepository : GenericRepository<UserSkill>, IUserSkillRepository
{
    public UserSkillRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<IEnumerable<UserSkill>> GetSkillsByAuthIdAsync(Guid authUserId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<UserSkill>()
            .Filter("auth_user_id", Operator.Equals, authUserId.ToString())
            .Get(cancellationToken);

        return response.Models;
    }
    public async Task<List<UserSkill>> AddRangeAsync(List<UserSkill> userSkills, CancellationToken cancellationToken = default)
    {
        if (!userSkills.Any())
        {
            return new List<UserSkill>();
        }
        
        var response = await _supabaseClient
            .From<UserSkill>()
            .Insert(userSkills, cancellationToken: cancellationToken);

        return response.Models.ToList();
    }
}
