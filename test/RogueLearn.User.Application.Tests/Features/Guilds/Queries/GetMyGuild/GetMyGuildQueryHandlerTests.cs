using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
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
        memberRepo.GetMembershipsByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember>
        {
            new() { GuildId = Guid.NewGuid(), AuthUserId = authId, Status = MemberStatus.Left }
        });

        var res = await sut.Handle(new GetMyGuildQuery(authId), CancellationToken.None);
        res.Should().BeNull();
    }

    [Fact]
    public async Task Handle_GuildNull_ReturnsNull()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var sut = new GetMyGuildQueryHandler(guildRepo, memberRepo);

        var authId = Guid.NewGuid();
        var guildId = Guid.NewGuid();
        memberRepo.GetMembershipsByUserAsync(authId, Arg.Any<CancellationToken>())
            .Returns(new List<GuildMember> { new() { GuildId = guildId, AuthUserId = authId, Status = MemberStatus.Active } });
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns((Guild?)null);

        var res = await sut.Handle(new GetMyGuildQuery(authId), CancellationToken.None);
        res.Should().BeNull();
    }
}

