using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.SkillDependencies.Commands.AddSkillDependency;
using RogueLearn.User.Application.Features.SkillDependencies.Commands.RemoveSkillDependency;
using RogueLearn.User.Application.Features.SkillDependencies.Queries.GetSkillDependencies;
using RogueLearn.User.Application.Features.Skills.Commands.CreateSkill;
using RogueLearn.User.Application.Features.Skills.Commands.UpdateSkill;
using RogueLearn.User.Application.Features.Skills.Queries.GetSkillById;
using RogueLearn.User.Application.Features.Skills.Queries.GetSkills;
using RogueLearn.User.Application.Features.Skills.Commands.DeleteSkill;

namespace RogueLearn.User.Api.Tests.Controllers;

public class AdminSkillsControllerTests
{
    [Fact]
    public async Task GetAll_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new AdminSkillsController(mediator);
        var res = await controller.GetAll();
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<CreateSkillCommand>()).Returns(new CreateSkillResponse { Id = Guid.NewGuid() });
        var controller = new AdminSkillsController(mediator);
        var res = await controller.Create(new CreateSkillCommand());
        res.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Update_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<UpdateSkillCommand>()).Returns(new UpdateSkillResponse());
        var controller = new AdminSkillsController(mediator);
        var res = await controller.Update(Guid.NewGuid(), new UpdateSkillCommand());
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RemoveDependency_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new AdminSkillsController(mediator);
        var res = await controller.RemoveDependency(Guid.NewGuid(), Guid.NewGuid());
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetDependencies_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetSkillDependenciesQuery>()).Returns(new GetSkillDependenciesResponse());
        var controller = new AdminSkillsController(mediator);
        var res = await controller.GetDependencies(Guid.NewGuid());
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetSkillByIdQuery>()).Returns(new GetSkillByIdResponse());
        var controller = new AdminSkillsController(mediator);
        var res = await controller.GetById(Guid.NewGuid());
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<DeleteSkillCommand>()).Returns(Task.FromResult(Unit.Value));
        var controller = new AdminSkillsController(mediator);
        var res = await controller.Delete(Guid.NewGuid());
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task AddDependency_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<AddSkillDependencyCommand>()).Returns(new AddSkillDependencyResponse());
        var controller = new AdminSkillsController(mediator);
        var res = await controller.AddDependency(Guid.NewGuid(), new AddSkillDependencyCommand());
        res.Result.Should().BeOfType<CreatedAtActionResult>();
    }
}
