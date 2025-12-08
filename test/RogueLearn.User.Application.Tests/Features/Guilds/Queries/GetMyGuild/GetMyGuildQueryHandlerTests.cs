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
    public async Task NoActiveMembership_ReturnsNull()
    {
        var repo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        memberRepo.GetMembershipsByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<GuildMember>());
        var sut = new GetMyGuildQueryHandler(repo, memberRepo);
        var res = await sut.Handle(new GetMyGuildQuery(Guid.NewGuid()), CancellationToken.None);
        res.Should().BeNull();
    }

    [Fact]
    public async Task GuildMissing_ReturnsNull()
    {
        var repo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var gid = Guid.NewGuid();
        memberRepo.GetMembershipsByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new[] { new GuildMember { GuildId = gid, Status = MemberStatus.Active } });
        repo.GetByIdAsync(gid, Arg.Any<CancellationToken>()).Returns((Guild?)null);
        var sut = new GetMyGuildQueryHandler(repo, memberRepo);
        var res = await sut.Handle(new GetMyGuildQuery(Guid.NewGuid()), CancellationToken.None);
        res.Should().BeNull();
    }

    [Fact]
    public async Task ReturnsGuildDtoWithMemberCount()
    {
        var repo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var gid = Guid.NewGuid();
        memberRepo.GetMembershipsByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new[] { new GuildMember { GuildId = gid, Status = MemberStatus.Active } });
        var guild = new Guild { Id = gid, Name = "G", Description = "D", IsPublic = true, IsLecturerGuild = false, MaxMembers = 50 };
        repo.GetByIdAsync(gid, Arg.Any<CancellationToken>()).Returns(guild);
        memberRepo.CountMembersAsync(gid, Arg.Any<CancellationToken>()).Returns(3);
        var sut = new GetMyGuildQueryHandler(repo, memberRepo);
        var res = await sut.Handle(new GetMyGuildQuery(Guid.NewGuid()), CancellationToken.None);
        res!.Id.Should().Be(gid);
        res.MemberCount.Should().Be(3);
    }
}
