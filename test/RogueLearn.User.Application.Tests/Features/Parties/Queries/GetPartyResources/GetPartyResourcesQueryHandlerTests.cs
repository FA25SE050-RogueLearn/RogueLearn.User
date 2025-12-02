using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Application.Features.Parties.Queries.GetPartyResources;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Queries.GetPartyResources;

public class GetPartyResourcesQueryHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_ReturnsMapped(GetPartyResourcesQuery query)
    {
        var repo = Substitute.For<IPartyStashItemRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new GetPartyResourcesQueryHandler(repo, mapper);
        var items = new List<PartyStashItem> { new() { Id = System.Guid.NewGuid(), PartyId = query.PartyId, Title = "T", Content = new object() } };
        repo.GetResourcesByPartyAsync(query.PartyId, Arg.Any<CancellationToken>()).Returns(items);
        mapper.Map<PartyStashItemDto>(items.First()).Returns(new PartyStashItemDto(items.First().Id, query.PartyId, System.Guid.NewGuid(), System.Guid.NewGuid(), "T", new object(), new[] { "tag" }, System.DateTimeOffset.UtcNow, System.DateTimeOffset.UtcNow));
        var result = await sut.Handle(query, CancellationToken.None);
        result.Count.Should().Be(1);
        result.First().Title.Should().Be("T");
    }
}