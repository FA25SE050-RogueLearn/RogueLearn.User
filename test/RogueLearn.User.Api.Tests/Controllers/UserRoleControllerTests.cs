using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.UserRoles.Commands.AssignRoleToUser;
using RogueLearn.User.Application.Features.UserRoles.Commands.RemoveRoleFromUser;
using RogueLearn.User.Application.Features.UserRoles.Queries.GetUserRoles;

namespace RogueLearn.User.Api.Tests.Controllers;

public class UserRoleControllerTests
{
    [Fact]
    public async Task AssignRoleToUser_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new UserRoleController(mediator);
        var cmd = new AssignRoleToUserCommand { AuthUserId = Guid.NewGuid(), RoleId = Guid.NewGuid() };

        var res = await controller.AssignRoleToUser(cmd);

        await mediator.Received(1).Send(Arg.Is<AssignRoleToUserCommand>(c => c.AuthUserId == cmd.AuthUserId && c.RoleId == cmd.RoleId));
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemoveRoleFromUser_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new UserRoleController(mediator);
        var cmd = new RemoveRoleFromUserCommand { AuthUserId = Guid.NewGuid(), RoleId = Guid.NewGuid() };

        var res = await controller.RemoveRoleFromUser(cmd);

        await mediator.Received(1).Send(Arg.Is<RemoveRoleFromUserCommand>(c => c.AuthUserId == cmd.AuthUserId && c.RoleId == cmd.RoleId));
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetUserRoles_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        var authUserId = Guid.NewGuid();
        var expected = new GetUserRolesResponse { UserId = Guid.NewGuid(), Roles = new List<UserRoleDto>() };
        mediator.Send(Arg.Is<GetUserRolesQuery>(q => q.AuthUserId == authUserId)).Returns(expected);

        var controller = new UserRoleController(mediator);
        var res = await controller.GetUserRoles(authUserId);

        res.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)res;
        ok.Value.Should().BeEquivalentTo(expected);
    }
}

