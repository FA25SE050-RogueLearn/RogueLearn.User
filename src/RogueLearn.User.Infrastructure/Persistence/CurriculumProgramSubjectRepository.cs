using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class CurriculumProgramSubjectRepository : GenericRepository<CurriculumProgramSubject>, ICurriculumProgramSubjectRepository
{
    public CurriculumProgramSubjectRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}