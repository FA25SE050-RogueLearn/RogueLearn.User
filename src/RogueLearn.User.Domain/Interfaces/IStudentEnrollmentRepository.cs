using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IStudentEnrollmentRepository : IGenericRepository<StudentEnrollment>
{
    Task<StudentEnrollment?> GetActiveForAuthUserAsync(Guid authUserId, CancellationToken cancellationToken = default);
}