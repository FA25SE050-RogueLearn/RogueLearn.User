using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class CurriculumStructureRepository : GenericRepository<CurriculumStructure>, ICurriculumStructureRepository
{
    public CurriculumStructureRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}