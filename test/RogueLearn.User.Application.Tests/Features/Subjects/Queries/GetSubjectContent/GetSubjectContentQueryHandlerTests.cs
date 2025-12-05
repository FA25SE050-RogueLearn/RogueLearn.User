using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Subjects.Queries.GetSubjectContent;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Subjects.Queries.GetSubjectContent;

public class GetSubjectContentQueryHandlerTests
{
    private GetSubjectContentQueryHandler CreateHandler(ISubjectRepository? subjectRepo = null)
    {
        subjectRepo ??= Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<ILogger<GetSubjectContentQueryHandler>>();
        return new GetSubjectContentQueryHandler(subjectRepo, logger);
    }

    [Fact]
    public async Task Handle_SubjectMissing_ThrowsNotFound()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var subjectId = Guid.NewGuid();
        repo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>())
            .Returns((Subject?)null);

        var sut = CreateHandler(repo);
        var q = new GetSubjectContentQuery { SubjectId = subjectId };
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(q, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoContent_ReturnsDefaultDto()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var subjectId = Guid.NewGuid();
        var subject = new Subject { Id = subjectId, SubjectCode = "CS101", SubjectName = "Intro", Content = null };
        repo.GetByIdAsync(subject.Id, Arg.Any<CancellationToken>()).Returns(subject);

        var sut = CreateHandler(repo);
        var result = await sut.Handle(new GetSubjectContentQuery { SubjectId = subject.Id }, CancellationToken.None);

        result.Should().NotBeNull();
        (result.CourseLearningOutcomes?.Count ?? 0).Should().Be(0);
        (result.SessionSchedule?.Count ?? 0).Should().Be(0);
        result.ConstructiveQuestions.Should().NotBeNull();
        result.ConstructiveQuestions.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ValidContent_DeserializesCorrectly()
    {
        var repo = Substitute.For<ISubjectRepository>();

        var content = new Dictionary<string, object>
        {
            ["CourseLearningOutcomes"] = new List<Dictionary<string, object>>
            {
                new() { ["Id"] = "CLO1", ["Details"] = "Understand basics" },
                new() { ["Id"] = "CLO2", ["Details"] = "Apply concepts" },
            },
            ["SessionSchedule"] = new List<Dictionary<string, object>>
            {
                new() { ["SessionNumber"] = 1, ["Topic"] = "Intro", ["Activities"] = new List<string> { "Lecture" }, ["Readings"] = new List<string> { "Book 1" }, ["suggestedUrl"] = "https://example.com/intro" },
                new() { ["SessionNumber"] = 2, ["Topic"] = "Advanced", ["Activities"] = new List<string> { "Lab" }, ["Readings"] = new List<string> { "Book 2" } }
            },
            ["ConstructiveQuestions"] = new List<Dictionary<string, object>>
            {
                new() { ["Name"] = "Q1", ["Question"] = "What is X?", ["SessionNumber"] = 1 }
            }
        };

        var subjectId = Guid.NewGuid();
        var subject = new Subject { Id = subjectId, SubjectCode = "CS101", SubjectName = "Intro", Content = content };
        repo.GetByIdAsync(subject.Id, Arg.Any<CancellationToken>()).Returns(subject);

        var sut = CreateHandler(repo);
        var result = await sut.Handle(new GetSubjectContentQuery { SubjectId = subject.Id }, CancellationToken.None);

        result.CourseLearningOutcomes.Should().HaveCount(2);
        result.SessionSchedule.Should().HaveCount(2);
        result.ConstructiveQuestions.Should().HaveCount(1);

        result.SessionSchedule![0].SuggestedUrl.Should().Be("https://example.com/intro");
        result.CourseLearningOutcomes![0].Id.Should().Be("CLO1");
        result.ConstructiveQuestions![0].Name.Should().Be("Q1");
    }
}