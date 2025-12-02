using System.Text.Json;
using FluentAssertions;
using RogueLearn.User.Application.DTOs;

namespace RogueLearn.User.Application.Tests.DTOs;

public class SubjectContentDtoTests
{
    [Fact]
    public void DefaultsAreInitialized()
    {
        var dto = new SubjectContentDto();

        dto.SubjectCode.Should().BeEmpty();
        dto.SubjectName.Should().BeEmpty();
        dto.LastUpdated.Should().BeNull();

        dto.Content.Should().NotBeNull();
        dto.Content.CourseLearningOutcomes.Should().NotBeNull();
        dto.Content.SessionSchedule.Should().NotBeNull();
        dto.Content.Assessments.Should().NotBeNull();
        dto.Content.RequiredTexts.Should().NotBeNull();
        dto.Content.RecommendedTexts.Should().NotBeNull();
        dto.Content.Prerequisites.Should().NotBeNull();
        dto.Content.AdditionalResources.Should().NotBeNull();
    }

    [Fact]
    public void RoundTripSerializationWorks()
    {
        var dto = new SubjectContentDto
        {
            SubjectId = Guid.NewGuid(),
            SubjectCode = "CODE",
            SubjectName = "Name",
            Content = new SyllabusContentDto
            {
                CourseDescription = "desc",
                CourseLearningOutcomes =
                {
                    new CourseLearningOutcomeDto { Id = "C1", Details = "D", BloomLevel = "Apply" }
                },
                SessionSchedule =
                {
                    new SessionScheduleDto
                    {
                        SessionNumber = 1,
                        Topic = "T",
                        Activities = { "A1" },
                        Readings = { "R1" },
                        ConstructiveQuestions = { new ConstructiveQuestionDto { Question = "Q", ExpectedAnswer = "E" } },
                        MappedSkills = { "S1" }
                    }
                },
                Assessments =
                {
                    new AssessmentDto { Type = "Quiz", WeightPercentage = 10, Description = "d", CountPerSemester = 2 }
                },
                RequiredTexts = { "Req" },
                RecommendedTexts = { "Rec" },
                GradingPolicy = "policy",
                AttendancePolicy = "att",
                Prerequisites = { "Pre" },
                AdditionalResources = { "Res" }
            }
        };

        var json = JsonSerializer.Serialize(dto);
        var back = JsonSerializer.Deserialize<SubjectContentDto>(json)!;

        back.SubjectCode.Should().Be("CODE");
        back.SubjectName.Should().Be("Name");
        back.Content.CourseLearningOutcomes.Should().HaveCount(1);
        back.Content.SessionSchedule.Should().HaveCount(1);
        back.Content.SessionSchedule[0].ConstructiveQuestions.Should().HaveCount(1);
        back.Content.Assessments.Should().HaveCount(1);
        back.Content.RequiredTexts.Should().Contain("Req");
        back.Content.RecommendedTexts.Should().Contain("Rec");
        back.Content.Prerequisites.Should().Contain("Pre");
        back.Content.AdditionalResources.Should().Contain("Res");
    }

    [Fact]
    public void NestedDtoPropertySettersWork()
    {
        var clo = new CourseLearningOutcomeDto { Id = "X", Details = "Y", BloomLevel = "Analyze" };
        clo.Id.Should().Be("X");
        clo.Details.Should().Be("Y");
        clo.BloomLevel.Should().Be("Analyze");

        var cq = new ConstructiveQuestionDto { Question = "Q", ExpectedAnswer = "A" };
        cq.Question.Should().Be("Q");
        cq.ExpectedAnswer.Should().Be("A");

        var ss = new SessionScheduleDto { SessionNumber = 2, Topic = "Topic" };
        ss.SessionNumber.Should().Be(2);
        ss.Topic.Should().Be("Topic");
        ss.Activities.Should().NotBeNull();
        ss.Readings.Should().NotBeNull();
        ss.ConstructiveQuestions.Should().NotBeNull();
        ss.MappedSkills.Should().NotBeNull();

        var assess = new AssessmentDto { Type = "Exam", WeightPercentage = 50, Description = "D", CountPerSemester = 1 };
        assess.Type.Should().Be("Exam");
        assess.WeightPercentage.Should().Be(50);
        assess.Description.Should().Be("D");
        assess.CountPerSemester.Should().Be(1);
    }
}