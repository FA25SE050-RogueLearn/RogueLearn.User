using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class ClassNodeRepository : GenericRepository<ClassNode>, IClassNodeRepository
{
  public ClassNodeRepository(Client supabaseClient) : base(supabaseClient)
  {
  }
}