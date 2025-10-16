using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class NoteSkillRepository : INoteSkillRepository
{
  private readonly Client _supabaseClient;

  public NoteSkillRepository(Client supabaseClient)
  {
    _supabaseClient = supabaseClient;
  }

  public async Task AddAsync(Guid noteId, Guid skillId, CancellationToken cancellationToken = default)
  {
    var model = new NoteSkill { NoteId = noteId, SkillId = skillId };
    await _supabaseClient
      .From<NoteSkill>()
      .Insert(model, cancellationToken: cancellationToken);
  }

  public async Task RemoveAsync(Guid noteId, Guid skillId, CancellationToken cancellationToken = default)
  {
    await _supabaseClient
      .From<NoteSkill>()
      .Where(x => x.NoteId == noteId && x.SkillId == skillId)
      .Delete(cancellationToken: cancellationToken);
  }

  public async Task<IEnumerable<Guid>> GetSkillIdsForNoteAsync(Guid noteId, CancellationToken cancellationToken = default)
  {
    var response = await _supabaseClient
      .From<NoteSkill>()
      .Where(x => x.NoteId == noteId)
      .Get(cancellationToken);

    return response.Models.Select(x => x.SkillId);
  }
}