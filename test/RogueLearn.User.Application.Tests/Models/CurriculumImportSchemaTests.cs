using FluentAssertions;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Tests.Models;

public class CurriculumImportSchemaTests
{
    [Fact]
    public void AssessmentItem_Can_Set_Properties()
    {
        var item = new AssessmentItem { Type = "Exam", Name = "Midterm", WeightPercentage = 30, Description = "Written" };
        item.Type.Should().Be("Exam");
        item.WeightPercentage.Should().Be(30);
    }
}