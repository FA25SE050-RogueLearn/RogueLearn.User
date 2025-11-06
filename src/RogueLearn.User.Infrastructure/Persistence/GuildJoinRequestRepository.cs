using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class GuildJoinRequestRepository : GenericRepository<GuildJoinRequest>, IGuildJoinRequestRepository
{
    public GuildJoinRequestRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<IReadOnlyList<GuildJoinRequest>> GetRequestsByGuildAsync(Guid guildId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<GuildJoinRequest>()
            .Filter("guild_id", Supabase.Postgrest.Constants.Operator.Equals, guildId.ToString())
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<IReadOnlyList<GuildJoinRequest>> GetPendingRequestsByGuildAsync(Guid guildId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<GuildJoinRequest>()
            .Filter("guild_id", Supabase.Postgrest.Constants.Operator.Equals, guildId.ToString())
            // Avoid enum serialization issues by filtering using string representation
            .Filter("status", Supabase.Postgrest.Constants.Operator.Equals, GuildJoinRequestStatus.Pending.ToString())
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<IReadOnlyList<GuildJoinRequest>> GetRequestsByRequesterAsync(Guid requesterAuthUserId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<GuildJoinRequest>()
            .Filter("requester_id", Supabase.Postgrest.Constants.Operator.Equals, requesterAuthUserId.ToString())
            .Get(cancellationToken);

        return response.Models;
    }
}