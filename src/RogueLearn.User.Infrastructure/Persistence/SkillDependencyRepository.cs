using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class SkillDependencyRepository : GenericRepository<SkillDependency>, ISkillDependencyRepository
{
    public SkillDependencyRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}
