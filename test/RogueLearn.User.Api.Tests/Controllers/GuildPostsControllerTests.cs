using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.GuildPosts.Commands.CreateGuildPost;
using RogueLearn.User.Application.Features.GuildPosts.Commands.DeleteGuildPost;
using RogueLearn.User.Application.Features.GuildPosts.Commands.EditGuildPost;
using RogueLearn.User.Application.Features.GuildPosts.Commands.GuildMasterActions;
using RogueLearn.User.Application.Features.GuildPosts.Commands.Comments.CreateGuildPostComment;
using RogueLearn.User.Application.Features.GuildPosts.Commands.Comments.EditGuildPostComment;
using RogueLearn.User.Application.Features.GuildPosts.Commands.Comments.DeleteGuildPostComment;
using RogueLearn.User.Application.Features.GuildPosts.Commands.Images;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using RogueLearn.User.Application.Features.GuildPosts.Queries.GetGuildPosts;
using RogueLearn.User.Application.Features.GuildPosts.Queries.GetGuildPostById;
using RogueLearn.User.Application.Features.GuildPosts.Queries.GetPinnedGuildPosts;
using RogueLearn.User.Application.Features.GuildPosts.Queries.GetGuildPostComments;
using RogueLearn.User.Application.Features.GuildPosts.Commands.Likes.UnlikeGuildPost;
using RogueLearn.User.Application.Features.GuildPosts.Commands.Likes.LikeGuildPost;

namespace RogueLearn.User.Api.Tests.Controllers;

