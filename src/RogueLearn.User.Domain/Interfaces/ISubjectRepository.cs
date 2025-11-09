using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface ISubjectRepository : IGenericRepository<Subject>
{
    Task<IEnumerable<Subject>> GetSubjectsByRoute(Guid routeId, CancellationToken cancellationToken = default);
}