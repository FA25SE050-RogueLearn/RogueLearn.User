using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.LecturerVerificationRequests.Queries.AdminListLecturerVerificationRequests;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.LecturerVerificationRequests.Queries.AdminListLecturerVerificationRequests;

public class AdminListLecturerVerificationRequestsQueryHandlerTests
{
    [Fact]
    public async Task Handle_FiltersDeclinedAndPaginates()
    {
        var repo = Substitute.For<ILecturerVerificationRequestRepository>();
        var sut = new AdminListLecturerVerificationRequestsQueryHandler(repo);

        var query = new AdminListLecturerVerificationRequestsQuery
        {
            Status = "declined",
            Page = 1,
            Size = 2,
            UserId = null
        };

        var docs1 = new Dictionary<string, object> { ["staffId"] = "S1", ["email"] = "e1@example.com" };
        var docs2 = new Dictionary<string, object> { ["staffId"] = "S2", ["email"] = "e2@example.com" };
        var list = new List<LecturerVerificationRequest>
        {
            new() { Id = System.Guid.NewGuid(), AuthUserId = System.Guid.NewGuid(), Status = VerificationStatus.Rejected, Documents = docs1 },
            new() { Id = System.Guid.NewGuid(), AuthUserId = System.Guid.NewGuid(), Status = VerificationStatus.Pending, Documents = docs2 },
            new() { Id = System.Guid.NewGuid(), AuthUserId = System.Guid.NewGuid(), Status = VerificationStatus.Rejected, Documents = docs2 }
        };

        repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(list);

        var resp = await sut.Handle(query, CancellationToken.None);
        resp.Items.All(i => i.Status == "declined").Should().BeTrue();
        resp.Items.Count.Should().BeLessThanOrEqualTo(2);
        resp.Total.Should().Be(2);
        resp.Items[0].StaffId.Should().NotBeNullOrEmpty();
        resp.Items[0].Email.Should().NotBeNullOrEmpty();
    }
}