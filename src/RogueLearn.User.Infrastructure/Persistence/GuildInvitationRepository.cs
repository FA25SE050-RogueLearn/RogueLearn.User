using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class GuildInvitationRepository : GenericRepository<GuildInvitation>, IGuildInvitationRepository
{
    public GuildInvitationRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<IEnumerable<GuildInvitation>> GetInvitationsByGuildAsync(Guid guildId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<GuildInvitation>()
            .Filter("guild_id", Supabase.Postgrest.Constants.Operator.Equals, guildId.ToString())
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<IEnumerable<GuildInvitation>> GetPendingInvitationsByGuildAsync(Guid guildId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<GuildInvitation>()
            .Filter("guild_id", Supabase.Postgrest.Constants.Operator.Equals, guildId.ToString())
            .Filter("status", Supabase.Postgrest.Constants.Operator.Equals, InvitationStatus.Pending.ToString())
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<GuildInvitation?> GetByGuildAndInviteeAsync(Guid guildId, Guid inviteeAuthUserId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<GuildInvitation>()
            .Filter("guild_id", Supabase.Postgrest.Constants.Operator.Equals, guildId.ToString())
            .Filter("invitee_id", Supabase.Postgrest.Constants.Operator.Equals, inviteeAuthUserId.ToString())
            .Get(cancellationToken);

        return response.Models.FirstOrDefault();
    }
}