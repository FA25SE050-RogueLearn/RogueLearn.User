using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Parties.Commands.TransferLeadership;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.TransferLeadership;

public class TransferPartyLeadershipCommandHandlerTests
{
    [Fact]
    public async Task Handle_TransfersLeadershipAndDemotesOthers()
    {
        var repo = Substitute.For<IPartyMemberRepository>();
        var partyId = Guid.NewGuid();
        var toUserId = Guid.NewGuid();

        var currentLeader = new PartyMember { PartyId = partyId, AuthUserId = Guid.NewGuid(), Role = PartyRole.Leader, Status = MemberStatus.Active };
        var otherLeader = new PartyMember { PartyId = partyId, AuthUserId = Guid.NewGuid(), Role = PartyRole.Leader, Status = MemberStatus.Active };
        var target = new PartyMember { PartyId = partyId, AuthUserId = toUserId, Role = PartyRole.Member, Status = MemberStatus.Active };

        repo.GetMembersByPartyAsync(partyId, Arg.Any<CancellationToken>()).Returns(new[] { currentLeader, otherLeader, target });

        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        roleRepo.GetByNameAsync("Party Leader", Arg.Any<CancellationToken>()).Returns(new Role { Id = Guid.NewGuid(), Name = "Party Leader" });
        var notification = Substitute.For<IPartyNotificationService>();
        var sut = new TransferPartyLeadershipCommandHandler(repo, userRoleRepo, roleRepo, notification);
        await sut.Handle(new TransferPartyLeadershipCommand(partyId, toUserId), CancellationToken.None);

        target.Role.Should().Be(PartyRole.Leader);
        await repo.Received(1).UpdateAsync(target, Arg.Any<CancellationToken>());
        currentLeader.Role.Should().Be(PartyRole.Member);
        otherLeader.Role.Should().Be(PartyRole.Member);
        await repo.Received(2).UpdateAsync(Arg.Is<PartyMember>(m => m.Role == PartyRole.Member), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ThrowsWhenNoLeader()
    {
        var repo = Substitute.For<IPartyMemberRepository>();
        var partyId = Guid.NewGuid();
        var toUserId = Guid.NewGuid();
        var target = new PartyMember { PartyId = partyId, AuthUserId = toUserId, Role = PartyRole.Member, Status = MemberStatus.Active };
        repo.GetMembersByPartyAsync(partyId, Arg.Any<CancellationToken>()).Returns(new[] { target });

        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        roleRepo.GetByNameAsync("Party Leader", Arg.Any<CancellationToken>()).Returns(new Role { Id = Guid.NewGuid(), Name = "Party Leader" });
        var notification = Substitute.For<IPartyNotificationService>();
        var sut = new TransferPartyLeadershipCommandHandler(repo, userRoleRepo, roleRepo, notification);
        var act = () => sut.Handle(new TransferPartyLeadershipCommand(partyId, toUserId), CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
    }

    [Fact]
    public async Task Handle_ThrowsWhenTargetNotActive()
    {
        var repo = Substitute.For<IPartyMemberRepository>();
        var partyId = Guid.NewGuid();
        var toUserId = Guid.NewGuid();
        var leader = new PartyMember { PartyId = partyId, AuthUserId = Guid.NewGuid(), Role = PartyRole.Leader, Status = MemberStatus.Active };
        var target = new PartyMember { PartyId = partyId, AuthUserId = toUserId, Role = PartyRole.Member, Status = MemberStatus.Inactive };
        repo.GetMembersByPartyAsync(partyId, Arg.Any<CancellationToken>()).Returns(new[] { leader, target });

        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        roleRepo.GetByNameAsync("Party Leader", Arg.Any<CancellationToken>()).Returns(new Role { Id = Guid.NewGuid(), Name = "Party Leader" });
        var notification = Substitute.For<IPartyNotificationService>();
        var sut = new TransferPartyLeadershipCommandHandler(repo, userRoleRepo, roleRepo, notification);
        var act = () => sut.Handle(new TransferPartyLeadershipCommand(partyId, toUserId), CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
    }
}