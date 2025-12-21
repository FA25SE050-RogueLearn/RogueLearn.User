using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class UserRoleRepository : GenericRepository<UserRole>, IUserRoleRepository
{
	public UserRoleRepository(Client supabaseClient) : base(supabaseClient)
	{
	}

	public async Task<IEnumerable<UserRole>> GetRolesForUserAsync(Guid authUserId, CancellationToken cancellationToken = default)
	{
		var response = await _supabaseClient
			.From<UserRole>()
			.Where(ur => ur.AuthUserId == authUserId)
			.Get(cancellationToken);

		return response.Models;
	}

	public async Task<IEnumerable<UserRole>> GetUsersByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
	{
		var response = await _supabaseClient
			.From<UserRole>()
			.Where(ur => ur.RoleId == roleId)
			.Get(cancellationToken);

		return response.Models;
	}
}