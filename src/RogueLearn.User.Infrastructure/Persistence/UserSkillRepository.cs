using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class UserSkillRepository : GenericRepository<UserSkill>, IUserSkillRepository
{
    public UserSkillRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}