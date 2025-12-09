using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.Roles.Commands.CreateRole;
using RogueLearn.User.Application.Features.Roles.Commands.DeleteRole;
using RogueLearn.User.Application.Features.Roles.Commands.UpdateRole;
using RogueLearn.User.Application.Features.Roles.Queries.GetAllRoles;

namespace RogueLearn.User.Api.Tests.Controllers;

public class RolesControllerTests
{
    [Fact]
    public async Task GetAllRoles_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        var expected = new GetAllRolesResponse { Roles = new List<RoleDto>() };
        mediator.Send(Arg.Any<GetAllRolesQuery>()).Returns(expected);

        var controller = new RolesController(mediator);
        var res = await controller.GetAllRoles();

        res.Result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)res.Result!;
        ok.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CreateRole_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        var expected = new CreateRoleResponse { Id = Guid.NewGuid(), Name = "Admin" };
        mediator.Send(Arg.Any<CreateRoleCommand>()).Returns(expected);

        var controller = new RolesController(mediator);
        var command = new CreateRoleCommand { Name = "Admin" };

        var res = await controller.CreateRole(command);

        res.Result.Should().BeOfType<CreatedAtActionResult>();
        var created = (CreatedAtActionResult)res.Result!;
        created.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task UpdateRole_Returns_Ok_And_Sets_Command_Id()
    {
        var mediator = Substitute.For<IMediator>();
        var expected = new UpdateRoleResponse { Id = Guid.NewGuid(), Name = "Admin" };
        mediator.Send(Arg.Any<UpdateRoleCommand>()).Returns(expected);

        var controller = new RolesController(mediator);
        var id = Guid.NewGuid();
        var command = new UpdateRoleCommand { Name = "Admin" };

        var res = await controller.UpdateRole(id, command);

        await mediator.Received(1).Send(Arg.Is<UpdateRoleCommand>(c => c.Id == id));
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteRole_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new RolesController(mediator);
        var id = Guid.NewGuid();

        var res = await controller.DeleteRole(id);

        await mediator.Received(1).Send(Arg.Is<DeleteRoleCommand>(c => c.Id == id));
        res.Should().BeOfType<NoContentResult>();
    }
}

