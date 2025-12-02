using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Parties.Commands.DeleteParty;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.DeleteParty;

public class DeletePartyHandlerTests
{
    [Fact]
    public async Task Handle_ThrowsWhenNotFound()
    {
        var repo = Substitute.For<IPartyRepository>();
        var sut = new DeletePartyCommandHandler(repo);
        var act = () => sut.Handle(new DeletePartyCommand(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DeletesWhenFound()
    {
        var repo = Substitute.For<IPartyRepository>();
        var party = new Party { Id = Guid.NewGuid() };
        repo.GetByIdAsync(party.Id, Arg.Any<CancellationToken>()).Returns(party);
        var sut = new DeletePartyCommandHandler(repo);
        await sut.Handle(new DeletePartyCommand(party.Id), CancellationToken.None);
        await repo.Received(1).DeleteAsync(party.Id, Arg.Any<CancellationToken>());
    }
}