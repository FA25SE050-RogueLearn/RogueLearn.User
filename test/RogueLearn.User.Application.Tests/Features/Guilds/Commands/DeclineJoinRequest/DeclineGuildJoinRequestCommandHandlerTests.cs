using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Commands.DeclineJoinRequest;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.DeclineJoinRequest;

public class DeclineGuildJoinRequestCommandHandlerTests
{
    [Fact]
    public async Task Handle_GuildNotFound_Throws()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var reqRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var cmd = new DeclineGuildJoinRequestCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns((Guild?)null);

        var sut = new DeclineGuildJoinRequestCommandHandler(guildRepo, reqRepo, notificationService);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RequestNotFound_Throws()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var reqRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var cmd = new DeclineGuildJoinRequestCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId });
        reqRepo.GetByIdAsync(cmd.RequestId, Arg.Any<CancellationToken>()).Returns((GuildJoinRequest?)null);

        var sut = new DeclineGuildJoinRequestCommandHandler(guildRepo, reqRepo, notificationService);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RequestBelongsToDifferentGuild_Throws()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var reqRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var cmd = new DeclineGuildJoinRequestCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId });
        reqRepo.GetByIdAsync(cmd.RequestId, Arg.Any<CancellationToken>()).Returns(new GuildJoinRequest { Id = cmd.RequestId, GuildId = Guid.NewGuid() });

        var sut = new DeclineGuildJoinRequestCommandHandler(guildRepo, reqRepo, notificationService);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RequestExpiredOrProcessed_Throws()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var reqRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var cmd = new DeclineGuildJoinRequestCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId });
        var req = new GuildJoinRequest { Id = cmd.RequestId, GuildId = cmd.GuildId, Status = GuildJoinRequestStatus.Accepted, ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1) };
        reqRepo.GetByIdAsync(cmd.RequestId, Arg.Any<CancellationToken>()).Returns(req);

        var sut = new DeclineGuildJoinRequestCommandHandler(guildRepo, reqRepo, notificationService);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_UpdatesStatus()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var reqRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var cmd = new DeclineGuildJoinRequestCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId });
        var req = new GuildJoinRequest { Id = cmd.RequestId, GuildId = cmd.GuildId, Status = GuildJoinRequestStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };
        reqRepo.GetByIdAsync(cmd.RequestId, Arg.Any<CancellationToken>()).Returns(req);

        var sut = new DeclineGuildJoinRequestCommandHandler(guildRepo, reqRepo, notificationService);
        await sut.Handle(cmd, CancellationToken.None);
        await reqRepo.Received(1).UpdateAsync(Arg.Is<GuildJoinRequest>(r => r.Status == GuildJoinRequestStatus.Declined && r.RespondedAt.HasValue), Arg.Any<CancellationToken>());
    }
}