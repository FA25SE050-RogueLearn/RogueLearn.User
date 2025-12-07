using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Application.Features.Parties.Queries.GetPartyResourceById;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Queries.GetPartyResourceById;

public class GetPartyResourceByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_Forbidden_ReturnsNull()
    {
        var query = new GetPartyResourceByIdQuery(System.Guid.NewGuid(), System.Guid.NewGuid());
        var repo = Substitute.For<IPartyStashItemRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new GetPartyResourceByIdQueryHandler(repo, mapper);
        repo.GetByIdAsync(query.StashItemId, Arg.Any<CancellationToken>()).Returns(new PartyStashItem { Id = query.StashItemId, PartyId = System.Guid.NewGuid() });
        var res = await sut.Handle(query, CancellationToken.None);
        res.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Success_ReturnsDto()
    {
        var partyId = System.Guid.NewGuid();
        var stashId = System.Guid.NewGuid();
        var query = new GetPartyResourceByIdQuery(partyId, stashId);
        var repo = Substitute.For<IPartyStashItemRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new GetPartyResourceByIdQueryHandler(repo, mapper);
        var item = new PartyStashItem { Id = query.StashItemId, PartyId = query.PartyId, Title = "T", Content = new object() };
        repo.GetByIdAsync(query.StashItemId, Arg.Any<CancellationToken>()).Returns(item);
        mapper.Map<PartyStashItemDto>(item).Returns(new PartyStashItemDto(item.Id, item.PartyId, System.Guid.NewGuid(), System.Guid.NewGuid(), "T", new object(), new[] { "tag" }, System.DateTimeOffset.UtcNow, System.DateTimeOffset.UtcNow));
        var res = await sut.Handle(query, CancellationToken.None);
        res!.Title.Should().Be("T");
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsNull()
    {
        var query = new GetPartyResourceByIdQuery(System.Guid.NewGuid(), System.Guid.NewGuid());
        var repo = Substitute.For<IPartyStashItemRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new GetPartyResourceByIdQueryHandler(repo, mapper);
        repo.GetByIdAsync(query.StashItemId, Arg.Any<CancellationToken>()).Returns((PartyStashItem?)null);
        var res = await sut.Handle(query, CancellationToken.None);
        res.Should().BeNull();
    }
}
