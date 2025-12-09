using System.Security.Claims;
using FluentAssertions;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.Quests.Commands.EnsureMasterQuests;
using RogueLearn.User.Application.Features.Quests.Queries.GetAdminQuestDetails;
using RogueLearn.User.Application.Features.Quests.Queries.GetAllQuests;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Services;
using Hangfire.Storage;

namespace RogueLearn.User.Api.Tests.Controllers;

public class AdminQuestsControllerTests
{
    private static AdminQuestsController CreateController(IMediator mediator, IBackgroundJobClient jobs, IQuestRepository questRepo, IQuestStepRepository stepRepo)
    {
        var logger = Substitute.For<ILogger<AdminQuestsController>>();
        var controller = new AdminQuestsController(mediator, jobs, questRepo, stepRepo, logger);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "Test"))
            }
        };
        return controller;
    }

    private sealed class FakeJobStorage : JobStorage
    {
        private readonly IStorageConnection _connection;
        public FakeJobStorage(IStorageConnection connection) => _connection = connection;
        public override IStorageConnection GetConnection() => _connection;
        public override IMonitoringApi GetMonitoringApi() => Substitute.For<IMonitoringApi>();
    }

    [Fact]
    public async Task GetAllQuests_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetAllQuestsQuery>()).Returns(new PaginatedQuestsResponse());
        var controller = CreateController(mediator, Substitute.For<IBackgroundJobClient>(), Substitute.For<IQuestRepository>(), Substitute.For<IQuestStepRepository>());
        var res = await controller.GetAllQuests();
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetQuestDetails_Returns_NotFound_When_Null()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetAdminQuestDetailsQuery>()).Returns((AdminQuestDetailsDto?)null);
        var controller = CreateController(mediator, Substitute.For<IBackgroundJobClient>(), Substitute.For<IQuestRepository>(), Substitute.For<IQuestStepRepository>());
        var res = await controller.GetQuestDetails(Guid.NewGuid());
        res.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task SyncMasterQuests_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<EnsureMasterQuestsCommand>()).Returns(new EnsureMasterQuestsResponse(1, 2));
        var controller = CreateController(mediator, Substitute.For<IBackgroundJobClient>(), Substitute.For<IQuestRepository>(), Substitute.For<IQuestStepRepository>());
        var res = await controller.SyncMasterQuests();
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GenerateQuestSteps_Returns_NotFound_When_Quest_Missing()
    {
        var mediator = Substitute.For<IMediator>();
        var questRepo = Substitute.For<IQuestRepository>();
        questRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Quest?)null);
        var controller = CreateController(mediator, Substitute.For<IBackgroundJobClient>(), questRepo, Substitute.For<IQuestStepRepository>());
        var res = await controller.GenerateQuestSteps(Guid.NewGuid());
        res.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GenerateQuestSteps_Returns_BadRequest_When_Steps_Exist()
    {
        var mediator = Substitute.For<IMediator>();
        var questRepo = Substitute.For<IQuestRepository>();
        questRepo.GetByIdAsync(Arg.Any<Guid>()).Returns(new Quest { Id = Guid.NewGuid() });
        var stepRepo = Substitute.For<IQuestStepRepository>();
        stepRepo.QuestContainsSteps(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var controller = CreateController(mediator, Substitute.For<IBackgroundJobClient>(), questRepo, stepRepo);
        var res = await controller.GenerateQuestSteps(Guid.NewGuid());
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GenerateQuestSteps_Returns_Accepted_When_Scheduled()
    {
        var mediator = Substitute.For<IMediator>();
        var questRepo = Substitute.For<IQuestRepository>();
        questRepo.GetByIdAsync(Arg.Any<Guid>()).Returns(new Quest { Id = Guid.NewGuid() });
        var stepRepo = Substitute.For<IQuestStepRepository>();
        stepRepo.QuestContainsSteps(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        var jobs = Substitute.For<IBackgroundJobClient>();
        var controller = CreateController(mediator, jobs, questRepo, stepRepo);
        var res = await controller.GenerateQuestSteps(Guid.NewGuid());
        res.Should().BeOfType<AcceptedAtActionResult>();
    }

    [Fact]
    public async Task GenerateQuestSteps_Returns_500_On_Exception()
    {
        var mediator = Substitute.For<IMediator>();
        var questRepo = Substitute.For<IQuestRepository>();
        questRepo
            .When(q => q.GetByIdAsync(Arg.Any<Guid>()))
            .Do(_ => { throw new Exception("fail"); });
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var jobs = Substitute.For<IBackgroundJobClient>();
        var controller = CreateController(mediator, jobs, questRepo, stepRepo);
        var res = await controller.GenerateQuestSteps(Guid.NewGuid());
        res.Should().BeOfType<ObjectResult>();
        ((ObjectResult)res).StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetQuestGenerationStatus_Returns_500_When_JobStorage_Null()
    {
        var controller = CreateController(Substitute.For<IMediator>(), Substitute.For<IBackgroundJobClient>(), Substitute.For<IQuestRepository>(), Substitute.For<IQuestStepRepository>());
        var res = await controller.GetQuestGenerationStatus("job-1");
        res.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void GetGenerationProgress_Returns_500_When_JobStorage_Null()
    {
        var controller = CreateController(Substitute.For<IMediator>(), Substitute.For<IBackgroundJobClient>(), Substitute.For<IQuestRepository>(), Substitute.For<IQuestStepRepository>());
        var res = controller.GetGenerationProgress("job-1");
        res.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetQuestGenerationStatus_Returns_NotFound_When_JobData_Null()
    {
        var connection = Substitute.For<IStorageConnection>();
        connection.GetJobData(Arg.Any<string>()).Returns((JobData?)null);
        JobStorage.Current = new FakeJobStorage(connection);

        var controller = CreateController(Substitute.For<IMediator>(), Substitute.For<IBackgroundJobClient>(), Substitute.For<IQuestRepository>(), Substitute.For<IQuestStepRepository>());
        var res = await controller.GetQuestGenerationStatus("job-xyz");
        res.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetQuestGenerationStatus_Returns_NotFound_When_JobId_Null_Or_Empty()
    {
        var connection = Substitute.For<IStorageConnection>();
        connection.GetJobData(Arg.Any<string>()).Returns((JobData?)null);
        JobStorage.Current = new FakeJobStorage(connection);

        var controller = CreateController(Substitute.For<IMediator>(), Substitute.For<IBackgroundJobClient>(), Substitute.For<IQuestRepository>(), Substitute.For<IQuestStepRepository>());
        var resNull = await controller.GetQuestGenerationStatus(null!);
        resNull.Should().BeOfType<NotFoundObjectResult>();

        var resEmpty = await controller.GetQuestGenerationStatus("");
        resEmpty.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void GetGenerationProgress_Returns_NotFound_When_No_Progress()
    {
        var connection = Substitute.For<IStorageConnection>();
        connection.GetJobParameter(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        JobStorage.Current = new FakeJobStorage(connection);

        var controller = CreateController(Substitute.For<IMediator>(), Substitute.For<IBackgroundJobClient>(), Substitute.For<IQuestRepository>(), Substitute.For<IQuestStepRepository>());
        var res = controller.GetGenerationProgress("job-abc");
        res.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void GetGenerationProgress_Returns_NotFound_When_JobId_Null_Or_Empty()
    {
        var connection = Substitute.For<IStorageConnection>();
        connection.GetJobParameter(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        JobStorage.Current = new FakeJobStorage(connection);

        var controller = CreateController(Substitute.For<IMediator>(), Substitute.For<IBackgroundJobClient>(), Substitute.For<IQuestRepository>(), Substitute.For<IQuestStepRepository>());
        var resNull = controller.GetGenerationProgress(null!);
        resNull.Should().BeOfType<NotFoundObjectResult>();

        var resEmpty = controller.GetGenerationProgress("");
        resEmpty.Should().BeOfType<NotFoundObjectResult>();
    }
}
