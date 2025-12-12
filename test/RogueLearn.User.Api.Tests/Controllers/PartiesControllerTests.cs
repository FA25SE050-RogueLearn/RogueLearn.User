using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.Parties.Commands.CreateParty;
using RogueLearn.User.Application.Features.Parties.Commands.LeaveParty;
using RogueLearn.User.Application.Features.Parties.Commands.RemoveMember;
using RogueLearn.User.Application.Features.Parties.Commands.TransferLeadership;
using RogueLearn.User.Application.Features.Parties.Commands.ConfigureParty;
using RogueLearn.User.Application.Features.Parties.Commands.InviteMember;
using RogueLearn.User.Application.Features.Parties.Commands.DeleteParty;
using RogueLearn.User.Application.Features.Parties.Commands.JoinPublicParty;
using RogueLearn.User.Application.Features.Parties.Commands.DeclineInvitation;
using RogueLearn.User.Application.Features.Parties.Commands.UpdatePartyResource;
using RogueLearn.User.Application.Features.Parties.Commands.AddPartyResource;
using RogueLearn.User.Application.Features.Parties.Commands.DeletePartyResource;
using RogueLearn.User.Application.Features.Parties.Queries.GetPartyById;
using RogueLearn.User.Application.Features.Parties.Queries.GetPartyMembers;
using RogueLearn.User.Application.Features.Parties.Queries.GetPendingInvitations;
using RogueLearn.User.Application.Features.Parties.Queries.GetMyPendingInvitations;
using RogueLearn.User.Application.Features.Parties.Queries.GetMyParties;
using RogueLearn.User.Application.Features.Parties.Queries.GetAllParties;
using RogueLearn.User.Application.Features.Parties.Queries.GetPartyResources;
using RogueLearn.User.Application.Features.Parties.Queries.GetPartyResourceById;
using RogueLearn.User.Application.Features.Parties.DTOs;

namespace RogueLearn.User.Api.Tests.Controllers;

public class PartiesControllerTests
{
    private static PartiesController CreateController(IMediator mediator, Guid userId)
    {
        var controller = new PartiesController(mediator);
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
    public async Task CreateParty_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<CreatePartyCommand>(), Arg.Any<CancellationToken>())
                .Returns(new CreatePartyResponse { PartyId = Guid.NewGuid() });
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.CreateParty(new CreatePartyCommand(), CancellationToken.None);
        res.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task LeaveParty_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.LeaveParty(Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemovePartyMember_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.RemovePartyMember(Guid.NewGuid(), Guid.NewGuid(), new RemovePartyMemberCommand(Guid.NewGuid(), Guid.NewGuid(), null), CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task TransferPartyLeadership_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.TransferPartyLeadership(Guid.NewGuid(), new TransferPartyLeadershipCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task AcceptPartyInvitation_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.AcceptPartyInvitation(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeclinePartyInvitation_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.DeclinePartyInvitation(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task JoinPublicParty_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.JoinPublicParty(Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ConfigurePartySettings_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.ConfigurePartySettings(Guid.NewGuid(), new ConfigurePartySettingsCommand(Guid.NewGuid(), "name", "desc", "public", 5), CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteParty_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.DeleteParty(Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetPartyById_Returns_NotFound_When_Null()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetPartyByIdQuery>(), Arg.Any<CancellationToken>())!.Returns((PartyDto?)null);
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetPartyById(Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetMembers_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetPartyMembersQuery>(), Arg.Any<CancellationToken>())
                .Returns(new List<PartyMemberDto>());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetMembers(Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task InviteMember_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.InviteMember(
            Guid.NewGuid(),
            new InviteMemberRequest(new[] { new RogueLearn.User.Application.Features.Parties.Commands.InviteMember.InviteTarget(Guid.NewGuid(), null) }, null, null, null),
            CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task InviteMemberToGame_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.InviteMemberToGame(
            Guid.NewGuid(),
            new InviteMemberRequest(new[] { new RogueLearn.User.Application.Features.Parties.Commands.InviteMember.InviteTarget(Guid.NewGuid(), null) }, null, null, null),
            CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetPendingInvitations_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetPendingInvitationsQuery>(), Arg.Any<CancellationToken>())
                .Returns(new List<PartyInvitationDto>());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetPendingInvitations(Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMyPendingInvitations_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetMyPendingInvitationsQuery>(), Arg.Any<CancellationToken>())
                .Returns(new List<PartyInvitationDto>());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetMyPendingInvitations(CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AddResource_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<AddPartyResourceCommand>(), Arg.Any<CancellationToken>())
                .Returns(new PartyStashItemDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "t", "c", new List<string>(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.AddResource(Guid.NewGuid(), new AddPartyResourceRequest(Guid.Empty, "t", "c", new List<string>()), CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetResources_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetPartyResourcesQuery>(), Arg.Any<CancellationToken>())
                .Returns(new List<PartyStashItemDto>());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetResources(Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetResourceById_Returns_NotFound_When_Null()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetPartyResourceByIdQuery>(), Arg.Any<CancellationToken>())
                .Returns((PartyStashItemDto?)null);
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetResourceById(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateResource_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.UpdateResource(Guid.NewGuid(), Guid.NewGuid(), new UpdatePartyResourceRequest("t", "c", new List<string>()), CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteResource_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.DeleteResource(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetMyParties_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetMyPartiesQuery>(), Arg.Any<CancellationToken>())
                .Returns(new List<PartyDto>());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetMyParties(CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAllParties_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetAllPartiesQuery>(), Arg.Any<CancellationToken>())
                .Returns(new List<PartyDto>());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetAllParties(CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPartyMemberRoles_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<RogueLearn.User.Application.Features.Parties.Queries.GetMemberRoles.GetPartyMemberRolesQuery>(), Arg.Any<CancellationToken>())
                .Returns(new List<RogueLearn.User.Domain.Enums.PartyRole>());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetPartyMemberRoles(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();
    }
}
