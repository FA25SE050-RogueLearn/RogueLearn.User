using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class CurriculumVersionRepository : GenericRepository<CurriculumVersion>, ICurriculumVersionRepository
{
    public CurriculumVersionRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}
