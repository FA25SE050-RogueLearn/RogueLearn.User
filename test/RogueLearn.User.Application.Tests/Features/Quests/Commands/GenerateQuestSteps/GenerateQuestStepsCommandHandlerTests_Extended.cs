using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Quests.Commands.GenerateQuestSteps;

public class GenerateQuestStepsCommandHandlerTests_Extended
{
    // Helper to create SUT with mocked dependencies
    private GenerateQuestStepsCommandHandler CreateSut(
        IQuestRepository questRepo = null,
        IQuestStepRepository stepRepo = null,
        ISubjectRepository subjectRepo = null,
        IQuestGenerationPlugin plugin = null,
        ITopicGrouperService topicGrouper = null,
        QuestStepsPromptBuilder promptBuilder = null)
    {
        questRepo ??= Substitute.For<IQuestRepository>();
        stepRepo ??= Substitute.For<IQuestStepRepository>();
        subjectRepo ??= Substitute.For<ISubjectRepository>();
        plugin ??= Substitute.For<IQuestGenerationPlugin>();
        topicGrouper ??= Substitute.For<ITopicGrouperService>();
        promptBuilder ??= Substitute.For<QuestStepsPromptBuilder>();

        // Mock other non-critical dependencies
        var logger = Substitute.For<ILogger<GenerateQuestStepsCommandHandler>>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();

        return new GenerateQuestStepsCommandHandler(
            questRepo, stepRepo, subjectRepo, logger, plugin, mapper,
            skillRepo, mappingRepo, promptBuilder, userSkillRepo, topicGrouper);
    }

    [Theory, AutoData]
    public async Task Handle_QuestAlreadyHasSteps_ThrowsBadRequest(GenerateQuestStepsCommand cmd)
    {
        // Arrange
        var stepRepo = Substitute.For<IQuestStepRepository>();
        stepRepo.QuestContainsSteps(cmd.QuestId, Arg.Any<CancellationToken>()).Returns(true);

        var sut = CreateSut(stepRepo: stepRepo);

        // Act & Assert
        // Corresponds to Excel Condition: Precondition failed (steps exist) -> Abnormal
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory, AutoData]
    public async Task Handle_ValidSyllabus_GeneratesSteps(GenerateQuestStepsCommand cmd)
    {
        // Arrange
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var topicGrouper = Substitute.For<ITopicGrouperService>();
        var plugin = Substitute.For<IQuestGenerationPlugin>();

        // Setup Quest & Subject
        var quest = new Quest { Id = cmd.QuestId, SubjectId = Guid.NewGuid() };
        questRepo.GetByIdAsync(cmd.QuestId, Arg.Any<CancellationToken>()).Returns(quest);

        var subjectContent = new Dictionary<string, object> {
            { "sessionSchedule", new List<object>() } // Simplified content structure
        };
        var subject = new Subject { Id = quest.SubjectId.Value, Content = subjectContent };
        subjectRepo.GetByIdAsync(subject.Id, Arg.Any<CancellationToken>()).Returns(subject);

        // Mock Topic Grouping logic
        var modules = new List<QuestStepDefinition> {
            new QuestStepDefinition { ModuleNumber = 1, Title = "Module 1" }
        };
        topicGrouper.GroupSessionsIntoModules(Arg.Any<List<SyllabusSessionDto>>()).Returns(modules);

        // Mock AI Generation response (valid JSON structure for 3 tracks)
        string aiResponse = @"
        {
            ""standard"": { ""activities"": [{ ""type"": ""Reading"", ""payload"": { ""experiencePoints"": 10 } }] },
            ""supportive"": { ""activities"": [] },
            ""challenging"": { ""activities"": [] }
        }";
        plugin.GenerateFromPromptAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(aiResponse);

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, plugin, topicGrouper);

        // Act
        // Corresponds to Excel Condition: Valid inputs -> Normal
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        // Expect steps to be saved to repo (1 module * 3 tracks = 3 steps if all present, simplified to checking calls)
        await stepRepo.Received().AddAsync(Arg.Is<QuestStep>(s => s.QuestId == cmd.QuestId), Arg.Any<CancellationToken>());
    }
}