using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.LecturerVerificationRequests.Commands.CreateLecturerVerificationRequest;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.LecturerVerificationRequests.Commands;

public class CreateLecturerVerificationRequestTests
{
    [Fact]
    public async Task Handle_CreatesPendingRequest()
    {
        var reqRepo = Substitute.For<ILecturerVerificationRequestRepository>();
        var profileRepo = Substitute.For<IUserProfileRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var logger = Substitute.For<ILogger<CreateLecturerVerificationRequestCommandHandler>>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.ILecturerNotificationService>();

        var authId = Guid.NewGuid();
        profileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, Email = "e@x.com" });
        reqRepo.AnyPendingAsync(authId, Arg.Any<CancellationToken>()).Returns(false);
        reqRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<LecturerVerificationRequest, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<LecturerVerificationRequest>());
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns((Role?)null);

        var sut = new CreateLecturerVerificationRequestCommandHandler(reqRepo, profileRepo, roleRepo, userRoleRepo, logger, notificationService);
        var cmd = new CreateLecturerVerificationRequestCommand { AuthUserId = authId, Email = "e@x.com", StaffId = "S" };
        var response = await sut.Handle(cmd, CancellationToken.None);

        response.Status.Should().Be("pending");
        await reqRepo.Received(1).AddAsync(Arg.Is<LecturerVerificationRequest>(r => r.Status == VerificationStatus.Pending), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ThrowsOnEmailMismatch()
    {
        var reqRepo = Substitute.For<ILecturerVerificationRequestRepository>();
        var profileRepo = Substitute.For<IUserProfileRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var logger = Substitute.For<ILogger<CreateLecturerVerificationRequestCommandHandler>>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.ILecturerNotificationService>();

        var authId = Guid.NewGuid();
        profileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, Email = "real@x.com" });

        var sut = new CreateLecturerVerificationRequestCommandHandler(reqRepo, profileRepo, roleRepo, userRoleRepo, logger, notificationService);
        var cmd = new CreateLecturerVerificationRequestCommand { AuthUserId = authId, Email = "fake@x.com", StaffId = "S" };
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task Handle_ThrowsOnPendingRequest()
    {
        var reqRepo = Substitute.For<ILecturerVerificationRequestRepository>();
        var profileRepo = Substitute.For<IUserProfileRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var logger = Substitute.For<ILogger<CreateLecturerVerificationRequestCommandHandler>>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.ILecturerNotificationService>();

        var authId = Guid.NewGuid();
        profileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, Email = "e@x.com" });
        reqRepo.AnyPendingAsync(authId, Arg.Any<CancellationToken>()).Returns(true);

        var sut = new CreateLecturerVerificationRequestCommandHandler(reqRepo, profileRepo, roleRepo, userRoleRepo, logger, notificationService);
        var cmd = new CreateLecturerVerificationRequestCommand { AuthUserId = authId, Email = "e@x.com", StaffId = "S" };
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_ThrowsWhenApprovedRequestExists()
    {
        var reqRepo = Substitute.For<ILecturerVerificationRequestRepository>();
        var profileRepo = Substitute.For<IUserProfileRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var logger = Substitute.For<ILogger<CreateLecturerVerificationRequestCommandHandler>>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.ILecturerNotificationService>();

        var authId = Guid.NewGuid();
        profileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, Email = "e@x.com" });
        reqRepo.AnyPendingAsync(authId, Arg.Any<CancellationToken>()).Returns(false);
        var approved = new LecturerVerificationRequest { Id = Guid.NewGuid(), AuthUserId = authId, Status = VerificationStatus.Approved, ReviewedAt = null };
        reqRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<LecturerVerificationRequest, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { approved });

        var sut = new CreateLecturerVerificationRequestCommandHandler(reqRepo, profileRepo, roleRepo, userRoleRepo, logger, notificationService);
        var cmd = new CreateLecturerVerificationRequestCommand { AuthUserId = authId, Email = "e@x.com", StaffId = "S" };
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_ThrowsWhenUserHasVerifiedLecturerRole()
    {
        var reqRepo = Substitute.For<ILecturerVerificationRequestRepository>();
        var profileRepo = Substitute.For<IUserProfileRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var logger = Substitute.For<ILogger<CreateLecturerVerificationRequestCommandHandler>>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.ILecturerNotificationService>();

        var authId = Guid.NewGuid();
        profileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, Email = "e@x.com" });
        reqRepo.AnyPendingAsync(authId, Arg.Any<CancellationToken>()).Returns(false);
        reqRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<LecturerVerificationRequest, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<LecturerVerificationRequest>());
        var role = new Role { Id = Guid.NewGuid(), Name = "Verified Lecturer" };
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(role);
        userRoleRepo.GetRolesForUserAsync(authId, Arg.Any<CancellationToken>()).Returns(new[] { new UserRole { AuthUserId = authId, RoleId = role.Id } });

        var sut = new CreateLecturerVerificationRequestCommandHandler(reqRepo, profileRepo, roleRepo, userRoleRepo, logger, notificationService);
        var cmd = new CreateLecturerVerificationRequestCommand { AuthUserId = authId, Email = "e@x.com", StaffId = "S" };
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_IncludesScreenshotUrlInDocuments()
    {
        var reqRepo = Substitute.For<ILecturerVerificationRequestRepository>();
        var profileRepo = Substitute.For<IUserProfileRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var logger = Substitute.For<ILogger<CreateLecturerVerificationRequestCommandHandler>>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.ILecturerNotificationService>();

        var authId = Guid.NewGuid();
        profileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, Email = "e@x.com" });
        reqRepo.AnyPendingAsync(authId, Arg.Any<CancellationToken>()).Returns(false);
        reqRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<LecturerVerificationRequest, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<LecturerVerificationRequest>());
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns((Role?)null);

        LecturerVerificationRequest? captured = null;
        reqRepo.AddAsync(Arg.Do<LecturerVerificationRequest>(r => captured = r), Arg.Any<CancellationToken>()).Returns(ci => captured!);

        var sut = new CreateLecturerVerificationRequestCommandHandler(reqRepo, profileRepo, roleRepo, userRoleRepo, logger, notificationService);
        var cmd = new CreateLecturerVerificationRequestCommand { AuthUserId = authId, Email = "e@x.com", StaffId = "S", ScreenshotUrl = "http://x" };
        _ = await sut.Handle(cmd, CancellationToken.None);

        captured!.Documents!.TryGetValue("screenshotUrl", out var url).Should().BeTrue();
        url!.ToString().Should().Be("http://x");
    }

    [Fact]
    public async Task Handle_AllowsWhenRoleExistsButUserDoesNotHaveIt()
    {
        var reqRepo = Substitute.For<ILecturerVerificationRequestRepository>();
        var profileRepo = Substitute.For<IUserProfileRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var logger = Substitute.For<ILogger<CreateLecturerVerificationRequestCommandHandler>>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.ILecturerNotificationService>();

        var authId = Guid.NewGuid();
        profileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, Email = "e@x.com" });
        reqRepo.AnyPendingAsync(authId, Arg.Any<CancellationToken>()).Returns(false);
        reqRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<LecturerVerificationRequest, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<LecturerVerificationRequest>());
        var role = new Role { Id = Guid.NewGuid(), Name = "Verified Lecturer" };
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(role);
        userRoleRepo.GetRolesForUserAsync(authId, Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<UserRole>());

        LecturerVerificationRequest? captured = null;
        reqRepo.AddAsync(Arg.Do<LecturerVerificationRequest>(r => captured = r), Arg.Any<CancellationToken>()).Returns(ci => captured!);

        var sut = new CreateLecturerVerificationRequestCommandHandler(reqRepo, profileRepo, roleRepo, userRoleRepo, logger, notificationService);
        var cmd = new CreateLecturerVerificationRequestCommand { AuthUserId = authId, Email = "e@x.com", StaffId = "S" };
        var resp = await sut.Handle(cmd, CancellationToken.None);

        resp.Status.Should().Be("pending");
        captured!.Status.Should().Be(VerificationStatus.Pending);
    }
}
