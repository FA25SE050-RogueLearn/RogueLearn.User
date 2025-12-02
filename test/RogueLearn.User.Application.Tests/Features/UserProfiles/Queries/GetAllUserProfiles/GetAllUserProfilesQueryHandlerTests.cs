using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetAllUserProfiles;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.UserProfiles.Queries.GetAllUserProfiles;

public class GetAllUserProfilesQueryHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_MapsProfilesAndRoles(GetAllUserProfilesQuery query)
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<GetAllUserProfilesQueryHandler>>();

        var p = new UserProfile { AuthUserId = System.Guid.NewGuid(), Username = "u" };
        userRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<UserProfile> { p });
        mapper.Map<UserProfileDto>(p).Returns(new UserProfileDto { AuthUserId = p.AuthUserId, Username = p.Username });
        var urs = new List<UserRole> { new UserRole { RoleId = System.Guid.NewGuid(), AuthUserId = p.AuthUserId } };
        userRoleRepo.GetRolesForUserAsync(p.AuthUserId, Arg.Any<CancellationToken>()).Returns(urs);
        roleRepo.GetByIdAsync(urs[0].RoleId, Arg.Any<CancellationToken>()).Returns(new Role { Id = urs[0].RoleId, Name = "RoleA" });

        var sut = new GetAllUserProfilesQueryHandler(userRepo, userRoleRepo, roleRepo, mapper, logger);
        var res = await sut.Handle(query, CancellationToken.None);
        res.UserProfiles.Should().HaveCount(1);
        res.UserProfiles[0].Roles.Should().ContainSingle(r => r == "RoleA");
    }

    [Theory]
    [AutoData]
    public async Task Handle_Empty_ReturnsEmptyList(GetAllUserProfilesQuery query)
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<GetAllUserProfilesQueryHandler>>();

        userRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns((IEnumerable<UserProfile>)new List<UserProfile>());

        var sut = new GetAllUserProfilesQueryHandler(userRepo, userRoleRepo, roleRepo, mapper, logger);
        var res = await sut.Handle(query, CancellationToken.None);
        res.UserProfiles.Should().BeEmpty();
    }
}