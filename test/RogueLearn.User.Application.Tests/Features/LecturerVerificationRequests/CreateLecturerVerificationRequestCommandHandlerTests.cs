using FluentAssertions;
using Moq;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.LecturerVerificationRequests.Commands.CreateLecturerVerificationRequest;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.LecturerVerificationRequests;

public class CreateLecturerVerificationRequestCommandHandlerTests
{
    private readonly Mock<ILecturerVerificationRequestRepository> _reqRepo = new();
    private readonly Mock<IUserProfileRepository> _profileRepo = new();
    private readonly CreateLecturerVerificationRequestCommandHandler _handler;

    public CreateLecturerVerificationRequestCommandHandlerTests()
    {
        _handler = new CreateLecturerVerificationRequestCommandHandler(_reqRepo.Object, _profileRepo.Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<CreateLecturerVerificationRequestCommandHandler>>());
    }

    [Fact]
    public async Task Create_WhenPendingExists_ShouldThrowConflict()
    {
        var authId = Guid.NewGuid();
        _profileRepo.Setup(x => x.GetByAuthIdAsync(authId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, Email = "user@example.com" });
        _reqRepo.Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<LecturerVerificationRequest, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var cmd = new CreateLecturerVerificationRequestCommand { AuthUserId = authId, Email = "user@example.com", StaffId = "FPT-123" };
        await FluentActions.Invoking(() => _handler.Handle(cmd, CancellationToken.None)).Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Create_WhenApprovedExists_ShouldThrowConflict()
    {
        var authId = Guid.NewGuid();
        _profileRepo.Setup(x => x.GetByAuthIdAsync(authId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, Email = "user@example.com" });
        _reqRepo.SetupSequence(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<LecturerVerificationRequest, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false) // pending
            .ReturnsAsync(true); // approved

        var cmd = new CreateLecturerVerificationRequestCommand { AuthUserId = authId, Email = "user@example.com", StaffId = "FPT-123" };
        await FluentActions.Invoking(() => _handler.Handle(cmd, CancellationToken.None)).Should().ThrowAsync<ConflictException>();
    }
}