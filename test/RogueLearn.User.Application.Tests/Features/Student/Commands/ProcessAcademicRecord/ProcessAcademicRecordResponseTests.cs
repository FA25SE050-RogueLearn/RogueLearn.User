using FluentAssertions;
using RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;

namespace RogueLearn.User.Application.Tests.Features.Student.Commands.ProcessAcademicRecord;

public class ProcessAcademicRecordResponseTests
{
    [Fact]
    public void DefaultValues_ShouldInitializeCorrectly()
    {
        var resp = new ProcessAcademicRecordResponse();
        resp.IsSuccess.Should().BeFalse();
        resp.Message.Should().Be(string.Empty);
        resp.LearningPathId.Should().Be(Guid.Empty);
        resp.SubjectsProcessed.Should().Be(0);
        resp.QuestsGenerated.Should().Be(0);
        resp.CalculatedGpa.Should().Be(0.0);
    }

    [Fact]
    public void Properties_ShouldBeAssignable()
    {
        var id = Guid.NewGuid();
        var resp = new ProcessAcademicRecordResponse
        {
            IsSuccess = true,
            Message = "Processed successfully",
            LearningPathId = id,
            SubjectsProcessed = 12,
            QuestsGenerated = 5,
            CalculatedGpa = 3.75
        };

        resp.IsSuccess.Should().BeTrue();
        resp.Message.Should().Be("Processed successfully");
        resp.LearningPathId.Should().Be(id);
        resp.SubjectsProcessed.Should().Be(12);
        resp.QuestsGenerated.Should().Be(5);
        resp.CalculatedGpa.Should().Be(3.75);
    }
}