using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Subjects.Queries.GetSubjectContent;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Tests.Features.Subjects.Queries.GetSubjectContent;

public class GetSubjectContentQueryHandlerTests
{
    [Fact]
    public async Task Handle_SubjectNotFound_Throws()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<ILogger<GetSubjectContentQueryHandler>>();
        var sut = new GetSubjectContentQueryHandler(repo, logger);
        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Subject?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(new GetSubjectContentQuery { SubjectId = id }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoContent_ReturnsDefaults()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<ILogger<GetSubjectContentQueryHandler>>();
        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new Subject { Id = id, SubjectCode = "S", SubjectName = "Name", Content = new Dictionary<string, object>() });
        var sut = new GetSubjectContentQueryHandler(repo, logger);
        var res = await sut.Handle(new GetSubjectContentQuery { SubjectId = id }, CancellationToken.None);
        res.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ValidDictionary_Deserializes()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<ILogger<GetSubjectContentQueryHandler>>();
        var id = Guid.NewGuid();
        var dict = new Dictionary<string, object>
        {
            ["CourseLearningOutcomes"] = new List<CourseLearningOutcome> { new CourseLearningOutcome { Id = "CLO1", Details = "d" } },
            ["SessionSchedule"] = new List<SyllabusSessionDto> { new SyllabusSessionDto { SessionNumber = 1, Topic = "t" } },
            ["ConstructiveQuestions"] = new List<ConstructiveQuestion> { new ConstructiveQuestion { Name = "n", Question = "q" } }
        };
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new Subject { Id = id, SubjectCode = "S", SubjectName = "Name", Content = dict });
        var sut = new GetSubjectContentQueryHandler(repo, logger);
        var res = await sut.Handle(new GetSubjectContentQuery { SubjectId = id }, CancellationToken.None);
        res.SessionSchedule!.Count.Should().Be(1);
        res.CourseLearningOutcomes!.Count.Should().Be(1);
        res.ConstructiveQuestions!.Count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_InvalidShape_ThrowsInvalidOperation()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<ILogger<GetSubjectContentQueryHandler>>();
        var id = Guid.NewGuid();
        var dict = new Dictionary<string, object>
        {
            ["SessionSchedule"] = "not an array"
        };
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new Subject { Id = id, SubjectCode = "S", SubjectName = "Name", Content = dict });
        var sut = new GetSubjectContentQueryHandler(repo, logger);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(new GetSubjectContentQuery { SubjectId = id }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_JObjectContent_Deserializes()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<ILogger<GetSubjectContentQueryHandler>>();
        var id = Guid.NewGuid();

        var jObj = Newtonsoft.Json.Linq.JObject.Parse("{\"courseDescription\":\"c\",\"courseLearningOutcomes\":[{\"id\":\"CLO1\",\"details\":\"d\"}],\"sessionSchedule\":[{\"sessionNumber\":1,\"topic\":\"t\"}],\"constructiveQuestions\":[{\"name\":\"n\",\"question\":\"q\"}]}");

        var dict = jObj.ToObject<Dictionary<string, object>>();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new Subject { Id = id, SubjectCode = "S", SubjectName = "Name", Content = dict! });
        var sut = new GetSubjectContentQueryHandler(repo, logger);
        var res = await sut.Handle(new GetSubjectContentQuery { SubjectId = id }, CancellationToken.None);
        res.CourseLearningOutcomes!.Count.Should().Be(1);
        res.SessionSchedule!.Count.Should().Be(1);
        res.ConstructiveQuestions!.Count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_DeepNestedJObject_ThrowsInvalidOperation()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<ILogger<GetSubjectContentQueryHandler>>();
        var id = Guid.NewGuid();

        var root = new Newtonsoft.Json.Linq.JObject();
        var cur = root;
        for (int i = 0; i < 80; i++)
        {
            var next = new Newtonsoft.Json.Linq.JObject();
            cur["n"] = next;
            cur = next;
        }
        var dict = new Dictionary<string, object> { ["root"] = root };

        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new Subject { Id = id, SubjectCode = "S", SubjectName = "Name", Content = dict });
        var sut = new GetSubjectContentQueryHandler(repo, logger);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(new GetSubjectContentQuery { SubjectId = id }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RepositoryThrows_Rethrows()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<ILogger<GetSubjectContentQueryHandler>>();
        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns<Task<Subject?>>(_ => throw new Exception("db error"));
        var sut = new GetSubjectContentQueryHandler(repo, logger);
        await Assert.ThrowsAsync<Exception>(() => sut.Handle(new GetSubjectContentQuery { SubjectId = id }, CancellationToken.None));
    }
}
