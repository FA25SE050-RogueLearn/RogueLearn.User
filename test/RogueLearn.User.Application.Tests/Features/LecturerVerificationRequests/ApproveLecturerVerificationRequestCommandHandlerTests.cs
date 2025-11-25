using Moq;
using RogueLearn.User.Application.Features.LecturerVerificationRequests.Commands.ApproveLecturerVerificationRequest;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.LecturerVerificationRequests;

public class ApproveLecturerVerificationRequestCommandHandlerTests
{
    private readonly Mock<ILecturerVerificationRequestRepository> _reqRepo = new();
    private readonly Mock<IUserProfileRepository> _profileRepo = new();
    private readonly Mock<IRoleRepository> _roleRepo = new();
    private readonly Mock<IUserRoleRepository> _userRoleRepo = new();
    private readonly ApproveLecturerVerificationRequestCommandHandler _handler;

    public ApproveLecturerVerificationRequestCommandHandlerTests()
    {
        _handler = new ApproveLecturerVerificationRequestCommandHandler(_reqRepo.Object, _profileRepo.Object, _roleRepo.Object, _userRoleRepo.Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<ApproveLecturerVerificationRequestCommandHandler>>());
    }

    [Fact]
    public async Task Approve_WhenAlreadyApproved_ShouldBeIdempotent()
    {
        var rid = Guid.NewGuid();
        var authId = Guid.NewGuid();
        _reqRepo.Setup(x => x.GetByIdAsync(rid, It.IsAny<CancellationToken>())).ReturnsAsync(new LecturerVerificationRequest { Id = rid, AuthUserId = authId, Status = RogueLearn.User.Domain.Enums.VerificationStatus.Approved });
        _profileRepo.Setup(x => x.GetByAuthIdAsync(authId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, Email = "user@example.com" });
        var roleId = Guid.NewGuid();
        _roleRepo.Setup(x => x.GetByNameAsync("Verified Lecturer", It.IsAny<CancellationToken>())).ReturnsAsync(new Role { Id = roleId, Name = "Verified Lecturer" });
        _userRoleRepo.Setup(x => x.GetRolesForUserAsync(authId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserRole> { new UserRole { AuthUserId = authId, RoleId = roleId } });

        var cmd = new ApproveLecturerVerificationRequestCommand { RequestId = rid, ReviewerAuthUserId = Guid.NewGuid(), Note = "OK" };
        await _handler.Handle(cmd, CancellationToken.None);

        _reqRepo.Verify(x => x.UpdateAsync(It.IsAny<LecturerVerificationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _userRoleRepo.Verify(x => x.AddAsync(It.IsAny<UserRole>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}