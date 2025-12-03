using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.RemoveMember;

public class RemoveGuildMemberCommandHandler : IRequestHandler<RemoveGuildMemberCommand, Unit>
{
    private readonly IGuildMemberRepository _memberRepository;
    private readonly IGuildRepository _guildRepository;
    private readonly IGuildNotificationService? _notificationService;

    public RemoveGuildMemberCommandHandler(IGuildMemberRepository memberRepository, IGuildRepository guildRepository, IGuildNotificationService notificationService)
    {
        _memberRepository = memberRepository;
        _guildRepository = guildRepository;
        _notificationService = notificationService;
    }

    public async Task<Unit> Handle(RemoveGuildMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetByIdAsync(request.MemberId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("GuildMember", request.MemberId.ToString());

        if (member.GuildId != request.GuildId)
        {
            throw new Exceptions.BadRequestException("Member does not belong to target guild.");
        }

        if (member.Role == GuildRole.GuildMaster)
        {
            throw new Exceptions.BadRequestException("Cannot remove GuildMaster. Transfer leadership first.");
        }

        await _memberRepository.DeleteAsync(member.Id, cancellationToken);

        // Decrement guild current member count after removal
        var guild = await _guildRepository.GetByIdAsync(request.GuildId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("Guild", request.GuildId.ToString());
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

        if (_notificationService != null)
        {
            await _notificationService.NotifyMemberRemovedAsync(request.GuildId, member.AuthUserId, cancellationToken);
        }

        return Unit.Value;
    }
}