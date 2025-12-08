using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Parties.Commands.CreateParty;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.CreateParty;

public class CreatePartyCommandHandlerTests
{
    [Fact]
    public async Task Handle_ThrowsWhenLeaderRoleMissing()
    {
        var partyRepo = Substitute.For<IPartyRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        roleRepo.GetByNameAsync("Party Leader", Arg.Any<CancellationToken>()).Returns((Role?)null);
        partyRepo.AddAsync(Arg.Any<Party>(), Arg.Any<CancellationToken>()).Returns(ci => { var p = ci.Arg<Party>(); p.Id = Guid.NewGuid(); return p; });

        var command = new CreatePartyCommand { CreatorAuthUserId = Guid.NewGuid(), Name = "P" };
        var sut = new CreatePartyCommandHandler(partyRepo, memberRepo, userRoleRepo, roleRepo);
        var act = () => sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_SucceedsAndAssignsLeaderRole()
    {
        var partyRepo = Substitute.For<IPartyRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var role = new Role { Id = Guid.NewGuid(), Name = "Party Leader" };
        roleRepo.GetByNameAsync("Party Leader", Arg.Any<CancellationToken>()).Returns(role);
        userRoleRepo.GetRolesForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<UserRole>());
        partyRepo.AddAsync(Arg.Any<Party>(), Arg.Any<CancellationToken>()).Returns(ci => { var p = ci.Arg<Party>(); p.Id = Guid.NewGuid(); return p; });

        var sut = new CreatePartyCommandHandler(partyRepo, memberRepo, userRoleRepo, roleRepo);
        var res = await sut.Handle(new CreatePartyCommand { CreatorAuthUserId = Guid.NewGuid(), Name = "P" }, CancellationToken.None);
        res.PartyId.Should().NotBeEmpty();
        await memberRepo.Received(1).AddAsync(Arg.Is<PartyMember>(m => m.Role == PartyRole.Leader), Arg.Any<CancellationToken>());
        await userRoleRepo.Received(1).AddAsync(Arg.Any<UserRole>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotDuplicateLeaderRole()
    {
        var partyRepo = Substitute.For<IPartyRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var role = new Role { Id = Guid.NewGuid(), Name = "Party Leader" };
        roleRepo.GetByNameAsync("Party Leader", Arg.Any<CancellationToken>()).Returns(role);
        userRoleRepo.GetRolesForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new[] { new UserRole { RoleId = role.Id } });
        partyRepo.AddAsync(Arg.Any<Party>(), Arg.Any<CancellationToken>()).Returns(ci => { var p = ci.Arg<Party>(); p.Id = Guid.NewGuid(); return p; });

        var sut = new CreatePartyCommandHandler(partyRepo, memberRepo, userRoleRepo, roleRepo);
        var res = await sut.Handle(new CreatePartyCommand { CreatorAuthUserId = Guid.NewGuid(), Name = "P" }, CancellationToken.None);
        res.PartyId.Should().NotBeEmpty();
        await userRoleRepo.DidNotReceive().AddAsync(Arg.Any<UserRole>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RoleMissing_ThrowsNotFound()
    {
        var partyRepo = Substitute.For<IPartyRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var sut = new CreatePartyCommandHandler(partyRepo, memberRepo, userRoleRepo, roleRepo);

        var party = new Party { Id = System.Guid.NewGuid() };
        partyRepo.AddAsync(Arg.Any<Party>(), Arg.Any<CancellationToken>()).Returns(party);
        memberRepo.AddAsync(Arg.Any<PartyMember>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<PartyMember>());
        roleRepo.GetByNameAsync("Party Leader", Arg.Any<CancellationToken>()).Returns((Role?)null);

        var cmd = new CreatePartyCommand { Name = "Name", MaxMembers = 5, IsPublic = true, CreatorAuthUserId = System.Guid.NewGuid() };
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_AddsPartyLeaderAndRole()
    {
        var partyRepo = Substitute.For<IPartyRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var sut = new CreatePartyCommandHandler(partyRepo, memberRepo, userRoleRepo, roleRepo);

        var creator = System.Guid.NewGuid();
        var party = new Party { Id = System.Guid.NewGuid() };
        partyRepo.AddAsync(Arg.Any<Party>(), Arg.Any<CancellationToken>()).Returns(party);
        memberRepo.AddAsync(Arg.Any<PartyMember>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<PartyMember>());

        var leaderRole = new Role { Id = System.Guid.NewGuid(), Name = "Party Leader" };
        roleRepo.GetByNameAsync("Party Leader", Arg.Any<CancellationToken>()).Returns(leaderRole);
        userRoleRepo.GetRolesForUserAsync(creator, Arg.Any<CancellationToken>()).Returns(new List<UserRole>());
        userRoleRepo.AddAsync(Arg.Any<UserRole>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserRole>());

        var cmd = new CreatePartyCommand { Name = "Name", MaxMembers = 5, IsPublic = true, CreatorAuthUserId = creator };
        var res = await sut.Handle(cmd, CancellationToken.None);

        await memberRepo.Received(1).AddAsync(Arg.Is<PartyMember>(m => m.Role == PartyRole.Leader && m.AuthUserId == creator && m.PartyId == party.Id), Arg.Any<CancellationToken>());
        await userRoleRepo.Received(1).AddAsync(Arg.Is<UserRole>(ur => ur.AuthUserId == creator && ur.RoleId == leaderRole.Id), Arg.Any<CancellationToken>());
        res.PartyId.Should().Be(party.Id);
        res.RoleGranted.Should().Be("PartyLeader");
    }

    [Fact]
    public async Task Handle_ExistingUserRole_NoDuplicateAdd()
    {
        var partyRepo = Substitute.For<IPartyRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var sut = new CreatePartyCommandHandler(partyRepo, memberRepo, userRoleRepo, roleRepo);

        var creator = System.Guid.NewGuid();
        var party = new Party { Id = System.Guid.NewGuid() };
        partyRepo.AddAsync(Arg.Any<Party>(), Arg.Any<CancellationToken>()).Returns(party);
        memberRepo.AddAsync(Arg.Any<PartyMember>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<PartyMember>());

        var leaderRole = new Role { Id = System.Guid.NewGuid(), Name = "Party Leader" };
        roleRepo.GetByNameAsync("Party Leader", Arg.Any<CancellationToken>()).Returns(leaderRole);
        userRoleRepo.GetRolesForUserAsync(creator, Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new() { Id = System.Guid.NewGuid(), AuthUserId = creator, RoleId = leaderRole.Id } });

        var cmd = new CreatePartyCommand { Name = "Name", MaxMembers = 5, IsPublic = true, CreatorAuthUserId = creator };
        var res = await sut.Handle(cmd, CancellationToken.None);

        await userRoleRepo.DidNotReceive().AddAsync(Arg.Any<UserRole>(), Arg.Any<CancellationToken>());
        res.PartyId.Should().Be(party.Id);
    }
}
