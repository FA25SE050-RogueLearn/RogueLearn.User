using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class ElectiveSourceRepository : GenericRepository<ElectiveSource>, IElectiveSourceRepository
{
    public ElectiveSourceRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}
