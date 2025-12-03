using System.Collections.Generic;
using FluentAssertions;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using Xunit;

namespace RogueLearn.User.Application.Tests.Services;

public class QuestStepsPromptBuilderTests
{
    private WeekContext BuildWeek(bool withResources)
    {
        var week = new WeekContext
        {
            WeekNumber = 1,
            TotalWeeks = 10,
            TopicsToCover = new List<string> { "Topic A", "Topic B" },
            AvailableResources = new List<ValidResource>
            {
                new() { Url = "https://example.com/a", SourceContext = "Tutorial" },
                new() { Url = "https://example.com/b", SourceContext = "Docs" }
            }
        };
        if (!withResources)
        {
            week.AvailableResources.Clear();
        }
        return week;
    }

    private AcademicContext BuildAcademicContext()
    {
        return new AcademicContext
        {
            CurrentGpa = 7.5,
            AttemptReason = QuestAttemptReason.CurrentlyStudying,
            PrerequisiteHistory = new List<PrerequisitePerformance>
            {
                new() { SubjectCode = "CS101", SubjectName = "Intro", PerformanceLevel = "Weak", Grade = "5.0" }
            },
            RelatedSubjects = new List<RelatedSubjectGrade>
            {
                new() { SubjectCode = "CS102", Grade = "7.0" }
            },
            StrengthAreas = new List<string> { "Algorithms" },
            ImprovementAreas = new List<string> { "Pointers" }
        };
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BuildPrompt_HandlesResourceVariants(bool withResources)
    {
        var builder = new QuestStepsPromptBuilder();
        var prompt = builder.BuildPrompt(
            BuildWeek(withResources),
            userContext: "Student X",
            relevantSkills: new List<Skill>(),
            subjectName: "C programming",
            courseDescription: "Basics",
            academicContext: BuildAcademicContext());

        if (!withResources)
        {
            prompt.Should().Contain("No External URLs Available");
            prompt.Should().Contain("0 `Reading` activities");
        }
        else
        {
            prompt.Should().Contain("Approved Resource Pool");
            prompt.Should().Contain("https://example.com/a");
            prompt.Should().Contain("https://example.com/b");
        }
    }

    [Fact]
    public void BuildPrompt_IncludesEscapeSequenceRules()
    {
        var builder = new QuestStepsPromptBuilder();
        var prompt = builder.BuildPrompt(
            BuildWeek(true),
            userContext: "Student X",
            relevantSkills: new List<Skill>(),
            subjectName: "C programming",
            courseDescription: "Basics",
            academicContext: BuildAcademicContext());

        prompt.Should().Contain("CRITICAL JSON STRING ENCODING RULES");
        prompt.Should().Contain("Double backslashes");
    }
}