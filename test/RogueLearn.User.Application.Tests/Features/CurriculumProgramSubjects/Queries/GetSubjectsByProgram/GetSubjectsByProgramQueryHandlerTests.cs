using FluentAssertions;
using NSubstitute;
using AutoMapper;
using RogueLearn.User.Application.Features.CurriculumProgramSubjects.Queries.GetSubjectsByProgram;
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.CurriculumProgramSubjects.Queries.GetSubjectsByProgram;

public class GetSubjectsByProgramQueryHandlerTests
{
    [Fact]
    public async Task Handle_SortsBySemesterThenCode_AndMaps()
    {
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GetSubjectsByProgramQueryHandler>>();

        var programId = Guid.NewGuid();
        var s1 = new Subject { Id = Guid.NewGuid(), SubjectCode = "B", Semester = 2 };
        var s2 = new Subject { Id = Guid.NewGuid(), SubjectCode = "A", Semester = 2 };
        var s3 = new Subject { Id = Guid.NewGuid(), SubjectCode = "C", Semester = null };
        subjectRepo.GetSubjectsByRoute(programId, Arg.Any<CancellationToken>()).Returns(new[] { s1, s2, s3 });

        var mapped = new List<SubjectDto> { new SubjectDto { Id = s3.Id }, new SubjectDto { Id = s2.Id }, new SubjectDto { Id = s1.Id } };
        mapper.Map<List<SubjectDto>>(Arg.Is<List<Subject>>(l => l[0].Id == s3.Id && l[1].Id == s2.Id && l[2].Id == s1.Id)).Returns(mapped);

        var sut = new GetSubjectsByProgramQueryHandler(subjectRepo, mapper, logger);
        var res = await sut.Handle(new GetSubjectsByProgramQuery { ProgramId = programId }, CancellationToken.None);

        res.Should().BeEquivalentTo(mapped);
        mapper.Received(1).Map<List<SubjectDto>>(Arg.Is<List<Subject>>(l => l.Select(x => x.Id).SequenceEqual(new[] { s3.Id, s2.Id, s1.Id })));
    }

    [Fact]
    public async Task Handle_EmptySubjects_ReturnsEmptyList()
    {
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GetSubjectsByProgramQueryHandler>>();

        var programId = Guid.NewGuid();
        subjectRepo.GetSubjectsByRoute(programId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Subject>());
        mapper.Map<List<SubjectDto>>(Arg.Any<List<Subject>>()).Returns(new List<SubjectDto>());

        var sut = new GetSubjectsByProgramQueryHandler(subjectRepo, mapper, logger);
        var res = await sut.Handle(new GetSubjectsByProgramQuery { ProgramId = programId }, CancellationToken.None);

        res.Should().NotBeNull();
        res.Should().BeEmpty();
    }
}
