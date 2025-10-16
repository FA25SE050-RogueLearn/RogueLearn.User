using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class NoteRepository : GenericRepository<Note>, INoteRepository
{
  public NoteRepository(Client supabaseClient) : base(supabaseClient)
  {
  }
}