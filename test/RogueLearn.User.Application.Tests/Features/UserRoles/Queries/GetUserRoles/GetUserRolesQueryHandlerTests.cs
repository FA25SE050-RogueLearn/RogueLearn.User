using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.UserRoles.Queries.GetUserRoles;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.UserRoles.Queries.GetUserRoles;

public class GetUserRolesQueryHandlerTests
{
    [Fact]
    public async Task Handle_UserNotFound_Throws()
    {
        var urRepo = Substitute.For<IUserRoleRepository>();
        var upRepo = Substitute.For<IUserProfileRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<GetUserRolesQueryHandler>>();
        var sut = new GetUserRolesQueryHandler(urRepo, upRepo, roleRepo, mapper, logger);

        var q = new GetUserRolesQuery { AuthUserId = Guid.NewGuid() };
        upRepo.GetByAuthIdAsync(q.AuthUserId, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(q, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ReturnsMappedRoles()
    {
        var urRepo = Substitute.For<IUserRoleRepository>();
        var upRepo = Substitute.For<IUserProfileRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<GetUserRolesQueryHandler>>();
        var sut = new GetUserRolesQueryHandler(urRepo, upRepo, roleRepo, mapper, logger);

        var authId = Guid.NewGuid();
        var q = new GetUserRolesQuery { AuthUserId = authId };
        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, Username = "u" };
        var ur = new UserRole { Id = Guid.NewGuid(), AuthUserId = authId, RoleId = Guid.NewGuid(), AssignedAt = DateTimeOffset.UtcNow };
        var role = new Role { Id = ur.RoleId, Name = "Admin", Description = "desc" };

        upRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        urRepo.GetRolesForUserAsync(authId, Arg.Any<CancellationToken>()).Returns(new List<UserRole> { ur });
        roleRepo.GetByIdAsync(ur.RoleId, Arg.Any<CancellationToken>()).Returns(role);
        mapper.Map<UserRoleDto>(ur).Returns(new UserRoleDto { RoleId = ur.RoleId, AssignedAt = ur.AssignedAt });

        var res = await sut.Handle(q, CancellationToken.None);
        res.UserId.Should().Be(profile.Id);
        res.Roles.Should().HaveCount(1);
        res.Roles[0].RoleId.Should().Be(role.Id);
        res.Roles[0].RoleName.Should().Be("Admin");
        res.Roles[0].Description.Should().Be("desc");
    }
}