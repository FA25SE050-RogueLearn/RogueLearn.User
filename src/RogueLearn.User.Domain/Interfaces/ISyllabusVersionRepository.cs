// RogueLearn.User/src/RogueLearn.User.Domain/Interfaces/ISyllabusVersionRepository.cs
using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface ISyllabusVersionRepository : IGenericRepository<SyllabusVersion>
{
    // MODIFIED: The previous GetActiveBySubjectIdAsync method is replaced with a more efficient batch version.
    /// <summary>
    /// Fetches all active syllabus versions for a given list of subject IDs in a single query.
    /// </summary>
    /// <param name="subjectIds">The list of subject IDs to retrieve syllabi for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An enumerable collection of active syllabus versions.</returns>
    Task<IEnumerable<SyllabusVersion>> GetActiveBySubjectIdsAsync(List<Guid> subjectIds, CancellationToken cancellationToken = default);
}