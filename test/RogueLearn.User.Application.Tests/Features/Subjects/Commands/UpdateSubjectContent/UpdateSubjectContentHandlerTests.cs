using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Subjects.Commands.UpdateSubjectContent;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Subjects.Commands.UpdateSubjectContent;

public class UpdateSubjectContentHandlerTests
{
    private static UpdateSubjectContentHandler CreateSut(
        ISubjectRepository? subjectRepo = null,
        ILogger<UpdateSubjectContentHandler>? logger = null)
    {
        subjectRepo ??= Substitute.For<ISubjectRepository>();
        logger ??= Substitute.For<ILogger<UpdateSubjectContentHandler>>();
        return new UpdateSubjectContentHandler(subjectRepo, logger);
    }

    [Fact]
    public async Task Handle_NullContent_ThrowsArgumentNull()
    {
        var auth = Guid.NewGuid();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var sut = CreateSut(subjectRepo);
        var cmd = new UpdateSubjectContentCommand { SubjectId = Guid.NewGuid(), Content = null! };
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_SubjectNotFound_ThrowsNotFound()
    {
        var subjectId = Guid.NewGuid();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns((Subject?)null);
        var sut = CreateSut(subjectRepo);
        var cmd = new UpdateSubjectContentCommand { SubjectId = subjectId, Content = new SyllabusContent { CourseDescription = "desc" } };
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Success_UpdatesContent_ReturnsContent()
    {
        var subjectId = Guid.NewGuid();
        var subject = new Subject { Id = subjectId, SubjectCode = "CS101", SubjectName = "Intro" };
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(subject);
        subjectRepo.UpdateAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => (Subject)ci[0]!);

        var sut = CreateSut(subjectRepo);
        var content = new SyllabusContent
        {
            CourseDescription = "Course",
            SessionSchedule = new List<SyllabusSessionDto>
            {
                new() { SessionNumber = 1, Topic = "Intro" },
                new() { SessionNumber = 2, Topic = "Basics" }
            },
            ConstructiveQuestions = new List<ConstructiveQuestion> { new() { Name = "Q1", Question = "?", SessionNumber = 1 } }
        };
        var cmd = new UpdateSubjectContentCommand { SubjectId = subjectId, Content = content };
        var res = await sut.Handle(cmd, CancellationToken.None);
        res.Should().BeSameAs(content);
        subject.Content.Should().NotBeNull();
        subject.Content!.ContainsKey("sessionSchedule").Should().BeTrue();
        await subjectRepo.Received(1).UpdateAsync(subject, Arg.Any<CancellationToken>());
    }
}