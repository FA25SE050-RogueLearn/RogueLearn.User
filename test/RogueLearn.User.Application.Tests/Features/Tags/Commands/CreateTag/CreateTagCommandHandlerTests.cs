using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Tags.Commands.CreateTag;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Tags.Commands.CreateTag;

public class CreateTagCommandHandlerTests
{
    [Fact]
    public async Task Handle_EmptyName_Throws()
    {
        var repo = Substitute.For<ITagRepository>();
        var logger = Substitute.For<ILogger<CreateTagCommandHandler>>();
        var sut = new CreateTagCommandHandler(repo, logger);
        var cmd = new CreateTagCommand { AuthUserId = Guid.NewGuid(), Name = " " };
        await Assert.ThrowsAsync<System.ArgumentException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExistingSlug_ReturnsExisting()
    {
        var repo = Substitute.For<ITagRepository>();
        var logger = Substitute.For<ILogger<CreateTagCommandHandler>>();
        var sut = new CreateTagCommandHandler(repo, logger);

        var cmd = new CreateTagCommand { AuthUserId = Guid.NewGuid(), Name = "MyTag" };
        var existing = new Tag { Id = System.Guid.NewGuid(), AuthUserId = cmd.AuthUserId, Name = cmd.Name };
        repo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<System.Func<Tag, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new System.Collections.Generic.List<Tag> { existing });

        var resp = await sut.Handle(cmd, CancellationToken.None);
        resp.Tag.Id.Should().Be(existing.Id);
        resp.Tag.Name.Should().Be(existing.Name);
        await repo.DidNotReceive().AddAsync(Arg.Any<Tag>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Success_Creates()
    {
        var repo = Substitute.For<ITagRepository>();
        var logger = Substitute.For<ILogger<CreateTagCommandHandler>>();
        var sut = new CreateTagCommandHandler(repo, logger);

        var cmd = new CreateTagCommand { AuthUserId = Guid.NewGuid(), Name = "NewTag" };
        repo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<System.Func<Tag, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new System.Collections.Generic.List<Tag>());
        var created = new Tag { Id = System.Guid.NewGuid(), AuthUserId = cmd.AuthUserId, Name = cmd.Name };
        repo.AddAsync(Arg.Any<Tag>(), Arg.Any<CancellationToken>()).Returns(created);

        var resp = await sut.Handle(cmd, CancellationToken.None);
        resp.Tag.Id.Should().Be(created.Id);
        resp.Tag.Name.Should().Be(cmd.Name);
    }
}