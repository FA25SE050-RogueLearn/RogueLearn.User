using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.Tags.Queries.GetMyTags;
using RogueLearn.User.Application.Features.Tags.DTOs;
using RogueLearn.User.Application.Features.Tags.Commands.CreateTag;
using RogueLearn.User.Application.Features.Tags.Commands.DeleteTag;
using RogueLearn.User.Application.Features.Tags.Queries.GetTagsForNote;
using RogueLearn.User.Application.Features.Tags.Commands.AttachTagToNote;
using RogueLearn.User.Application.Features.Tags.Commands.CreateTagAndAttachToNote;
using RogueLearn.User.Application.Features.Tags.Commands.RemoveTagFromNote;
using RogueLearn.User.Application.Features.Tags.Commands.UpdateTag;

namespace RogueLearn.User.Api.Tests.Controllers;

public class TagsControllerTests
{
    private static TagsController CreateController(IMediator mediator, Guid userId)
    {
        var controller = new TagsController(mediator);
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
    public async Task GetMyTags_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        var userId = Guid.NewGuid();
        var expected = new GetMyTagsResponse { Tags = new List<TagDto> { new() { Id = Guid.NewGuid(), Name = "algorithms" } } };
        mediator.Send(Arg.Is<GetMyTagsQuery>(q => q.AuthUserId == userId && q.Search == null), Arg.Any<CancellationToken>())
                .Returns(expected);

        var controller = CreateController(mediator, userId);
        var res = await controller.GetMyTags(null, CancellationToken.None);

        res.Result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)res.Result!;
        ok.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CreateTag_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        var userId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var resp = new CreateTagResponse { Tag = new TagDto { Id = tagId, Name = "x" } };
        mediator.Send(Arg.Any<CreateTagCommand>(), Arg.Any<CancellationToken>()).Returns(resp);
        var controller = CreateController(mediator, userId);
        var res = await controller.CreateTag(new CreateTagCommand { Name = "x" }, CancellationToken.None);
        res.Result.Should().BeOfType<CreatedResult>();
    }

    [Fact]
    public async Task DeleteTag_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.DeleteTag(Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetTagsForNote_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var expected = new GetTagsForNoteResponse { Tags = new List<TagDto>() };
        mediator.Send(Arg.Any<GetTagsForNoteQuery>(), Arg.Any<CancellationToken>()).Returns(expected);
        var res = await controller.GetTagsForNote(Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AttachTag_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<AttachTagToNoteCommand>(), Arg.Any<CancellationToken>()).Returns(new AttachTagToNoteResponse());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.AttachTag(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateAndAttach_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<CreateTagAndAttachToNoteCommand>(), Arg.Any<CancellationToken>()).Returns(new CreateTagAndAttachToNoteResponse());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.CreateAndAttach(Guid.NewGuid(), new CreateTagAndAttachToNoteCommand { Name = "x" }, CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RemoveTag_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.RemoveTag(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateTag_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<UpdateTagCommand>(), Arg.Any<CancellationToken>()).Returns(new UpdateTagResponse { Tag = new TagDto { Id = Guid.NewGuid(), Name = "y" } });
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.UpdateTag(Guid.NewGuid(), new UpdateTagCommand { Name = "y" }, CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }
}
