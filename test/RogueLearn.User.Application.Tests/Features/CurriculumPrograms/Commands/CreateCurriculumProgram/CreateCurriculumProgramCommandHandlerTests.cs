using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.CurriculumPrograms.Commands.CreateCurriculumProgram;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.CurriculumPrograms.Commands.CreateCurriculumProgram;

public class CreateCurriculumProgramCommandHandlerTests
{
    [Fact]
    public async Task Handle_Success_ReturnsResponse()
    {
        var repo = Substitute.For<ICurriculumProgramRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new CreateCurriculumProgramCommandHandler(repo, mapper);
        var cmd = new CreateCurriculumProgramCommand
        {
            ProgramName = "PN",
            ProgramCode = "PC",
            DegreeLevel = DegreeLevel.Bachelor
        };
        var created = new CurriculumProgram { Id = System.Guid.NewGuid(), ProgramName = cmd.ProgramName, ProgramCode = cmd.ProgramCode, DegreeLevel = cmd.DegreeLevel };
        repo.AddAsync(Arg.Any<CurriculumProgram>(), Arg.Any<CancellationToken>()).Returns(created);
        mapper.Map<CreateCurriculumProgramResponse>(created).Returns(new CreateCurriculumProgramResponse { Id = created.Id, ProgramName = created.ProgramName, ProgramCode = created.ProgramCode, DegreeLevel = created.DegreeLevel });

        var resp = await sut.Handle(cmd, CancellationToken.None);
        resp.Id.Should().Be(created.Id);
        resp.ProgramCode.Should().Be("PC");
    }
}