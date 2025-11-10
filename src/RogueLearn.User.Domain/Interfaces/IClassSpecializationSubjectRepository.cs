// RogueLearn.User/src/RogueLearn.User.Domain/Interfaces/IClassSpecializationSubjectRepository.cs
using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

/// <summary>
/// Defines the repository contract for managing the link between Classes and their specialized Subjects.
/// </summary>
public interface IClassSpecializationSubjectRepository : IGenericRepository<ClassSpecializationSubject>
{
    /// <summary>
    /// Retrieves all Subject entities that are specifically associated with a given Class.
    /// This is a specialized query to handle the many-to-many relationship.
    /// </summary>
    /// <param name="classId">The unique identifier of the Class.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of Subject entities linked to the specified class.</returns>
    Task<IEnumerable<Subject>> GetSubjectByClassIdAsync(Guid classId, CancellationToken cancellationToken);
}