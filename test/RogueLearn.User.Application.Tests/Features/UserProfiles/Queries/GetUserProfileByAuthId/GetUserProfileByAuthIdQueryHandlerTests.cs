using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;
using RogueLearn.User.Application.Mappings;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.UserProfiles.Queries.GetUserProfileByAuthId;

public class GetUserProfileByAuthIdQueryHandlerTests
{

    [Fact]
    public async Task Handle_NotFound_ReturnsNull()
    {
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<GetUserProfileByAuthIdQueryHandler>>();

        var sut = new GetUserProfileByAuthIdQueryHandler(userProfileRepo, userRoleRepo, roleRepo, mapper, logger);

        var authId = Guid.NewGuid();
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);

        var result = await sut.Handle(new GetUserProfileByAuthIdQuery(authId), CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Found_NoRoles_ReturnsDtoWithEmptyRoles()
    {
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<GetUserProfileByAuthIdQueryHandler>>();

        var sut = new GetUserProfileByAuthIdQueryHandler(userProfileRepo, userRoleRepo, roleRepo, mapper, logger);

        var authId = Guid.NewGuid();
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            AuthUserId = authId,
            Username = "jdoe",
            Email = "jdoe@example.com",
            FirstName = "John",
            LastName = "Doe",
            ClassId = Guid.NewGuid(),
            RouteId = Guid.NewGuid(),
            OnboardingCompleted = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        mapper.Map<UserProfileDto>(profile).Returns(new UserProfileDto
        {
            Id = profile.Id,
            AuthUserId = profile.AuthUserId,
            Username = profile.Username,
            Email = profile.Email,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            OnboardingCompleted = profile.OnboardingCompleted,
            CreatedAt = profile.CreatedAt,
            ProfileImageUrl = profile.ProfileImageUrl,
            Bio = profile.Bio,
            PreferencesJson = null,
            ClassId = profile.ClassId,
            RouteId = profile.RouteId,
            Roles = new List<string>()
        });
        userRoleRepo.GetRolesForUserAsync(authId, Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<UserRole>());

        var result = await sut.Handle(new GetUserProfileByAuthIdQuery(authId), CancellationToken.None);
        result.Should().NotBeNull();
        result!.AuthUserId.Should().Be(authId);
        result.Username.Should().Be("jdoe");
        result.Email.Should().Be("jdoe@example.com");
        result.Roles.Should().NotBeNull();
        result.Roles.Should().BeEmpty();
        result.ClassId.Should().Be(profile.ClassId);
        result.RouteId.Should().Be(profile.RouteId);
    }

    [Fact]
    public async Task Handle_Found_WithRoles_ReturnsRoleNames_FilteringEmpty()
    {
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<GetUserProfileByAuthIdQueryHandler>>();

        var sut = new GetUserProfileByAuthIdQueryHandler(userProfileRepo, userRoleRepo, roleRepo, mapper, logger);

        var authId = Guid.NewGuid();
        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, Username = "jane", Email = "jane@example.com" };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        mapper.Map<UserProfileDto>(profile).Returns(new UserProfileDto
        {
            Id = profile.Id,
            AuthUserId = profile.AuthUserId,
            Username = profile.Username,
            Email = profile.Email,
            Roles = new List<string>()
        });

        var roleIdA = Guid.NewGuid();
        var roleIdB = Guid.NewGuid();
        userRoleRepo.GetRolesForUserAsync(authId, Arg.Any<CancellationToken>()).Returns(new List<UserRole>
        {
            new() { AuthUserId = authId, RoleId = roleIdA },
            new() { AuthUserId = authId, RoleId = roleIdA }, // duplicate to ensure distinct
            new() { AuthUserId = authId, RoleId = roleIdB }
        });

        roleRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ids = (IEnumerable<Guid>)ci[0]!;
                // Return roles including one with empty name to test filtering
                var list = new List<Role>
                {
                    new() { Id = roleIdA, Name = "Admin" },
                    new() { Id = roleIdB, Name = string.Empty }
                };
                return list.Where(r => ids.Contains(r.Id));
            });

        var result = await sut.Handle(new GetUserProfileByAuthIdQuery(authId), CancellationToken.None);
        result.Should().NotBeNull();
        result!.Roles.Should().ContainSingle().And.Contain("Admin");
    }
}