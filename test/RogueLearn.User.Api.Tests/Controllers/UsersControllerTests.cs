using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.Student.Queries.GetAcademicStatus;
using RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;
using RogueLearn.User.Application.Features.UserContext.Queries.GetUserContextByAuthId;
using RogueLearn.User.Application.Features.UserProfiles.Commands.UpdateMyProfile;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetAllUserProfiles;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;
using RogueLearn.User.Application.Features.UserFullInfo.Queries.GetFullUserInfo;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Api.Tests.Controllers;

public class UsersControllerTests
{
    private static UsersController CreateController(IMediator mediator, Guid userId)
    {
        var controller = new UsersController(mediator);
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
    public async Task GetMyAcademicStatus_Returns_NotFound_When_Null()
    {
        var mediator = Substitute.For<IMediator>();
        var userId = Guid.NewGuid();
        mediator.Send(Arg.Is<GetAcademicStatusQuery>(q => q.AuthUserId == userId), Arg.Any<CancellationToken>())
                .Returns((GetAcademicStatusResponse?)null);

        var controller = CreateController(mediator, userId);
        var res = await controller.GetMyAcademicStatus(CancellationToken.None);

        res.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFound = (NotFoundObjectResult)res.Result!;
        notFound.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMyAcademicStatus_Returns_Ok_When_Found()
    {
        var mediator = Substitute.For<IMediator>();
        var userId = Guid.NewGuid();
        var dto = new GetAcademicStatusResponse { EnrollmentId = Guid.NewGuid() };
        mediator.Send(Arg.Is<GetAcademicStatusQuery>(q => q.AuthUserId == userId), Arg.Any<CancellationToken>())
                .Returns(dto);

        var controller = CreateController(mediator, userId);
        var res = await controller.GetMyAcademicStatus(CancellationToken.None);

        res.Result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)res.Result!;
        ok.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task ProcessMyAcademicRecord_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        var userId = Guid.NewGuid();
        mediator.Send(Arg.Any<ProcessAcademicRecordCommand>(), Arg.Any<CancellationToken>()).Returns(new ProcessAcademicRecordResponse());
        var controller = CreateController(mediator, userId);
        var res = await controller.ProcessMyAcademicRecord("<html>", Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMyContext_Returns_NotFound_When_Null()
    {
        var mediator = Substitute.For<IMediator>();
        var userId = Guid.NewGuid();
        mediator.Send(Arg.Any<GetUserContextByAuthIdQuery>(), Arg.Any<CancellationToken>()).Returns((UserContextDto?)null);
        var controller = CreateController(mediator, userId);
        var res = await controller.GetMyContext(CancellationToken.None);
        res.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetMyContext_Returns_Ok_When_Found()
    {
        var mediator = Substitute.For<IMediator>();
        var userId = Guid.NewGuid();
        mediator.Send(Arg.Any<GetUserContextByAuthIdQuery>(), Arg.Any<CancellationToken>()).Returns(new UserContextDto());
        var controller = CreateController(mediator, userId);
        var res = await controller.GetMyContext(CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMyFullInfo_Returns_NotFound_When_Null()
    {
        var mediator = Substitute.For<IMediator>();
        var userId = Guid.NewGuid();
        mediator.Send(Arg.Any<GetFullUserInfoQuery>(), Arg.Any<CancellationToken>()).Returns((FullUserInfoResponse?)null);
        var controller = CreateController(mediator, userId);
        var res = await controller.GetMyFullInfo();
        res.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetMyFullInfo_Returns_Ok_When_Found()
    {
        var mediator = Substitute.For<IMediator>();
        var userId = Guid.NewGuid();
        mediator.Send(Arg.Any<GetFullUserInfoQuery>(), Arg.Any<CancellationToken>()).Returns(new FullUserInfoResponse());
        var controller = CreateController(mediator, userId);
        var res = await controller.GetMyFullInfo();
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PatchMyProfileForm_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        var userId = Guid.NewGuid();
        mediator.Send(Arg.Any<UpdateMyProfileCommand>(), Arg.Any<CancellationToken>()).Returns(new UserProfileDto());
        var controller = CreateController(mediator, userId);
        var res = await controller.PatchMyProfileForm(new UpdateMyProfileCommand(), null, CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PatchMyProfileForm_Sets_Image_Bytes_And_Metadata()
    {
        var mediator = Substitute.For<IMediator>();
        var userId = Guid.NewGuid();
        mediator.Send(Arg.Any<UpdateMyProfileCommand>(), Arg.Any<CancellationToken>()).Returns(new UserProfileDto());

        var controller = CreateController(mediator, userId);

        var bytes = new byte[] { 1, 2, 3, 4 };
        var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, name: "profileImage", fileName: "avatar.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var cmd = new UpdateMyProfileCommand();
        var res = await controller.PatchMyProfileForm(cmd, file, CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();

        await mediator.Received(1).Send(
            Arg.Is<UpdateMyProfileCommand>(c =>
                c.ProfileImageBytes != null && c.ProfileImageBytes.Length == bytes.Length &&
                c.ProfileImageContentType == "image/png" &&
                c.ProfileImageFileName == "avatar.png"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdminGetAllUserProfiles_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        mediator.Send(Arg.Any<GetAllUserProfilesQuery>(), Arg.Any<CancellationToken>()).Returns(new GetAllUserProfilesResponse());
        var res = await controller.AdminGetAllUserProfiles(CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AdminGetByAuthId_Returns_NotFound_When_Null()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        mediator.Send(Arg.Any<GetUserProfileByAuthIdQuery>(), Arg.Any<CancellationToken>()).Returns((UserProfileDto?)null);
        var res = await controller.AdminGetByAuthId(Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task AdminGetByAuthId_Returns_Ok_When_Found()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        mediator.Send(Arg.Any<GetUserProfileByAuthIdQuery>(), Arg.Any<CancellationToken>()).Returns(new UserProfileDto());
        var res = await controller.AdminGetByAuthId(Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AdminGetUserContext_Returns_NotFound_When_Null()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        mediator.Send(Arg.Any<GetUserContextByAuthIdQuery>(), Arg.Any<CancellationToken>()).Returns((UserContextDto?)null);
        var res = await controller.AdminGetUserContext(Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task AdminGetUserContext_Returns_Ok_When_Found()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        mediator.Send(Arg.Any<GetUserContextByAuthIdQuery>(), Arg.Any<CancellationToken>()).Returns(new UserContextDto());
        var res = await controller.AdminGetUserContext(Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AdminGetFullInfo_Returns_NotFound_When_Null()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        mediator.Send(Arg.Any<GetFullUserInfoQuery>(), Arg.Any<CancellationToken>()).Returns((FullUserInfoResponse?)null);
        var res = await controller.AdminGetFullInfo(Guid.NewGuid());
        res.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task AdminGetFullInfo_Returns_Ok_When_Found()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        mediator.Send(Arg.Any<GetFullUserInfoQuery>(), Arg.Any<CancellationToken>()).Returns(new FullUserInfoResponse());
        var res = await controller.AdminGetFullInfo(Guid.NewGuid());
        res.Result.Should().BeOfType<OkObjectResult>();
    }
}
