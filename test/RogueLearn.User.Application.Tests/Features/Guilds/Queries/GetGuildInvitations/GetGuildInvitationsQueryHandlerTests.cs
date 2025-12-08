using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    [Fact]
    public async Task Handle_ReturnsMapped()
    {
        var query = new GetGuildInvitationsQuery(System.Guid.NewGuid());
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

    [Fact]
    public async Task Handle_Empty_ReturnsEmpty()
    {
        var query = new GetGuildInvitationsQuery(System.Guid.NewGuid());
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var profileRepo = Substitute.For<IUserProfileRepository>();
        var sut = new GetGuildInvitationsQueryHandler(invRepo, guildRepo, profileRepo);

        invRepo.GetInvitationsByGuildAsync(query.GuildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation>());

        var result = await sut.Handle(query, CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MultipleItems_MapsNamesAndStatuses()
    {
        var query = new GetGuildInvitationsQuery(System.Guid.NewGuid());
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var profileRepo = Substitute.For<IUserProfileRepository>();
        var sut = new GetGuildInvitationsQueryHandler(invRepo, guildRepo, profileRepo);

        var invitee1 = System.Guid.NewGuid();
        var invitee2 = System.Guid.NewGuid();
        var invs = new List<GuildInvitation>
        {
            new() { Id = System.Guid.NewGuid(), GuildId = query.GuildId, InviteeId = invitee1, Status = InvitationStatus.Pending, Message = "m1" },
            new() { Id = System.Guid.NewGuid(), GuildId = query.GuildId, InviteeId = invitee2, Status = InvitationStatus.Accepted, Message = "m2" }
        };

        invRepo.GetInvitationsByGuildAsync(query.GuildId, Arg.Any<CancellationToken>()).Returns(invs);
        guildRepo.GetByIdsAsync(Arg.Any<IEnumerable<System.Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<Guild> { new() { Id = query.GuildId, Name = "G" } });
        profileRepo.GetByAuthIdAsync(invitee1, Arg.Any<CancellationToken>()).Returns(new UserProfile { FirstName = "Ada", LastName = "Lovelace" });
        profileRepo.GetByAuthIdAsync(invitee2, Arg.Any<CancellationToken>()).Returns(new UserProfile { Username = "user2" });

        var result = (await sut.Handle(query, CancellationToken.None)).ToList();
        result.Count.Should().Be(2);
        result[0].GuildName.Should().Be("G");
        result[0].InviteeName.Should().Be("Ada Lovelace");
        result[0].Status.Should().Be(InvitationStatus.Pending);
        result[1].GuildName.Should().Be("G");
        result[1].InviteeName.Should().Be("user2");
        result[1].Status.Should().Be(InvitationStatus.Accepted);
    }

    [Fact]
    public async Task Handle_MissingProfileAndGuild_FallbacksToEmptyStrings()
    {
        var query = new GetGuildInvitationsQuery(System.Guid.NewGuid());
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var profileRepo = Substitute.For<IUserProfileRepository>();
        var sut = new GetGuildInvitationsQueryHandler(invRepo, guildRepo, profileRepo);

        var invitee = System.Guid.NewGuid();
        var otherGuild = System.Guid.NewGuid();
        var invs = new List<GuildInvitation>
        {
            new() { Id = System.Guid.NewGuid(), GuildId = otherGuild, InviteeId = invitee, Status = InvitationStatus.Pending, Message = "m" }
        };

        invRepo.GetInvitationsByGuildAsync(query.GuildId, Arg.Any<CancellationToken>()).Returns(invs);
        guildRepo.GetByIdsAsync(Arg.Any<IEnumerable<System.Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<Guild>()); // missing guild
        profileRepo.GetByAuthIdAsync(invitee, Arg.Any<CancellationToken>()).Returns((UserProfile?)null); // missing profile

        var result = (await sut.Handle(query, CancellationToken.None)).ToList();
        result.Count.Should().Be(1);
        result[0].GuildName.Should().BeEmpty();
        result[0].InviteeName.Should().BeEmpty();
    }
}
