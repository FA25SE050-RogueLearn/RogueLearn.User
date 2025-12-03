using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using RogueLearn.User.Application.Features.Guilds.Queries.GetMyGuild;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Queries.GetMyGuild;

public class GetMyGuildQueryHandlerTests
{
    [Fact]
    public async Task Handle_NoActiveMembership_ReturnsNull()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var sut = new GetMyGuildQueryHandler(guildRepo, memberRepo);

        var authId = Guid.NewGuid();
        memberRepo.GetMembershipsByUserAsync(authId, Arg.Any<CancellationToken>())
            .Returns(new List<GuildMember>());

        var result = await sut.Handle(new GetMyGuildQuery(authId), CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReturnsGuildSummary()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var sut = new GetMyGuildQueryHandler(guildRepo, memberRepo);

        var authId = Guid.NewGuid();
        var guildId = Guid.NewGuid();
        var activeMembership = new GuildMember { GuildId = guildId, AuthUserId = authId, Status = MemberStatus.Active };
        memberRepo.GetMembershipsByUserAsync(authId, Arg.Any<CancellationToken>())
            .Returns(new List<GuildMember> { activeMembership });

        var guild = new Guild { Id = guildId, Name = "G", Description = "D", IsPublic = true, IsLecturerGuild = false, MaxMembers = 50, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = Guid.NewGuid() };
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(guild);
        memberRepo.CountMembersAsync(guildId, Arg.Any<CancellationToken>()).Returns(7);

        var result = await sut.Handle(new GetMyGuildQuery(authId), CancellationToken.None);
        result.Should().NotBeNull();
        result!.Id.Should().Be(guildId);
        result.MemberCount.Should().Be(7);
    }

    [Fact]
    public void Query_CarriesValue()
    {
        var id = Guid.NewGuid();
        var q = new GetMyGuildQuery(id);
        q.AuthUserId.Should().Be(id);
    }
}