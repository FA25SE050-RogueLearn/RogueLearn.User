using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Roles.Commands.DeleteRole;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Roles.Commands.DeleteRole;

public class DeleteRoleCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_NotFound_Throws(DeleteRoleCommand cmd)
    {
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var logger = Substitute.For<ILogger<DeleteRoleCommandHandler>>();

        roleRepo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns((Role?)null);

        var sut = new DeleteRoleCommandHandler(roleRepo, userRoleRepo, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_AssignedToUsers_Throws(DeleteRoleCommand cmd)
    {
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var logger = Substitute.For<ILogger<DeleteRoleCommandHandler>>();

        var role = new Role { Id = cmd.Id, Name = "r" };
        roleRepo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns(role);
        userRoleRepo.GetUsersByRoleIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new UserRole { RoleId = cmd.Id, AuthUserId = Guid.NewGuid() } });

        var sut = new DeleteRoleCommandHandler(roleRepo, userRoleRepo, logger);
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_Deletes(DeleteRoleCommand cmd)
    {
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var logger = Substitute.For<ILogger<DeleteRoleCommandHandler>>();

        var role = new Role { Id = cmd.Id, Name = "r" };
        roleRepo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns(role);
        userRoleRepo.GetUsersByRoleIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns(new List<UserRole>());

        var sut = new DeleteRoleCommandHandler(roleRepo, userRoleRepo, logger);
        await sut.Handle(cmd, CancellationToken.None);
        await roleRepo.Received(1).DeleteAsync(cmd.Id, Arg.Any<CancellationToken>());
    }
}