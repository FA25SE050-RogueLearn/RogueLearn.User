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
            .Where(i => i.GuildId == guildId)
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<IEnumerable<GuildInvitation>> GetPendingInvitationsByGuildAsync(Guid guildId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<GuildInvitation>()
            .Where(i => i.GuildId == guildId && i.Status == InvitationStatus.Pending)
            .Get(cancellationToken);

        return response.Models;
    }
}