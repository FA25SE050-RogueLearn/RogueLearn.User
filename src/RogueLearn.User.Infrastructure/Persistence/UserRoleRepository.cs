// RogueLearn.User/src/RogueLearn.User.Infrastructure/Persistence/UserRoleRepository.cs
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
}