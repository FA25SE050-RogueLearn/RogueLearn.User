using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.Meetings.Commands.UpsertMeeting;
using RogueLearn.User.Application.Features.Meetings.Commands.UpsertParticipants;
using RogueLearn.User.Application.Features.Meetings.Commands.ProcessArtifactsAndSummarize;
using RogueLearn.User.Application.Features.Meetings.Commands.CreateOrUpdateSummary;
using RogueLearn.User.Application.Features.Meetings.Queries.GetMeetingDetails;
using RogueLearn.User.Application.Features.Meetings.Queries.GetPartyMeetings;
using RogueLearn.User.Application.Features.Meetings.Queries.GetGuildMeetings;
using RogueLearn.User.Application.Features.Meetings.DTOs;

namespace RogueLearn.User.Api.Tests.Controllers;

public class MeetingsControllerTests
{
    private static MeetingsController CreateController(IMediator mediator, Guid? userId = null)
    {
        var controller = new MeetingsController(mediator);
        var identity = userId.HasValue
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }, "Test")
            : new ClaimsIdentity();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return controller;
    }

    [Fact]
    public async Task UpsertMeeting_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<UpsertMeetingCommand>(), Arg.Any<CancellationToken>())
                .Returns(new MeetingDto { MeetingId = Guid.NewGuid() });
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.UpsertMeeting(new MeetingDto { OrganizerId = Guid.Empty }, CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpsertMeeting_Returns_Unauthorized_When_No_Auth_And_Organizer_Empty()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, null);
        var res = await controller.UpsertMeeting(new MeetingDto { OrganizerId = Guid.Empty }, CancellationToken.None);
        res.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task UpsertParticipants_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<UpsertParticipantsCommand>(), Arg.Any<CancellationToken>())
                .Returns(new List<MeetingParticipantDto>());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.UpsertParticipants(Guid.NewGuid(), new List<MeetingParticipantDto>(), CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ProcessArtifactsAndSummarize_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.ProcessArtifactsAndSummarize(Guid.NewGuid(), new ProcessArtifactsRequest { AccessToken = "token", Artifacts = new List<ArtifactInputDto>() }, CancellationToken.None);
        res.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task CreateOrUpdateSummary_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.CreateOrUpdateSummary(Guid.NewGuid(), new CreateSummaryRequest { Content = "x" }, CancellationToken.None);
        res.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task GetMeetingDetails_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetMeetingDetailsQuery>(), Arg.Any<CancellationToken>())
                .Returns(new MeetingDetailsDto());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetMeetingDetails(Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPartyMeetings_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetPartyMeetingsQuery>(), Arg.Any<CancellationToken>())
                .Returns(new List<MeetingDto>());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetPartyMeetings(Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetGuildMeetings_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetGuildMeetingsQuery>(), Arg.Any<CancellationToken>())
                .Returns(new List<MeetingDto>());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetGuildMeetings(Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }
}
