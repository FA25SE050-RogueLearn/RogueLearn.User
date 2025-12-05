using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Features.Parties.Commands.AddPartyResource;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.AddPartyResource;

public class AddPartyResourceCommandHandlerTests
{
    [Fact]
    public async Task Handle_AddsResourceAndSendsNotification()
    {
        var repo = Substitute.For<IPartyStashItemRepository>();
        var notif = Substitute.For<IPartyNotificationService>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new AddPartyResourceCommandHandler(repo, notif, mapper);

        var cmd = new AddPartyResourceCommand(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(), "T", new { text = "body" }, new[] { "tag1" });
        repo.AddAsync(Arg.Any<PartyStashItem>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<PartyStashItem>());
        mapper.Map<PartyStashItemDto>(Arg.Any<PartyStashItem>()).Returns(new PartyStashItemDto(System.Guid.NewGuid(), cmd.PartyId, cmd.OriginalNoteId, cmd.SharedByUserId, cmd.Title, cmd.Content, cmd.Tags, System.DateTimeOffset.UtcNow, System.DateTimeOffset.UtcNow));

        var resp = await sut.Handle(cmd, CancellationToken.None);
        Assert.Equal(cmd.PartyId, resp.PartyId);
    }
}