using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class NoteQuestRepository : INoteQuestRepository
{
  private readonly Client _supabaseClient;

  public NoteQuestRepository(Client supabaseClient)
  {
    _supabaseClient = supabaseClient;
  }

  public async Task AddAsync(Guid noteId, Guid questId, CancellationToken cancellationToken = default)
  {
    var model = new NoteQuest { NoteId = noteId, QuestId = questId };
    await _supabaseClient
      .From<NoteQuest>()
      .Insert(model, cancellationToken: cancellationToken);
  }

  public async Task RemoveAsync(Guid noteId, Guid questId, CancellationToken cancellationToken = default)
  {
    await _supabaseClient
      .From<NoteQuest>()
      .Where(x => x.NoteId == noteId && x.QuestId == questId)
      .Delete(cancellationToken: cancellationToken);
  }

  public async Task<IEnumerable<Guid>> GetQuestIdsForNoteAsync(Guid noteId, CancellationToken cancellationToken = default)
  {
    var response = await _supabaseClient
      .From<NoteQuest>()
      .Where(x => x.NoteId == noteId)
      .Get(cancellationToken);

    return response.Models.Select(x => x.QuestId);
  }

  public async Task<IEnumerable<Guid>> GetNoteIdsByQuestIdAsync(Guid questId, CancellationToken cancellationToken = default)
  {
    var response = await _supabaseClient
      .From<NoteQuest>()
      .Where(x => x.QuestId == questId)
      .Get(cancellationToken);

    return response.Models.Select(x => x.NoteId);
  }
}