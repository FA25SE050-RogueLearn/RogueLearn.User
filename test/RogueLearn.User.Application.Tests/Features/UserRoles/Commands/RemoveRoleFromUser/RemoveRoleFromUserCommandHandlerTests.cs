using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.UserRoles.Commands.RemoveRoleFromUser;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.UserRoles.Commands.RemoveRoleFromUser;

public class RemoveRoleFromUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_UserNotFound_Throws()
    {
        var urRepo = Substitute.For<IUserRoleRepository>();
        var upRepo = Substitute.For<IUserProfileRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var logger = Substitute.For<ILogger<RemoveRoleFromUserCommandHandler>>();
        var sut = new RemoveRoleFromUserCommandHandler(urRepo, upRepo, roleRepo, logger);

        var cmd = new RemoveRoleFromUserCommand { AuthUserId = Guid.NewGuid(), RoleId = Guid.NewGuid() };
        upRepo.GetByAuthIdAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RoleNotFound_Throws()
    {
        var urRepo = Substitute.For<IUserRoleRepository>();
        var upRepo = Substitute.For<IUserProfileRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var logger = Substitute.For<ILogger<RemoveRoleFromUserCommandHandler>>();
        var sut = new RemoveRoleFromUserCommandHandler(urRepo, upRepo, roleRepo, logger);

        var cmd = new RemoveRoleFromUserCommand { AuthUserId = Guid.NewGuid(), RoleId = Guid.NewGuid() };
        upRepo.GetByAuthIdAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Id = Guid.NewGuid(), AuthUserId = cmd.AuthUserId, Username = "u" });
        roleRepo.GetByIdAsync(cmd.RoleId).Returns((Role?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotAssigned_DoesNothing()
    {
        var urRepo = Substitute.For<IUserRoleRepository>();
        var upRepo = Substitute.For<IUserProfileRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var logger = Substitute.For<ILogger<RemoveRoleFromUserCommandHandler>>();
        var sut = new RemoveRoleFromUserCommandHandler(urRepo, upRepo, roleRepo, logger);

        var cmd = new RemoveRoleFromUserCommand { AuthUserId = Guid.NewGuid(), RoleId = Guid.NewGuid() };
        upRepo.GetByAuthIdAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Id = Guid.NewGuid(), AuthUserId = cmd.AuthUserId, Username = "u" });
        roleRepo.GetByIdAsync(cmd.RoleId).Returns(new Role { Id = cmd.RoleId, Name = "R" });
        urRepo.GetRolesForUserAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(new List<UserRole>());

        await sut.Handle(cmd, CancellationToken.None);
        await urRepo.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RemovesAssignedRole()
    {
        var urRepo = Substitute.For<IUserRoleRepository>();
        var upRepo = Substitute.For<IUserProfileRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var logger = Substitute.For<ILogger<RemoveRoleFromUserCommandHandler>>();
        var sut = new RemoveRoleFromUserCommandHandler(urRepo, upRepo, roleRepo, logger);

        var cmd = new RemoveRoleFromUserCommand { AuthUserId = Guid.NewGuid(), RoleId = Guid.NewGuid() };
        var assigned = new UserRole { Id = Guid.NewGuid(), AuthUserId = cmd.AuthUserId, RoleId = cmd.RoleId };
        upRepo.GetByAuthIdAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Id = Guid.NewGuid(), AuthUserId = cmd.AuthUserId, Username = "u" });
        roleRepo.GetByIdAsync(cmd.RoleId).Returns(new Role { Id = cmd.RoleId, Name = "R" });
        urRepo.GetRolesForUserAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(new List<UserRole> { assigned });

        await sut.Handle(cmd, CancellationToken.None);
        await urRepo.Received(1).DeleteAsync(assigned.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RemovingVerifiedLecturer_UpdatesMasterGuilds()
    {
        var urRepo = Substitute.For<IUserRoleRepository>();
        var upRepo = Substitute.For<IUserProfileRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var gmRepo = Substitute.For<IGuildMemberRepository>();
        var gRepo = Substitute.For<IGuildRepository>();
        var logger = Substitute.For<ILogger<RemoveRoleFromUserCommandHandler>>();
        var sut = new RemoveRoleFromUserCommandHandler(urRepo, upRepo, roleRepo, gmRepo, gRepo, logger);

        var authId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var cmd = new RemoveRoleFromUserCommand { AuthUserId = authId, RoleId = roleId };

        upRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, Username = "u" });
        roleRepo.GetByIdAsync(roleId).Returns(new Role { Id = roleId, Name = "Verified Lecturer" });

        var assigned = new UserRole { Id = Guid.NewGuid(), AuthUserId = authId, RoleId = roleId };
        urRepo.GetRolesForUserAsync(authId, Arg.Any<CancellationToken>()).Returns(new List<UserRole> { assigned });

        var guildId = Guid.NewGuid();
        gmRepo.GetMembershipsByUserAsync(authId, Arg.Any<CancellationToken>())
              .Returns(new List<GuildMember> { new GuildMember { GuildId = guildId, AuthUserId = authId, Role = GuildRole.GuildMaster, Status = MemberStatus.Active } });
        var guild = new Guild { Id = guildId, MaxMembers = 100, IsLecturerGuild = true };
        gRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(guild);
        gmRepo.CountActiveMembersAsync(guildId, Arg.Any<CancellationToken>()).Returns(5);

        await sut.Handle(cmd, CancellationToken.None);

        await urRepo.Received(1).DeleteAsync(assigned.Id, Arg.Any<CancellationToken>());
        await gRepo.Received(1).UpdateAsync(Arg.Is<Guild>(g => g.Id == guildId && g.IsLecturerGuild == false && g.MaxMembers == 50), Arg.Any<CancellationToken>());
    }
}
