using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Guilds.Commands.InviteMember;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.InviteMember;

public class InviteGuildMembersCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_SelfInvite_Throws(InviteGuildMembersCommand cmd)
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new InviteGuildMembersCommandHandler(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo);

        invRepo.GetPendingInvitationsByGuildAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation>());
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, MaxMembers = 50 });
        var target = new InviteTarget(cmd.InviterAuthUserId, null);
        cmd = new InviteGuildMembersCommand(cmd.GuildId, cmd.InviterAuthUserId, new[] { target }, "msg");
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_EmailResolvesAndCreates(InviteGuildMembersCommand cmd)
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new InviteGuildMembersCommandHandler(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo);

        invRepo.GetPendingInvitationsByGuildAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation>());
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, MaxMembers = 50 });
        var invitee = new UserProfile { Id = Guid.NewGuid(), AuthUserId = Guid.NewGuid(), Email = "target@example.com" };
        userRepo.GetByEmailAsync(invitee.Email, Arg.Any<CancellationToken>()).Returns(invitee);
        invRepo.AddAsync(Arg.Any<GuildInvitation>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<GuildInvitation>());

        var target = new InviteTarget(null, invitee.Email);
        cmd = new InviteGuildMembersCommand(cmd.GuildId, cmd.InviterAuthUserId, new[] { target }, "msg");
        var resp = await sut.Handle(cmd, CancellationToken.None);
        Assert.True(resp.InvitationIds.Count >= 1);
    }
}