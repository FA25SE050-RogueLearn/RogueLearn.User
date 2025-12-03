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

        var authId = Guid.NewGuid();
        profileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, Email = "e@x.com" });
        reqRepo.AnyPendingAsync(authId, Arg.Any<CancellationToken>()).Returns(false);
        reqRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<LecturerVerificationRequest, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<LecturerVerificationRequest>());
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns((Role?)null);

        var sut = new CreateLecturerVerificationRequestCommandHandler(reqRepo, profileRepo, roleRepo, userRoleRepo, logger);
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

        var authId = Guid.NewGuid();
        profileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, Email = "real@x.com" });

        var sut = new CreateLecturerVerificationRequestCommandHandler(reqRepo, profileRepo, roleRepo, userRoleRepo, logger);
        var cmd = new CreateLecturerVerificationRequestCommand { AuthUserId = authId, Email = "fake@x.com", StaffId = "S" };
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>();
    }
}