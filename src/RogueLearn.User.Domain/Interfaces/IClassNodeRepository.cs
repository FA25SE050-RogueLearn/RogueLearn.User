using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IClassNodeRepository : IGenericRepository<ClassNode>
{
    Task<IEnumerable<ClassNode>> GetByClassAndTitleAsync(Guid classId, string title, CancellationToken cancellationToken = default);
}