using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Application.Features.Parties.Queries.GetMyParties;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Queries.GetMyParties;

public class GetMyPartiesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsCreatedAndMemberParties()
    {
        var query = new GetMyPartiesQuery(System.Guid.NewGuid());
        var partyRepo = Substitute.For<IPartyRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new GetMyPartiesQueryHandler(partyRepo, memberRepo, mapper);

        var created = new Party { Id = System.Guid.NewGuid(), Name = "Created", CreatedBy = query.AuthUserId };
        partyRepo.GetPartiesByCreatorAsync(query.AuthUserId, Arg.Any<CancellationToken>()).Returns(new List<Party> { created });

        var membership = new PartyMember { PartyId = System.Guid.NewGuid(), AuthUserId = query.AuthUserId, Status = MemberStatus.Active };
        memberRepo.GetMembershipsByUserAsync(query.AuthUserId, Arg.Any<CancellationToken>()).Returns(new List<PartyMember> { membership });
        partyRepo.GetByIdAsync(membership.PartyId, Arg.Any<CancellationToken>()).Returns(new Party { Id = membership.PartyId, Name = "Member" });

        mapper.Map<PartyDto>(Arg.Any<Party>()).Returns(ci => { var p = ci.Arg<Party>(); return new PartyDto(p.Id, p.Name, null!, RogueLearn.User.Domain.Enums.PartyType.StudyGroup, 10, true, p.CreatedBy, p.CreatedAt); });

        var res = await sut.Handle(query, CancellationToken.None);
        res.Select(p => p.Name).Should().Contain(new[] { "Created", "Member" });
    }

    [Fact]
    public async Task Handle_DeduplicatesParties()
    {
        var query = new GetMyPartiesQuery(System.Guid.NewGuid());
        var partyRepo = Substitute.For<IPartyRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new GetMyPartiesQueryHandler(partyRepo, memberRepo, mapper);

        var samePartyId = System.Guid.NewGuid();
        var created = new Party { Id = samePartyId, Name = "Same", CreatedBy = query.AuthUserId };
        partyRepo.GetPartiesByCreatorAsync(query.AuthUserId, Arg.Any<CancellationToken>()).Returns(new List<Party> { created });

        var membership = new PartyMember { PartyId = samePartyId, AuthUserId = query.AuthUserId, Status = MemberStatus.Active };
        memberRepo.GetMembershipsByUserAsync(query.AuthUserId, Arg.Any<CancellationToken>()).Returns(new List<PartyMember> { membership });
        partyRepo.GetByIdAsync(samePartyId, Arg.Any<CancellationToken>()).Returns(created);

        mapper.Map<PartyDto>(Arg.Any<Party>()).Returns(ci => { var p = ci.Arg<Party>(); return new PartyDto(p.Id, p.Name, null!, RogueLearn.User.Domain.Enums.PartyType.StudyGroup, 10, true, p.CreatedBy, p.CreatedAt); });

        var res = await sut.Handle(query, CancellationToken.None);
        res.Count.Should().Be(1);
        res[0].Name.Should().Be("Same");
    }
}
