using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetAllUserProfiles;
using RogueLearn.User.Application.Features.UserFullInfo.Queries.GetFullUserInfo;

namespace RogueLearn.User.Api.Tests.Controllers;

public class ProfilesControllerTests
{
    private static ProfilesController CreateController(IMediator mediator, Guid userId)
    {
        var controller = new ProfilesController(mediator);
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
    public async Task GetMyProfile_Returns_NotFound_When_Null()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetUserProfileByAuthIdQuery>(), Arg.Any<CancellationToken>()).Returns((UserProfileDto?)null);
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetMyProfile(CancellationToken.None);
        res.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetUserProfileSocial_Returns_NotFound_When_Null()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetFullUserInfoQuery>(), Arg.Any<CancellationToken>()).Returns((FullUserInfoResponse?)null);
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetUserProfileSocial(Guid.NewGuid());
        res.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetUserProfileSocial_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        var full = new FullUserInfoResponse
        {
            Profile = new(),
            Auth = new(),
            Counts = new(),
            Relations = new()
        };
        mediator.Send(Arg.Any<GetFullUserInfoQuery>(), Arg.Any<CancellationToken>()).Returns(full);
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetUserProfileSocial(Guid.NewGuid());
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUserProfileByAuthId_Returns_NotFound_When_Null()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetUserProfileByAuthIdQuery>(), Arg.Any<CancellationToken>()).Returns((UserProfileDto?)null);
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetUserProfileByAuthId(Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetUserProfileByAuthId_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetUserProfileByAuthIdQuery>(), Arg.Any<CancellationToken>()).Returns(new UserProfileDto());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetUserProfileByAuthId(Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAllUserProfilesAuthorized_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetAllUserProfilesQuery>(), Arg.Any<CancellationToken>()).Returns(new GetAllUserProfilesResponse());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetAllUserProfilesAuthorized(CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }
}
