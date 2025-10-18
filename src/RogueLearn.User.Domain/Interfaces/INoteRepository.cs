// RogueLearn.User/src/RogueLearn.User.Domain/Interfaces/INoteRepository.cs
using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface INoteRepository : IGenericRepository<Note>
{
    Task<IEnumerable<Note>> GetByUserAsync(Guid authUserId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Note>> SearchByUserAsync(Guid authUserId, string search, CancellationToken cancellationToken = default);
    Task<IEnumerable<Note>> GetPublicAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Note>> SearchPublicAsync(string search, CancellationToken cancellationToken = default);
}