using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class NoteTagRepository : INoteTagRepository
{
  private readonly Client _supabaseClient;

  public NoteTagRepository(Client supabaseClient)
  {
    _supabaseClient = supabaseClient;
  }

  public async Task AddAsync(Guid noteId, Guid tagId, CancellationToken cancellationToken = default)
  {
    var model = new NoteTag { NoteId = noteId, TagId = tagId };
    await _supabaseClient
      .From<NoteTag>()
      .Insert(model, cancellationToken: cancellationToken);
  }

  public async Task RemoveAsync(Guid noteId, Guid tagId, CancellationToken cancellationToken = default)
  {
    await _supabaseClient
      .From<NoteTag>()
      .Where(x => x.NoteId == noteId && x.TagId == tagId)
      .Delete(cancellationToken: cancellationToken);
  }

  public async Task<IEnumerable<Guid>> GetTagIdsForNoteAsync(Guid noteId, CancellationToken cancellationToken = default)
  {
    var response = await _supabaseClient
      .From<NoteTag>()
      .Where(x => x.NoteId == noteId)
      .Get(cancellationToken);

    return response.Models.Select(x => x.TagId);
  }

  public async Task<IEnumerable<Guid>> GetNoteIdsByTagIdAsync(Guid tagId, CancellationToken cancellationToken = default)
  {
    var response = await _supabaseClient
      .From<NoteTag>()
      .Where(x => x.TagId == tagId)
      .Get(cancellationToken);

    return response.Models.Select(x => x.NoteId);
  }
}