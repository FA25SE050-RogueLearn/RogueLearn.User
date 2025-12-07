using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramDetails;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.CurriculumPrograms.Queries.GetCurriculumProgramDetails;

public class GetCurriculumProgramDetailsQueryHandlerTests
{
    [Fact]
    public async Task Handle_NoProgramId_ThrowsBadRequest()
    {
        var cpRepo = Substitute.For<ICurriculumProgramRepository>();
        var cpsRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var subjRepo = Substitute.For<ISubjectRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<GetCurriculumProgramDetailsQueryHandler>>();
        var sut = new GetCurriculumProgramDetailsQueryHandler(cpRepo, cpsRepo, subjRepo, mapper, logger);

        var q = new GetCurriculumProgramDetailsQuery { ProgramId = null };
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(q, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var cpRepo = Substitute.For<ICurriculumProgramRepository>();
        var cpsRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var subjRepo = Substitute.For<ISubjectRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<GetCurriculumProgramDetailsQueryHandler>>();
        var sut = new GetCurriculumProgramDetailsQueryHandler(cpRepo, cpsRepo, subjRepo, mapper, logger);

        var programId = Guid.NewGuid();
        cpRepo.GetByIdAsync(programId, Arg.Any<CancellationToken>()).Returns((CurriculumProgram?)null);
        var q = new GetCurriculumProgramDetailsQuery { ProgramId = programId, VersionId = Guid.NewGuid() };
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(q, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoSubjects_ReturnsAnalysis()
    {
        var cpRepo = Substitute.For<ICurriculumProgramRepository>();
        var cpsRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var subjRepo = Substitute.For<ISubjectRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<GetCurriculumProgramDetailsQueryHandler>>();
        var sut = new GetCurriculumProgramDetailsQueryHandler(cpRepo, cpsRepo, subjRepo, mapper, logger);

        var programId = Guid.NewGuid();
        var program = new CurriculumProgram { Id = programId, ProgramCode = "PC", CreatedAt = DateTimeOffset.UtcNow };
        cpRepo.GetByIdAsync(programId, Arg.Any<CancellationToken>()).Returns(program);
        mapper.Map<CurriculumProgramDetailsResponse>(program).Returns(new CurriculumProgramDetailsResponse { Id = programId });
        cpsRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>())
               .Returns(Array.Empty<CurriculumProgramSubject>());

        var q = new GetCurriculumProgramDetailsQuery { ProgramId = programId };
        var resp = await sut.Handle(q, CancellationToken.None);
        resp.CurriculumVersions.Count.Should().Be(0);
        resp.Analysis.TotalVersions.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithSubjects_ComputesAnalysis()
    {
        var cpRepo = Substitute.For<ICurriculumProgramRepository>();
        var cpsRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var subjRepo = Substitute.For<ISubjectRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<GetCurriculumProgramDetailsQueryHandler>>();

        var programId = Guid.NewGuid();
        var program = new CurriculumProgram { Id = programId, ProgramCode = "PC", CreatedAt = DateTimeOffset.UtcNow };
        cpRepo.GetByIdAsync(programId, Arg.Any<CancellationToken>()).Returns(program);
        mapper.Map<CurriculumProgramDetailsResponse>(program).Returns(new CurriculumProgramDetailsResponse { Id = programId });

        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();
        cpsRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>())
               .Returns(new[]
               {
                   new CurriculumProgramSubject { ProgramId = programId, SubjectId = s1 },
                   new CurriculumProgramSubject { ProgramId = programId, SubjectId = s2 }
               });

        subjRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            new Subject { Id = s1, SubjectCode = "S1", SubjectName = "A", Credits = 3, Semester = 1, Content = new Dictionary<string, object> { ["SessionSchedule"] = new object[] { new { SessionNumber = 1 } } } },
            new Subject { Id = s2, SubjectCode = "S2", SubjectName = "B", Credits = 3, Semester = 2, Content = null }
        });

        var sut = new GetCurriculumProgramDetailsQueryHandler(cpRepo, cpsRepo, subjRepo, mapper, logger);
        var resp = await sut.Handle(new GetCurriculumProgramDetailsQuery { ProgramId = programId }, CancellationToken.None);

        resp.CurriculumVersions.Should().HaveCount(1);
        resp.Analysis.TotalSubjects.Should().Be(2);
        resp.Analysis.SubjectsWithContent.Should().Be(1);
        resp.Analysis.SubjectsWithoutContent.Should().Be(1);
        resp.CurriculumVersions[0].Analysis.TotalSubjects.Should().Be(2);
    }
}