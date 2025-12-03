using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.CurriculumPrograms.Commands.DeleteCurriculumProgram;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.CurriculumPrograms.Commands.DeleteCurriculumProgram;

public class DeleteCurriculumProgramCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_NotFound_Throws(DeleteCurriculumProgramCommand cmd)
    {
        var repo = Substitute.For<ICurriculumProgramRepository>();
        var sut = new DeleteCurriculumProgramCommandHandler(repo);
        repo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns((CurriculumProgram?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_Deletes(DeleteCurriculumProgramCommand cmd)
    {
        var repo = Substitute.For<ICurriculumProgramRepository>();
        var sut = new DeleteCurriculumProgramCommandHandler(repo);
        repo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns(new CurriculumProgram { Id = cmd.Id });
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).DeleteAsync(cmd.Id, Arg.Any<CancellationToken>());
    }
}