using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Application.Features.Parties.Queries.GetAllParties;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Queries.GetAllParties;

public class GetAllPartiesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedList()
    {
        var repo = Substitute.For<IPartyRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new GetAllPartiesQueryHandler(repo, mapper);

        var p1 = new Party { Id = System.Guid.NewGuid(), Name = "Alpha", PartyType = PartyType.ProjectTeam };
        var p2 = new Party { Id = System.Guid.NewGuid(), Name = "Beta", PartyType = PartyType.StudyGroup };
        repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Party> { p1, p2 });

        mapper.Map<PartyDto>(p1).Returns(new PartyDto(p1.Id, p1.Name, p1.Description ?? string.Empty, p1.PartyType, p1.MaxMembers, p1.IsPublic, p1.CreatedBy, p1.CreatedAt));
        mapper.Map<PartyDto>(p2).Returns(new PartyDto(p2.Id, p2.Name, p2.Description ?? string.Empty, p2.PartyType, p2.MaxMembers, p2.IsPublic, p2.CreatedBy, p2.CreatedAt));

        var res = await sut.Handle(new GetAllPartiesQuery(), CancellationToken.None);
        res.Should().HaveCount(2);
        res.Should().ContainEquivalentOf(new PartyDto(p1.Id, p1.Name, p1.Description ?? string.Empty, p1.PartyType, p1.MaxMembers, p1.IsPublic, p1.CreatedBy, p1.CreatedAt));
        res.Should().ContainEquivalentOf(new PartyDto(p2.Id, p2.Name, p2.Description ?? string.Empty, p2.PartyType, p2.MaxMembers, p2.IsPublic, p2.CreatedBy, p2.CreatedAt));
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmpty()
    {
        var repo = Substitute.For<IPartyRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new GetAllPartiesQueryHandler(repo, mapper);

        repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Party>());
        var res = await sut.Handle(new GetAllPartiesQuery(), CancellationToken.None);
        res.Should().NotBeNull();
        res.Should().BeEmpty();
    }
}
