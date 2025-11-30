// RogueLearn.User/src/RogueLearn.User.Domain/Interfaces/ISubjectRepository.cs
using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface ISubjectRepository : IGenericRepository<Subject>
{
    Task<IEnumerable<Subject>> GetSubjectsByRoute(Guid routeId, CancellationToken cancellationToken = default);

    Task<Subject?> GetSubjectForUserContextAsync(string subjectCode, Guid authUserId, CancellationToken cancellationToken = default);
    Task<Subject?> GetByCodeAsync(string subjectCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<Subject>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    // Added for paginated search
    Task<(IEnumerable<Subject> Items, int TotalCount)> GetPagedSubjectsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
}
