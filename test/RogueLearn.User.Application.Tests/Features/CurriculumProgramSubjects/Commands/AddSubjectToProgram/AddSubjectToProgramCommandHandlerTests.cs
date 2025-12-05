using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.CurriculumProgramSubjects.Commands.AddSubjectToProgram;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.CurriculumProgramSubjects.Commands.AddSubjectToProgram;

public class AddSubjectToProgramCommandHandlerTests
{
    private AddSubjectToProgramCommandHandler CreateHandler(
        ICurriculumProgramRepository? programRepo = null,
        ISubjectRepository? subjectRepo = null,
        ICurriculumProgramSubjectRepository? mappingRepo = null)
    {
        programRepo ??= Substitute.For<ICurriculumProgramRepository>();
        subjectRepo ??= Substitute.For<ISubjectRepository>();
        mappingRepo ??= Substitute.For<ICurriculumProgramSubjectRepository>();
        var logger = Substitute.For<ILogger<AddSubjectToProgramCommandHandler>>();
        return new AddSubjectToProgramCommandHandler(programRepo, subjectRepo, mappingRepo, logger);
    }

    [Fact]
    public async Task Handle_ProgramNotFound_ThrowsNotFound()
    {
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var mappingRepo = Substitute.For<ICurriculumProgramSubjectRepository>();

        var programId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        programRepo.ExistsAsync(programId, Arg.Any<CancellationToken>()).Returns(false);
        var sut = CreateHandler(programRepo, subjectRepo, mappingRepo);

        var cmd = new AddSubjectToProgramCommand { ProgramId = programId, SubjectId = subjectId };
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SubjectNotFound_ThrowsNotFound()
    {
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var mappingRepo = Substitute.For<ICurriculumProgramSubjectRepository>();

        var programId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        programRepo.ExistsAsync(programId, Arg.Any<CancellationToken>()).Returns(true);
        subjectRepo.ExistsAsync(subjectId, Arg.Any<CancellationToken>()).Returns(false);

        var sut = CreateHandler(programRepo, subjectRepo, mappingRepo);

        var cmd = new AddSubjectToProgramCommand { ProgramId = programId, SubjectId = subjectId };
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MappingExists_ThrowsConflict()
    {
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var mappingRepo = Substitute.For<ICurriculumProgramSubjectRepository>();

        var programId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        programRepo.ExistsAsync(programId, Arg.Any<CancellationToken>()).Returns(true);
        subjectRepo.ExistsAsync(subjectId, Arg.Any<CancellationToken>()).Returns(true);
        mappingRepo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>())
                   .Returns(true);

        var sut = CreateHandler(programRepo, subjectRepo, mappingRepo);
        var cmd = new AddSubjectToProgramCommand { ProgramId = programId, SubjectId = subjectId };

        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_ReturnsResponse()
    {
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var mappingRepo = Substitute.For<ICurriculumProgramSubjectRepository>();

        var programId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        programRepo.ExistsAsync(programId, Arg.Any<CancellationToken>()).Returns(true);
        subjectRepo.ExistsAsync(subjectId, Arg.Any<CancellationToken>()).Returns(true);
        mappingRepo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>())
                   .Returns(false);

        var newMap = new CurriculumProgramSubject
        {
            ProgramId = programId,
            SubjectId = subjectId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        mappingRepo.AddAsync(Arg.Any<CurriculumProgramSubject>(), Arg.Any<CancellationToken>())
                   .Returns(newMap);

        var sut = CreateHandler(programRepo, subjectRepo, mappingRepo);
        var cmd = new AddSubjectToProgramCommand { ProgramId = programId, SubjectId = subjectId };
        var resp = await sut.Handle(cmd, CancellationToken.None);

        resp.ProgramId.Should().Be(newMap.ProgramId);
        resp.SubjectId.Should().Be(newMap.SubjectId);
        resp.CreatedAt.Should().Be(newMap.CreatedAt);
        await mappingRepo.Received(1).AddAsync(Arg.Any<CurriculumProgramSubject>(), Arg.Any<CancellationToken>());
    }
}