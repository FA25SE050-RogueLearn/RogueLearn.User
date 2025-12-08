using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Queries.GetMemberRoles;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Queries.GetMemberRoles;

public class GetGuildMemberRolesQueryHandlerTests
{
    [Fact]
    public async Task Handle_NoMember_ReturnsEmpty()
    {
        var repo = Substitute.For<IGuildMemberRepository>();
        var sut = new GetGuildMemberRolesQueryHandler(repo);
        var query = new GetGuildMemberRolesQuery(Guid.NewGuid(), Guid.NewGuid());
        repo.GetMemberAsync(query.GuildId, query.MemberAuthUserId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        var result = await sut.Handle(query, CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsRole()
    {
        var repo = Substitute.For<IGuildMemberRepository>();
        var sut = new GetGuildMemberRolesQueryHandler(repo);
        var query = new GetGuildMemberRolesQuery(Guid.NewGuid(), Guid.NewGuid());
        repo.GetMemberAsync(query.GuildId, query.MemberAuthUserId, Arg.Any<CancellationToken>()).Returns(new GuildMember { Role = GuildRole.Member });
        var result = await sut.Handle(query, CancellationToken.None);
        result.Should().ContainSingle().Which.Should().Be(GuildRole.Member);
    }
}
