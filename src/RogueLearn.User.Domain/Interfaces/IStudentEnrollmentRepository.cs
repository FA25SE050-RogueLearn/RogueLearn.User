using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IStudentEnrollmentRepository : IGenericRepository<StudentEnrollment>
{
    // Retrieve the active enrollment for a given auth user using explicit string filters
    Task<StudentEnrollment?> GetActiveForAuthUserAsync(Guid authUserId, CancellationToken cancellationToken = default);
}