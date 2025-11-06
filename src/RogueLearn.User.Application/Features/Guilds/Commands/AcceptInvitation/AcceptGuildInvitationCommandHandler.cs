using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.AcceptInvitation;

public class AcceptGuildInvitationCommandHandler : IRequestHandler<AcceptGuildInvitationCommand, Unit>
{
    private readonly IGuildInvitationRepository _invitationRepository;
    private readonly IGuildMemberRepository _memberRepository;
    private readonly IGuildRepository _guildRepository;

    public AcceptGuildInvitationCommandHandler(IGuildInvitationRepository invitationRepository, IGuildMemberRepository memberRepository, IGuildRepository guildRepository)
    {
        _invitationRepository = invitationRepository;
        _memberRepository = memberRepository;
        _guildRepository = guildRepository;
    }

    public async Task<Unit> Handle(AcceptGuildInvitationCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guildRepository.GetByIdAsync(request.GuildId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("Guild", request.GuildId.ToString());

        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("GuildInvitation", request.InvitationId.ToString());

        if (invitation.GuildId != request.GuildId)
        {
            throw new Exceptions.BadRequestException("Invitation does not belong to target guild.");
        }

        if (invitation.Status != InvitationStatus.Pending || invitation.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new Exceptions.BadRequestException("Invitation is not valid.");
        }

        if (invitation.InviteeId != request.AuthUserId)
        {
            throw new Exceptions.ForbiddenException("Invitation not intended for this user.");
        }

        // Enforce one-guild-per-user constraints before adding membership
        // 1) If the user is already an active member of any other guild, they cannot join another guild
        var memberships = await _memberRepository.GetMembershipsByUserAsync(request.AuthUserId, cancellationToken);
        if (memberships.Any(m => m.Status == MemberStatus.Active && m.GuildId != request.GuildId))
        {
            throw new Exceptions.BadRequestException("User already belongs to a guild and cannot join another guild.");
        }

        // 2) If the user has already created a guild, they cannot participate in any other guild
        var createdGuilds = await _guildRepository.GetGuildsByCreatorAsync(request.AuthUserId, cancellationToken);
        if (createdGuilds.Any(g => g.Id != request.GuildId))
        {
            throw new Exceptions.BadRequestException("User is the creator of another guild and cannot join a different guild.");
        }

        // Capacity check
        var count = await _memberRepository.CountActiveMembersAsync(request.GuildId, cancellationToken);
        if (count >= guild.MaxMembers)
        {
            throw new Exceptions.BadRequestException("Guild is at maximum capacity.");
        }

        // Add membership if not exists
        var existing = await _memberRepository.GetMemberAsync(request.GuildId, request.AuthUserId, cancellationToken);
        if (existing is null)
        {
            var member = new GuildMember
            {
                GuildId = request.GuildId,
                AuthUserId = request.AuthUserId,
                Role = GuildRole.Member,
                Status = MemberStatus.Active,
                JoinedAt = DateTimeOffset.UtcNow
            };
            await _memberRepository.AddAsync(member, cancellationToken);

            // Update guild current member count after successful addition
            var newActiveCount = await _memberRepository.CountActiveMembersAsync(request.GuildId, cancellationToken);
            guild.CurrentMemberCount = newActiveCount;
            guild.UpdatedAt = DateTimeOffset.UtcNow;
            await _guildRepository.UpdateAsync(guild, cancellationToken);
        }

        invitation.Status = InvitationStatus.Accepted;
        invitation.RespondedAt = DateTimeOffset.UtcNow;
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);

        return Unit.Value;
    }
}