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

    [Fact]
    public void BuildPrompt_HandlesResourceVariants()
    {
        var builder = new QuestStepsPromptBuilder();
        foreach (var withResources in new[] { false, true })
        {
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

    [Fact]
    public void BuildPrompt_IncludesSkillsJson_WhenSkillsProvided()
    {
        var builder = new QuestStepsPromptBuilder();
        var prompt = builder.BuildPrompt(
            BuildWeek(true),
            userContext: "Student X",
            relevantSkills: new List<Skill>{ new() { Id = Guid.NewGuid(), Name = "Algorithms" } },
            subjectName: "C programming",
            courseDescription: "Basics",
            academicContext: BuildAcademicContext());
        prompt.Should().Contain("```json");
        prompt.ToLowerInvariant().Should().Contain("algorithms");
    }

    [Fact]
    public void BuildPrompt_Warns_When_No_Skills()
    {
        var builder = new QuestStepsPromptBuilder();
        var prompt = builder.BuildPrompt(
            BuildWeek(false),
            userContext: "Student Y",
            relevantSkills: new List<Skill>(),
            subjectName: "Data Structures",
            courseDescription: "Theory",
            academicContext: BuildAcademicContext());
        prompt.Should().Contain("WARNING: No skills provided");
    }

    [Fact]
    public void BuildPrompt_Includes_ErrorHint_When_Provided()
    {
        var builder = new QuestStepsPromptBuilder();
        var prompt = builder.BuildPrompt(
            BuildWeek(true),
            userContext: "Student Z",
            relevantSkills: new List<Skill>(),
            subjectName: "C programming",
            courseDescription: "Basics",
            academicContext: BuildAcademicContext(),
            userClass: null,
            errorHint: "JSON not valid");
        prompt.Should().Contain("## CORRECTION REQUIRED");
        prompt.Should().Contain("JSON not valid");
    }

    [Fact]
    public void GetPersonalizationInstructions_Covers_Gpa_Branches_And_AttemptReason()
    {
        var builder = new QuestStepsPromptBuilder();
        var m = typeof(QuestStepsPromptBuilder).GetMethod("GetPersonalizationInstructions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var ctxHigh = new AcademicContext { CurrentGpa = 8.6, AttemptReason = QuestAttemptReason.FirstTime };
        var ctxGood = new AcademicContext { CurrentGpa = 7.5, AttemptReason = QuestAttemptReason.CurrentlyStudying };
        var ctxLow = new AcademicContext { CurrentGpa = 6.0, AttemptReason = QuestAttemptReason.Retake };
        ctxLow.PrerequisiteHistory.Add(new PrerequisitePerformance { SubjectCode = "CS101", SubjectName = "Intro", PerformanceLevel = "Weak" });
        ctxLow.ImprovementAreas.Add("Pointers");
        ctxLow.StrengthAreas.Add("Loops");
        var sHigh = (string)m!.Invoke(builder, new object[] { ctxHigh })!;
        var sGood = (string)m!.Invoke(builder, new object[] { ctxGood })!;
        var sLow = (string)m!.Invoke(builder, new object[] { ctxLow })!;
        sHigh.Should().Contain("High Achiever");
        sGood.Should().Contain("Good Performance");
        sLow.Should().Contain("Needs Support");
        sLow.Should().Contain("Retake Student");
        sLow.Should().Contain("Foundation Gaps");
        sLow.Should().Contain("Remediation Focus");
        sLow.Should().Contain("Leverage Strengths");
    }
}
