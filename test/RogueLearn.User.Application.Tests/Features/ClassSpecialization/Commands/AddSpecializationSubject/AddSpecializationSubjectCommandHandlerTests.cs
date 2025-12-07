using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.ClassSpecialization.Commands.AddSpecializationSubject;
using RogueLearn.User.Application.Features.ClassSpecialization.Queries.GetSpecializationSubjects;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.ClassSpecialization.Commands.AddSpecializationSubject;

public class AddSpecializationSubjectCommandHandlerTests
{
    [Fact]
    public async Task Handle_ClassMissing_Throws()
    {
        var cmd = new AddSpecializationSubjectCommand { ClassId = System.Guid.NewGuid(), SubjectId = System.Guid.NewGuid(), Semester = 1, PlaceholderSubjectCode = "CODE" };
        var repo = Substitute.For<IClassSpecializationSubjectRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<AddSpecializationSubjectCommandHandler>>();
        var sut = new AddSpecializationSubjectCommandHandler(repo, classRepo, subjectRepo, mapper, logger);

        classRepo.ExistsAsync(cmd.ClassId, Arg.Any<CancellationToken>()).Returns(false);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SubjectMissing_Throws()
    {
        var cmd = new AddSpecializationSubjectCommand { ClassId = System.Guid.NewGuid(), SubjectId = System.Guid.NewGuid(), Semester = 1, PlaceholderSubjectCode = "CODE" };
        var repo = Substitute.For<IClassSpecializationSubjectRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<AddSpecializationSubjectCommandHandler>>();
        var sut = new AddSpecializationSubjectCommandHandler(repo, classRepo, subjectRepo, mapper, logger);

        classRepo.ExistsAsync(cmd.ClassId, Arg.Any<CancellationToken>()).Returns(true);
        subjectRepo.ExistsAsync(cmd.SubjectId, Arg.Any<CancellationToken>()).Returns(false);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Duplicate_Throws()
    {
        var cmd = new AddSpecializationSubjectCommand { ClassId = System.Guid.NewGuid(), SubjectId = System.Guid.NewGuid(), Semester = 1, PlaceholderSubjectCode = "CODE" };
        var repo = Substitute.For<IClassSpecializationSubjectRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<AddSpecializationSubjectCommandHandler>>();
        var sut = new AddSpecializationSubjectCommandHandler(repo, classRepo, subjectRepo, mapper, logger);

        classRepo.ExistsAsync(cmd.ClassId, Arg.Any<CancellationToken>()).Returns(true);
        subjectRepo.ExistsAsync(cmd.SubjectId, Arg.Any<CancellationToken>()).Returns(true);
        repo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<System.Func<ClassSpecializationSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new ClassSpecializationSubject());
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_ReturnsDto()
    {
        var cmd = new AddSpecializationSubjectCommand { ClassId = System.Guid.NewGuid(), SubjectId = System.Guid.NewGuid(), Semester = 1, PlaceholderSubjectCode = "CODE" };
        var repo = Substitute.For<IClassSpecializationSubjectRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<AddSpecializationSubjectCommandHandler>>();
        var sut = new AddSpecializationSubjectCommandHandler(repo, classRepo, subjectRepo, mapper, logger);

        classRepo.ExistsAsync(cmd.ClassId, Arg.Any<CancellationToken>()).Returns(true);
        subjectRepo.ExistsAsync(cmd.SubjectId, Arg.Any<CancellationToken>()).Returns(true);
        repo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<System.Func<ClassSpecializationSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns((ClassSpecializationSubject?)null);

        var created = new ClassSpecializationSubject { Id = System.Guid.NewGuid(), ClassId = cmd.ClassId, SubjectId = cmd.SubjectId, Semester = cmd.Semester, PlaceholderSubjectCode = cmd.PlaceholderSubjectCode };
        repo.AddAsync(Arg.Any<ClassSpecializationSubject>(), Arg.Any<CancellationToken>()).Returns(created);
        mapper.Map<SpecializationSubjectDto>(created).Returns(new SpecializationSubjectDto { ClassId = created.ClassId, SubjectId = created.SubjectId, Semester = created.Semester, PlaceholderSubjectCode = created.PlaceholderSubjectCode });

        var resp = await sut.Handle(cmd, CancellationToken.None);
        resp.ClassId.Should().Be(cmd.ClassId);
        resp.SubjectId.Should().Be(cmd.SubjectId);
    }
}