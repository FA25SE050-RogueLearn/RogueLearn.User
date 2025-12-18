using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.UserProfiles.Commands.UpdateMyProfile;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.UserProfiles.Commands.UpdateMyProfile;

public class UpdateMyProfileCommandHandlerTests
{
    private static UpdateMyProfileCommandHandler CreateSut(
        IUserProfileRepository? userProfileRepository = null,
        IUserRoleRepository? userRoleRepository = null,
        IRoleRepository? roleRepository = null,
        IAvatarStorage? avatarStorage = null,
        IMapper? mapper = null,
        ILogger<UpdateMyProfileCommandHandler>? logger = null,
        IClassRepository? classRepository = null,
        ICurriculumProgramRepository? curriculumProgramRepository = null)
    {
        userProfileRepository ??= Substitute.For<IUserProfileRepository>();
        userRoleRepository ??= Substitute.For<IUserRoleRepository>();
        roleRepository ??= Substitute.For<IRoleRepository>();
        avatarStorage ??= Substitute.For<IAvatarStorage>();
        mapper ??= Substitute.For<IMapper>();
        logger ??= Substitute.For<ILogger<UpdateMyProfileCommandHandler>>();
        classRepository ??= Substitute.For<IClassRepository>();
        curriculumProgramRepository ??= Substitute.For<ICurriculumProgramRepository>();
        return new UpdateMyProfileCommandHandler(userProfileRepository, userRoleRepository, roleRepository, avatarStorage, mapper, logger, classRepository, curriculumProgramRepository);
    }

