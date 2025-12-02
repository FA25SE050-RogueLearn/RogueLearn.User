using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Commands.UpdateGuildMeritPoints;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.UpdateGuildMeritPoints;

public class UpdateGuildMeritPointsCommandHandlerTests
{
    [Fact]
    public async Task Handle_IncrementsMeritPoints()
    {
        var repo = Substitute.For<IGuildRepository>();
        var guild = new Guild { Id = Guid.NewGuid(), MeritPoints = 10 };
        repo.GetByIdAsync(guild.Id, Arg.Any<CancellationToken>()).Returns(guild);

        var sut = new UpdateGuildMeritPointsCommandHandler(repo);
        await sut.Handle(new UpdateGuildMeritPointsCommand(guild.Id, 5), CancellationToken.None);

        guild.MeritPoints.Should().Be(15);
        await repo.Received(1).UpdateAsync(guild, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DecrementsMeritPoints()
    {
        var repo = Substitute.For<IGuildRepository>();
        var guild = new Guild { Id = Guid.NewGuid(), MeritPoints = 10 };
        repo.GetByIdAsync(guild.Id, Arg.Any<CancellationToken>()).Returns(guild);

        var sut = new UpdateGuildMeritPointsCommandHandler(repo);
        await sut.Handle(new UpdateGuildMeritPointsCommand(guild.Id, -3), CancellationToken.None);

        guild.MeritPoints.Should().Be(7);
        await repo.Received(1).UpdateAsync(guild, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ThrowsNotFoundWhenGuildMissing()
    {
        var repo = Substitute.For<IGuildRepository>();
        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Guild?)null);

        var sut = new UpdateGuildMeritPointsCommandHandler(repo);
        var act = () => sut.Handle(new UpdateGuildMeritPointsCommand(id, 1), CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
    }

    [Fact]
    public void Command_CarriesValues()
    {
        var id = Guid.NewGuid();
        var cmd = new UpdateGuildMeritPointsCommand(id, 42);
        cmd.GuildId.Should().Be(id);
        cmd.PointsDelta.Should().Be(42);
    }
}