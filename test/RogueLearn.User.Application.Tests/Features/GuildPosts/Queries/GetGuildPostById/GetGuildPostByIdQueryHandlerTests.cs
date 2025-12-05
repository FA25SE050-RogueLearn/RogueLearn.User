using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.GuildPosts.Queries.GetGuildPostById;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Queries.GetGuildPostById;

public class GetGuildPostByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_ReturnsNull()
    {
        var query = new GetGuildPostByIdQuery(System.Guid.NewGuid(), System.Guid.NewGuid());
        var repo = Substitute.For<IGuildPostRepository>();
        var sut = new GetGuildPostByIdQueryHandler(repo);
        repo.GetByIdAsync(query.GuildId, query.PostId, Arg.Any<CancellationToken>()).Returns((GuildPost?)null);
        var res = await sut.Handle(query, CancellationToken.None);
        res.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Found_Maps()
    {
        var query = new GetGuildPostByIdQuery(System.Guid.NewGuid(), System.Guid.NewGuid());
        var repo = Substitute.For<IGuildPostRepository>();
        var sut = new GetGuildPostByIdQueryHandler(repo);
        var post = new GuildPost { Id = query.PostId, GuildId = query.GuildId, AuthorId = Guid.NewGuid(), Title = "T", Content = "C", IsPinned = true, IsLocked = false };
        repo.GetByIdAsync(query.GuildId, query.PostId, Arg.Any<CancellationToken>()).Returns(post);
        var res = await sut.Handle(query, CancellationToken.None);
        res!.Id.Should().Be(query.PostId);
        res.GuildId.Should().Be(query.GuildId);
        res.IsPinned.Should().BeTrue();
    }
}