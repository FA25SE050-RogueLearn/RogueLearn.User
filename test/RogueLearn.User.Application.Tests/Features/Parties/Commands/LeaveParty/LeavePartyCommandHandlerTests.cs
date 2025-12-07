using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Features.Parties.Commands.LeaveParty;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.LeaveParty;

public class LeavePartyCommandHandlerTests
{
    [Fact]
    public async Task Handle_LeaderSoleMember_DeletesParty()
    {
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var partyRepo = Substitute.For<IPartyRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var sut = new LeavePartyCommandHandler(memberRepo, partyRepo, userRoleRepo, roleRepo);

        var partyId = System.Guid.NewGuid();
        var userId = System.Guid.NewGuid();
        var leader = new PartyMember { Id = System.Guid.NewGuid(), PartyId = partyId, AuthUserId = userId, Role = PartyRole.Leader, Status = MemberStatus.Active };
        memberRepo.GetMemberAsync(partyId, userId, Arg.Any<CancellationToken>()).Returns(leader);
        memberRepo.CountActiveMembersAsync(partyId, Arg.Any<CancellationToken>()).Returns(1);

        var leaderRole = new Role { Id = System.Guid.NewGuid(), Name = "Party Leader" };
        roleRepo.GetByNameAsync("Party Leader", Arg.Any<CancellationToken>()).Returns(leaderRole);
        userRoleRepo.GetRolesForUserAsync(userId, Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new() { Id = System.Guid.NewGuid(), AuthUserId = userId, RoleId = leaderRole.Id } });

        await sut.Handle(new LeavePartyCommand(partyId, userId), CancellationToken.None);
        await memberRepo.Received(1).DeleteAsync(leader.Id, Arg.Any<CancellationToken>());
        await partyRepo.Received(1).DeleteAsync(partyId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LeaderWithSuccessor_PromotesNextOwner()
    {
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var partyRepo = Substitute.For<IPartyRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var sut = new LeavePartyCommandHandler(memberRepo, partyRepo, userRoleRepo, roleRepo);

        var partyId = System.Guid.NewGuid();
        var leaderId = System.Guid.NewGuid();
        var leader = new PartyMember { Id = System.Guid.NewGuid(), PartyId = partyId, AuthUserId = leaderId, Role = PartyRole.Leader, Status = MemberStatus.Active, JoinedAt = System.DateTimeOffset.UtcNow.AddDays(-10) };
        var successor = new PartyMember { Id = System.Guid.NewGuid(), PartyId = partyId, AuthUserId = System.Guid.NewGuid(), Role = PartyRole.Member, Status = MemberStatus.Active, JoinedAt = System.DateTimeOffset.UtcNow.AddDays(-5) };

        memberRepo.GetMemberAsync(partyId, leaderId, Arg.Any<CancellationToken>()).Returns(leader);
        memberRepo.CountActiveMembersAsync(partyId, Arg.Any<CancellationToken>()).Returns(2);
        memberRepo.GetMembersByPartyAsync(partyId, Arg.Any<CancellationToken>()).Returns(new List<PartyMember> { leader, successor });

        var leaderRole = new Role { Id = System.Guid.NewGuid(), Name = "Party Leader" };
        roleRepo.GetByNameAsync("Party Leader", Arg.Any<CancellationToken>()).Returns(leaderRole);
        userRoleRepo.GetRolesForUserAsync(leaderId, Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new() { Id = System.Guid.NewGuid(), AuthUserId = leaderId, RoleId = leaderRole.Id } });
        userRoleRepo.GetRolesForUserAsync(successor.AuthUserId, Arg.Any<CancellationToken>()).Returns(new List<UserRole>());

        await sut.Handle(new LeavePartyCommand(partyId, leaderId), CancellationToken.None);
        await memberRepo.Received(1).UpdateAsync(Arg.Is<PartyMember>(m => m.Id == successor.Id && m.Role == PartyRole.Leader), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LeaderWithSuccessor_RoleMissing_ThrowsNotFound()
    {
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var partyRepo = Substitute.For<IPartyRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var sut = new LeavePartyCommandHandler(memberRepo, partyRepo, userRoleRepo, roleRepo);

        var partyId = System.Guid.NewGuid();
        var leaderId = System.Guid.NewGuid();
        var leader = new PartyMember { Id = System.Guid.NewGuid(), PartyId = partyId, AuthUserId = leaderId, Role = PartyRole.Leader, Status = MemberStatus.Active, JoinedAt = System.DateTimeOffset.UtcNow.AddDays(-10) };
        var successor = new PartyMember { Id = System.Guid.NewGuid(), PartyId = partyId, AuthUserId = System.Guid.NewGuid(), Role = PartyRole.Member, Status = MemberStatus.Active, JoinedAt = System.DateTimeOffset.UtcNow.AddDays(-5) };

        memberRepo.GetMemberAsync(partyId, leaderId, Arg.Any<CancellationToken>()).Returns(leader);
        memberRepo.CountActiveMembersAsync(partyId, Arg.Any<CancellationToken>()).Returns(2);
        memberRepo.GetMembersByPartyAsync(partyId, Arg.Any<CancellationToken>()).Returns(new List<PartyMember> { leader, successor });
        roleRepo.GetByNameAsync("Party Leader", Arg.Any<CancellationToken>()).Returns((Role?)null);

        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.NotFoundException>(() => sut.Handle(new LeavePartyCommand(partyId, leaderId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_LeaderMultipleMembers_NoEligible_SoDeletesParty()
    {
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var partyRepo = Substitute.For<IPartyRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var sut = new LeavePartyCommandHandler(memberRepo, partyRepo, userRoleRepo, roleRepo);

        var partyId = System.Guid.NewGuid();
        var leaderId = System.Guid.NewGuid();
        var leader = new PartyMember { Id = System.Guid.NewGuid(), PartyId = partyId, AuthUserId = leaderId, Role = PartyRole.Leader, Status = MemberStatus.Active };
        var leftMember = new PartyMember { Id = System.Guid.NewGuid(), PartyId = partyId, AuthUserId = System.Guid.NewGuid(), Role = PartyRole.Member, Status = MemberStatus.Left };

        memberRepo.GetMemberAsync(partyId, leaderId, Arg.Any<CancellationToken>()).Returns(leader);
        memberRepo.CountActiveMembersAsync(partyId, Arg.Any<CancellationToken>()).Returns(2);
        memberRepo.GetMembersByPartyAsync(partyId, Arg.Any<CancellationToken>()).Returns(new List<PartyMember> { leader, leftMember });

        await sut.Handle(new LeavePartyCommand(partyId, leaderId), CancellationToken.None);
        await memberRepo.Received(1).DeleteAsync(leader.Id, Arg.Any<CancellationToken>());
        await partyRepo.Received(1).DeleteAsync(partyId, Arg.Any<CancellationToken>());
    }
}
