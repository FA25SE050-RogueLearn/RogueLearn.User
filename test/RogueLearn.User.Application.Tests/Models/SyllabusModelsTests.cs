using FluentAssertions;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Tests.Models;

public class SyllabusModelsTests
{
    [Fact]
    public void SyllabusMaterial_Can_Set_Properties()
    {
        var material = new SyllabusMaterial
        {
            MaterialDescription = "Book",
            Author = "Doe",
            Publisher = "Pub",
            PublishedDate = new DateOnly(2020, 1, 1),
            Edition = "2nd",
            ISBN = "123-456",
            IsMainMaterial = true,
            IsHardCopy = false,
            IsOnline = true,
            Note = "Optional"
        };
        material.MaterialDescription.Should().Be("Book");
        material.ISBN.Should().Be("123-456");
        material.IsOnline.Should().BeTrue();
    }

    [Fact]
    public void SyllabusSession_Can_Set_Properties()
    {
        var session = new SyllabusSession
        {
            SessionNumber = 1,
            Topic = "Intro",
            LearningTeachingType = "Lecture",
            LO = "LO1",
            ITU = "ITU",
            StudentMaterials = "Slides",
            SDownload = "url",
            StudentTasks = "Read",
            URLs = "http://example"
        };
        session.Topic.Should().Be("Intro");
        session.URLs.Should().Contain("http");
    }
}