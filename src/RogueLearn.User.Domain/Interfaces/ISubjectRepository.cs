// RogueLearn.User/src/RogueLearn.User.Domain/Interfaces/ISubjectRepository.cs
using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface ISubjectRepository : IGenericRepository<Subject>
{
    Task<IEnumerable<Subject>> GetSubjectsByRoute(Guid routeId, CancellationToken cancellationToken = default);

    // REPLACED: The previous method is removed in favor of a more comprehensive one.
    // This new method will find a subject by its code within the full context of a user's
    // program and their chosen specialization class.
    Task<Subject?> GetSubjectForUserContextAsync(string subjectCode, Guid authUserId, CancellationToken cancellationToken = default);
    Task<Subject?> GetByCodeAsync(string subjectCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<Subject>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
