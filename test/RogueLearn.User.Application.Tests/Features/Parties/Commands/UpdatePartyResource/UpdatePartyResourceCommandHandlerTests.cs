using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Parties.Commands.UpdatePartyResource;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.UpdatePartyResource;

public class UpdatePartyResourceCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_WrongParty_Throws(UpdatePartyResourceCommand cmd)
    {
        var repo = Substitute.For<IPartyStashItemRepository>();
        var sut = new UpdatePartyResourceCommandHandler(repo);
        var item = new PartyStashItem { Id = cmd.StashItemId, PartyId = System.Guid.NewGuid() };
        repo.GetByIdAsync(cmd.StashItemId, Arg.Any<CancellationToken>()).Returns(item);
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_Updates(UpdatePartyResourceCommand cmd)
    {
        var repo = Substitute.For<IPartyStashItemRepository>();
        var sut = new UpdatePartyResourceCommandHandler(repo);
        var item = new PartyStashItem { Id = cmd.StashItemId, PartyId = cmd.PartyId };
        repo.GetByIdAsync(cmd.StashItemId, Arg.Any<CancellationToken>()).Returns(item);
        repo.UpdateAsync(Arg.Any<PartyStashItem>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<PartyStashItem>());

        var request = new UpdatePartyResourceRequest("T", new object(), new[] { "tag" });
        cmd = new UpdatePartyResourceCommand(cmd.PartyId, cmd.StashItemId, cmd.ActorAuthUserId, request);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).UpdateAsync(Arg.Is<PartyStashItem>(s => s.Title == "T"), Arg.Any<CancellationToken>());
    }
}