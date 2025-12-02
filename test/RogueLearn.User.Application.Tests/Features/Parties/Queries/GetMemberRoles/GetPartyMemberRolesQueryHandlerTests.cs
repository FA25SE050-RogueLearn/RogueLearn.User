using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Parties.Queries.GetMemberRoles;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Parties.Queries.GetMemberRoles;

public class GetPartyMemberRolesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsEmptyWhenNotMember()
    {
        var repo = Substitute.For<IPartyMemberRepository>();
        var sut = new GetPartyMemberRolesQueryHandler(repo);
        var res = await sut.Handle(new GetPartyMemberRolesQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
        res.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsSingleRole()
    {
        var repo = Substitute.For<IPartyMemberRepository>();
        var partyId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        repo.GetMemberAsync(partyId, authId, Arg.Any<CancellationToken>()).Returns(new PartyMember { PartyId = partyId, AuthUserId = authId, Role = PartyRole.Leader });
        var sut = new GetPartyMemberRolesQueryHandler(repo);
        var res = await sut.Handle(new GetPartyMemberRolesQuery(partyId, authId), CancellationToken.None);
        res.Should().ContainSingle(r => r == PartyRole.Leader);
    }
}