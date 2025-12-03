using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Parties.Commands.JoinPublicParty;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.JoinPublicParty;

public class JoinPublicPartyHandlerTests
{
    [Fact]
    public async Task Handle_ThrowsWhenPartyMissingOrPrivate()
    {
        var partyRepo = Substitute.For<IPartyRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var sut = new JoinPublicPartyCommandHandler(partyRepo, memberRepo);

        partyRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Party?)null);
        var act1 = () => sut.Handle(new JoinPublicPartyCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
        await act1.Should().ThrowAsync<BadRequestException>();

        var party = new Party { Id = Guid.NewGuid(), IsPublic = false, MaxMembers = 5 };
        partyRepo.GetByIdAsync(party.Id, Arg.Any<CancellationToken>()).Returns(party);
        var act2 = () => sut.Handle(new JoinPublicPartyCommand(party.Id, Guid.NewGuid()), CancellationToken.None);
        await act2.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task Handle_ThrowsWhenAtCapacity()
    {
        var partyRepo = Substitute.For<IPartyRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var sut = new JoinPublicPartyCommandHandler(partyRepo, memberRepo);
        var party = new Party { Id = Guid.NewGuid(), IsPublic = true, MaxMembers = 1 };
        partyRepo.GetByIdAsync(party.Id, Arg.Any<CancellationToken>()).Returns(party);
        memberRepo.CountActiveMembersAsync(party.Id, Arg.Any<CancellationToken>()).Returns(1);
        var act = () => sut.Handle(new JoinPublicPartyCommand(party.Id, Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task Handle_ReactivatesExistingMemberOrAddsNew()
    {
        var partyRepo = Substitute.For<IPartyRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var sut = new JoinPublicPartyCommandHandler(partyRepo, memberRepo);
        var party = new Party { Id = Guid.NewGuid(), IsPublic = true, MaxMembers = 5 };
        partyRepo.GetByIdAsync(party.Id, Arg.Any<CancellationToken>()).Returns(party);
        memberRepo.CountActiveMembersAsync(party.Id, Arg.Any<CancellationToken>()).Returns(0);

        var userId = Guid.NewGuid();
        var existing = new PartyMember { PartyId = party.Id, AuthUserId = userId, Status = MemberStatus.Inactive };
        memberRepo.GetMemberAsync(party.Id, userId, Arg.Any<CancellationToken>()).Returns(existing);
        await sut.Handle(new JoinPublicPartyCommand(party.Id, userId), CancellationToken.None);
        await memberRepo.Received(1).UpdateAsync(Arg.Is<PartyMember>(m => m.Status == MemberStatus.Active), Arg.Any<CancellationToken>());

        memberRepo.GetMemberAsync(party.Id, userId, Arg.Any<CancellationToken>()).Returns((PartyMember?)null);
        await sut.Handle(new JoinPublicPartyCommand(party.Id, userId), CancellationToken.None);
        await memberRepo.Received(1).AddAsync(Arg.Is<PartyMember>(m => m.Role == PartyRole.Member && m.Status == MemberStatus.Active), Arg.Any<CancellationToken>());
    }
}