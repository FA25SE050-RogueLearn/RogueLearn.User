using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Application.Features.Parties.Queries.GetAllParties;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Queries.GetAllParties;

public class GetAllPartiesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMapped()
    {
        var repo = Substitute.For<IPartyRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new GetAllPartiesQueryHandler(repo, mapper);
        var items = new List<Party> { new() { Id = System.Guid.NewGuid(), Name = "P" } };
        repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(items);
        mapper.Map<PartyDto>(items.First()).Returns(new PartyDto(items.First().Id, "P", null!, RogueLearn.User.Domain.Enums.PartyType.StudyGroup, 10, true, System.Guid.NewGuid(), System.DateTimeOffset.UtcNow));
        var result = await sut.Handle(new GetAllPartiesQuery(), CancellationToken.None);
        result.Count.Should().Be(1);
        result.First().Name.Should().Be("P");
    }
}