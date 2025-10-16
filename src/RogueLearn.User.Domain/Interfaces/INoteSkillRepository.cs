namespace RogueLearn.User.Domain.Interfaces;

public interface INoteSkillRepository
{
    Task AddAsync(Guid noteId, Guid skillId, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid noteId, Guid skillId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Guid>> GetSkillIdsForNoteAsync(Guid noteId, CancellationToken cancellationToken = default);
}