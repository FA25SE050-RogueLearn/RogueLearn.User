using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class ClassRepository : GenericRepository<Class>, IClassRepository
{
  public ClassRepository(Client supabaseClient) : base(supabaseClient)
  {
  }
}