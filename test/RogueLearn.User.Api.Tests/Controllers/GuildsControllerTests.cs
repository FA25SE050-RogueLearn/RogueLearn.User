using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.Guilds.Commands.ApplyJoinRequest;
using RogueLearn.User.Application.Features.Guilds.Commands.ApproveJoinRequest;
using RogueLearn.User.Application.Features.Guilds.Commands.ConfigureGuild;
using RogueLearn.User.Application.Features.Guilds.Commands.RemoveMember;
using RogueLearn.User.Application.Features.Guilds.Commands.CreateGuild;
using RogueLearn.User.Application.Features.Guilds.Commands.DeleteGuild;
using RogueLearn.User.Application.Features.Guilds.Commands.LeaveGuild;
using RogueLearn.User.Application.Features.Guilds.Commands.AcceptInvitation;
using RogueLearn.User.Application.Features.Guilds.Commands.DeclineInvitation;
using RogueLearn.User.Application.Features.Guilds.Commands.DeclineJoinRequest;
using RogueLearn.User.Application.Features.Guilds.Commands.InviteMember;
using RogueLearn.User.Application.Features.Guilds.Commands.TransferLeadership;
using RogueLearn.User.Application.Features.Guilds.Queries.GetGuildById;
using RogueLearn.User.Application.Features.Guilds.Queries.GetGuildDashboard;
using RogueLearn.User.Application.Features.Guilds.Queries.GetGuildInvitations;
using RogueLearn.User.Application.Features.Guilds.Queries.GetGuildJoinRequests;
using RogueLearn.User.Application.Features.Guilds.Queries.GetGuildMembers;
using RogueLearn.User.Application.Features.Guilds.Queries.GetMemberRoles;
using RogueLearn.User.Application.Features.Guilds.Queries.GetMyGuild;
using RogueLearn.User.Application.Features.Guilds.Queries.GetMyJoinRequests;
using RogueLearn.User.Application.Features.Guilds.Queries.GetMyGuildInvitations;
using RogueLearn.User.Application.Features.Guilds.Queries.GetAllGuilds;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Api.Tests.Controllers;

