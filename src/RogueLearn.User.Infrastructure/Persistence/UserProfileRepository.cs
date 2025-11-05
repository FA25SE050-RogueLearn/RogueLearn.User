// RogueLearn.User/src/RogueLearn.User.Infrastructure/Persistence/UserProfileRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class UserProfileRepository : GenericRepository<UserProfile>, IUserProfileRepository
{
	public UserProfileRepository(Client supabaseClient) : base(supabaseClient)
	{
	}

	public async Task<UserProfile?> GetByAuthIdAsync(Guid authId, CancellationToken cancellationToken = default)
	{
		var response = await _supabaseClient
			.From<UserProfile>()
			.Where(p => p.AuthUserId == authId)
			.Single(cancellationToken);

		return response;
	}

	public async Task<UserProfile?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
	{
		var response = await _supabaseClient
			.From<UserProfile>()
			.Where(p => p.Email == email)
			.Single(cancellationToken);

		return response;
	}
}