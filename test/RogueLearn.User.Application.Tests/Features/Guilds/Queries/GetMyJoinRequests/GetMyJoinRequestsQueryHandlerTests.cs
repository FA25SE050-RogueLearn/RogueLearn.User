using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Queries.GetMyJoinRequests;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Queries.GetMyJoinRequests;

public class GetMyJoinRequestsQueryHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_PendingOnly_Filters(GetMyJoinRequestsQuery query)
    {
        var joinRepo = Substitute.For<IGuildJoinRequestRepository>();
        var profileRepo = Substitute.For<IUserProfileRepository>();
        var sut = new GetMyJoinRequestsQueryHandler(joinRepo, profileRepo);

        query = new GetMyJoinRequestsQuery(query.AuthUserId, true);
        var list = new List<GuildJoinRequest> {
            new() { Id = System.Guid.NewGuid(), GuildId = System.Guid.NewGuid(), RequesterId = query.AuthUserId, Status = GuildJoinRequestStatus.Pending },
            new() { Id = System.Guid.NewGuid(), GuildId = System.Guid.NewGuid(), RequesterId = query.AuthUserId, Status = GuildJoinRequestStatus.Accepted }
        };
        joinRepo.GetRequestsByRequesterAsync(query.AuthUserId, Arg.Any<CancellationToken>()).Returns(list);
        profileRepo.GetByAuthIdAsync(query.AuthUserId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Username = "u" });

        var result = await sut.Handle(query, CancellationToken.None);
        result.Count.Should().Be(1);
        result[0].Status.Should().Be(GuildJoinRequestStatus.Pending);
        result[0].RequesterName.Should().Be("u");
    }
}