using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class UserSkillRewardRepository : GenericRepository<UserSkillReward>, IUserSkillRewardRepository
{
    public UserSkillRewardRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}