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
using System.Text.Json;
using NewtonsoftJson = Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
    public async Task Handle_SystemTextJsonThrows_WrappedInvalidOperation()
    {
        var subjectId = Guid.NewGuid();
        var subject = new Subject { Id = subjectId, SubjectCode = "CS800", SubjectName = "SerializeErr" };
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(subject);
        subjectRepo.UpdateAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns<Task<Subject>>(_ => throw new JsonException("boom"));

        var sut = CreateSut(subjectRepo);
        var content = new SyllabusContent { CourseDescription = "Desc", SessionSchedule = new List<SyllabusSessionDto> { new() { SessionNumber = 1, Topic = "Intro" } } };
        var act = () => sut.Handle(new UpdateSubjectContentCommand { SubjectId = subjectId, Content = content }, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Failed to serialize DTO: boom*");
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

    [Fact]
    public async Task Handle_Serializes_IgnoresNullProperties()
    {
        var subjectId = Guid.NewGuid();
        var subject = new Subject { Id = subjectId, SubjectCode = "CS202", SubjectName = "Data Structures" };
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(subject);
        subjectRepo.UpdateAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => (Subject)ci[0]!);

        var sut = CreateSut(subjectRepo);
        var content = new SyllabusContent
        {
            CourseDescription = "Desc",
            CourseLearningOutcomes = null,
            SessionSchedule = new List<SyllabusSessionDto> { new() { SessionNumber = 1, Topic = "Intro" } }
        };

        var cmd = new UpdateSubjectContentCommand { SubjectId = subjectId, Content = content };
        var res = await sut.Handle(cmd, CancellationToken.None);

        res.Should().BeSameAs(content);
        subject.Content.Should().NotBeNull();
        subject.Content!.ContainsKey("courseLearningOutcomes").Should().BeFalse();
        subject.Content!.ContainsKey("sessionSchedule").Should().BeTrue();
        await subjectRepo.Received(1).UpdateAsync(subject, Arg.Any<CancellationToken>());
    }

    

    [Fact]
    public async Task Handle_UpdateFails_Rethrows()
    {
        var subjectId = Guid.NewGuid();
        var subject = new Subject { Id = subjectId, SubjectCode = "CS401", SubjectName = "Systems" };
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(subject);
        subjectRepo.UpdateAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns<Task<Subject>>(_ => throw new Exception("db failure"));

        var content = new SyllabusContent
        {
            CourseDescription = "Desc",
            SessionSchedule = new List<SyllabusSessionDto> { new() { SessionNumber = 1, Topic = "Intro" } }
        };

        var sut = CreateSut(subjectRepo);
        await Assert.ThrowsAsync<Exception>(() => sut.Handle(new UpdateSubjectContentCommand { SubjectId = subjectId, Content = content }, CancellationToken.None));
    }

    [System.Text.Json.Serialization.JsonConverter(typeof(FailingContentConverter))]
    private class BadContent : SyllabusContent { }

    private class FailingContentConverter : System.Text.Json.Serialization.JsonConverter<BadContent>
    {
        public override BadContent? Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
        public override void Write(System.Text.Json.Utf8JsonWriter writer, BadContent value, System.Text.Json.JsonSerializerOptions options)
        {
            throw new System.Text.Json.JsonException("fail");
        }
    }

    private class BadCourseLearningOutcome : CourseLearningOutcome
    {
        public new string Details
        {
            get => throw new Exception("boom");
            set { }
        }
    }

    private class CycleContent : SyllabusContent
    {
        public CycleContent? Child { get; set; }
    }

    

    

    

    private class NullDictConverter : NewtonsoftJson.JsonConverter<Dictionary<string, object>>
    {
        public override Dictionary<string, object>? ReadJson(NewtonsoftJson.JsonReader reader, Type objectType, Dictionary<string, object>? existingValue, bool hasExistingValue, NewtonsoftJson.JsonSerializer serializer)
        {
            _ = JObject.Load(reader);
            return null;
        }
        public override void WriteJson(NewtonsoftJson.JsonWriter writer, Dictionary<string, object>? value, NewtonsoftJson.JsonSerializer serializer)
        {
            writer.WriteNull();
        }
    }

    [Fact]
    public async Task Handle_DeserializeReturnsNull_ThrowsInvalidOperation()
    {
        var subjectId = Guid.NewGuid();
        var subject = new Subject { Id = subjectId, SubjectCode = "CS600", SubjectName = "Topics" };
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(subject);

        var original = NewtonsoftJson.JsonConvert.DefaultSettings;
        NewtonsoftJson.JsonConvert.DefaultSettings = () => new NewtonsoftJson.JsonSerializerSettings { Converters = new List<NewtonsoftJson.JsonConverter> { new NullDictConverter() } };
        try
        {
            var sut = CreateSut(subjectRepo);
            var content = new SyllabusContent { CourseDescription = "Desc" };
            var act = () => sut.Handle(new UpdateSubjectContentCommand { SubjectId = subjectId, Content = content }, CancellationToken.None);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Failed to convert content to Dictionary*");
        }
        finally
        {
            NewtonsoftJson.JsonConvert.DefaultSettings = original;
        }
    }

    private class ThrowingDictConverter : NewtonsoftJson.JsonConverter<Dictionary<string, object>>
    {
        public override Dictionary<string, object>? ReadJson(NewtonsoftJson.JsonReader reader, Type objectType, Dictionary<string, object>? existingValue, bool hasExistingValue, NewtonsoftJson.JsonSerializer serializer)
        {
            throw new NewtonsoftJson.JsonException("boom");
        }
        public override void WriteJson(NewtonsoftJson.JsonWriter writer, Dictionary<string, object>? value, NewtonsoftJson.JsonSerializer serializer)
        {
            writer.WriteNull();
        }
    }

    [Fact]
    public async Task Handle_NewtonsoftThrows_WrappedAsInvalidOperation()
    {
        var subjectId = Guid.NewGuid();
        var subject = new Subject { Id = subjectId, SubjectCode = "CS700", SubjectName = "Errors" };
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(subject);

        var original = NewtonsoftJson.JsonConvert.DefaultSettings;
        NewtonsoftJson.JsonConvert.DefaultSettings = () => new NewtonsoftJson.JsonSerializerSettings { Converters = new List<NewtonsoftJson.JsonConverter> { new ThrowingDictConverter() } };
        try
        {
            var sut = CreateSut(subjectRepo);
            var content = new SyllabusContent { CourseDescription = "Desc" };
            var act = () => sut.Handle(new UpdateSubjectContentCommand { SubjectId = subjectId, Content = content }, CancellationToken.None);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Failed to prepare dictionary for DB: boom*");
        }
        finally
        {
            NewtonsoftJson.JsonConvert.DefaultSettings = original;
        }
    }

    [Fact]
    public async Task Handle_SelfReferentialCycle_DoesNotThrow()
    {
        var subjectId = Guid.NewGuid();
        var subject = new Subject { Id = subjectId, SubjectCode = "CS900", SubjectName = "Cycle" };
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(subject);

        var sut = CreateSut(subjectRepo);
        var bad = new BadContent { CourseDescription = "Desc" };
        await sut.Handle(new UpdateSubjectContentCommand { SubjectId = subjectId, Content = bad }, CancellationToken.None);
        subject.Content.Should().NotBeNull();
    }
}
