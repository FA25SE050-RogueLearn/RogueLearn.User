namespace RogueLearn.User.Domain.Interfaces;

public interface INoteQuestRepository
{
    Task AddAsync(Guid noteId, Guid questId, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid noteId, Guid questId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Guid>> GetQuestIdsForNoteAsync(Guid noteId, CancellationToken cancellationToken = default);
}