using System.Threading;
using System.Threading.Tasks;
using Moq;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using RogueLearn.User.Application.Features.GuildPosts.Queries.GetGuildPosts;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Queries;

public class GetGuildPostsQueryHandlerTests
{
    [Fact]
    public async Task GetGuildPosts_Should_Return_Dtos()
    {
        var repo = new Mock<IGuildPostRepository>();
        var guildId = Guid.NewGuid();

        repo.Setup(r => r.GetByGuildAsync(guildId, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GuildPost> {
                new GuildPost { Id = Guid.NewGuid(), GuildId = guildId, Title = "t1" },
                new GuildPost { Id = Guid.NewGuid(), GuildId = guildId, Title = "t2" }
            });

        var handler = new GetGuildPostsQueryHandler(repo.Object);
        var result = await handler.Handle(new GetGuildPostsQuery(guildId, null, null, null, null, 1, 20), CancellationToken.None);

        Assert.NotNull(result);
        Assert.All(result, p => Assert.Equal(guildId, p.GuildId));
    }
}