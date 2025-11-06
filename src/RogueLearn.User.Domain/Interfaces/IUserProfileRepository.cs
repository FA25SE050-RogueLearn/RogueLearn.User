// RogueLearn.User/src/RogueLearn.User.Domain/Interfaces/IUserProfileRepository.cs
using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IUserProfileRepository : IGenericRepository<UserProfile>
{
	Task<UserProfile?> GetByAuthIdAsync(Guid authId, CancellationToken cancellationToken = default);
	Task<UserProfile?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}