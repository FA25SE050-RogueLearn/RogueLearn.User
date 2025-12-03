using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
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
    [Theory]
    [AutoData]
    public async Task Handle_NotFound_Throws(UpdateRoleCommand cmd)
    {
        var roleRepo = Substitute.For<IRoleRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<UpdateRoleCommandHandler>>();

        roleRepo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns((Role?)null);

        var sut = new UpdateRoleCommandHandler(roleRepo, mapper, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_UpdatesAndMaps(UpdateRoleCommand cmd)
    {
        var roleRepo = Substitute.For<IRoleRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<UpdateRoleCommandHandler>>();

        var role = new Role { Id = cmd.Id, Name = "old", Description = "d" };
        var updated = new Role { Id = cmd.Id, Name = cmd.Name, Description = cmd.Description };
        roleRepo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns(role);
        roleRepo.UpdateAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>()).Returns(updated);
        mapper.Map<UpdateRoleResponse>(updated).Returns(new UpdateRoleResponse { Id = updated.Id, Name = updated.Name, Description = updated.Description });

        var sut = new UpdateRoleCommandHandler(roleRepo, mapper, logger);
        var res = await sut.Handle(cmd, CancellationToken.None);
        res.Id.Should().Be(updated.Id);
        res.Name.Should().Be(updated.Name);
        await roleRepo.Received(1).UpdateAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
    }
}