    [Fact]
    public async Task Handle_ProfileNotFound_ThrowsNotFound()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var authId = Guid.NewGuid();
        userRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);
        var sut = CreateSut(userProfileRepository: userRepo);
        var cmd = new UpdateMyProfileCommand { AuthUserId = authId };
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_UploadImage_SetsProfileImageUrl_AndHydratesRoles()
    {
        var authId = Guid.NewGuid();
        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, Username = "u", Email = "e@x.com" };

        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        userRepo.UpdateAsync(Arg.Any<UserProfile>(), Arg.Any<CancellationToken>())
            .Returns(ci => (UserProfile)ci[0]!);

        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var avatar = Substitute.For<IAvatarStorage>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<UpdateMyProfileCommandHandler>>();

        var url = "https://cdn/avatars/u.png";
        avatar.SaveAvatarAsync(authId, Arg.Any<byte[]>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(url);

        mapper.Map<UserProfileDto>(Arg.Any<UserProfile>()).Returns(new UserProfileDto { Id = profile.Id, AuthUserId = profile.AuthUserId, Username = profile.Username, Email = profile.Email, Roles = new List<string>() });

        var roleIdA = Guid.NewGuid();
        var roleIdB = Guid.NewGuid();
        userRoleRepo.GetRolesForUserAsync(authId, Arg.Any<CancellationToken>()).Returns(new List<UserRole>
        {
            new() { AuthUserId = authId, RoleId = roleIdA },
            new() { AuthUserId = authId, RoleId = roleIdA },
            new() { AuthUserId = authId, RoleId = roleIdB }
        });

        roleRepo.GetByIdAsync(roleIdA, Arg.Any<CancellationToken>()).Returns(new Role { Id = roleIdA, Name = "Member" });
        roleRepo.GetByIdAsync(roleIdB, Arg.Any<CancellationToken>()).Returns(new Role { Id = roleIdB, Name = string.Empty });

        var sut = CreateSut(userRepo, userRoleRepo, roleRepo, avatar, mapper, logger);
        var cmd = new UpdateMyProfileCommand { AuthUserId = authId, ProfileImageBytes = new byte[10], ProfileImageContentType = "image/png", ProfileImageFileName = "a.png" };
        var res = await sut.Handle(cmd, CancellationToken.None);
        res.Should().NotBeNull();
        profile.ProfileImageUrl.Should().Be(url);
        res.Roles.Should().ContainSingle().And.Contain("Member");
        await avatar.Received(1).SaveAvatarAsync(authId, Arg.Any<byte[]>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await userRepo.Received(1).UpdateAsync(profile, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AvatarUploadThrows_BubblesExceptionAndLogsError()
    {
        var authId = Guid.NewGuid();
        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId };
        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);

        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var avatar = Substitute.For<IAvatarStorage>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<UpdateMyProfileCommandHandler>>();

        avatar.SaveAvatarAsync(authId, Arg.Any<byte[]>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<Task<string>>(_ => throw new Exception("fail"));

        var sut = CreateSut(userRepo, userRoleRepo, roleRepo, avatar, mapper, logger);
        var cmd = new UpdateMyProfileCommand { AuthUserId = authId, ProfileImageBytes = new byte[10], ProfileImageContentType = "image/png", ProfileImageFileName = "a.png" };
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<Exception>();
        logger.ReceivedWithAnyArgs(1).Log<object>(default, default, default!, default!, default!);
        await userRepo.DidNotReceive().UpdateAsync(Arg.Any<UserProfile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProfileImageUrlPatch_SetsOrClearsUrl()
    {
        var authId = Guid.NewGuid();
        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ProfileImageUrl = "old" };
        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        userRepo.UpdateAsync(Arg.Any<UserProfile>(), Arg.Any<CancellationToken>())
            .Returns(ci => (UserProfile)ci[0]!);
        var mapper = Substitute.For<IMapper>();
        mapper.Map<UserProfileDto>(Arg.Any<UserProfile>()).Returns(new UserProfileDto { AuthUserId = authId });

        var sut = CreateSut(userRepo, Substitute.For<IUserRoleRepository>(), Substitute.For<IRoleRepository>(), Substitute.For<IAvatarStorage>(), mapper, Substitute.For<ILogger<UpdateMyProfileCommandHandler>>());

        var cmdClear = new UpdateMyProfileCommand { AuthUserId = authId, ProfileImageUrl = "   " };
        var res1 = await sut.Handle(cmdClear, CancellationToken.None);
        res1.Should().NotBeNull();
        profile.ProfileImageUrl.Should().BeNull();

        profile.ProfileImageUrl = null;
        var cmdSet = new UpdateMyProfileCommand { AuthUserId = authId, ProfileImageUrl = "https://cdn/u.png" };
        var res2 = await sut.Handle(cmdSet, CancellationToken.None);
        res2.Should().NotBeNull();
        profile.ProfileImageUrl.Should().Be("https://cdn/u.png");
    }

    [Fact]
    public async Task Handle_UpdateOtherFields_AndPreferencesJson()
    {
        var authId = Guid.NewGuid();
        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, FirstName = "A", LastName = "B", Bio = "x" };
        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        userRepo.UpdateAsync(Arg.Any<UserProfile>(), Arg.Any<CancellationToken>())
            .Returns(ci => (UserProfile)ci[0]!);
        var mapper = Substitute.For<IMapper>();
        mapper.Map<UserProfileDto>(Arg.Any<UserProfile>()).Returns(new UserProfileDto { AuthUserId = authId });

        var sut = CreateSut(userRepo, Substitute.For<IUserRoleRepository>(), Substitute.For<IRoleRepository>(), Substitute.For<IAvatarStorage>(), mapper, Substitute.For<ILogger<UpdateMyProfileCommandHandler>>());
        var cmd = new UpdateMyProfileCommand
        {
            AuthUserId = authId,
            FirstName = "",
            LastName = "NewL",
            Bio = "",
            PreferencesJson = "{\"theme\":\"dark\"}"
        };
        var res = await sut.Handle(cmd, CancellationToken.None);
        res.Should().NotBeNull();
        profile.FirstName.Should().BeNull();
        profile.LastName.Should().Be("NewL");
        profile.Bio.Should().Be("");
        await userRepo.Received(1).UpdateAsync(profile, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PreferencesJson_Invalid_ThrowsJsonException()
    {
        var authId = Guid.NewGuid();
        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId };
        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        var mapper = Substitute.For<IMapper>();
        var sut = CreateSut(userRepo, Substitute.For<IUserRoleRepository>(), Substitute.For<IRoleRepository>(), Substitute.For<IAvatarStorage>(), mapper, Substitute.For<ILogger<UpdateMyProfileCommandHandler>>());

        var cmd = new UpdateMyProfileCommand { AuthUserId = authId, PreferencesJson = "{bad" };
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<System.Text.Json.JsonException>();
    }

    [Fact]
    public async Task Handle_PreferencesJson_Empty_SetsNull()
    {
        var authId = Guid.NewGuid();
        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId };
        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        userRepo.UpdateAsync(Arg.Any<UserProfile>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserProfile>());
        var mapper = Substitute.For<IMapper>();
        mapper.Map<UserProfileDto>(Arg.Any<UserProfile>()).Returns(new UserProfileDto { AuthUserId = authId });
        var sut = CreateSut(userRepo, Substitute.For<IUserRoleRepository>(), Substitute.For<IRoleRepository>(), Substitute.For<IAvatarStorage>(), mapper, Substitute.For<ILogger<UpdateMyProfileCommandHandler>>());

        var cmd = new UpdateMyProfileCommand { AuthUserId = authId, PreferencesJson = "   " };
        var res = await sut.Handle(cmd, CancellationToken.None);
        res.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ClassId_NotExists_ThrowsNotFound()
    {
        var authId = Guid.NewGuid();
        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId };
        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        var classRepo = Substitute.For<IClassRepository>();
        classRepo.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        var sut = CreateSut(userRepo, Substitute.For<IUserRoleRepository>(), Substitute.For<IRoleRepository>(), Substitute.For<IAvatarStorage>(), Substitute.For<IMapper>(), Substitute.For<ILogger<UpdateMyProfileCommandHandler>>(), classRepo);

        var cmd = new UpdateMyProfileCommand { AuthUserId = authId, ClassId = Guid.NewGuid() };
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_RouteId_NotExists_ThrowsNotFound()
    {
        var authId = Guid.NewGuid();
        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId };
        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        programRepo.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        var sut = CreateSut(userRepo, Substitute.For<IUserRoleRepository>(), Substitute.For<IRoleRepository>(), Substitute.For<IAvatarStorage>(), Substitute.For<IMapper>(), Substitute.For<ILogger<UpdateMyProfileCommandHandler>>(), Substitute.For<IClassRepository>(), programRepo);

        var cmd = new UpdateMyProfileCommand { AuthUserId = authId, RouteId = Guid.NewGuid() };
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Assigns_ClassId_And_RouteId_WhenValid()
    {
        var authId = Guid.NewGuid();
        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId };
        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        userRepo.UpdateAsync(Arg.Any<UserProfile>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserProfile>());
        var mapper = Substitute.For<IMapper>();
        mapper.Map<UserProfileDto>(Arg.Any<UserProfile>()).Returns(new UserProfileDto { AuthUserId = authId });

        var classRepo = Substitute.For<IClassRepository>();
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var classId = Guid.NewGuid();
        var routeId = Guid.NewGuid();
        classRepo.ExistsAsync(classId, Arg.Any<CancellationToken>()).Returns(true);
        programRepo.ExistsAsync(routeId, Arg.Any<CancellationToken>()).Returns(true);

        var sut = CreateSut(userRepo, Substitute.For<IUserRoleRepository>(), Substitute.For<IRoleRepository>(), Substitute.For<IAvatarStorage>(), mapper, Substitute.For<ILogger<UpdateMyProfileCommandHandler>>(), classRepo, programRepo);
        var cmd = new UpdateMyProfileCommand { AuthUserId = authId, ClassId = classId, RouteId = routeId };
        var res = await sut.Handle(cmd, CancellationToken.None);

        res.Should().NotBeNull();
        profile.ClassId.Should().Be(classId);
        profile.RouteId.Should().Be(routeId);
    }
}