public class GuildPostsControllerTests
{
    private static GuildPostsController CreateController(IMediator mediator, Guid userId)
    {
        var controller = new GuildPostsController(mediator);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                }, "Test"))
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetGuildPosts_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetGuildPostsQuery>(), Arg.Any<CancellationToken>()).Returns(new List<GuildPostDto>());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetGuildPosts(Guid.NewGuid(), null, null, null, null, 1, 20, CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPinnedGuildPosts_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetPinnedGuildPostsQuery>(), Arg.Any<CancellationToken>()).Returns(new List<GuildPostDto>());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetPinnedGuildPosts(Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetGuildPostById_Returns_Ok_When_Found()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetGuildPostByIdQuery>(), Arg.Any<CancellationToken>()).Returns(new GuildPostDto { Id = Guid.NewGuid() });
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetGuildPostById(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetGuildPostById_Returns_NotFound_When_Null()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetGuildPostByIdQuery>(), Arg.Any<CancellationToken>()).Returns((GuildPostDto?)null);
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetGuildPostById(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateGuildPost_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        var expected = new CreateGuildPostResponse { PostId = Guid.NewGuid() };
        mediator.Send(Arg.Any<CreateGuildPostCommand>(), Arg.Any<CancellationToken>()).Returns(expected);
        var userId = Guid.NewGuid();
        var controller = CreateController(mediator, userId);
        var req = new CreateGuildPostRequest { Title = "t", Content = "c" };
        var res = await controller.CreateGuildPost(Guid.NewGuid(), req, CancellationToken.None);
        res.Should().BeOfType<CreatedAtActionResult>();
        var created = (CreatedAtActionResult)res;
        created.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CreateGuildPostForm_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        var expected = new CreateGuildPostResponse { PostId = Guid.NewGuid() };
        mediator.Send(Arg.Any<CreateGuildPostCommand>(), Arg.Any<CancellationToken>()).Returns(expected);
        var controller = CreateController(mediator, Guid.NewGuid());
        var files = new FormFileCollection();
        var res = await controller.CreateGuildPostForm(Guid.NewGuid(), "t", "c", new[] { "tag" }, files, CancellationToken.None);
        res.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateGuildPostForm_With_File_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        var expected = new CreateGuildPostResponse { PostId = Guid.NewGuid() };
        mediator.Send(Arg.Any<CreateGuildPostCommand>(), Arg.Any<CancellationToken>()).Returns(expected);
        var controller = CreateController(mediator, Guid.NewGuid());
        var files = new FormFileCollection
        {
            new FormFile(new MemoryStream(new byte[]{1,2,3}), 0, 3, "file", "a.png") { Headers = new HeaderDictionary(), ContentType = "image/png" }
        };
        var res = await controller.CreateGuildPostForm(Guid.NewGuid(), "t", "c", new[] { "tag" }, files, CancellationToken.None);
        res.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task UploadGuildPostImages_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        var expected = new UploadGuildPostImagesResponse { ImageUrls = new List<string> { "u" } };
        mediator.Send(Arg.Any<UploadGuildPostImagesCommand>(), Arg.Any<CancellationToken>()).Returns(expected);
        var controller = CreateController(mediator, Guid.NewGuid());
        var files = new FormFileCollection();
        var res = await controller.UploadGuildPostImages(Guid.NewGuid(), Guid.NewGuid(), files, CancellationToken.None);
        res.Should().BeOfType<CreatedResult>();
        var created = (CreatedResult)res;
        created.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task UploadGuildPostImages_With_File_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        var expected = new UploadGuildPostImagesResponse { ImageUrls = new List<string> { "u" } };
        mediator.Send(Arg.Any<UploadGuildPostImagesCommand>(), Arg.Any<CancellationToken>()).Returns(expected);
        var controller = CreateController(mediator, Guid.NewGuid());
        var files = new FormFileCollection
        {
            new FormFile(new MemoryStream(new byte[]{1}), 0, 1, "file", "b.jpg") { Headers = new HeaderDictionary(), ContentType = "image/jpeg" }
        };
        var res = await controller.UploadGuildPostImages(Guid.NewGuid(), Guid.NewGuid(), files, CancellationToken.None);
        res.Should().BeOfType<CreatedResult>();
    }

    [Fact]
    public async Task EditGuildPost_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<EditGuildPostCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);
        var userId = Guid.NewGuid();
        var controller = CreateController(mediator, userId);
        var req = new EditGuildPostRequest { Title = "t", Content = "c" };
        var guildId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var res = await controller.EditGuildPost(guildId, postId, req, CancellationToken.None);
        await mediator.Received(1).Send(Arg.Is<EditGuildPostCommand>(c => c.GuildId == guildId && c.PostId == postId && c.AuthorAuthUserId == userId));
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task EditGuildPostForm_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<EditGuildPostCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);
        var controller = CreateController(mediator, Guid.NewGuid());
        var files = new FormFileCollection();
        var res = await controller.EditGuildPostForm(Guid.NewGuid(), Guid.NewGuid(), "t", "c", new[] { "tag" }, files, CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task EditGuildPostForm_With_File_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<EditGuildPostCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);
        var controller = CreateController(mediator, Guid.NewGuid());
        var files = new FormFileCollection
        {
            new FormFile(new MemoryStream(new byte[]{1,2}), 0, 2, "file", "c.gif") { Headers = new HeaderDictionary(), ContentType = "image/gif" }
        };
        var res = await controller.EditGuildPostForm(Guid.NewGuid(), Guid.NewGuid(), "t", "c", new[] { "tag" }, files, CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteGuildPost_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<DeleteGuildPostCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);
        var userId = Guid.NewGuid();
        var controller = CreateController(mediator, userId);
        var guildId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var res = await controller.DeleteGuildPost(guildId, postId, CancellationToken.None);
        await mediator.Received(1).Send(Arg.Is<DeleteGuildPostCommand>(c => c.GuildId == guildId && c.PostId == postId && c.RequesterAuthUserId == userId));
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task CreateGuildPostComment_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        var expected = new CreateGuildPostCommentResponse { CommentId = Guid.NewGuid() };
        mediator.Send(Arg.Any<CreateGuildPostCommentCommand>(), Arg.Any<CancellationToken>()).Returns(expected);
        var controller = CreateController(mediator, Guid.NewGuid());
        var req = new CreateGuildPostCommentRequest { Content = "c" };
        var res = await controller.CreateGuildPostComment(Guid.NewGuid(), Guid.NewGuid(), req, CancellationToken.None);
        res.Should().BeOfType<CreatedResult>();
        var created = (CreatedResult)res;
        created.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task EditGuildPostComment_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<EditGuildPostCommentCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);
        var controller = CreateController(mediator, Guid.NewGuid());
        var req = new EditGuildPostCommentRequest { Content = "c" };
        var res = await controller.EditGuildPostComment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), req, CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteGuildPostComment_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<DeleteGuildPostCommentCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.DeleteGuildPostComment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetGuildPostComments_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetGuildPostCommentsQuery>(), Arg.Any<CancellationToken>()).Returns(new List<GuildPostCommentDto>());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetGuildPostComments(Guid.NewGuid(), Guid.NewGuid(), 1, 20, null, CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task LikeGuildPost_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<LikeGuildPostCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);
        var userId = Guid.NewGuid();
        var controller = CreateController(mediator, userId);
        var res = await controller.LikeGuildPost(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UnlikeGuildPost_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<UnlikeGuildPostCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.UnlikeGuildPost(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task PinUnpinLockUnlock_Return_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<PinGuildPostCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);
        mediator.Send(Arg.Any<UnpinGuildPostCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);
        mediator.Send(Arg.Any<LockGuildPostCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);
        mediator.Send(Arg.Any<UnlockGuildPostCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);
        var controller = CreateController(mediator, Guid.NewGuid());
        var guildId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        (await controller.PinGuildPost(guildId, postId, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.UnpinGuildPost(guildId, postId, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.LockGuildPost(guildId, postId, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.UnlockGuildPost(guildId, postId, CancellationToken.None)).Should().BeOfType<NoContentResult>();
    }
}
