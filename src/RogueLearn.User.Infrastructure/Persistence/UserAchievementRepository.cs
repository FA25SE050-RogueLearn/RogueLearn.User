using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class UserAchievementRepository : GenericRepository<UserAchievement>, IUserAchievementRepository
{
    public UserAchievementRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}
