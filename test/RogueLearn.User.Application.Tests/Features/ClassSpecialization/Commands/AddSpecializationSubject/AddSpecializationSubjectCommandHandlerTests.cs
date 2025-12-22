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
        var cmd = new AddSpecializationSubjectCommand { ClassId = System.Guid.NewGuid(), SubjectId = System.Guid.NewGuid(), Semester = 1 };
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
        var cmd = new AddSpecializationSubjectCommand { ClassId = System.Guid.NewGuid(), SubjectId = System.Guid.NewGuid(), Semester = 1 };
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




}