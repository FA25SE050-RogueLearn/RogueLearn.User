using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Parties.Commands.CreateParty;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.CreateParty;

public class CreatePartyHandlerTests
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

        var sut = new CreatePartyCommandHandler(partyRepo, memberRepo, userRoleRepo, roleRepo);
        var act = () => sut.Handle(new CreatePartyCommand { CreatorAuthUserId = Guid.NewGuid(), Name = "P" }, CancellationToken.None);
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
}