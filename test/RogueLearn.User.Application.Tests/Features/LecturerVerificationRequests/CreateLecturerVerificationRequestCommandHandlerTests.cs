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
    private readonly Mock<IRoleRepository> _roleRepo = new();
    private readonly Mock<IUserRoleRepository> _userRoleRepo = new();
    private readonly CreateLecturerVerificationRequestCommandHandler _handler;

    public CreateLecturerVerificationRequestCommandHandlerTests()
    {
        _handler = new CreateLecturerVerificationRequestCommandHandler(
            _reqRepo.Object,
            _profileRepo.Object,
            _roleRepo.Object,
            _userRoleRepo.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<CreateLecturerVerificationRequestCommandHandler>>()
        );
    }

    [Fact]
    public async Task Create_WhenPendingExists_ShouldThrowConflict()
    {
        var authId = Guid.NewGuid();
        _profileRepo.Setup(x => x.GetByAuthIdAsync(authId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, Email = "user@example.com" });
        _reqRepo.Setup(x => x.AnyPendingAsync(authId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _reqRepo.Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<LecturerVerificationRequest, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<LecturerVerificationRequest>());
        _roleRepo.Setup(x => x.GetByNameAsync("Verified Lecturer", It.IsAny<CancellationToken>())).ReturnsAsync((Role?)null);
        _userRoleRepo.Setup(x => x.GetRolesForUserAsync(authId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<UserRole>());

        var cmd = new CreateLecturerVerificationRequestCommand { AuthUserId = authId, Email = "user@example.com", StaffId = "FPT-123" };
        await FluentActions.Invoking(() => _handler.Handle(cmd, CancellationToken.None)).Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Create_WhenApprovedExists_ShouldThrowConflict()
    {
        var authId = Guid.NewGuid();
        _profileRepo.Setup(x => x.GetByAuthIdAsync(authId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, Email = "user@example.com" });
        _reqRepo.Setup(x => x.AnyPendingAsync(authId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _reqRepo.Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<LecturerVerificationRequest, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new LecturerVerificationRequest
                {
                    Id = Guid.NewGuid(),
                    AuthUserId = authId,
                    Status = VerificationStatus.Approved,
                    ReviewedAt = null
                }
            });
        _roleRepo.Setup(x => x.GetByNameAsync("Verified Lecturer", It.IsAny<CancellationToken>())).ReturnsAsync((Role?)null);
        _userRoleRepo.Setup(x => x.GetRolesForUserAsync(authId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<UserRole>());

        var cmd = new CreateLecturerVerificationRequestCommand { AuthUserId = authId, Email = "user@example.com", StaffId = "FPT-123" };
        await FluentActions.Invoking(() => _handler.Handle(cmd, CancellationToken.None)).Should().ThrowAsync<ConflictException>();
    }
}