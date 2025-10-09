using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class SkillRepository : GenericRepository<Skill>, ISkillRepository
{
    public SkillRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}
