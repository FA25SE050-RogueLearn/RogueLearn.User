using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Roles.Commands.UpdateRole;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Roles.Commands.UpdateRole;

public class UpdateRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var roleRepo = Substitute.For<IRoleRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<UpdateRoleCommandHandler>>();

        var cmd = new UpdateRoleCommand { Id = Guid.NewGuid(), Name = "n" };
        roleRepo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns((Role?)null);

        var sut = new UpdateRoleCommandHandler(roleRepo, mapper, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_UpdatesAndMaps()
    {
        var roleRepo = Substitute.For<IRoleRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<UpdateRoleCommandHandler>>();

        var roleId = Guid.NewGuid();
        var cmd = new UpdateRoleCommand { Id = roleId, Name = "new", Description = "desc" };
        var role = new Role { Id = roleId, Name = "old", Description = "d" };
        var updated = new Role { Id = roleId, Name = cmd.Name, Description = cmd.Description };
        roleRepo.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);
        roleRepo.UpdateAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>()).Returns(updated);
        mapper.Map<UpdateRoleResponse>(updated).Returns(new UpdateRoleResponse { Id = updated.Id, Name = updated.Name, Description = updated.Description });

        var sut = new UpdateRoleCommandHandler(roleRepo, mapper, logger);
        var res = await sut.Handle(cmd, CancellationToken.None);
        res.Id.Should().Be(updated.Id);
        res.Name.Should().Be(updated.Name);
        await roleRepo.Received(1).UpdateAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
    }
}