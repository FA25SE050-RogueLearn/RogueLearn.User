using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface ILecturerVerificationRequestRepository : IGenericRepository<LecturerVerificationRequest>
{
    Task<bool> AnyPendingAsync(Guid authUserId, CancellationToken cancellationToken = default);
    Task<bool> AnyApprovedAsync(Guid authUserId, CancellationToken cancellationToken = default);
}