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

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.RevokePartyRole;

public class RevokePartyRoleCommandHandlerTests
{
    [Fact]
    public async Task RevokePartyRole_SetsBaselineMember()
    {
        var repo = new Mock<IPartyMemberRepository>(MockBehavior.Strict);
        var partyId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        repo.Setup(r => r.IsLeaderAsync(partyId, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.GetMemberAsync(partyId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PartyMember { PartyId = partyId, AuthUserId = memberId, Role = PartyRole.CoLeader });
        repo.Setup(r => r.UpdateAsync(It.IsAny<PartyMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyMember m, CancellationToken _) => m);

        var handler = new RevokePartyRoleCommandHandler(repo.Object);
        await handler.Handle(new RevokePartyRoleCommand(partyId, memberId, PartyRole.CoLeader, actorId), CancellationToken.None);

        repo.Verify(r => r.UpdateAsync(It.Is<PartyMember>(m => m.Role == PartyRole.Member), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokePartyRole_Throws_WhenRevokingMemberBaseline()
    {
        var repo = new Mock<IPartyMemberRepository>(MockBehavior.Strict);
        var partyId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        repo.Setup(r => r.IsLeaderAsync(partyId, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.GetMemberAsync(partyId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PartyMember { PartyId = partyId, AuthUserId = memberId, Role = PartyRole.Member });

        var handler = new RevokePartyRoleCommandHandler(repo.Object);
        await Assert.ThrowsAsync<UnprocessableEntityException>(() => handler.Handle(new RevokePartyRoleCommand(partyId, memberId, PartyRole.Member, actorId), CancellationToken.None));
    }

    [Fact]
    public async Task RevokePartyRole_NoOp_WhenMemberDoesNotHaveRole()
    {
        var repo = new Mock<IPartyMemberRepository>(MockBehavior.Strict);
        var partyId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        repo.Setup(r => r.IsLeaderAsync(partyId, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.GetMemberAsync(partyId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PartyMember { PartyId = partyId, AuthUserId = memberId, Role = PartyRole.Member });

        var handler = new RevokePartyRoleCommandHandler(repo.Object);
        await handler.Handle(new RevokePartyRoleCommand(partyId, memberId, PartyRole.CoLeader, actorId), CancellationToken.None);

        repo.Verify(r => r.UpdateAsync(It.IsAny<PartyMember>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}