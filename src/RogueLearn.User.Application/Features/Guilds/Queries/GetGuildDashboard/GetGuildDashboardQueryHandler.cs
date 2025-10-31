using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetGuildDashboard;

public class GetGuildDashboardQueryHandler : IRequestHandler<GetGuildDashboardQuery, GuildDashboardDto>
{
    private readonly IGuildRepository _guildRepository;
    private readonly IGuildMemberRepository _guildMemberRepository;
    private readonly IGuildInvitationRepository _guildInvitationRepository;

    public GetGuildDashboardQueryHandler(
        IGuildRepository guildRepository,
        IGuildMemberRepository guildMemberRepository,
        IGuildInvitationRepository guildInvitationRepository)
    {
        _guildRepository = guildRepository;
        _guildMemberRepository = guildMemberRepository;
        _guildInvitationRepository = guildInvitationRepository;
    }

    public async Task<GuildDashboardDto> Handle(GetGuildDashboardQuery request, CancellationToken cancellationToken)
    {
        var guild = await _guildRepository.GetByIdAsync(request.GuildId, cancellationToken)
            ?? throw new NotFoundException("Guild", request.GuildId.ToString());

        var activeCount = await _guildMemberRepository.CountActiveMembersAsync(request.GuildId, cancellationToken);
        var invitations = await _guildInvitationRepository.GetInvitationsByGuildAsync(request.GuildId, cancellationToken);
        var pendingCount = invitations.Count(i => i.Status == InvitationStatus.Pending);
        var acceptedCount = invitations.Count(i => i.Status == InvitationStatus.Accepted);

        return new GuildDashboardDto
        {
            GuildId = guild.Id,
            Name = guild.Name,
            ActiveMemberCount = activeCount,
            PendingInvitationCount = pendingCount,
            AcceptedInvitationCount = acceptedCount,
            MaxMembers = guild.MaxMembers
        };
    }
}