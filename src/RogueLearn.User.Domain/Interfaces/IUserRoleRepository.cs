// RogueLearn.User/src/RogueLearn.User.Domain/Interfaces/IUserRoleRepository.cs
using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IUserRoleRepository : IGenericRepository<UserRole>
{
	Task<IEnumerable<UserRole>> GetRolesForUserAsync(Guid authUserId, CancellationToken cancellationToken = default);
}