using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class CurriculumVersionActivationRepository : GenericRepository<CurriculumVersionActivation>, ICurriculumVersionActivationRepository
{
    public CurriculumVersionActivationRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}
