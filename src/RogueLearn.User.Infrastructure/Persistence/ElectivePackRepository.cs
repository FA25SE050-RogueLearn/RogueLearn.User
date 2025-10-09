using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class ElectivePackRepository : GenericRepository<ElectivePack>, IElectivePackRepository
{
    public ElectivePackRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}
