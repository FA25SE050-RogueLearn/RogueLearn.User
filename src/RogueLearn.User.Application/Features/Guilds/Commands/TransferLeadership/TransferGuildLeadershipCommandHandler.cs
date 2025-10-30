using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.TransferLeadership;

public class TransferGuildLeadershipCommandHandler : IRequestHandler<TransferGuildLeadershipCommand, Unit>
{
    private readonly IGuildMemberRepository _memberRepository;

    public TransferGuildLeadershipCommandHandler(IGuildMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task<Unit> Handle(TransferGuildLeadershipCommand request, CancellationToken cancellationToken)
    {
        // Find current GuildMaster
        var members = await _memberRepository.GetMembersByGuildAsync(request.GuildId, cancellationToken);
        var currentMaster = members.FirstOrDefault(m => m.Role == GuildRole.GuildMaster);
        if (currentMaster is null)
        {
            throw new Application.Exceptions.NotFoundException("GuildMaster", request.GuildId.ToString());
        }

        var newMaster = members.FirstOrDefault(m => m.AuthUserId == request.ToUserId && m.Status == MemberStatus.Active)
                        ?? throw new Application.Exceptions.NotFoundException("GuildMember", request.ToUserId.ToString());

        // Demote current master to Member (could be Officer if desired)
        currentMaster.Role = GuildRole.Member;
        await _memberRepository.UpdateAsync(currentMaster, cancellationToken);

        // Promote new master
        newMaster.Role = GuildRole.GuildMaster;
        await _memberRepository.UpdateAsync(newMaster, cancellationToken);

        return Unit.Value;
    }
}