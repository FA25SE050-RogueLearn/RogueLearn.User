using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Parties.Commands.ConfigureParty;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.ConfigureParty;

public class ConfigurePartyHandlerTests
{
    [Fact]
    public async Task Handle_ThrowsWhenNotFound()
    {
        var repo = Substitute.For<IPartyRepository>();
        var sut = new ConfigurePartySettingsCommandHandler(repo);
        var act = () => sut.Handle(new ConfigurePartySettingsCommand(Guid.NewGuid(), "n", "d", "public", 5), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ThrowsWhenMaxMembersTooSmall()
    {
        var repo = Substitute.For<IPartyRepository>();
        var party = new Party { Id = Guid.NewGuid(), CurrentMemberCount = 5, MaxMembers = 6 };
        repo.GetByIdAsync(party.Id, Arg.Any<CancellationToken>()).Returns(party);
        var sut = new ConfigurePartySettingsCommandHandler(repo);
        var act = () => sut.Handle(new ConfigurePartySettingsCommand(party.Id, "n", "d", "public", 5), CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task Handle_UpdatesParty()
    {
        var repo = Substitute.For<IPartyRepository>();
        var party = new Party { Id = Guid.NewGuid(), CurrentMemberCount = 2, MaxMembers = 6 };
        repo.GetByIdAsync(party.Id, Arg.Any<CancellationToken>()).Returns(party);
        var sut = new ConfigurePartySettingsCommandHandler(repo);
        await sut.Handle(new ConfigurePartySettingsCommand(party.Id, "n", "d", "private", 6), CancellationToken.None);
        await repo.Received(1).UpdateAsync(Arg.Is<Party>(p => !p.IsPublic && p.Name == "n" && p.Description == "d" && p.MaxMembers == 6), Arg.Any<CancellationToken>());
    }
}