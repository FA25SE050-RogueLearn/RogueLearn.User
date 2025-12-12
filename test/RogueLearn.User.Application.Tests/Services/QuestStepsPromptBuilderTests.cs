using System.Collections.Generic;
using FluentAssertions;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using Xunit;

namespace RogueLearn.User.Application.Tests.Services;

public class QuestStepsPromptBuilderTests
{
    private QuestStepDefinition BuildModule(bool withResources)
    {
        var module = new QuestStepDefinition
        {
            ModuleNumber = 1,
            Title = "Topic A & Topic B",
            Sessions = new List<SyllabusSessionDto>
            {
                new() { SessionNumber = 1, Topic = "Topic A", SuggestedUrl = withResources ? "https://example.com/a" : "" },
                new() { SessionNumber = 2, Topic = "Topic B", SuggestedUrl = withResources ? "https://example.com/b" : "" }
            }
        };
        return module;
    }

    private AcademicContext BuildAcademicContext() => new AcademicContext { CurrentGpa = 7.5 };

    [Fact]
    public void BuildMasterPrompt_HandlesResourceVariants()
    {
        var builder = new QuestStepsPromptBuilder();
        foreach (var withResources in new[] { false, true })
        {
            var prompt = builder.BuildMasterPrompt(
                BuildModule(withResources),
                relevantSkills: new List<Skill>(),
                subjectName: "C programming",
                courseDescription: "Basics",
                userClass: null);

            if (!withResources)
            {
                prompt.ToLowerInvariant().Should().Contain("no url provided");
            }
            else
            {
                prompt.Should().Contain("https://example.com/a");
                prompt.Should().Contain("https://example.com/b");
            }
        }
    }

    [Fact]
    public void BuildMasterPrompt_IncludesOutputRules()
    {
        var builder = new QuestStepsPromptBuilder();
        var prompt = builder.BuildMasterPrompt(
            BuildModule(true),
            relevantSkills: new List<Skill>(),
            subjectName: "C programming",
            courseDescription: "Basics",
            userClass: null);

        prompt.Should().Contain("MUST DO");
        prompt.Should().Contain("MUST NOT DO");
    }

    [Fact]
    public void BuildMasterPrompt_IncludesSkillsJson_WhenSkillsProvided()
    {
        var builder = new QuestStepsPromptBuilder();
        var prompt = builder.BuildMasterPrompt(
            BuildModule(true),
            relevantSkills: new List<Skill>{ new() { Id = Guid.NewGuid(), Name = "Algorithms" } },
            subjectName: "C programming",
            courseDescription: "Basics",
            userClass: null);
        prompt.Should().Contain("```json");
        prompt.ToLowerInvariant().Should().Contain("algorithms");
    }

    // Removed: prompt now includes output-schema examples for clarity even if no skills
    [Fact]
    public void BuildMasterPrompt_Includes_ErrorHint_When_Provided()
    {
        var builder = new QuestStepsPromptBuilder();
        var prompt = builder.BuildMasterPrompt(
            BuildModule(true),
            relevantSkills: new List<Skill>(),
            subjectName: "C programming",
            courseDescription: "Basics",
            userClass: null,
            errorHint: "JSON not valid");
        prompt.Should().Contain("CORRECTION REQUIRED");
        prompt.Should().Contain("JSON not valid");
    }

    [Fact]
    public void BuildMasterPrompt_Includes_Final_Checklist()
    {
        var builder = new QuestStepsPromptBuilder();
        var prompt = builder.BuildMasterPrompt(
            BuildModule(true),
            relevantSkills: new List<Skill>(),
            subjectName: "C programming",
            courseDescription: "Basics",
            userClass: null);
        prompt.Should().Contain("FINAL VALIDATION CHECKLIST");
    }
}
