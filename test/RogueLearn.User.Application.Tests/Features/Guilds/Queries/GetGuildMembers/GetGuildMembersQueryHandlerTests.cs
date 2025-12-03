using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using RogueLearn.User.Application.Features.Guilds.Queries.GetGuildMembers;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Queries.GetGuildMembers;

public class GetGuildMembersQueryHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_ReturnsMapped(GetGuildMembersQuery query)
    {
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var profileRepo = Substitute.For<IUserProfileRepository>();
        var sut = new GetGuildMembersQueryHandler(memberRepo, profileRepo);

        var members = new List<GuildMember> { new() { GuildId = query.GuildId, AuthUserId = System.Guid.NewGuid(), Role = RogueLearn.User.Domain.Enums.GuildRole.Member } };
        memberRepo.GetMembersByGuildAsync(query.GuildId, Arg.Any<CancellationToken>()).Returns(members);
        profileRepo.GetByAuthIdAsync(members[0].AuthUserId, Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = members[0].AuthUserId, Username = "u" });

        var result = await sut.Handle(query, CancellationToken.None);
        result.Count().Should().Be(1);
        result.First().Role.Should().Be(RogueLearn.User.Domain.Enums.GuildRole.Member);
    }
}