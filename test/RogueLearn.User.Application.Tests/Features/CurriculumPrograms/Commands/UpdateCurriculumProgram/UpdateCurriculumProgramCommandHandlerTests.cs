using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.CurriculumPrograms.Commands.UpdateCurriculumProgram;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.CurriculumPrograms.Commands.UpdateCurriculumProgram;

public class UpdateCurriculumProgramCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_NotFound_Throws(UpdateCurriculumProgramCommand cmd)
    {
        var repo = Substitute.For<ICurriculumProgramRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new UpdateCurriculumProgramCommandHandler(repo, mapper);
        repo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns((CurriculumProgram?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_Updates(UpdateCurriculumProgramCommand cmd)
    {
        var repo = Substitute.For<ICurriculumProgramRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new UpdateCurriculumProgramCommandHandler(repo, mapper);
        var program = new CurriculumProgram { Id = cmd.Id, ProgramName = "Old", ProgramCode = "OC", DegreeLevel = DegreeLevel.Bachelor };
        repo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns(program);
        repo.UpdateAsync(Arg.Any<CurriculumProgram>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<CurriculumProgram>());

        cmd.ProgramName = "New";
        cmd.ProgramCode = "NC";
        cmd.DegreeLevel = DegreeLevel.Master;
        mapper.Map<UpdateCurriculumProgramResponse>(Arg.Any<CurriculumProgram>()).Returns(new UpdateCurriculumProgramResponse { Id = cmd.Id, ProgramName = cmd.ProgramName, ProgramCode = cmd.ProgramCode, DegreeLevel = cmd.DegreeLevel });
        var resp = await sut.Handle(cmd, CancellationToken.None);
        resp.ProgramName.Should().Be("New");
    }
}