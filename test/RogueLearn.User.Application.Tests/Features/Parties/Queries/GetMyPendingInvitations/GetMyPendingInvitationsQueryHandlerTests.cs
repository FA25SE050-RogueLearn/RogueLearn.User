using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Application.Features.Parties.Queries.GetMyPendingInvitations;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Queries.GetMyPendingInvitations;

public class GetMyPendingInvitationsQueryHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_ReturnsMapped(GetMyPendingInvitationsQuery query)
    {
        var invRepo = Substitute.For<IPartyInvitationRepository>();
        var partyRepo = Substitute.For<IPartyRepository>();
        var profileRepo = Substitute.For<IUserProfileRepository>();
        var sut = new GetMyPendingInvitationsQueryHandler(invRepo, partyRepo, profileRepo);

        var invs = new List<PartyInvitation> { new() { Id = System.Guid.NewGuid(), PartyId = System.Guid.NewGuid(), InviteeId = query.AuthUserId, InviterId = System.Guid.NewGuid(), Status = InvitationStatus.Pending, Message = "m" } };
        invRepo.GetPendingInvitationsByInviteeAsync(query.AuthUserId, Arg.Any<CancellationToken>()).Returns(invs);
        partyRepo.GetByIdsAsync(Arg.Any<IEnumerable<System.Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<Party> { new() { Id = invs[0].PartyId, Name = "P" } });
        profileRepo.GetByAuthIdAsync(query.AuthUserId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Username = "u" });

        var result = await sut.Handle(query, CancellationToken.None);
        result.Count.Should().Be(1);
        result.First().PartyName.Should().Be("P");
        result.First().InviteeName.Should().Be("u");
    }
}