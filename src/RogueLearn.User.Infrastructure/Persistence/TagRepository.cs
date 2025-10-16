using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class TagRepository : GenericRepository<Tag>, ITagRepository
{
  public TagRepository(Client supabaseClient) : base(supabaseClient)
  {
  }
}