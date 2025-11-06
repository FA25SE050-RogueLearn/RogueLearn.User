using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.LeaveGuild;

public class LeaveGuildCommandHandler : IRequestHandler<LeaveGuildCommand, Unit>
{
    private readonly IGuildMemberRepository _memberRepository;
    private readonly IGuildRepository _guildRepository;

    public LeaveGuildCommandHandler(IGuildMemberRepository memberRepository, IGuildRepository guildRepository)
    {
        _memberRepository = memberRepository;
        _guildRepository = guildRepository;
    }

    public async Task<Unit> Handle(LeaveGuildCommand request, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetMemberAsync(request.GuildId, request.AuthUserId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("GuildMember", request.AuthUserId.ToString());

        // If GuildMaster and there are other active members, disallow
        if (member.Role == GuildRole.GuildMaster)
        {
            var activeCount = await _memberRepository.CountActiveMembersAsync(request.GuildId, cancellationToken);
            if (activeCount > 1)
            {
                throw new Exceptions.BadRequestException("GuildMaster cannot leave while other members exist. Transfer leadership first.");
            }
        }

        member.Status = MemberStatus.Inactive;
        member.LeftAt = DateTimeOffset.UtcNow;
        await _memberRepository.UpdateAsync(member, cancellationToken);

        // Decrement guild current member count after leaving
        var guild = await _guildRepository.GetByIdAsync(request.GuildId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("Guild", request.GuildId.ToString());
        var newActiveCount = await _memberRepository.CountActiveMembersAsync(request.GuildId, cancellationToken);
        guild.CurrentMemberCount = newActiveCount;
        guild.UpdatedAt = DateTimeOffset.UtcNow;
        await _guildRepository.UpdateAsync(guild, cancellationToken);
        return Unit.Value;
    }
}