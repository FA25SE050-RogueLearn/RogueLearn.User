using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace RogueLearn.User.Infrastructure.Persistence;

public class GuildMemberRepository : GenericRepository<GuildMember>, IGuildMemberRepository
{
    public GuildMemberRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<IEnumerable<GuildMember>> GetMembersByGuildAsync(Guid guildId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<GuildMember>()
            .Filter("guild_id", Operator.Equals, guildId.ToString())
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<GuildMember?> GetMemberAsync(Guid guildId, Guid authUserId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<GuildMember>()
            .Filter("guild_id", Operator.Equals, guildId.ToString())
            .Filter("auth_user_id", Operator.Equals, authUserId.ToString())
            .Single(cancellationToken);

        return response;
    }

    public async Task<bool> IsGuildMasterAsync(Guid guildId, Guid authUserId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<GuildMember>()
            .Filter("guild_id", Operator.Equals, guildId.ToString())
            .Filter("auth_user_id", Operator.Equals, authUserId.ToString())
            .Filter("role", Operator.Equals, GuildRole.GuildMaster.ToString())
            .Filter("status", Operator.Equals, MemberStatus.Active.ToString())
            .Get(cancellationToken);

        return response.Models.Any();
    }

    public async Task<int> CountActiveMembersAsync(Guid guildId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<GuildMember>()
            .Filter("guild_id", Operator.Equals, guildId.ToString())
            .Filter("status", Operator.Equals, MemberStatus.Active.ToString())
            .Get(cancellationToken);

        return response.Models.Count;
    }

    public async Task<IEnumerable<GuildMember>> GetMembershipsByUserAsync(Guid authUserId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<GuildMember>()
            .Filter("auth_user_id", Operator.Equals, authUserId.ToString())
            .Get(cancellationToken);

        return response.Models;
    }
}