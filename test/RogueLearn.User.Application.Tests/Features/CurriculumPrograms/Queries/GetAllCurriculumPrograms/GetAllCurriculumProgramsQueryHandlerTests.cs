using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetAllCurriculumPrograms;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.CurriculumPrograms.Queries.GetAllCurriculumPrograms;

public class GetAllCurriculumProgramsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedList()
    {
        var repo = Substitute.For<ICurriculumProgramRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<GetAllCurriculumProgramsQueryHandler>>();
        var sut = new GetAllCurriculumProgramsQueryHandler(repo, mapper, logger);

        var programs = new List<CurriculumProgram> { new() { Id = System.Guid.NewGuid(), ProgramCode = "PC" } };
        repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(programs);
        var dtos = new List<CurriculumProgramDto> { new() { Id = programs[0].Id, ProgramCode = "PC" } };
        mapper.Map<List<CurriculumProgramDto>>(programs).Returns(dtos);

        var result = await sut.Handle(new GetAllCurriculumProgramsQuery(), CancellationToken.None);
        result.Count.Should().Be(1);
        result[0].ProgramCode.Should().Be("PC");
    }
}