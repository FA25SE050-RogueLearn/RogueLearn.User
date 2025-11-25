using FluentAssertions;
using Moq;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.LecturerVerificationRequests.Commands.DeclineLecturerVerificationRequest;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.LecturerVerificationRequests;

public class DeclineLecturerVerificationRequestCommandHandlerTests
{
    private readonly Mock<ILecturerVerificationRequestRepository> _reqRepo = new();
    private readonly Mock<IUserProfileRepository> _profileRepo = new();
    private readonly Mock<RogueLearn.User.Application.Interfaces.IMessageBus> _bus = new();
    private readonly DeclineLecturerVerificationRequestCommandHandler _handler;

    public DeclineLecturerVerificationRequestCommandHandlerTests()
    {
        _handler = new DeclineLecturerVerificationRequestCommandHandler(_reqRepo.Object, _profileRepo.Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<DeclineLecturerVerificationRequestCommandHandler>>());
    }

    [Fact]
    public async Task Decline_WithoutReason_ShouldThrowBadRequest()
    {
        var cmd = new DeclineLecturerVerificationRequestCommand { RequestId = Guid.NewGuid(), ReviewerAuthUserId = Guid.NewGuid(), Reason = "" };
        await FluentActions.Invoking(() => _handler.Handle(cmd, CancellationToken.None)).Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task Decline_WithReason_ShouldUpdateAndPublish()
    {
        var rid = Guid.NewGuid();
        var authId = Guid.NewGuid();
        _reqRepo.Setup(x => x.GetByIdAsync(rid, It.IsAny<CancellationToken>())).ReturnsAsync(new LecturerVerificationRequest { Id = rid, AuthUserId = authId });
        _profileRepo.Setup(x => x.GetByAuthIdAsync(authId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, Email = "user@example.com" });

        var cmd = new DeclineLecturerVerificationRequestCommand { RequestId = rid, ReviewerAuthUserId = Guid.NewGuid(), Reason = "Insufficient docs" };
        await _handler.Handle(cmd, CancellationToken.None);

        _reqRepo.Verify(x => x.UpdateAsync(It.IsAny<LecturerVerificationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        // No domain events published anymore.
    }
}