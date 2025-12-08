using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Guilds.Commands.DeleteGuild;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.DeleteGuild;

public class DeleteGuildCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var repo = Substitute.For<IGuildRepository>();
        var guildId = System.Guid.NewGuid();
        var cmd = new DeleteGuildCommand(guildId);
        repo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns((Guild?)null);

        var sut = new DeleteGuildCommandHandler(repo);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_Deletes()
    {
        var repo = Substitute.For<IGuildRepository>();
        var guildId = System.Guid.NewGuid();
        var cmd = new DeleteGuildCommand(guildId);
        var g = new Guild { Id = guildId, Name = "n" };
        repo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(g);

        var sut = new DeleteGuildCommandHandler(repo);
        await sut.Handle(cmd, CancellationToken.None);

        await repo.Received(1).DeleteAsync(guildId, Arg.Any<CancellationToken>());
    }
}