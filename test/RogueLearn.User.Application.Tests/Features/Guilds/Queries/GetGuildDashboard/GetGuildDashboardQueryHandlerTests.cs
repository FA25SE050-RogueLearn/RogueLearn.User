using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Guilds.Queries.GetGuildDashboard;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Queries.GetGuildDashboard;

public class GetGuildDashboardQueryHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var query = new GetGuildDashboardQuery(System.Guid.NewGuid());
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var sut = new GetGuildDashboardQueryHandler(guildRepo, memberRepo, invRepo);

        guildRepo.GetByIdAsync(query.GuildId, Arg.Any<CancellationToken>()).Returns((Guild?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_ComputesCounts()
    {
        var query = new GetGuildDashboardQuery(System.Guid.NewGuid());
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var sut = new GetGuildDashboardQueryHandler(guildRepo, memberRepo, invRepo);

        guildRepo.GetByIdAsync(query.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = query.GuildId, Name = "G", MaxMembers = 50 });
        memberRepo.CountActiveMembersAsync(query.GuildId, Arg.Any<CancellationToken>()).Returns(7);
        invRepo.GetInvitationsByGuildAsync(query.GuildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation> {
            new() { Status = InvitationStatus.Pending },
            new() { Status = InvitationStatus.Accepted },
            new() { Status = InvitationStatus.Accepted }
        });

        var result = await sut.Handle(query, CancellationToken.None);
        result.ActiveMemberCount.Should().Be(7);
        result.PendingInvitationCount.Should().Be(1);
        result.AcceptedInvitationCount.Should().Be(2);
        result.MaxMembers.Should().Be(50);
    }
}