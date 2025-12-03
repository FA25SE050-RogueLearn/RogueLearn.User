using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Features.Parties.Commands.LeaveParty;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.LeaveParty;

public class LeavePartyCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_LeaderWithSuccessor_TransfersAndDeletesMember(LeavePartyCommand cmd)
    {
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var partyRepo = Substitute.For<IPartyRepository>();
        var sut = new LeavePartyCommandHandler(memberRepo, partyRepo);

        var leader = new PartyMember { Id = Guid.NewGuid(), PartyId = cmd.PartyId, AuthUserId = cmd.AuthUserId, Role = PartyRole.Leader, Status = MemberStatus.Active, JoinedAt = DateTimeOffset.UtcNow.AddDays(-10) };
        var successor = new PartyMember { Id = Guid.NewGuid(), PartyId = cmd.PartyId, AuthUserId = Guid.NewGuid(), Role = PartyRole.Member, Status = MemberStatus.Active, JoinedAt = DateTimeOffset.UtcNow.AddDays(-9) };
        memberRepo.GetMemberAsync(cmd.PartyId, cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(leader);
        memberRepo.CountActiveMembersAsync(cmd.PartyId, Arg.Any<CancellationToken>()).Returns(2);
        memberRepo.GetMembersByPartyAsync(cmd.PartyId, Arg.Any<CancellationToken>()).Returns(new List<PartyMember> { leader, successor });

        await sut.Handle(cmd, CancellationToken.None);
        await memberRepo.Received(1).UpdateAsync(Arg.Is<PartyMember>(m => m.Id == successor.Id && m.Role == PartyRole.Leader), Arg.Any<CancellationToken>());
        await memberRepo.Received(1).DeleteAsync(leader.Id, Arg.Any<CancellationToken>());
    }
}