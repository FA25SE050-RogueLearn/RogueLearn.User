using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.CurriculumPrograms.Commands.DeleteCurriculumProgram;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.CurriculumPrograms.Commands.DeleteCurriculumProgram;

public class DeleteCurriculumProgramCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var repo = Substitute.For<ICurriculumProgramRepository>();
        var sut = new DeleteCurriculumProgramCommandHandler(repo);
        var cmd = new DeleteCurriculumProgramCommand { Id = System.Guid.NewGuid() };
        repo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns((CurriculumProgram?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_Deletes()
    {
        var repo = Substitute.For<ICurriculumProgramRepository>();
        var sut = new DeleteCurriculumProgramCommandHandler(repo);
        var id = System.Guid.NewGuid();
        var cmd = new DeleteCurriculumProgramCommand { Id = id };
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new CurriculumProgram { Id = id });
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }
}