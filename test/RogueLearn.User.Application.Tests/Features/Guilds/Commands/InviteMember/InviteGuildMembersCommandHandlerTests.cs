using FluentAssertions;
using Moq;
using RogueLearn.User.Application.Features.Guilds.Commands.InviteMember;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.InviteMember;

public class InviteGuildMembersCommandHandlerTests
{
    private readonly Mock<IGuildInvitationRepository> _invitationRepository = new();
    private readonly Mock<IUserProfileRepository> _userProfileRepository = new();

    private readonly InviteGuildMembersCommandHandler _handler;

    public InviteGuildMembersCommandHandlerTests()
    {
        _handler = new InviteGuildMembersCommandHandler(
            _invitationRepository.Object,
            _userProfileRepository.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenSelfInvite()
    {
        var guildId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cmd = new InviteGuildMembersCommand(
            guildId,
            userId,
            new[] { new InviteTarget(userId, null) },
            "Join"
        );

        _invitationRepository
            .Setup(r => r.GetPendingInvitationsByGuildAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GuildInvitation>());

        Func<Task> act = async () => await _handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>();
        _invitationRepository.Verify(r => r.AddAsync(It.IsAny<GuildInvitation>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}