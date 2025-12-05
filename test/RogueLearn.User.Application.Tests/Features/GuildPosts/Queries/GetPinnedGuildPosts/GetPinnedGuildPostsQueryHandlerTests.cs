using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.GuildPosts.Queries.GetPinnedGuildPosts;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Queries.GetPinnedGuildPosts;

public class GetPinnedGuildPostsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsPinned()
    {
        var query = new GetPinnedGuildPostsQuery(System.Guid.NewGuid());
        var repo = Substitute.For<IGuildPostRepository>();
        var sut = new GetPinnedGuildPostsQueryHandler(repo);
        var posts = new List<GuildPost> { new() { Id = System.Guid.NewGuid(), GuildId = query.GuildId, Title = "T", IsPinned = true } };
        repo.GetPinnedByGuildAsync(query.GuildId, Arg.Any<CancellationToken>()).Returns(posts);
        var res = await sut.Handle(query, CancellationToken.None);
        res.Count().Should().Be(1);
        res.First().IsPinned.Should().BeTrue();
    }
}