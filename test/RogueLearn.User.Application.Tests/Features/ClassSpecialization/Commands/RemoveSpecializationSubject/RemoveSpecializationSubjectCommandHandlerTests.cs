using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.ClassSpecialization.Commands.RemoveSpecializationSubject;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.ClassSpecialization.Commands.RemoveSpecializationSubject;

public class RemoveSpecializationSubjectCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_NotFound_Throws(RemoveSpecializationSubjectCommand cmd)
    {
        var repo = Substitute.For<IClassSpecializationSubjectRepository>();
        var sut = new RemoveSpecializationSubjectCommandHandler(repo);
        repo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<System.Func<ClassSpecializationSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns((ClassSpecializationSubject?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_Deletes(RemoveSpecializationSubjectCommand cmd)
    {
        var repo = Substitute.For<IClassSpecializationSubjectRepository>();
        var sut = new RemoveSpecializationSubjectCommandHandler(repo);
        var mapping = new ClassSpecializationSubject { Id = System.Guid.NewGuid(), ClassId = cmd.ClassId, SubjectId = cmd.SubjectId };
        repo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<System.Func<ClassSpecializationSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(mapping);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).DeleteAsync(mapping.Id, Arg.Any<CancellationToken>());
    }
}