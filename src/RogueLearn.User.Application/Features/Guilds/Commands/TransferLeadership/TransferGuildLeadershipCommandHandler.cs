using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.TransferLeadership;

public class TransferGuildLeadershipCommandHandler : IRequestHandler<TransferGuildLeadershipCommand, Unit>
{
    private readonly IGuildMemberRepository _memberRepository;
    private readonly IGuildNotificationService? _notificationService;

    public TransferGuildLeadershipCommandHandler(IGuildMemberRepository memberRepository, IGuildNotificationService notificationService)
    {
        _memberRepository = memberRepository;
        _notificationService = notificationService;
    }

    public async Task<Unit> Handle(TransferGuildLeadershipCommand request, CancellationToken cancellationToken)
    {
        var members = await _memberRepository.GetMembersByGuildAsync(request.GuildId, cancellationToken);

        var newMaster = members.FirstOrDefault(m => m.AuthUserId == request.ToUserId && m.Status == MemberStatus.Active)
                        ?? throw new Application.Exceptions.NotFoundException("GuildMember", request.ToUserId.ToString());

        var masters = members.Where(m => m.Role == GuildRole.GuildMaster).ToList();
        if (!masters.Any())
        {
            throw new Application.Exceptions.NotFoundException("GuildMaster", request.GuildId.ToString());
        }

        foreach (var master in masters)
        {
            if (master.AuthUserId != newMaster.AuthUserId)
            {
                master.Role = GuildRole.Member;
                await _memberRepository.UpdateAsync(master, cancellationToken);
            }
        }

        newMaster.Role = GuildRole.GuildMaster;
        await _memberRepository.UpdateAsync(newMaster, cancellationToken);

        if (_notificationService != null)
        {
            await _notificationService.NotifyLeadershipTransferredAsync(request.GuildId, newMaster.AuthUserId, cancellationToken);
        }

        return Unit.Value;
    }
}