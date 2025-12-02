using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.UserRoles.Commands.AssignRoleToUser;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.UserRoles.Commands.AssignRoleToUser;

public class AssignRoleToUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_UserNotFound_Throws()
    {
        var urRepo = Substitute.For<IUserRoleRepository>();
        var upRepo = Substitute.For<IUserProfileRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var logger = Substitute.For<ILogger<AssignRoleToUserCommandHandler>>();
        var sut = new AssignRoleToUserCommandHandler(urRepo, upRepo, roleRepo, logger);

        var cmd = new AssignRoleToUserCommand { AuthUserId = Guid.NewGuid(), RoleId = Guid.NewGuid() };
        upRepo.GetByAuthIdAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RoleNotFound_Throws()
    {
        var urRepo = Substitute.For<IUserRoleRepository>();
        var upRepo = Substitute.For<IUserProfileRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var logger = Substitute.For<ILogger<AssignRoleToUserCommandHandler>>();
        var sut = new AssignRoleToUserCommandHandler(urRepo, upRepo, roleRepo, logger);

        var cmd = new AssignRoleToUserCommand { AuthUserId = Guid.NewGuid(), RoleId = Guid.NewGuid() };
        upRepo.GetByAuthIdAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Id = Guid.NewGuid(), AuthUserId = cmd.AuthUserId, Username = "u" });
        roleRepo.GetByIdAsync(cmd.RoleId, Arg.Any<CancellationToken>()).Returns((Role?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlreadyHasRole_ThrowsBadRequest()
    {
        var urRepo = Substitute.For<IUserRoleRepository>();
        var upRepo = Substitute.For<IUserProfileRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var logger = Substitute.For<ILogger<AssignRoleToUserCommandHandler>>();
        var sut = new AssignRoleToUserCommandHandler(urRepo, upRepo, roleRepo, logger);

        var cmd = new AssignRoleToUserCommand { AuthUserId = Guid.NewGuid(), RoleId = Guid.NewGuid() };
        upRepo.GetByAuthIdAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Id = Guid.NewGuid(), AuthUserId = cmd.AuthUserId, Username = "u" });
        roleRepo.GetByIdAsync(cmd.RoleId, Arg.Any<CancellationToken>()).Returns(new Role { Id = cmd.RoleId, Name = "R" });
        urRepo.GetRolesForUserAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new() { AuthUserId = cmd.AuthUserId, RoleId = cmd.RoleId } });

        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AssignsRole()
    {
        var urRepo = Substitute.For<IUserRoleRepository>();
        var upRepo = Substitute.For<IUserProfileRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var logger = Substitute.For<ILogger<AssignRoleToUserCommandHandler>>();
        var sut = new AssignRoleToUserCommandHandler(urRepo, upRepo, roleRepo, logger);

        var cmd = new AssignRoleToUserCommand { AuthUserId = Guid.NewGuid(), RoleId = Guid.NewGuid() };
        upRepo.GetByAuthIdAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Id = Guid.NewGuid(), AuthUserId = cmd.AuthUserId, Username = "u" });
        roleRepo.GetByIdAsync(cmd.RoleId, Arg.Any<CancellationToken>()).Returns(new Role { Id = cmd.RoleId, Name = "R" });
        urRepo.GetRolesForUserAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(new List<UserRole>());

        await sut.Handle(cmd, CancellationToken.None);
        await urRepo.Received(1).AddAsync(Arg.Is<UserRole>(ur => ur.AuthUserId == cmd.AuthUserId && ur.RoleId == cmd.RoleId), Arg.Any<CancellationToken>());
    }
}