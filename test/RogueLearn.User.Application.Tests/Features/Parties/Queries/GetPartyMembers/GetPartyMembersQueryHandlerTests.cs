using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Application.Features.Parties.Queries.GetPartyMembers;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Queries.GetPartyMembers;

public class GetPartyMembersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsEnrichedMembers()
    {
        var query = new GetPartyMembersQuery(System.Guid.NewGuid());
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var profileRepo = Substitute.For<IUserProfileRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new GetPartyMembersQueryHandler(memberRepo, profileRepo, mapper);

        var member = new PartyMember { Id = System.Guid.NewGuid(), PartyId = query.PartyId, AuthUserId = System.Guid.NewGuid(), Role = PartyRole.Member, Status = MemberStatus.Active };
        memberRepo.GetMembersByPartyAsync(query.PartyId, Arg.Any<CancellationToken>()).Returns(new List<PartyMember> { member });
        var profile = new UserProfile { AuthUserId = member.AuthUserId, Username = "u", Email = "e@example.com", FirstName = "F", LastName = "L", ProfileImageUrl = "img", Level = 2, ExperiencePoints = 100, Bio = "bio" };
        profileRepo.GetByAuthIdAsync(member.AuthUserId, Arg.Any<CancellationToken>()).Returns(profile);

        mapper.Map<PartyMemberDto>(Arg.Any<PartyMember>()).Returns(ci =>
            new PartyMemberDto(member.Id, member.PartyId, member.AuthUserId, member.Role, member.Status, member.JoinedAt, profile.Username, profile.Email, profile.FirstName, profile.LastName, profile.ProfileImageUrl, profile.Level, profile.ExperiencePoints, profile.Bio)
        );

        var result = await sut.Handle(query, CancellationToken.None);
        result.Should().HaveCount(1);
        result.First().Username.Should().Be("u");
        result.First().Role.Should().Be(PartyRole.Member);
    }
}