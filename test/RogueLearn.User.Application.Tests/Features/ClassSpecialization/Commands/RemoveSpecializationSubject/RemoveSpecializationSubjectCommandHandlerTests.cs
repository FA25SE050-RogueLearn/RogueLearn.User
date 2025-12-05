using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.ClassSpecialization.Commands.RemoveSpecializationSubject;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.ClassSpecialization.Commands.RemoveSpecializationSubject;

public class RemoveSpecializationSubjectCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var cmd = new RemoveSpecializationSubjectCommand { ClassId = System.Guid.NewGuid(), SubjectId = System.Guid.NewGuid() };
        var repo = Substitute.For<IClassSpecializationSubjectRepository>();
        var sut = new RemoveSpecializationSubjectCommandHandler(repo);
        repo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<System.Func<ClassSpecializationSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns((ClassSpecializationSubject?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_Deletes()
    {
        var cmd = new RemoveSpecializationSubjectCommand { ClassId = System.Guid.NewGuid(), SubjectId = System.Guid.NewGuid() };
        var repo = Substitute.For<IClassSpecializationSubjectRepository>();
        var sut = new RemoveSpecializationSubjectCommandHandler(repo);
        var mapping = new ClassSpecializationSubject { Id = System.Guid.NewGuid(), ClassId = cmd.ClassId, SubjectId = cmd.SubjectId };
        repo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<System.Func<ClassSpecializationSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(mapping);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).DeleteAsync(mapping.Id, Arg.Any<CancellationToken>());
    }
}