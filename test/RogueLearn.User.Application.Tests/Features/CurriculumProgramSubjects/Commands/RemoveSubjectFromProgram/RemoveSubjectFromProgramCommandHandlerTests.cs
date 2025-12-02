using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.CurriculumProgramSubjects.Commands.RemoveSubjectFromProgram;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.CurriculumProgramSubjects.Commands.RemoveSubjectFromProgram;

public class RemoveSubjectFromProgramCommandHandlerTests
{
    private RemoveSubjectFromProgramCommandHandler CreateHandler(ICurriculumProgramSubjectRepository? mappingRepo = null)
    {
        mappingRepo ??= Substitute.For<ICurriculumProgramSubjectRepository>();
        var logger = Substitute.For<ILogger<RemoveSubjectFromProgramCommandHandler>>();
        return new RemoveSubjectFromProgramCommandHandler(mappingRepo, logger);
    }

    [Theory]
    [AutoData]
    public async Task Handle_MappingMissing_IsIdempotent(Guid programId, Guid subjectId)
    {
        var mappingRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        mappingRepo.FirstOrDefaultAsync(
                Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns((CurriculumProgramSubject?)null);

        var sut = CreateHandler(mappingRepo);
        var cmd = new RemoveSubjectFromProgramCommand { ProgramId = programId, SubjectId = subjectId };
        await sut.Handle(cmd, CancellationToken.None);

        await mappingRepo.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [AutoData]
    public async Task Handle_MappingExists_Deletes(Guid programId, Guid subjectId)
    {
        var mappingRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var mapping = new CurriculumProgramSubject { ProgramId = programId, SubjectId = subjectId, Id = Guid.NewGuid() };
        mappingRepo.FirstOrDefaultAsync(
                Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(mapping);

        var sut = CreateHandler(mappingRepo);
        var cmd = new RemoveSubjectFromProgramCommand { ProgramId = programId, SubjectId = subjectId };
        await sut.Handle(cmd, CancellationToken.None);

        await mappingRepo.Received(1).DeleteAsync(mapping.Id, Arg.Any<CancellationToken>());
    }
}