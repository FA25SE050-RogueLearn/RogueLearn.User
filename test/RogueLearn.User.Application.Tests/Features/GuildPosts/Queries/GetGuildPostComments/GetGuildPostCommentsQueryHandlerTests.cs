using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.GuildPosts.Queries.GetGuildPostComments;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Queries.GetGuildPostComments;

public class GetGuildPostCommentsQueryHandlerTests
{
    [Fact]
    public async Task Handle_PostMissing_Throws()
    {
        var query = new GetGuildPostCommentsQuery(System.Guid.NewGuid(), System.Guid.NewGuid());
        var postRepo = Substitute.For<IGuildPostRepository>();
        var commentRepo = Substitute.For<IGuildPostCommentRepository>();
        var sut = new GetGuildPostCommentsQueryHandler(postRepo, commentRepo);
        postRepo.GetByIdAsync(query.GuildId, query.PostId, Arg.Any<CancellationToken>()).Returns((GuildPost?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ReturnsMappedWithReplyCounts()
    {
        var query = new GetGuildPostCommentsQuery(System.Guid.NewGuid(), System.Guid.NewGuid());
        var postRepo = Substitute.For<IGuildPostRepository>();
        var commentRepo = Substitute.For<IGuildPostCommentRepository>();
        var sut = new GetGuildPostCommentsQueryHandler(postRepo, commentRepo);
        postRepo.GetByIdAsync(query.GuildId, query.PostId, Arg.Any<CancellationToken>()).Returns(new GuildPost { Id = query.PostId, GuildId = query.GuildId });
        var comments = new List<GuildPostComment> { new() { Id = System.Guid.NewGuid(), PostId = query.PostId, AuthorId = System.Guid.NewGuid(), Content = "C" } };
        commentRepo.GetByPostAsync(query.PostId, query.Page, query.Size, Arg.Any<CancellationToken>()).Returns(comments);
        commentRepo.CountRepliesAsync(comments[0].Id, Arg.Any<CancellationToken>()).Returns(2);

        var res = await sut.Handle(query, CancellationToken.None);
        res.Count().Should().Be(1);
        res.First().ReplyCount.Should().Be(2);
    }
}