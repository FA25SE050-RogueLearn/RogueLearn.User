using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.RemoveMember;

public class RemoveGuildMemberCommandHandler : IRequestHandler<RemoveGuildMemberCommand, Unit>
{
    private readonly IGuildMemberRepository _memberRepository;
    private readonly IGuildRepository _guildRepository;

    public RemoveGuildMemberCommandHandler(IGuildMemberRepository memberRepository, IGuildRepository guildRepository)
    {
        _memberRepository = memberRepository;
        _guildRepository = guildRepository;
    }

    public async Task<Unit> Handle(RemoveGuildMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetByIdAsync(request.MemberId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("GuildMember", request.MemberId.ToString());

        if (member.GuildId != request.GuildId)
        {
            throw new Exceptions.BadRequestException("Member does not belong to target guild.");
        }

        if (member.Role == RogueLearn.User.Domain.Enums.GuildRole.GuildMaster)
        {
            throw new Exceptions.BadRequestException("Cannot remove GuildMaster. Transfer leadership first.");
        }

        member.Status = MemberStatus.Inactive;
        member.LeftAt = DateTimeOffset.UtcNow;
        await _memberRepository.UpdateAsync(member, cancellationToken);

        // Decrement guild current member count after removal
        var guild = await _guildRepository.GetByIdAsync(request.GuildId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("Guild", request.GuildId.ToString());
        var newActiveCount = await _memberRepository.CountActiveMembersAsync(request.GuildId, cancellationToken);
        guild.CurrentMemberCount = newActiveCount;
        guild.UpdatedAt = DateTimeOffset.UtcNow;
        await _guildRepository.UpdateAsync(guild, cancellationToken);

        return Unit.Value;
    }
}