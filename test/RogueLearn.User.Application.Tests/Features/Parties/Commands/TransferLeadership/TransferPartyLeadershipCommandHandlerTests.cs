using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Parties.Commands.TransferLeadership;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.TransferLeadership;

public class TransferPartyLeadershipCommandHandlerTests
{
    [Fact]
    public async Task Handle_NoLeaders_ThrowsNotFound()
    {
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();
        var sut = new TransferPartyLeadershipCommandHandler(memberRepo, userRoleRepo, roleRepo, notify);

        var partyId = System.Guid.NewGuid();
        var toUser = System.Guid.NewGuid();
        memberRepo.GetMembersByPartyAsync(partyId, Arg.Any<CancellationToken>()).Returns(new List<PartyMember> { new() { AuthUserId = toUser, Status = MemberStatus.Active, Role = PartyRole.Member } });

        var cmd = new TransferPartyLeadershipCommand(partyId, toUser);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Succeeds_UpdatesLeaderAndRoles()
    {
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();
        var sut = new TransferPartyLeadershipCommandHandler(memberRepo, userRoleRepo, roleRepo, notify);

        var partyId = System.Guid.NewGuid();
        var newLeaderId = System.Guid.NewGuid();
        var oldLeaderId = System.Guid.NewGuid();
        var leaderRole = new Role { Id = System.Guid.NewGuid(), Name = "Party Leader" };
        roleRepo.GetByNameAsync("Party Leader", Arg.Any<CancellationToken>()).Returns(leaderRole);
        userRoleRepo.GetRolesForUserAsync(oldLeaderId, Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new() { Id = System.Guid.NewGuid(), AuthUserId = oldLeaderId, RoleId = leaderRole.Id } });
        userRoleRepo.GetRolesForUserAsync(newLeaderId, Arg.Any<CancellationToken>()).Returns(new List<UserRole>());

        var members = new List<PartyMember>
        {
            new() { AuthUserId = newLeaderId, Status = MemberStatus.Active, Role = PartyRole.Member },
            new() { AuthUserId = oldLeaderId, Status = MemberStatus.Active, Role = PartyRole.Leader }
        };
        memberRepo.GetMembersByPartyAsync(partyId, Arg.Any<CancellationToken>()).Returns(members);

        var cmd = new TransferPartyLeadershipCommand(partyId, newLeaderId);
        await sut.Handle(cmd, CancellationToken.None);
        await memberRepo.Received().UpdateAsync(Arg.Is<PartyMember>(m => m.AuthUserId == oldLeaderId && m.Role == PartyRole.Member), Arg.Any<CancellationToken>());
        await memberRepo.Received().UpdateAsync(Arg.Is<PartyMember>(m => m.AuthUserId == newLeaderId && m.Role == PartyRole.Leader), Arg.Any<CancellationToken>());
        await userRoleRepo.Received().AddAsync(Arg.Is<UserRole>(ur => ur.AuthUserId == newLeaderId && ur.RoleId == leaderRole.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Record_Creates_With_Values()
    {
        var partyId = Guid.NewGuid();
        var toUserId = Guid.NewGuid();
        var cmd = new TransferPartyLeadershipCommand(partyId, toUserId);
        cmd.PartyId.Should().Be(partyId);
        cmd.ToUserId.Should().Be(toUserId);
    }
}
