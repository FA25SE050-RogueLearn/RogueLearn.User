// RogueLearn.User/src/RogueLearn.User.Domain/Interfaces/ISyllabusVersionRepository.cs
using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface ISyllabusVersionRepository : IGenericRepository<SyllabusVersion>
{
    // MODIFICATION: Using the new, more specific method name.
    Task<IEnumerable<SyllabusVersion>> GetActiveBySubjectIdAsync(Guid subjectId, CancellationToken cancellationToken = default);
}