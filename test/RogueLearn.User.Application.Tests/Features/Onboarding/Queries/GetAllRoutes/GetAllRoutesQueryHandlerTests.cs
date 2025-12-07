using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Onboarding.Queries.GetAllRoutes;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Onboarding.Queries.GetAllRoutes;

public class GetAllRoutesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMapped()
    {
        var repo = Substitute.For<ICurriculumProgramRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<GetAllRoutesQueryHandler>>();
        var sut = new GetAllRoutesQueryHandler(repo, mapper, logger);

        var programs = new List<CurriculumProgram> { new() { Id = System.Guid.NewGuid() } };
        repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(programs);
        mapper.Map<List<RouteDto>>(programs).Returns(new List<RouteDto> { new() { Id = programs[0].Id } });

        var result = await sut.Handle(new GetAllRoutesQuery(), CancellationToken.None);
        result.Count.Should().Be(1);
        result[0].Id.Should().Be(programs[0].Id);
    }

    [Fact]
    public async Task Handle_DtosContainDescription()
    {
        var repo = Substitute.For<ICurriculumProgramRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<GetAllRoutesQueryHandler>>();
        var sut = new GetAllRoutesQueryHandler(repo, mapper, logger);

        var programs = new List<CurriculumProgram> { new() { Id = System.Guid.NewGuid(), Description = "desc" } };
        repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(programs);
        mapper.Map<List<RouteDto>>(programs).Returns(new List<RouteDto> { new() { Id = programs[0].Id, Description = "desc" } });

        var result = await sut.Handle(new GetAllRoutesQuery(), CancellationToken.None);
        result.Should().HaveCount(1);
        result[0].Description.Should().Be("desc");
    }
}