public class GuildsControllerTests
{
    private static GuildsController CreateController(IMediator mediator, Guid userId)
    {
        var controller = new GuildsController(mediator);
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
    public async Task GetGuildById_Returns_Ok_And_NotFound()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        mediator.Send(Arg.Any<GetGuildByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new GuildDto { Id = Guid.NewGuid(), Name = "g" });
        (await controller.GetGuildById(Guid.NewGuid())).Should().BeOfType<OkObjectResult>();
        mediator.Send(Arg.Any<GetGuildByIdQuery>(), Arg.Any<CancellationToken>())!
            .Returns((GuildDto?)null);
        (await controller.GetGuildById(Guid.NewGuid())).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetGuildMembers_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetGuildMembersQuery>(), Arg.Any<CancellationToken>()).Returns(new List<GuildMemberDto>());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetGuildMembers(Guid.NewGuid());
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetGuildInvitations_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetGuildInvitationsQuery>(), Arg.Any<CancellationToken>()).Returns(new List<GuildInvitationDto>());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetGuildInvitations(Guid.NewGuid());
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetGuildDashboard_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetGuildDashboardQuery>(), Arg.Any<CancellationToken>()).Returns(new GuildDashboardDto());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetGuildDashboard(Guid.NewGuid());
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMyGuild_Returns_NoContent_And_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        var userId = Guid.NewGuid();
        var controller = CreateController(mediator, userId);
        mediator.Send(Arg.Is<GetMyGuildQuery>(q => q.AuthUserId == userId), Arg.Any<CancellationToken>())
            .Returns((GuildDto?)null);
        (await controller.GetMyGuild()).Should().BeOfType<NoContentResult>();
        mediator.Send(Arg.Is<GetMyGuildQuery>(q => q.AuthUserId == userId), Arg.Any<CancellationToken>())
            .Returns(new GuildDto { Id = Guid.NewGuid(), Name = "x" });
        (await controller.GetMyGuild()).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ConfigureGuildSettings_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<ConfigureGuildSettingsCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Unit.Value));
        var controller = CreateController(mediator, Guid.NewGuid());
        var cmd = new ConfigureGuildSettingsCommand(Guid.NewGuid(), Guid.NewGuid(), "name", "desc", "public", 10);
        var res = await controller.ConfigureGuildSettings(Guid.NewGuid(), cmd);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ApplyJoinRequest_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<ApplyGuildJoinRequestCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Unit.Value));
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.ApplyJoinRequest(Guid.NewGuid(), new ApplyGuildJoinRequestRequest("hi"));
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetGuildJoinRequests_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetGuildJoinRequestsQuery>(), Arg.Any<CancellationToken>()).Returns(new List<GuildJoinRequestDto>());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetGuildJoinRequests(Guid.NewGuid(), true);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ApproveJoinRequest_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<ApproveGuildJoinRequestCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Unit.Value));
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.ApproveJoinRequest(Guid.NewGuid(), Guid.NewGuid());
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetMyJoinRequests_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        var userId = Guid.NewGuid();
        mediator.Send(Arg.Is<GetMyJoinRequestsQuery>(q => q.AuthUserId == userId), Arg.Any<CancellationToken>()).Returns(new List<GuildJoinRequestDto>());
        var controller = CreateController(mediator, userId);
        var res = await controller.GetMyJoinRequests(true);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RemoveMember_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<RemoveGuildMemberCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Unit.Value));
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.RemoveMember(Guid.NewGuid(), Guid.NewGuid(), new RemoveGuildMemberCommand(Guid.Empty, Guid.Empty, null));
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteGuild_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<DeleteGuildCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Unit.Value));
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.DeleteGuild(Guid.NewGuid());
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetGuildMemberRoles_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetGuildMemberRolesQuery>()).Returns(new List<GuildRole> { GuildRole.Member });
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetGuildMemberRoles(Guid.NewGuid(), Guid.NewGuid());
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateGuild_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<CreateGuildCommand>()).Returns(new CreateGuildResponse { GuildId = Guid.NewGuid() });
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.CreateGuild(new CreateGuildCommand());
        res.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task GetAllGuilds_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetAllGuildsQuery>(), Arg.Any<CancellationToken>()).Returns(new List<GuildDto>());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetAllGuilds(CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAllGuildsFull_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetAllGuildsFullQuery>(), Arg.Any<CancellationToken>()).Returns(new List<GuildFullDto>());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetAllGuildsFull(CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task InviteGuildMembers_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<InviteGuildMembersCommand>(), Arg.Any<CancellationToken>()).Returns(new InviteGuildMembersResponse(new List<Guid>{ Guid.NewGuid() }));
        var controller = CreateController(mediator, Guid.NewGuid());
        var req = new InviteGuildMembersRequest(new List<InviteTarget> { new InviteTarget(null, "u1@example.com") }, "hello");
        var res = await controller.InviteGuildMembers(Guid.NewGuid(), req, CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AcceptInvitation_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<AcceptGuildInvitationCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Unit.Value));
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.AcceptInvitation(Guid.NewGuid(), Guid.NewGuid());
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeclineInvitation_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<DeclineGuildInvitationCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Unit.Value));
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.DeclineInvitation(Guid.NewGuid(), Guid.NewGuid());
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeclineJoinRequest_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<DeclineGuildJoinRequestCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Unit.Value));
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.DeclineJoinRequest(Guid.NewGuid(), Guid.NewGuid());
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetMyGuildInvitations_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        var userId = Guid.NewGuid();
        mediator.Send(Arg.Is<GetMyGuildInvitationsQuery>(q => q.AuthUserId == userId), Arg.Any<CancellationToken>()).Returns(new List<GuildInvitationDto>());
        var controller = CreateController(mediator, userId);
        var res = await controller.GetMyGuildInvitations(true);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task TransferLeadership_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<TransferGuildLeadershipCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Unit.Value));
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.TransferLeadership(Guid.NewGuid(), new TransferGuildLeadershipCommand(Guid.Empty, Guid.Empty));
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task LeaveGuild_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<LeaveGuildCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Unit.Value));
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.LeaveGuild(Guid.NewGuid());
        res.Should().BeOfType<NoContentResult>();
    }
}
