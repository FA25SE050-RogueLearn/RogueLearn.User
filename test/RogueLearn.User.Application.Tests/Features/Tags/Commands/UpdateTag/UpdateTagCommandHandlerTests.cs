using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Tags.Commands.UpdateTag;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Tags.Commands.UpdateTag;

public class UpdateTagCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var repo = Substitute.For<ITagRepository>();
        var logger = Substitute.For<ILogger<UpdateTagCommandHandler>>();
        var sut = new UpdateTagCommandHandler(repo, logger);
        var cmd = new UpdateTagCommand { AuthUserId = Guid.NewGuid(), TagId = Guid.NewGuid(), Name = "Name" };
        repo.GetByIdAsync(cmd.TagId, Arg.Any<CancellationToken>()).Returns((Tag?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Forbidden_Throws()
    {
        var repo = Substitute.For<ITagRepository>();
        var logger = Substitute.For<ILogger<UpdateTagCommandHandler>>();
        var sut = new UpdateTagCommandHandler(repo, logger);
        var cmd = new UpdateTagCommand { AuthUserId = Guid.NewGuid(), TagId = Guid.NewGuid(), Name = "Name" };
        var tag = new Tag { Id = cmd.TagId, AuthUserId = Guid.NewGuid(), Name = "Old" };
        repo.GetByIdAsync(cmd.TagId, Arg.Any<CancellationToken>()).Returns(tag);
        var forbiddenCmd = new UpdateTagCommand { AuthUserId = Guid.NewGuid(), TagId = cmd.TagId, Name = cmd.Name };
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(forbiddenCmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EmptyName_Throws()
    {
        var repo = Substitute.For<ITagRepository>();
        var logger = Substitute.For<ILogger<UpdateTagCommandHandler>>();
        var sut = new UpdateTagCommandHandler(repo, logger);
        var cmd = new UpdateTagCommand { AuthUserId = Guid.NewGuid(), TagId = Guid.NewGuid(), Name = "Name" };
        var tag = new Tag { Id = cmd.TagId, AuthUserId = cmd.AuthUserId, Name = "Old" };
        repo.GetByIdAsync(cmd.TagId, Arg.Any<CancellationToken>()).Returns(tag);
        var invalidCmd = new UpdateTagCommand { AuthUserId = cmd.AuthUserId, TagId = cmd.TagId, Name = "  " };
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => sut.Handle(invalidCmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Conflict_Throws()
    {
        var repo = Substitute.For<ITagRepository>();
        var logger = Substitute.For<ILogger<UpdateTagCommandHandler>>();
        var sut = new UpdateTagCommandHandler(repo, logger);
        var cmd = new UpdateTagCommand { AuthUserId = Guid.NewGuid(), TagId = Guid.NewGuid(), Name = "Name" };
        var tag = new Tag { Id = cmd.TagId, AuthUserId = cmd.AuthUserId, Name = "Old" };
        repo.GetByIdAsync(cmd.TagId, Arg.Any<CancellationToken>()).Returns(tag);

        var other = new Tag { Id = Guid.NewGuid(), AuthUserId = cmd.AuthUserId, Name = cmd.Name };
        repo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Tag, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Tag> { tag, other });

        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_Updates()
    {
        var repo = Substitute.For<ITagRepository>();
        var logger = Substitute.For<ILogger<UpdateTagCommandHandler>>();
        var sut = new UpdateTagCommandHandler(repo, logger);
        var cmd = new UpdateTagCommand { AuthUserId = Guid.NewGuid(), TagId = Guid.NewGuid(), Name = "NewName" };
        var tag = new Tag { Id = cmd.TagId, AuthUserId = cmd.AuthUserId, Name = "Old" };
        repo.GetByIdAsync(cmd.TagId, Arg.Any<CancellationToken>()).Returns(tag);
        repo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Tag, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Tag> { tag });
        repo.UpdateAsync(Arg.Any<Tag>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Tag>());

        var resp = await sut.Handle(cmd, CancellationToken.None);
        resp.Tag.Id.Should().Be(tag.Id);
        resp.Tag.Name.Should().Be(cmd.Name);
        await repo.Received(1).UpdateAsync(Arg.Any<Tag>(), Arg.Any<CancellationToken>());
    }
}