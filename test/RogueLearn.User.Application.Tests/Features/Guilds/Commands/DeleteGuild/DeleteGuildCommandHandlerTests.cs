using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Guilds.Commands.DeleteGuild;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.DeleteGuild;

public class DeleteGuildCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_NotFound_Throws(DeleteGuildCommand cmd)
    {
        var repo = Substitute.For<IGuildRepository>();
        repo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns((Guild?)null);

        var sut = new DeleteGuildCommandHandler(repo);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_Deletes(DeleteGuildCommand cmd)
    {
        var repo = Substitute.For<IGuildRepository>();
        var g = new Guild { Id = cmd.GuildId, Name = "n" };
        repo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(g);

        var sut = new DeleteGuildCommandHandler(repo);
        await sut.Handle(cmd, CancellationToken.None);

        await repo.Received(1).DeleteAsync(cmd.GuildId, Arg.Any<CancellationToken>());
    }
}