using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
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
    [Theory]
    [AutoData]
    public async Task Handle_NotFound_Throws(UpdateTagCommand cmd)
    {
        var repo = Substitute.For<ITagRepository>();
        var logger = Substitute.For<ILogger<UpdateTagCommandHandler>>();
        var sut = new UpdateTagCommandHandler(repo, logger);

        repo.GetByIdAsync(cmd.TagId, Arg.Any<CancellationToken>()).Returns((Tag?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Forbidden_Throws(UpdateTagCommand cmd)
    {
        var repo = Substitute.For<ITagRepository>();
        var logger = Substitute.For<ILogger<UpdateTagCommandHandler>>();
        var sut = new UpdateTagCommandHandler(repo, logger);

        var tag = new Tag { Id = cmd.TagId, AuthUserId = Guid.NewGuid(), Name = "Old" };
        cmd.AuthUserId = Guid.NewGuid();
        repo.GetByIdAsync(cmd.TagId, Arg.Any<CancellationToken>()).Returns(tag);
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_EmptyName_Throws(UpdateTagCommand cmd)
    {
        var repo = Substitute.For<ITagRepository>();
        var logger = Substitute.For<ILogger<UpdateTagCommandHandler>>();
        var sut = new UpdateTagCommandHandler(repo, logger);

        var tag = new Tag { Id = cmd.TagId, AuthUserId = cmd.AuthUserId, Name = "Old" };
        repo.GetByIdAsync(cmd.TagId, Arg.Any<CancellationToken>()).Returns(tag);
        cmd.Name = "  ";
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Conflict_Throws(UpdateTagCommand cmd)
    {
        var repo = Substitute.For<ITagRepository>();
        var logger = Substitute.For<ILogger<UpdateTagCommandHandler>>();
        var sut = new UpdateTagCommandHandler(repo, logger);

        var tag = new Tag { Id = cmd.TagId, AuthUserId = cmd.AuthUserId, Name = "Old" };
        repo.GetByIdAsync(cmd.TagId, Arg.Any<CancellationToken>()).Returns(tag);

        var other = new Tag { Id = Guid.NewGuid(), AuthUserId = cmd.AuthUserId, Name = cmd.Name };
        repo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Tag, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Tag> { tag, other });

        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_Updates(UpdateTagCommand cmd)
    {
        var repo = Substitute.For<ITagRepository>();
        var logger = Substitute.For<ILogger<UpdateTagCommandHandler>>();
        var sut = new UpdateTagCommandHandler(repo, logger);

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