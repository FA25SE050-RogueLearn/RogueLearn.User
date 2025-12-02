using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Queries.GetGuildInvitations;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Queries.GetGuildInvitations;

public class GetGuildInvitationsQueryHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_ReturnsMapped(GetGuildInvitationsQuery query)
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var profileRepo = Substitute.For<IUserProfileRepository>();
        var sut = new GetGuildInvitationsQueryHandler(invRepo, guildRepo, profileRepo);

        var invs = new List<GuildInvitation> { new() { Id = System.Guid.NewGuid(), GuildId = query.GuildId, InviteeId = System.Guid.NewGuid(), Status = InvitationStatus.Pending, Message = "m" } };
        invRepo.GetInvitationsByGuildAsync(query.GuildId, Arg.Any<CancellationToken>()).Returns(invs);
        guildRepo.GetByIdsAsync(Arg.Any<IEnumerable<System.Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<Guild> { new() { Id = query.GuildId, Name = "G" } });
        profileRepo.GetByAuthIdAsync(invs[0].InviteeId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Username = "u" });

        var result = await sut.Handle(query, CancellationToken.None);
        result.Count().Should().Be(1);
        result.First().GuildName.Should().Be("G");
        result.First().InviteeName.Should().Be("u");
    }
}