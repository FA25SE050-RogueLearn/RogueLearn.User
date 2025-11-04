using System.Threading;
using System.Threading.Tasks;
using Moq;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Guilds.Commands.AcceptInvitation;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.AcceptInvitation;

public class AcceptGuildInvitationCommandHandlerTests
{
    [Fact]
    public async Task AcceptInvitation_Throws_WhenUserAlreadyMemberOfDifferentGuild()
    {
        var invitationRepo = new Mock<IGuildInvitationRepository>(MockBehavior.Strict);
        var memberRepo = new Mock<IGuildMemberRepository>(MockBehavior.Strict);
        var guildRepo = new Mock<IGuildRepository>(MockBehavior.Strict);

        var guildId = Guid.NewGuid();
        var otherGuildId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();

        guildRepo.Setup(r => r.GetByIdAsync(guildId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Guild { Id = guildId, MaxMembers = 50 });
        invitationRepo.Setup(r => r.GetByIdAsync(inviteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuildInvitation
            {
                Id = inviteId,
                GuildId = guildId,
                InviteeId = userId,
                Status = InvitationStatus.Pending,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
            });
        memberRepo.Setup(r => r.GetMembershipsByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new GuildMember { GuildId = otherGuildId, AuthUserId = userId, Status = MemberStatus.Active }
            });

        var handler = new AcceptGuildInvitationCommandHandler(invitationRepo.Object, memberRepo.Object, guildRepo.Object);
        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new AcceptGuildInvitationCommand(guildId, inviteId, userId), CancellationToken.None));
    }

    [Fact]
    public async Task AcceptInvitation_Throws_WhenUserIsCreatorOfAnotherGuild()
    {
        var invitationRepo = new Mock<IGuildInvitationRepository>(MockBehavior.Strict);
        var memberRepo = new Mock<IGuildMemberRepository>(MockBehavior.Strict);
        var guildRepo = new Mock<IGuildRepository>(MockBehavior.Strict);

        var guildId = Guid.NewGuid();
        var otherGuildId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();

        guildRepo.Setup(r => r.GetByIdAsync(guildId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Guild { Id = guildId, MaxMembers = 50 });
        invitationRepo.Setup(r => r.GetByIdAsync(inviteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuildInvitation
            {
                Id = inviteId,
                GuildId = guildId,
                InviteeId = userId,
                Status = InvitationStatus.Pending,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
            });
        memberRepo.Setup(r => r.GetMembershipsByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<GuildMember>());
        guildRepo.Setup(r => r.GetGuildsByCreatorAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Guild { Id = otherGuildId, CreatedBy = userId } });

        var handler = new AcceptGuildInvitationCommandHandler(invitationRepo.Object, memberRepo.Object, guildRepo.Object);
        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new AcceptGuildInvitationCommand(guildId, inviteId, userId), CancellationToken.None));
    }
}