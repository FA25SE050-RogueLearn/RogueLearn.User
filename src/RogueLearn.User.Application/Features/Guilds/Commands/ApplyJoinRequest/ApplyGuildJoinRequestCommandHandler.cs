using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.ApplyJoinRequest;

public class ApplyGuildJoinRequestCommandHandler : IRequestHandler<ApplyGuildJoinRequestCommand, Unit>
{
    private readonly IGuildRepository _guildRepository;
    private readonly IGuildMemberRepository _memberRepository;
    private readonly IGuildJoinRequestRepository _joinRequestRepository;

    public ApplyGuildJoinRequestCommandHandler(
        IGuildRepository guildRepository,
        IGuildMemberRepository memberRepository,
        IGuildJoinRequestRepository joinRequestRepository)
    {
        _guildRepository = guildRepository;
        _memberRepository = memberRepository;
        _joinRequestRepository = joinRequestRepository;
    }

    public async Task<Unit> Handle(ApplyGuildJoinRequestCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guildRepository.GetByIdAsync(request.GuildId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("Guild", request.GuildId.ToString());

        // Already a member of this guild?
        var existingMember = await _memberRepository.GetMemberAsync(request.GuildId, request.AuthUserId, cancellationToken);
        if (existingMember is not null && existingMember.Status == MemberStatus.Active)
        {
            throw new Exceptions.BadRequestException("You are already a member of this guild.");
        }

        // Enforce single-guild membership policy
        var memberships = await _memberRepository.GetMembershipsByUserAsync(request.AuthUserId, cancellationToken);
        if (memberships.Any(m => m.Status == MemberStatus.Active && m.GuildId != request.GuildId))
        {
            throw new Exceptions.BadRequestException("You already belong to a different guild.");
        }

        // Prevent guild creators from joining other guilds
        var createdGuilds = await _guildRepository.GetGuildsByCreatorAsync(request.AuthUserId, cancellationToken);
        if (createdGuilds.Any(g => g.Id != request.GuildId))
        {
            throw new Exceptions.BadRequestException("Guild creators cannot join other guilds.");
        }

        // Capacity check
        var activeCount = await _memberRepository.CountActiveMembersAsync(request.GuildId, cancellationToken);
        if (activeCount >= guild.MaxMembers)
        {
            throw new Exceptions.BadRequestException("Guild is at maximum capacity.");
        }

        // Check for existing pending request
        var myRequests = await _joinRequestRepository.GetRequestsByRequesterAsync(request.AuthUserId, cancellationToken);
        var existingPendingForGuild = myRequests.FirstOrDefault(r => r.GuildId == request.GuildId && r.Status == GuildJoinRequestStatus.Pending);
        if (existingPendingForGuild is not null)
        {
            throw new Exceptions.BadRequestException("You already have a pending join request for this guild.");
        }

        if (!guild.RequiresApproval && guild.IsPublic)
        {
            // Auto-join: create membership and record an accepted request for audit
            if (existingMember is null)
            {
                var newMember = new GuildMember
                {
                    GuildId = request.GuildId,
                    AuthUserId = request.AuthUserId,
                    Role = GuildRole.Member,
                    Status = MemberStatus.Active,
                    JoinedAt = DateTimeOffset.UtcNow
                };
                await _memberRepository.AddAsync(newMember, cancellationToken);
            }

            var acceptedRecord = new GuildJoinRequest
            {
                GuildId = request.GuildId,
                RequesterId = request.AuthUserId,
                Message = request.Message,
                Status = GuildJoinRequestStatus.Accepted,
                CreatedAt = DateTimeOffset.UtcNow,
                RespondedAt = DateTimeOffset.UtcNow
            };
            await _joinRequestRepository.AddAsync(acceptedRecord, cancellationToken);
        }
        else
        {
            // Create a pending request for approval
            var reqEntity = new GuildJoinRequest
            {
                GuildId = request.GuildId,
                RequesterId = request.AuthUserId,
                Message = request.Message,
                Status = GuildJoinRequestStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _joinRequestRepository.AddAsync(reqEntity, cancellationToken);
        }

        return Unit.Value;
    }
}