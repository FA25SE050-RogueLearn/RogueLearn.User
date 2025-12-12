using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.LecturerVerificationRequests;
using RogueLearn.User.Application.Features.LecturerVerificationRequests.Commands.ApproveLecturerVerificationRequest;
using RogueLearn.User.Application.Features.LecturerVerificationRequests.Commands.DeclineLecturerVerificationRequest;
using RogueLearn.User.Application.Features.LecturerVerificationRequests.Commands.CreateLecturerVerificationRequest;
using RogueLearn.User.Application.Features.LecturerVerificationRequests.Queries.AdminGetLecturerVerificationRequest;
using RogueLearn.User.Application.Features.LecturerVerificationRequests.Queries.AdminListLecturerVerificationRequests;
using RogueLearn.User.Application.Features.LecturerVerificationRequests.Queries.GetMyLecturerVerificationRequests;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Api.Tests.Controllers;

public class LecturerVerificationControllerTests
{
    private static LecturerVerificationController CreateController(IMediator mediator, ILecturerVerificationProofStorage storage, Guid userId)
    {
        var controller = new LecturerVerificationController(mediator, storage);
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
    public async Task CreateRequest_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        var storage = Substitute.For<ILecturerVerificationProofStorage>();
        var userId = Guid.NewGuid();
        mediator.Send(Arg.Any<CreateLecturerVerificationRequestCommand>(), Arg.Any<CancellationToken>())
                .Returns(new CreateLecturerVerificationRequestResponse());
        var controller = CreateController(mediator, storage, userId);
        var res = await controller.CreateRequest(new CreateLecturerVerificationRequestCommand(), CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMyRequests_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        var storage = Substitute.For<ILecturerVerificationProofStorage>();
        var controller = CreateController(mediator, storage, Guid.NewGuid());
        var res = await controller.GetMyRequests(CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AdminGet_Returns_NotFound_When_Null()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<AdminGetLecturerVerificationRequestQuery>(), Arg.Any<CancellationToken>()).Returns((AdminLecturerVerificationRequestDetail?)null);
        var controller = CreateController(mediator, Substitute.For<ILecturerVerificationProofStorage>(), Guid.NewGuid());
        var res = await controller.AdminGet(Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task AdminApprove_Returns_NoContent()
    {
        var controller = CreateController(Substitute.For<IMediator>(), Substitute.For<ILecturerVerificationProofStorage>(), Guid.NewGuid());
        var res = await controller.AdminApprove(Guid.NewGuid(), new ApproveLecturerVerificationRequestCommand(), CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task AdminDecline_Returns_NoContent()
    {
        var controller = CreateController(Substitute.For<IMediator>(), Substitute.For<ILecturerVerificationProofStorage>(), Guid.NewGuid());
        var res = await controller.AdminDecline(Guid.NewGuid(), new DeclineLecturerVerificationRequestCommand(), CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task CreateRequestForm_Returns_Ok_With_Screenshot()
    {
        var mediator = Substitute.For<IMediator>();
        var storage = Substitute.For<ILecturerVerificationProofStorage>();
        storage.UploadAsync(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(Task.FromResult("http://file"));
        mediator.Send(Arg.Any<CreateLecturerVerificationRequestCommand>(), Arg.Any<CancellationToken>())
                .Returns(new CreateLecturerVerificationRequestResponse());
        var controller = CreateController(mediator, storage, Guid.NewGuid());
        var file = new FormFile(new MemoryStream(new byte[]{1,2,3}), 0, 3, "s", "proof.png") { Headers = new HeaderDictionary(), ContentType = "image/png" };
        var form = new CreateLecturerVerificationFormData { Email = "a@b.com", StaffId = "123", Screenshot = file };
        var res = await controller.CreateRequestForm(form, CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AdminList_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<AdminListLecturerVerificationRequestsQuery>(), Arg.Any<CancellationToken>())
                .Returns(new AdminListLecturerVerificationRequestsResponse { Items = new(), Page = 1, Size = 20, Total = 0 });
        var controller = CreateController(mediator, Substitute.For<ILecturerVerificationProofStorage>(), Guid.NewGuid());
        var res = await controller.AdminList(null, null, 1, 20, CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();
    }
}
