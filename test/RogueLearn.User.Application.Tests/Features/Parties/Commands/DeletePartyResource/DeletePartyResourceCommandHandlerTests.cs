using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Parties.Commands.DeletePartyResource;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.DeletePartyResource;

public class DeletePartyResourceCommandHandlerTests
{
    [Fact]
    public async Task Handle_WrongParty_Throws()
    {
        var cmd = new DeletePartyResourceCommand(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid());
        var repo = Substitute.For<IPartyStashItemRepository>();
        var sut = new DeletePartyResourceCommandHandler(repo);
        var item = new PartyStashItem { Id = cmd.StashItemId, PartyId = System.Guid.NewGuid() };
        repo.GetByIdAsync(cmd.StashItemId, Arg.Any<CancellationToken>()).Returns(item);
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_Deletes()
    {
        var partyId = System.Guid.NewGuid();
        var stashId = System.Guid.NewGuid();
        var cmd = new DeletePartyResourceCommand(partyId, stashId, System.Guid.NewGuid());
        var repo = Substitute.For<IPartyStashItemRepository>();
        var sut = new DeletePartyResourceCommandHandler(repo);
        var item = new PartyStashItem { Id = cmd.StashItemId, PartyId = cmd.PartyId };
        repo.GetByIdAsync(cmd.StashItemId, Arg.Any<CancellationToken>()).Returns(item);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).DeleteAsync(item.Id, Arg.Any<CancellationToken>());
    }
}