using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.LecturerVerificationRequests.Queries.AdminGetLecturerVerificationRequest;
using RogueLearn.User.Application.Features.LecturerVerificationRequests.Queries.GetMyLecturerVerificationRequests;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.LecturerVerificationRequests.Queries;

public class LecturerVerificationQueriesTests
{
    [Fact]
    public async Task AdminGet_ReturnsMappedDetail()
    {
        var repo = Substitute.For<ILecturerVerificationRequestRepository>();
        var id = Guid.NewGuid();
        var entity = new LecturerVerificationRequest
        {
            Id = id,
            AuthUserId = Guid.NewGuid(),
            Documents = new Dictionary<string, object> { ["email"] = "e@x.com", ["staffId"] = "S" },
            Status = VerificationStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow
        };
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(entity);

        var sut = new AdminGetLecturerVerificationRequestQueryHandler(repo);
        var result = await sut.Handle(new AdminGetLecturerVerificationRequestQuery { RequestId = id }, CancellationToken.None);
        result!.Email.Should().Be("e@x.com");
        result.StaffId.Should().Be("S");
    }

    [Fact]
    public async Task GetMy_ReturnsOrderedList()
    {
        var repo = Substitute.For<ILecturerVerificationRequestRepository>();
        var authId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var list = new List<LecturerVerificationRequest>
        {
            new() { Id = Guid.NewGuid(), AuthUserId = authId, Status = VerificationStatus.Pending, SubmittedAt = now.AddMinutes(-1) },
            new() { Id = Guid.NewGuid(), AuthUserId = authId, Status = VerificationStatus.Approved, SubmittedAt = now.AddMinutes(-2) }
        };
        repo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<LecturerVerificationRequest, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(list);

        var sut = new GetMyLecturerVerificationRequestsQueryHandler(repo);
        var result = await sut.Handle(new GetMyLecturerVerificationRequestsQuery { AuthUserId = authId }, CancellationToken.None);
        result.Should().HaveCount(2);
        result.First().SubmittedAt.Should().Be(list.OrderByDescending(x => x.SubmittedAt).First().SubmittedAt);
    }
}