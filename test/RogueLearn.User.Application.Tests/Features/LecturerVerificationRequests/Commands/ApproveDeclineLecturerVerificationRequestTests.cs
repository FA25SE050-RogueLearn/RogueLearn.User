using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.LecturerVerificationRequests.Commands.ApproveLecturerVerificationRequest;
using RogueLearn.User.Application.Features.LecturerVerificationRequests.Commands.DeclineLecturerVerificationRequest;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.LecturerVerificationRequests.Commands;

public class ApproveDeclineLecturerVerificationRequestTests
{
    [Fact]
    public async Task Approve_AssignsRoleAndUpdatesGuilds()
    {
        var reqRepo = Substitute.For<ILecturerVerificationRequestRepository>();
        var profileRepo = Substitute.For<IUserProfileRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var logger = Substitute.For<ILogger<ApproveLecturerVerificationRequestCommandHandler>>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.ILecturerNotificationService>();

        var authId = Guid.NewGuid();
        var req = new LecturerVerificationRequest { Id = Guid.NewGuid(), AuthUserId = authId, Status = VerificationStatus.Pending };
        reqRepo.GetByIdAsync(req.Id, Arg.Any<CancellationToken>()).Returns(req);
        profileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, Email = "e@x.com" });
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(new Role { Id = Guid.NewGuid(), Name = "Verified Lecturer" });
        userRoleRepo.GetRolesForUserAsync(authId, Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<UserRole>());
        guildRepo.GetGuildsByCreatorAsync(authId, Arg.Any<CancellationToken>()).Returns(new[] { new Guild { Id = Guid.NewGuid(), IsLecturerGuild = false } });

        var sut = new ApproveLecturerVerificationRequestCommandHandler(reqRepo, profileRepo, roleRepo, userRoleRepo, guildRepo, logger, notificationService);
        await sut.Handle(new ApproveLecturerVerificationRequestCommand { RequestId = req.Id, ReviewerAuthUserId = Guid.NewGuid() }, CancellationToken.None);

        req.Status.Should().Be(VerificationStatus.Approved);
        await userRoleRepo.Received(1).AddAsync(Arg.Any<UserRole>(), Arg.Any<CancellationToken>());
        await guildRepo.Received(1).UpdateAsync(Arg.Is<Guild>(g => g.IsLecturerGuild), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Decline_SetsRejectedWithReason()
    {
        var reqRepo = Substitute.For<ILecturerVerificationRequestRepository>();
        var profileRepo = Substitute.For<IUserProfileRepository>();
        var logger = Substitute.For<ILogger<DeclineLecturerVerificationRequestCommandHandler>>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.ILecturerNotificationService>();

        var authId = Guid.NewGuid();
        var req = new LecturerVerificationRequest { Id = Guid.NewGuid(), AuthUserId = authId, Status = VerificationStatus.Pending };
        reqRepo.GetByIdAsync(req.Id, Arg.Any<CancellationToken>()).Returns(req);
        profileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, Email = "e@x.com" });

        var sut = new DeclineLecturerVerificationRequestCommandHandler(reqRepo, profileRepo, logger, notificationService);
        await sut.Handle(new DeclineLecturerVerificationRequestCommand { RequestId = req.Id, ReviewerAuthUserId = Guid.NewGuid(), Reason = "bad" }, CancellationToken.None);

        req.Status.Should().Be(VerificationStatus.Rejected);
        req.ReviewNotes.Should().Be("bad");
    }
}