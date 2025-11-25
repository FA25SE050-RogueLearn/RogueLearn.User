using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.UpdateMemberContributionPoints;

public class UpdateMemberContributionPointsCommandHandler : IRequestHandler<UpdateMemberContributionPointsCommand, Unit>
{
    private readonly IGuildMemberRepository _memberRepository;

    public UpdateMemberContributionPointsCommandHandler(IGuildMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task<Unit> Handle(UpdateMemberContributionPointsCommand request, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetMemberAsync(request.GuildId, request.MemberAuthUserId, cancellationToken)
            ?? throw new Application.Exceptions.NotFoundException("GuildMember", request.MemberAuthUserId.ToString());

        member.ContributionPoints += request.PointsDelta;
        await _memberRepository.UpdateAsync(member, cancellationToken);

        var members = await _memberRepository.GetMembersByGuildAsync(request.GuildId, cancellationToken);
        var activeOrdered = members
            .Where(m => m.Status == RogueLearn.User.Domain.Enums.MemberStatus.Active)
            .OrderByDescending(m => m.ContributionPoints)
            .ThenBy(m => m.JoinedAt)
            .ToList();

        for (int i = 0; i < activeOrdered.Count; i++)
        {
            activeOrdered[i].RankWithinGuild = i + 1;
        }

        var nonActive = members.Where(m => m.Status != RogueLearn.User.Domain.Enums.MemberStatus.Active).ToList();
        foreach (var m in nonActive)
        {
            m.RankWithinGuild = null;
        }

        await _memberRepository.UpdateRangeAsync(activeOrdered.Concat(nonActive), cancellationToken);

        return Unit.Value;
    }
}