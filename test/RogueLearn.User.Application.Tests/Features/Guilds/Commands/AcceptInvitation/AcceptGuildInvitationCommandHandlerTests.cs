using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Commands.AcceptInvitation;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.AcceptInvitation;

public class AcceptGuildInvitationCommandHandlerTests
{
    [Fact]
    public async Task Handle_Success_AddsMemberAndUpdatesInvitation()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var joinReqRepo = Substitute.For<IGuildJoinRequestRepository>();
        var sut = new AcceptGuildInvitationCommandHandler(invRepo, memberRepo, guildRepo, notificationService, joinReqRepo);

        var cmd = new AcceptGuildInvitationCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var guild = new Guild { Id = cmd.GuildId, MaxMembers = 50, CurrentMemberCount = 0 };
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(guild);

        var inv = new GuildInvitation { Id = cmd.InvitationId, GuildId = cmd.GuildId, InviteeId = cmd.AuthUserId, Status = InvitationStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };
        invRepo.GetByIdAsync(cmd.InvitationId, Arg.Any<CancellationToken>()).Returns(inv);

        memberRepo.GetMemberAsync(cmd.GuildId, cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        memberRepo.CountActiveMembersAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(1);
        memberRepo.GetMembersByGuildAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { new() { AuthUserId = cmd.AuthUserId, Status = MemberStatus.Active } });

        await sut.Handle(cmd, CancellationToken.None);

        await memberRepo.Received(1).AddAsync(Arg.Any<GuildMember>(), Arg.Any<CancellationToken>());
        await invRepo.Received(1).UpdateAsync(Arg.Is<GuildInvitation>(i => i.Status == InvitationStatus.Accepted), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExpiredInvitation_Throws()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var joinReqRepo = Substitute.For<IGuildJoinRequestRepository>();
        var sut = new AcceptGuildInvitationCommandHandler(invRepo, memberRepo, guildRepo, notificationService, joinReqRepo);

        var cmd = new AcceptGuildInvitationCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var guild = new Guild { Id = cmd.GuildId, MaxMembers = 50, CurrentMemberCount = 0 };
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(guild);
        var inv = new GuildInvitation { Id = cmd.InvitationId, GuildId = cmd.GuildId, InviteeId = cmd.AuthUserId, Status = InvitationStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1) };
        invRepo.GetByIdAsync(cmd.InvitationId, Arg.Any<CancellationToken>()).Returns(inv);

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(act);
    }

    [Fact]
    public async Task Handle_CapacityFull_Throws()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var joinReqRepo = Substitute.For<IGuildJoinRequestRepository>();
        var sut = new AcceptGuildInvitationCommandHandler(invRepo, memberRepo, guildRepo, notificationService, joinReqRepo);

        var cmd = new AcceptGuildInvitationCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var guild = new Guild { Id = cmd.GuildId, MaxMembers = 1, CurrentMemberCount = 1 };
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(guild);
        var inv = new GuildInvitation { Id = cmd.InvitationId, GuildId = cmd.GuildId, InviteeId = cmd.AuthUserId, Status = InvitationStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };
        invRepo.GetByIdAsync(cmd.InvitationId, Arg.Any<CancellationToken>()).Returns(inv);

        memberRepo.CountActiveMembersAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(1);
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(act);
    }

    [Fact]
    public async Task Handle_WrongGuildOnInvitation_Throws()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var joinReqRepo = Substitute.For<IGuildJoinRequestRepository>();
        var sut = new AcceptGuildInvitationCommandHandler(invRepo, memberRepo, guildRepo, notificationService, joinReqRepo);

        var cmd = new AcceptGuildInvitationCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var guild = new Guild { Id = cmd.GuildId, MaxMembers = 50, CurrentMemberCount = 0 };
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(guild);

        var inv = new GuildInvitation { Id = cmd.InvitationId, GuildId = Guid.NewGuid(), InviteeId = cmd.AuthUserId, Status = InvitationStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };
        invRepo.GetByIdAsync(cmd.InvitationId, Arg.Any<CancellationToken>()).Returns(inv);

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(act);
    }

    [Fact]
    public async Task Handle_InviteeMismatch_ThrowsForbidden()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var joinReqRepo = Substitute.For<IGuildJoinRequestRepository>();
        var sut = new AcceptGuildInvitationCommandHandler(invRepo, memberRepo, guildRepo, notificationService, joinReqRepo);

        var cmd = new AcceptGuildInvitationCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var guild = new Guild { Id = cmd.GuildId, MaxMembers = 50, CurrentMemberCount = 0 };
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(guild);
        var inv = new GuildInvitation { Id = cmd.InvitationId, GuildId = cmd.GuildId, InviteeId = Guid.NewGuid(), Status = InvitationStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };
        invRepo.GetByIdAsync(cmd.InvitationId, Arg.Any<CancellationToken>()).Returns(inv);

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.ForbiddenException>(act);
    }

    [Fact]
    public async Task Handle_ExistingMember_DoesNotAddAgain_StillAcceptsInvitation()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var joinReqRepo = Substitute.For<IGuildJoinRequestRepository>();
        var sut = new AcceptGuildInvitationCommandHandler(invRepo, memberRepo, guildRepo, notificationService, joinReqRepo);

        var cmd = new AcceptGuildInvitationCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var guild = new Guild { Id = cmd.GuildId, MaxMembers = 50, CurrentMemberCount = 0 };
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(guild);
        var inv = new GuildInvitation { Id = cmd.InvitationId, GuildId = cmd.GuildId, InviteeId = cmd.AuthUserId, Status = InvitationStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };
        invRepo.GetByIdAsync(cmd.InvitationId, Arg.Any<CancellationToken>()).Returns(inv);

        memberRepo.GetMembershipsByUserAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember>());
        memberRepo.CountActiveMembersAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(0);
        memberRepo.GetMemberAsync(cmd.GuildId, cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(new GuildMember { GuildId = cmd.GuildId, AuthUserId = cmd.AuthUserId, Status = MemberStatus.Active });

        await sut.Handle(cmd, CancellationToken.None);
        await memberRepo.DidNotReceive().AddAsync(Arg.Any<GuildMember>(), Arg.Any<CancellationToken>());
        await invRepo.Received(1).UpdateAsync(Arg.Is<GuildInvitation>(i => i.Status == InvitationStatus.Accepted), Arg.Any<CancellationToken>());
    }
}
