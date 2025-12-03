using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Queries.GetGuildJoinRequests;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Queries.GetGuildJoinRequests;

public class GetGuildJoinRequestsQueryHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_PendingOnly_Filters(GetGuildJoinRequestsQuery query)
    {
        var joinRepo = Substitute.For<IGuildJoinRequestRepository>();
        var profileRepo = Substitute.For<IUserProfileRepository>();
        var sut = new GetGuildJoinRequestsQueryHandler(joinRepo, profileRepo);

        query = new GetGuildJoinRequestsQuery(query.GuildId, true);
        var list = new List<GuildJoinRequest> {
            new() { Id = System.Guid.NewGuid(), GuildId = query.GuildId, RequesterId = System.Guid.NewGuid(), Status = GuildJoinRequestStatus.Pending, Message = "m" },
            new() { Id = System.Guid.NewGuid(), GuildId = query.GuildId, RequesterId = System.Guid.NewGuid(), Status = GuildJoinRequestStatus.Accepted }
        };
        joinRepo.GetPendingRequestsByGuildAsync(query.GuildId, Arg.Any<CancellationToken>()).Returns(list.Where(r => r.Status == GuildJoinRequestStatus.Pending).ToList());
        profileRepo.GetByAuthIdAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { Username = "u" });

        var result = await sut.Handle(query, CancellationToken.None);
        result.Count.Should().Be(1);
        result[0].Status.Should().Be(GuildJoinRequestStatus.Pending);
        result[0].RequesterName.Should().Be("u");
    }
}