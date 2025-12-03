using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using RogueLearn.User.Application.Features.GuildPosts.Queries.GetGuildPosts;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Queries.GetGuildPosts;

public class GetGuildPostsQueryHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_ReturnsMapped(GetGuildPostsQuery query)
    {
        var repo = Substitute.For<IGuildPostRepository>();
        var sut = new GetGuildPostsQueryHandler(repo);

        var post = new GuildPost
        {
            Id = System.Guid.NewGuid(),
            GuildId = query.GuildId,
            AuthorId = System.Guid.NewGuid(),
            Title = "Title",
            Content = "content",
            Tags = new[] { "tag1" },
            Attachments = new Dictionary<string, object> { ["key"] = "val" },
            IsPinned = false,
            IsLocked = false,
            Status = GuildPostStatus.published,
            CommentCount = 3,
            LikeCount = 5
        };

        repo.GetByGuildAsync(query.GuildId, query.Tag, query.AuthorId, query.Pinned, query.Search, query.Page, query.Size, Arg.Any<CancellationToken>())
            .Returns(new List<GuildPost> { post });

        var result = await sut.Handle(query, CancellationToken.None);
        var dto = result.First();
        dto.GuildId.Should().Be(post.GuildId);
        dto.Title.Should().Be("Title");
        dto.CommentCount.Should().Be(3);
        dto.LikeCount.Should().Be(5);
    }
}