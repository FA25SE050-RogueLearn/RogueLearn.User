using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.Achievements.Commands.CreateAchievement;
using RogueLearn.User.Application.Features.Achievements.Commands.UpdateAchievement;
using RogueLearn.User.Application.Features.Achievements.Queries.GetAllAchievements;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Api.Tests.Controllers;

public class AchievementsControllerTests
{
    private static AchievementsController CreateController(IMediator mediator, IAchievementImageStorage storage)
    {
        return new AchievementsController(mediator, storage);
    }

    [Fact]
    public async Task GetAll_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetAllAchievementsQuery>()).Returns(new GetAllAchievementsResponse());
        var controller = CreateController(mediator, Substitute.For<IAchievementImageStorage>());
        var res = await controller.GetAll();
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<CreateAchievementCommand>()).Returns(new CreateAchievementResponse { Id = Guid.NewGuid() });
        var controller = CreateController(mediator, Substitute.For<IAchievementImageStorage>());
        var res = await controller.Create(new CreateAchievementCommand());
        res.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateFromUpload_Attaches_Icon_And_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<CreateAchievementCommand>()).Returns(new CreateAchievementResponse { Id = Guid.NewGuid() });
        var storage = Substitute.For<IAchievementImageStorage>();
        storage.SaveIconAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>()).Returns("https://cdn/icon.png");
        var controller = CreateController(mediator, storage);
        var file = new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "icon", "icon.png") { Headers = new HeaderDictionary(), ContentType = "image/png" };
        var cmd = new CreateAchievementCommand { Key = "badge.test" };
        var res = await controller.CreateFromUpload(cmd, file);
        res.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Update_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<UpdateAchievementCommand>()).Returns(new UpdateAchievementResponse());
        var controller = CreateController(mediator, Substitute.For<IAchievementImageStorage>());
        var res = await controller.Update(Guid.NewGuid(), new UpdateAchievementCommand());
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateFromUpload_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<UpdateAchievementCommand>()).Returns(new UpdateAchievementResponse());
        var storage = Substitute.For<IAchievementImageStorage>();
        storage.SaveIconAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>()).Returns("https://cdn/icon.png");
        var controller = CreateController(mediator, storage);
        var file = new FormFile(new MemoryStream(new byte[] { 1, 2 }), 0, 2, "icon", "icon.png") { Headers = new HeaderDictionary(), ContentType = "image/png" };
        var cmd = new UpdateAchievementCommand { Key = "badge.test" };
        var res = await controller.UpdateFromUpload(Guid.NewGuid(), cmd, file);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Substitute.For<IAchievementImageStorage>());
        var res = await controller.Delete(Guid.NewGuid());
        res.Should().BeOfType<NoContentResult>();
    }
}

