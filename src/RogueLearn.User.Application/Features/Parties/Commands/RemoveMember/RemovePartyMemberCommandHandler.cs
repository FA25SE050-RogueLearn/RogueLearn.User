using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Commands.RemoveMember;

public class RemovePartyMemberCommandHandler : IRequestHandler<RemovePartyMemberCommand, Unit>
{
    private readonly IPartyMemberRepository _memberRepository;

    public RemovePartyMemberCommandHandler(IPartyMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task<Unit> Handle(RemovePartyMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetByIdAsync(request.MemberId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("PartyMember", request.MemberId.ToString());

        if (member.PartyId != request.PartyId)
        {
            throw new Exceptions.BadRequestException("Member does not belong to target party.");
        }

        if (member.Role == PartyRole.Leader)
        {
            throw new Exceptions.BadRequestException("Cannot remove Leader. Transfer leadership first.");
        }

        member.Status = MemberStatus.Inactive;
        member.LeftAt = DateTimeOffset.UtcNow;
        await _memberRepository.UpdateAsync(member, cancellationToken);

        return Unit.Value;
    }
}