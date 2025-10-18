using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace RogueLearn.User.Infrastructure.Persistence;

public class NoteRepository : GenericRepository<Note>, INoteRepository
{
  public NoteRepository(Client supabaseClient) : base(supabaseClient)
  {
  }

  public async Task<IEnumerable<Note>> GetByUserAsync(Guid authUserId, CancellationToken cancellationToken = default)
  {
    var response = await _supabaseClient
      .From<Note>()
      .Where(x => x.AuthUserId == authUserId)
      .Get(cancellationToken);

    return response.Models;
  }

  public async Task<IEnumerable<Note>> SearchByUserAsync(Guid authUserId, string search, CancellationToken cancellationToken = default)
  {
    var response = await _supabaseClient
      .From<Note>()
      .Where(x => x.AuthUserId == authUserId)
      .Filter("title", Operator.ILike, $"%{search}%")
      .Get(cancellationToken);

    return response.Models;
  }

  public async Task<IEnumerable<Note>> GetPublicAsync(CancellationToken cancellationToken = default)
  {
    var response = await _supabaseClient
      .From<Note>()
      .Where(x => x.IsPublic == true)
      .Get(cancellationToken);

    return response.Models;
  }

  public async Task<IEnumerable<Note>> SearchPublicAsync(string search, CancellationToken cancellationToken = default)
  {
    var response = await _supabaseClient
      .From<Note>()
      .Where(x => x.IsPublic == true)
      .Filter("title", Operator.ILike, $"%{search}%")
      .Get(cancellationToken);

    return response.Models;
  }
}
