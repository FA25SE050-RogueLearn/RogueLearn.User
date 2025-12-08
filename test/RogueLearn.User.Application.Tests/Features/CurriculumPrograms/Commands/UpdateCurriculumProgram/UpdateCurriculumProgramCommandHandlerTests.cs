using System.Threading;
using System.Threading.Tasks;
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
    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var repo = Substitute.For<ICurriculumProgramRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new UpdateCurriculumProgramCommandHandler(repo, mapper);
        var cmd = new UpdateCurriculumProgramCommand { Id = System.Guid.NewGuid(), ProgramName = "N", ProgramCode = "NC", DegreeLevel = DegreeLevel.Master };
        repo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns((CurriculumProgram?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_Updates()
    {
        var repo = Substitute.For<ICurriculumProgramRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new UpdateCurriculumProgramCommandHandler(repo, mapper);
        var id = System.Guid.NewGuid();
        var cmd = new UpdateCurriculumProgramCommand { Id = id, ProgramName = "New", ProgramCode = "NC", DegreeLevel = DegreeLevel.Master };
        var program = new CurriculumProgram { Id = id, ProgramName = "Old", ProgramCode = "OC", DegreeLevel = DegreeLevel.Bachelor };
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(program);
        repo.UpdateAsync(Arg.Any<CurriculumProgram>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<CurriculumProgram>());
        mapper.Map<UpdateCurriculumProgramResponse>(Arg.Any<CurriculumProgram>()).Returns(new UpdateCurriculumProgramResponse { Id = cmd.Id, ProgramName = cmd.ProgramName, ProgramCode = cmd.ProgramCode, DegreeLevel = cmd.DegreeLevel });
        var resp = await sut.Handle(cmd, CancellationToken.None);
        resp.ProgramName.Should().Be("New");
    }
}