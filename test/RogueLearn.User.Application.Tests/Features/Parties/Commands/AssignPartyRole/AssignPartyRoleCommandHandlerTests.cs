using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Parties.Commands.ManageRoles;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.AssignPartyRole;

public class AssignPartyRoleCommandHandlerTests
{
    [Fact]
    public async Task AssignPartyRole_Idempotent_ForLeader_ShouldNotUpdate()
    {
        var repo = new Mock<IPartyMemberRepository>(MockBehavior.Strict);
        var partyId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        repo.Setup(r => r.IsLeaderAsync(partyId, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.GetMemberAsync(partyId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PartyMember { PartyId = partyId, AuthUserId = memberId, Role = PartyRole.Member });

        var handler = new AssignPartyRoleCommandHandler(repo.Object);
        await handler.Handle(new AssignPartyRoleCommand(partyId, memberId, PartyRole.Member, actorId), CancellationToken.None);

        repo.Verify(r => r.UpdateAsync(It.IsAny<PartyMember>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AssignPartyRole_Throws_WhenActorNotLeader()
    {
        var repo = new Mock<IPartyMemberRepository>(MockBehavior.Strict);
        var partyId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        repo.Setup(r => r.IsLeaderAsync(partyId, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = new AssignPartyRoleCommandHandler(repo.Object);
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new AssignPartyRoleCommand(partyId, memberId, PartyRole.Member, actorId), CancellationToken.None));
    }

    [Fact]
    public async Task AssignPartyRole_Throws_WhenAssigningLeader()
    {
        var repo = new Mock<IPartyMemberRepository>(MockBehavior.Strict);
        var partyId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        repo.Setup(r => r.IsLeaderAsync(partyId, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.GetMemberAsync(partyId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PartyMember { PartyId = partyId, AuthUserId = memberId, Role = PartyRole.Member });

        var handler = new AssignPartyRoleCommandHandler(repo.Object);
        await Assert.ThrowsAsync<UnprocessableEntityException>(() => handler.Handle(new AssignPartyRoleCommand(partyId, memberId, PartyRole.Leader, actorId), CancellationToken.None));
    }
}