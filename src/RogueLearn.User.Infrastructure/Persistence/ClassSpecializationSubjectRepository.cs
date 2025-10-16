using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class ClassSpecializationSubjectRepository : GenericRepository<ClassSpecializationSubject>, IClassSpecializationSubjectRepository
{
  public ClassSpecializationSubjectRepository(Client supabaseClient) : base(supabaseClient)
  {
  }
}