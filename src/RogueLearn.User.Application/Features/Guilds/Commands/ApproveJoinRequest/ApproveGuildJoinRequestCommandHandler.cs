using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.ApproveJoinRequest;

public class ApproveGuildJoinRequestCommandHandler : IRequestHandler<ApproveGuildJoinRequestCommand, Unit>
{
    private readonly IGuildRepository _guildRepository;
    private readonly IGuildMemberRepository _memberRepository;
    private readonly IGuildJoinRequestRepository _joinRequestRepository;

    public ApproveGuildJoinRequestCommandHandler(
        IGuildRepository guildRepository,
        IGuildMemberRepository memberRepository,
        IGuildJoinRequestRepository joinRequestRepository)
    {
        _guildRepository = guildRepository;
        _memberRepository = memberRepository;
        _joinRequestRepository = joinRequestRepository;
    }

    public async Task<Unit> Handle(ApproveGuildJoinRequestCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guildRepository.GetByIdAsync(request.GuildId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("Guild", request.GuildId.ToString());

        var joinReq = await _joinRequestRepository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("GuildJoinRequest", request.RequestId.ToString());

        if (joinReq.GuildId != request.GuildId)
        {
            throw new Exceptions.BadRequestException("Join request does not belong to target guild.");
        }

        if (joinReq.Status != GuildJoinRequestStatus.Pending || (joinReq.ExpiresAt.HasValue && joinReq.ExpiresAt <= DateTimeOffset.UtcNow))
        {
            throw new Exceptions.BadRequestException("Join request is not valid.");
        }

        // Enforce single-guild policy and creator restriction
        var memberships = await _memberRepository.GetMembershipsByUserAsync(joinReq.RequesterId, cancellationToken);
        if (memberships.Any(m => m.Status == MemberStatus.Active && m.GuildId != request.GuildId))
        {
            throw new Exceptions.BadRequestException("Requester already belongs to a different guild.");
        }

        var createdGuilds = await _guildRepository.GetGuildsByCreatorAsync(joinReq.RequesterId, cancellationToken);
        if (createdGuilds.Any(g => g.Id != request.GuildId))
        {
            throw new Exceptions.BadRequestException("Requester is the creator of another guild and cannot join.");
        }

        // Capacity check
        var activeCount = await _memberRepository.CountActiveMembersAsync(request.GuildId, cancellationToken);
        if (activeCount >= guild.MaxMembers)
        {
            throw new Exceptions.BadRequestException("Guild is at maximum capacity.");
        }

        // Add membership if not exists
        var existingMember = await _memberRepository.GetMemberAsync(request.GuildId, joinReq.RequesterId, cancellationToken);
        if (existingMember is null)
        {
            var newMember = new GuildMember
            {
                GuildId = request.GuildId,
                AuthUserId = joinReq.RequesterId,
                Role = GuildRole.Member,
                Status = MemberStatus.Active,
                JoinedAt = DateTimeOffset.UtcNow
            };
            await _memberRepository.AddAsync(newMember, cancellationToken);

            var newActiveCount = await _memberRepository.CountActiveMembersAsync(request.GuildId, cancellationToken);
            guild.CurrentMemberCount = newActiveCount;
            guild.UpdatedAt = DateTimeOffset.UtcNow;
            await _guildRepository.UpdateAsync(guild, cancellationToken);

            var members = await _memberRepository.GetMembersByGuildAsync(request.GuildId, cancellationToken);
            var activeOrdered = members
                .Where(m => m.Status == MemberStatus.Active)
                .OrderByDescending(m => m.ContributionPoints)
                .ThenBy(m => m.JoinedAt)
                .ToList();

            for (int i = 0; i < activeOrdered.Count; i++)
            {
                activeOrdered[i].RankWithinGuild = i + 1;
            }

            var nonActive = members.Where(m => m.Status != MemberStatus.Active).ToList();
            foreach (var m in nonActive)
            {
                m.RankWithinGuild = null;
            }

            await _memberRepository.UpdateRangeAsync(activeOrdered.Concat(nonActive), cancellationToken);

            var otherRequests = await _joinRequestRepository.GetRequestsByRequesterAsync(joinReq.RequesterId, cancellationToken);
            var toRemove = otherRequests.Where(r => r.GuildId != request.GuildId && r.Status == GuildJoinRequestStatus.Pending).Select(r => r.Id).ToList();
            if (toRemove.Any())
            {
                await _joinRequestRepository.DeleteRangeAsync(toRemove, cancellationToken);
            }
        }

        joinReq.Status = GuildJoinRequestStatus.Accepted;
        joinReq.RespondedAt = DateTimeOffset.UtcNow;
        await _joinRequestRepository.UpdateAsync(joinReq, cancellationToken);

        return Unit.Value;
    }
}