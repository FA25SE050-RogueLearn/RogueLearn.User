using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class SyllabusVersionRepository : GenericRepository<SyllabusVersion>, ISyllabusVersionRepository
{
    public SyllabusVersionRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}
