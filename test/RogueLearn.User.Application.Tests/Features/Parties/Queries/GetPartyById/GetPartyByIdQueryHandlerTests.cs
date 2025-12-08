using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Application.Features.Parties.Queries.GetPartyById;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Queries.GetPartyById;

public class GetPartyByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMapped()
    {
        var query = new GetPartyByIdQuery(System.Guid.NewGuid());
        var repo = Substitute.For<IPartyRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new GetPartyByIdQueryHandler(repo, mapper);
        var party = new Party { Id = query.PartyId, Name = "P" };
        repo.GetByIdAsync(query.PartyId, Arg.Any<CancellationToken>()).Returns(party);
        mapper.Map<PartyDto>(party).Returns(new PartyDto(query.PartyId, party.Name, null!, RogueLearn.User.Domain.Enums.PartyType.StudyGroup, 10, true, System.Guid.NewGuid(), System.DateTimeOffset.UtcNow));
        var res = await sut.Handle(query, CancellationToken.None);
        res!.Id.Should().Be(query.PartyId);
        res.Name.Should().Be("P");
    }
}