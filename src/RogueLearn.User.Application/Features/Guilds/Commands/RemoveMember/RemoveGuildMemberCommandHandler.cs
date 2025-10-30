using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.RemoveMember;

public class RemoveGuildMemberCommandHandler : IRequestHandler<RemoveGuildMemberCommand, Unit>
{
    private readonly IGuildMemberRepository _memberRepository;

    public RemoveGuildMemberCommandHandler(IGuildMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
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

        return Unit.Value;
    }
}