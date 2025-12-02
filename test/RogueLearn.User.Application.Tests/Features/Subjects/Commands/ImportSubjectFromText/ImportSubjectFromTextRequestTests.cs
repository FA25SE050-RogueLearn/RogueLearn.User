using FluentAssertions;
using RogueLearn.User.Application.Features.Subjects.Commands.ImportSubjectFromText;

namespace RogueLearn.User.Application.Tests.Features.Subjects.Commands.ImportSubjectFromText;

public class ImportSubjectFromTextRequestTests
{
    [Fact]
    public void DefaultValues_ShouldInitializeCorrectly()
    {
        var request = new ImportSubjectFromTextRequest();
        request.RawText.Should().Be(string.Empty);
        request.Semester.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldBeAssignable()
    {
        var request = new ImportSubjectFromTextRequest
        {
            RawText = "<p>Course syllabus</p>",
            Semester = 3
        };

        request.RawText.Should().Be("<p>Course syllabus</p>");
        request.Semester.Should().Be(3);
    }
}