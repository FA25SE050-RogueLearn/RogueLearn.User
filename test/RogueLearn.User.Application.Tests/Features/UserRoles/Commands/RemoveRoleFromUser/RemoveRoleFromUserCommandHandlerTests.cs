using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.UserRoles.Commands.RemoveRoleFromUser;
using RogueLearn.User.Domain.Entities;
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
}