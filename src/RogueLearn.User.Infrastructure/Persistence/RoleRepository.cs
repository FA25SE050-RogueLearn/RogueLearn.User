// RogueLearn.User/src/RogueLearn.User.Infrastructure/Persistence/RoleRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase.Postgrest;
using Client = Supabase.Client;

namespace RogueLearn.User.Infrastructure.Persistence;

public class RoleRepository : GenericRepository<Role>, IRoleRepository
{
	public RoleRepository(Client supabaseClient) : base(supabaseClient)
	{
	}

	public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
	{
		var response = await _supabaseClient
			.From<Role>()
			.Where(r => r.Name == name)
			.Single(cancellationToken);

		return response;
	}
	public async Task<IEnumerable<Role>> GetByIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default)
	{
		var ids = roleIds.ToList();
		if (!ids.Any()) return Enumerable.Empty<Role>();

		var response = await _supabaseClient
			.From<Role>()
			.Filter("id", Constants.Operator.In, ids.Select(id => id.ToString()).ToList())
			.Get(cancellationToken);

		return response.Models;
	}
}