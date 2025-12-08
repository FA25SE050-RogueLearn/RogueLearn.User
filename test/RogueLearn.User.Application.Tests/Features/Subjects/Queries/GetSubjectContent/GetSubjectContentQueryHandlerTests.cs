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
        res.CourseLearningOutcomes!.Should().HaveCount(1);
        res.SessionSchedule!.Should().HaveCount(1);
        res.ConstructiveQuestions!.Should().HaveCount(1);
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
    public async Task Handle_RepositoryThrows_Rethrows()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<ILogger<GetSubjectContentQueryHandler>>();
        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns<Task<Subject?>>(_ => throw new Exception("db error"));
        var sut = new GetSubjectContentQueryHandler(repo, logger);
        await Assert.ThrowsAsync<Exception>(() => sut.Handle(new GetSubjectContentQuery { SubjectId = id }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SerializationSelfReference_ThrowsInvalidOperation()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<ILogger<GetSubjectContentQueryHandler>>();
        var id = Guid.NewGuid();

        var dict = new Dictionary<string, object>();
        dict["self"] = dict;
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new Subject { Id = id, SubjectCode = "S", SubjectName = "Name", Content = dict });

        var sut = new GetSubjectContentQueryHandler(repo, logger);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(new GetSubjectContentQuery { SubjectId = id }, CancellationToken.None));
    }

    [Newtonsoft.Json.JsonConverter(typeof(NullDictConverter))]
    private class NullDict : Dictionary<string, object> { }
    private class NullDictConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(NullDict).IsAssignableFrom(objectType);
        public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer)
        {
            writer.WriteNull();
        }
        public override object? ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer) => null;
    }

    [Fact]
    public async Task Handle_DeserializationNull_ReturnsDefaults()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<ILogger<GetSubjectContentQueryHandler>>();
        var id = Guid.NewGuid();
        var subject = new Subject { Id = id, SubjectCode = "S", SubjectName = "Name", Content = new NullDict() };
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(subject);
        var sut = new GetSubjectContentQueryHandler(repo, logger);
        var res = await sut.Handle(new GetSubjectContentQuery { SubjectId = id }, CancellationToken.None);
        res.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_UnexpectedError_Rethrows()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<ILogger<GetSubjectContentQueryHandler>>();
        var id = Guid.NewGuid();
        var subject = new Subject { Id = id, SubjectCode = "S", SubjectName = "Name", Content = new Dictionary<string, object> { { "courseDescription", "desc" } } };
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(subject);
        logger
            .When(l => l.Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("Serialized Dictionary")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!
            ))
            .Do(_ => { throw new InvalidOperationException("boom"); });

        var sut = new GetSubjectContentQueryHandler(repo, logger);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(new GetSubjectContentQuery { SubjectId = id }, CancellationToken.None));
    }
}
