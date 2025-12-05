using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetAllCurriculumPrograms;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramById;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.CurriculumPrograms.Queries.GetCurriculumProgramById;

public class GetCurriculumProgramByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var repo = Substitute.For<ICurriculumProgramRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<GetCurriculumProgramByIdQueryHandler>>();
        var sut = new GetCurriculumProgramByIdQueryHandler(repo, mapper, logger);

        var query = new GetCurriculumProgramByIdQuery { Id = Guid.NewGuid() };
        repo.GetByIdAsync(query.Id, Arg.Any<CancellationToken>()).Returns((CurriculumProgram?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_ReturnsDto()
    {
        var repo = Substitute.For<ICurriculumProgramRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<GetCurriculumProgramByIdQueryHandler>>();
        var sut = new GetCurriculumProgramByIdQueryHandler(repo, mapper, logger);

        var query = new GetCurriculumProgramByIdQuery { Id = Guid.NewGuid() };
        var program = new CurriculumProgram { Id = query.Id, ProgramCode = "PC" };
        repo.GetByIdAsync(query.Id, Arg.Any<CancellationToken>()).Returns(program);
        var dto = new CurriculumProgramDto { Id = program.Id, ProgramCode = program.ProgramCode };
        mapper.Map<CurriculumProgramDto>(program).Returns(dto);

        var result = await sut.Handle(query, CancellationToken.None);
        result.Id.Should().Be(program.Id);
        result.ProgramCode.Should().Be("PC");
    }
}