using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Queries.GetMyGuildInvitations;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Queries.GetMyGuildInvitations;

public class GetMyGuildInvitationsQueryHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_ReturnsMappedAndFilters(GetMyGuildInvitationsQuery query)
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var profileRepo = Substitute.For<IUserProfileRepository>();
        var sut = new GetMyGuildInvitationsQueryHandler(invRepo, guildRepo, profileRepo);

        query = new GetMyGuildInvitationsQuery(query.AuthUserId, true);
        var invs = new List<GuildInvitation> {
            new() { Id = System.Guid.NewGuid(), GuildId = System.Guid.NewGuid(), InviteeId = query.AuthUserId, Status = InvitationStatus.Pending },
            new() { Id = System.Guid.NewGuid(), GuildId = System.Guid.NewGuid(), InviteeId = query.AuthUserId, Status = InvitationStatus.Accepted }
        };
        invRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<System.Func<GuildInvitation, bool>>>(), Arg.Any<CancellationToken>()).Returns(invs);
        guildRepo.GetByIdsAsync(Arg.Any<IEnumerable<System.Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<Guild> { new() { Id = invs[0].GuildId, Name = "G" } });
        profileRepo.GetByAuthIdAsync(query.AuthUserId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Username = "u" });

        var result = await sut.Handle(query, CancellationToken.None);
        result.Count.Should().Be(1);
        result[0].Status.Should().Be(InvitationStatus.Pending);
    }
}