using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.GuildPosts.Queries.GetGuildPostComments;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Queries.GetGuildPostComments;

public class GetGuildPostCommentsQueryHandlerTests
{
    [Fact]
    public async Task Handle_CachesAuthorProfile_AndCountsReplies()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var commentRepo = Substitute.For<IGuildPostCommentRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();

        var guildId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        postRepo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(new GuildPost { Id = postId, GuildId = guildId });

        var authorId = Guid.NewGuid();
        var comments = new[]
        {
            new GuildPostComment { Id = Guid.NewGuid(), PostId = postId, AuthorId = authorId, Content = "c1" },
            new GuildPostComment { Id = Guid.NewGuid(), PostId = postId, AuthorId = authorId, Content = "c2" }
        };
        commentRepo.GetByPostAsync(postId, 1, 20, Arg.Any<CancellationToken>()).Returns(comments);
        commentRepo.CountRepliesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(2);
        userRepo.GetByAuthIdAsync(authorId, Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = authorId, Username = "u", Email = "e", ProfileImageUrl = "a" });

        var sut = new GetGuildPostCommentsQueryHandler(postRepo, commentRepo, userRepo);
        var res = await sut.Handle(new GetGuildPostCommentsQuery(guildId, postId, 1, 20, null), CancellationToken.None);

        res.Should().HaveCount(2);
        res.First().AuthorUsername.Should().Be("u");
        res.First().ReplyCount.Should().Be(2);
        await userRepo.Received(1).GetByAuthIdAsync(authorId, Arg.Any<CancellationToken>());
    }
}
