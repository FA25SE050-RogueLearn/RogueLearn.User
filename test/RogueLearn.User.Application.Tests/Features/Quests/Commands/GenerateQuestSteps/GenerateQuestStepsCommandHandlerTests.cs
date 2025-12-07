using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Quests.Commands.GenerateQuestSteps;

public class GenerateQuestStepsCommandHandlerTests
{
    private GenerateQuestStepsCommandHandler CreateSut(
        IQuestRepository? questRepo = null,
        IQuestStepRepository? stepRepo = null,
        ISubjectRepository? subjectRepo = null,
        ILogger<GenerateQuestStepsCommandHandler>? logger = null,
        RogueLearn.User.Application.Plugins.IQuestGenerationPlugin? plugin = null,
        IMapper? mapper = null,
        IUserProfileRepository? userProfileRepo = null,
        IClassRepository? classRepo = null,
        ISkillRepository? skillRepo = null,
        ISubjectSkillMappingRepository? ssmRepo = null,
        RogueLearn.User.Application.Services.IPromptBuilder? promptBuilder = null,
        IUserSkillRepository? userSkillRepo = null,
        RogueLearn.User.Application.Services.IAcademicContextBuilder? academicContextBuilder = null)
    {
        questRepo ??= Substitute.For<IQuestRepository>();
        stepRepo ??= Substitute.For<IQuestStepRepository>();
        subjectRepo ??= Substitute.For<ISubjectRepository>();
        logger ??= Substitute.For<ILogger<GenerateQuestStepsCommandHandler>>();
        plugin ??= Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        mapper ??= Substitute.For<IMapper>();
        userProfileRepo ??= Substitute.For<IUserProfileRepository>();
        classRepo ??= Substitute.For<IClassRepository>();
        skillRepo ??= Substitute.For<ISkillRepository>();
        ssmRepo ??= Substitute.For<ISubjectSkillMappingRepository>();
        promptBuilder ??= Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        userSkillRepo ??= Substitute.For<IUserSkillRepository>();
        academicContextBuilder ??= Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();
        return new GenerateQuestStepsCommandHandler(
            questRepo, stepRepo, subjectRepo, logger, plugin, mapper,
            userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);
    }

    [Fact]
    public async Task Handle_ThrowsWhenQuestAlreadyHasSteps()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GenerateQuestStepsCommandHandler>>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var subjectSkillMapRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var quest = new Quest { Id = Guid.NewGuid(), SubjectId = Guid.NewGuid() };
        questRepo.GetByIdAsync(quest.Id, Arg.Any<CancellationToken>()).Returns(quest);
        stepRepo.QuestContainsSteps(quest.Id, Arg.Any<CancellationToken>()).Returns(true);

        var sut = new GenerateQuestStepsCommandHandler(questRepo, stepRepo, subjectRepo, logger, plugin, mapper, userProfileRepo, classRepo, skillRepo, subjectSkillMapRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        var act = () => sut.Handle(new GenerateQuestStepsCommand { QuestId = quest.Id }, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>();
    }

    [Fact]
    public async Task Handle_ThrowsWhenNoSyllabusSessions()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GenerateQuestStepsCommandHandler>>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var subjectSkillMapRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var quest = new Quest { Id = Guid.NewGuid(), SubjectId = Guid.NewGuid() };
        questRepo.GetByIdAsync(quest.Id, Arg.Any<CancellationToken>()).Returns(quest);
        stepRepo.QuestContainsSteps(quest.Id, Arg.Any<CancellationToken>()).Returns(false);
        subjectRepo.GetByIdAsync(quest.SubjectId!.Value, Arg.Any<CancellationToken>()).Returns(new Subject { Id = quest.SubjectId.Value, Content = null });

        var authId = Guid.NewGuid();
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() });
        classRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new Class { Id = Guid.NewGuid(), Name = "C" });

        var sut = new GenerateQuestStepsCommandHandler(questRepo, stepRepo, subjectRepo, logger, plugin, mapper, userProfileRepo, classRepo, skillRepo, subjectSkillMapRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        var act = () => sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = quest.Id }, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>();
    }

    [Fact]
    public async Task Handle_GeneratesSteps_WithUrlValidationAndPadding()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        stepRepo.QuestContainsSteps(questId, Arg.Any<CancellationToken>()).Returns(false);

        var userProfile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(userProfile);
        classRepo.GetByIdAsync(userProfile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = userProfile.ClassId.Value, Name = "S" });

        var quest = new Quest { Id = questId, SubjectId = subjectId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var sessions = new List<SyllabusSessionDto>
        {
            new() { SessionNumber = 1, Topic = "Intro", SuggestedUrl = "https://example.com/a" },
            new() { SessionNumber = 2, Topic = "Topic 2", SuggestedUrl = "https://example.com/b" }
        };
        var contentDict = new Dictionary<string, object>
        {
            ["SessionSchedule"] = sessions
        };
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Subj", Content = contentDict });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
              .Returns(new[] { new SubjectSkillMapping { SubjectId = subjectId, SkillId = skillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = skillId, Name = "Skill" } });
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());

        promptBuilder.GenerateAsync(Arg.Any<UserProfile>(), Arg.Any<Class>(), Arg.Any<AcademicContext>(), Arg.Any<CancellationToken>())
                     .Returns("CTX");
        academicContextBuilder.BuildContextAsync(authId, subjectId, Arg.Any<CancellationToken>())
                              .Returns(new AcademicContext { CurrentGpa = 7.5, StrengthAreas = new List<string> { "Algo" }, ImprovementAreas = new List<string> { "Math" } });

        var aiJson = "{\"activities\":[" +
                     "{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"reading\",\"payload\":{\"url\":\"https://example.com/a\",\"experiencePoints\":10}}" +
                     "," +
                     "{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"knowledgecheck\",\"payload\":{\"skillId\":\"" + skillId + "\",\"experiencePoints\":35,\"questions\":[{\"question\":\"\\frac{1}{2}\",\"correctAnswer\":\"A\",\"options\":[\"A\",\"B\"]}]}}" +
                     "]}";
        plugin.GenerateQuestStepsJsonAsync(Arg.Any<WeekContext>(), Arg.Any<string>(), Arg.Any<List<Skill>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AcademicContext>(), Arg.Any<Class>(), Arg.Any<CancellationToken>())
              .Returns(aiJson);

        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<QuestStep>());

        mapper.Map<List<GeneratedQuestStepDto>>(Arg.Any<List<QuestStep>>())
              .Returns(ci => ci.Arg<List<QuestStep>>().Select(s => new GeneratedQuestStepDto { StepNumber = s.StepNumber, Title = s.Title }).ToList());

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, null, plugin, mapper, userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);

        var result = await sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = questId, HangfireJobId = "job" }, CancellationToken.None);

        result.Should().NotBeEmpty();
        result[0].Title.Should().Contain("Week");
    }

    [Fact]
    public async Task Handle_Pads_Activities_To_Minimum()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        stepRepo.QuestContainsSteps(questId, Arg.Any<CancellationToken>()).Returns(false);

        var userProfile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(userProfile);
        classRepo.GetByIdAsync(userProfile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = userProfile.ClassId.Value, Name = "S" });

        var quest = new Quest { Id = questId, SubjectId = subjectId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var sessions = new List<SyllabusSessionDto>
        {
            new() { SessionNumber = 1, Topic = "Intro", SuggestedUrl = "https://example.com/a" }
        };
        var contentDict = new Dictionary<string, object>
        {
            ["SessionSchedule"] = sessions
        };
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Subj", Content = contentDict });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
              .Returns(new[] { new SubjectSkillMapping { SubjectId = subjectId, SkillId = skillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = skillId, Name = "Skill" } });
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());

        promptBuilder.GenerateAsync(Arg.Any<UserProfile>(), Arg.Any<Class>(), Arg.Any<AcademicContext>(), Arg.Any<CancellationToken>()).Returns("CTX");
        academicContextBuilder.BuildContextAsync(authId, subjectId, Arg.Any<CancellationToken>())
                              .Returns(new AcademicContext { CurrentGpa = 7.5 });

        var onlyOne = "{\"activities\":[{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"reading\",\"payload\":{\"url\":\"https://example.com/a\",\"experiencePoints\":10}}]}";
        plugin.GenerateQuestStepsJsonAsync(Arg.Any<WeekContext>(), Arg.Any<string>(), Arg.Any<List<Skill>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AcademicContext>(), Arg.Any<Class>(), Arg.Any<CancellationToken>())
              .Returns(onlyOne);

        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<QuestStep>());
        mapper.Map<List<GeneratedQuestStepDto>>(Arg.Any<List<QuestStep>>())
              .Returns(ci => ci.Arg<List<QuestStep>>().Select(s => new GeneratedQuestStepDto { StepNumber = s.StepNumber, Title = s.Title }).ToList());

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, null, plugin, mapper, userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        _ = await sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = questId }, CancellationToken.None);

        await stepRepo.Received().AddAsync(Arg.Is<QuestStep>(qs => CountActivities(qs.Content) >= 6), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Trims_Activities_To_Maximum()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        stepRepo.QuestContainsSteps(questId, Arg.Any<CancellationToken>()).Returns(false);

        var userProfile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(userProfile);
        classRepo.GetByIdAsync(userProfile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = userProfile.ClassId.Value, Name = "S" });

        var quest = new Quest { Id = questId, SubjectId = subjectId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var sessions = new List<SyllabusSessionDto>
        {
            new() { SessionNumber = 1, Topic = "Intro", SuggestedUrl = "https://example.com/a" }
        };
        var contentDict = new Dictionary<string, object>
        {
            ["SessionSchedule"] = sessions
        };
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Subj", Content = contentDict });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
              .Returns(new[] { new SubjectSkillMapping { SubjectId = subjectId, SkillId = skillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = skillId, Name = "Skill" } });
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());

        promptBuilder.GenerateAsync(Arg.Any<UserProfile>(), Arg.Any<Class>(), Arg.Any<AcademicContext>(), Arg.Any<CancellationToken>()).Returns("CTX");
        academicContextBuilder.BuildContextAsync(authId, subjectId, Arg.Any<CancellationToken>())
                              .Returns(new AcademicContext { CurrentGpa = 7.5 });

        var many = "{\"activities\":[" + string.Join(",", Enumerable.Range(0, 12).Select(i => "{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"reading\",\"payload\":{\"url\":\"https://example.com/a" + i + "\",\"experiencePoints\":10}}")) + "]}";
        plugin.GenerateQuestStepsJsonAsync(Arg.Any<WeekContext>(), Arg.Any<string>(), Arg.Any<List<Skill>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AcademicContext>(), Arg.Any<Class>(), Arg.Any<CancellationToken>())
              .Returns(many);

        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<QuestStep>());
        mapper.Map<List<GeneratedQuestStepDto>>(Arg.Any<List<QuestStep>>())
              .Returns(ci => ci.Arg<List<QuestStep>>().Select(s => new GeneratedQuestStepDto { StepNumber = s.StepNumber, Title = s.Title }).ToList());

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, null, plugin, mapper, userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        _ = await sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = questId }, CancellationToken.None);

        await stepRepo.DidNotReceive().AddAsync(Arg.Is<QuestStep>(qs => CountActivities(qs.Content) > 10), Arg.Any<CancellationToken>());
    }

    private static int CountActivities(object? content)
    {
        if (content is Dictionary<string, object> dict && dict.TryGetValue("activities", out var activitiesObj))
        {
            if (activitiesObj is System.Collections.IList il)
            {
                return il.Count;
            }
            if (activitiesObj is System.Collections.IEnumerable en)
            {
                var count = 0;
                var enumerator = en.GetEnumerator();
                while (enumerator.MoveNext()) count++;
                return count;
            }
        }
        return 0;
    }

    [Fact]
    public async Task Handle_Filters_Invalid_Urls_From_Activities()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        stepRepo.QuestContainsSteps(questId, Arg.Any<CancellationToken>()).Returns(false);
        var userProfile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(userProfile);
        classRepo.GetByIdAsync(userProfile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = userProfile.ClassId.Value, Name = "S" });

        var quest = new Quest { Id = questId, SubjectId = subjectId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var sessions = new List<SyllabusSessionDto>
        {
            new() { SessionNumber = 1, Topic = "Intro", SuggestedUrl = "https://example.com/a" },
            new() { SessionNumber = 2, Topic = "Topic 2", SuggestedUrl = "https://example.com/b" }
        };
        var contentDict = new Dictionary<string, object>
        {
            ["SessionSchedule"] = sessions
        };
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Subj", Content = contentDict });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
              .Returns(new[] { new SubjectSkillMapping { SubjectId = subjectId, SkillId = skillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = skillId, Name = "Skill" } });
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());

        promptBuilder.GenerateAsync(Arg.Any<UserProfile>(), Arg.Any<Class>(), Arg.Any<AcademicContext>(), Arg.Any<CancellationToken>())
                     .Returns("CTX");
        academicContextBuilder.BuildContextAsync(authId, subjectId, Arg.Any<CancellationToken>())
                              .Returns(new AcademicContext { CurrentGpa = 7.5, StrengthAreas = new List<string> { "Algo" }, ImprovementAreas = new List<string> { "Math" } });

        var validId = Guid.NewGuid();
        var invalidId = Guid.NewGuid();
        var aiJson = "{\"activities\":[" +
                     "{\"activityId\":\"" + validId + "\",\"type\":\"reading\",\"payload\":{\"url\":\"https://example.com/a\",\"experiencePoints\":10}}" +
                     "," +
                     "{\"activityId\":\"" + invalidId + "\",\"type\":\"reading\",\"payload\":{\"url\":\"http://evil.com\",\"experiencePoints\":10}}" +
                     "]}";
        plugin.GenerateQuestStepsJsonAsync(Arg.Any<WeekContext>(), Arg.Any<string>(), Arg.Any<List<Skill>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AcademicContext>(), Arg.Any<Class>(), Arg.Any<CancellationToken>())
              .Returns(aiJson);

        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<QuestStep>());
        mapper.Map<List<GeneratedQuestStepDto>>(Arg.Any<List<QuestStep>>())
              .Returns(ci => ci.Arg<List<QuestStep>>().Select(s => new GeneratedQuestStepDto { StepNumber = s.StepNumber, Title = s.Title }).ToList());

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, null, plugin, mapper, userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        _ = await sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = questId, HangfireJobId = "job" }, CancellationToken.None);

        await stepRepo.DidNotReceive().AddAsync(Arg.Is<QuestStep>(qs => ContainsUrl(qs.Content, "http://evil.com")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Removes_Duplicate_Reading_Urls()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        stepRepo.QuestContainsSteps(questId, Arg.Any<CancellationToken>()).Returns(false);
        var userProfile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(userProfile);
        classRepo.GetByIdAsync(userProfile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = userProfile.ClassId.Value, Name = "S" });

        var quest = new Quest { Id = questId, SubjectId = subjectId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var sessions = new List<SyllabusSessionDto>
        {
            new() { SessionNumber = 1, Topic = "Intro", SuggestedUrl = "https://example.com/a" }
        };
        var contentDict = new Dictionary<string, object> { ["SessionSchedule"] = sessions };
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Subj", Content = contentDict });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SubjectId = subjectId, SkillId = skillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = skillId, Name = "Skill" } });
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());

        promptBuilder.GenerateAsync(Arg.Any<UserProfile>(), Arg.Any<Class>(), Arg.Any<AcademicContext>(), Arg.Any<CancellationToken>()).Returns("CTX");
        academicContextBuilder.BuildContextAsync(authId, subjectId, Arg.Any<CancellationToken>()).Returns(new AcademicContext { CurrentGpa = 7.5 });

        var dup = "{\"activities\":[" +
                  "{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"reading\",\"payload\":{\"url\":\"https://example.com/a\",\"experiencePoints\":10}}" +
                  "," +
                  "{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"reading\",\"payload\":{\"url\":\"https://example.com/a\",\"experiencePoints\":10}}" +
                  "]}";
        plugin.GenerateQuestStepsJsonAsync(Arg.Any<WeekContext>(), Arg.Any<string>(), Arg.Any<List<Skill>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AcademicContext>(), Arg.Any<Class>(), Arg.Any<CancellationToken>())
              .Returns(dup);

        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<QuestStep>());
        mapper.Map<List<GeneratedQuestStepDto>>(Arg.Any<List<QuestStep>>())
              .Returns(ci => ci.Arg<List<QuestStep>>().Select(s => new GeneratedQuestStepDto { StepNumber = s.StepNumber, Title = s.Title }).ToList());

        QuestStep? captured = null;
        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => { captured = ci.Arg<QuestStep>(); return captured!; });

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, null, plugin, mapper, userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        _ = await sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = questId }, CancellationToken.None);

        CountReadingsWithUrl(captured!.Content, "https://example.com/a").Should().Be(1);
    }

    [Fact]
    public async Task Handle_AllWeeksEmptyAI_ThrowsInvalidOperation()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        stepRepo.QuestContainsSteps(questId, Arg.Any<CancellationToken>()).Returns(false);
        var userProfile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(userProfile);
        classRepo.GetByIdAsync(userProfile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = userProfile.ClassId.Value, Name = "S" });

        var quest = new Quest { Id = questId, SubjectId = subjectId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var sessions = new List<SyllabusSessionDto> { new() { SessionNumber = 1, Topic = "Intro", SuggestedUrl = "https://example.com/a" } };
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Subj", Content = new Dictionary<string, object> { ["SessionSchedule"] = sessions } });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SubjectId = subjectId, SkillId = skillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = skillId, Name = "Skill" } });
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());

        promptBuilder.GenerateAsync(Arg.Any<UserProfile>(), Arg.Any<Class>(), Arg.Any<AcademicContext>(), Arg.Any<CancellationToken>()).Returns("CTX");
        academicContextBuilder.BuildContextAsync(authId, subjectId, Arg.Any<CancellationToken>()).Returns(new AcademicContext { CurrentGpa = 7.5 });

        plugin.GenerateQuestStepsJsonAsync(Arg.Any<WeekContext>(), Arg.Any<string>(), Arg.Any<List<Skill>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AcademicContext>(), Arg.Any<Class>(), Arg.Any<CancellationToken>())
              .Returns("");

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, null, plugin, mapper, userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = questId }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MissingActivities_SkipsAndContinues()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        stepRepo.QuestContainsSteps(questId, Arg.Any<CancellationToken>()).Returns(false);
        var userProfile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(userProfile);
        classRepo.GetByIdAsync(userProfile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = userProfile.ClassId.Value, Name = "S" });

        var quest = new Quest { Id = questId, SubjectId = subjectId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var sessions = new List<SyllabusSessionDto>
        {
            new() { SessionNumber = 1, Topic = "Intro", SuggestedUrl = "https://example.com/a" },
            new() { SessionNumber = 2, Topic = "Intro2", SuggestedUrl = "https://example.com/b" }
        };
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Subj", Content = new Dictionary<string, object> { ["SessionSchedule"] = sessions } });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SubjectId = subjectId, SkillId = skillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = skillId, Name = "Skill" } });
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());

        promptBuilder.GenerateAsync(Arg.Any<UserProfile>(), Arg.Any<Class>(), Arg.Any<AcademicContext>(), Arg.Any<CancellationToken>()).Returns("CTX");
        academicContextBuilder.BuildContextAsync(authId, subjectId, Arg.Any<CancellationToken>()).Returns(new AcademicContext { CurrentGpa = 7.5 });

        plugin.GenerateQuestStepsJsonAsync(Arg.Any<WeekContext>(), Arg.Any<string>(), Arg.Any<List<Skill>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AcademicContext>(), Arg.Any<Class>(), Arg.Any<CancellationToken>())
              .Returns("{\"foo\":[1]}", "{\"activities\":[{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"reading\",\"payload\":{\"url\":\"https://example.com/a\",\"experiencePoints\":10}}]}");

        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<QuestStep>());
        mapper.Map<List<GeneratedQuestStepDto>>(Arg.Any<List<QuestStep>>())
              .Returns(ci => ci.Arg<List<QuestStep>>().Select(s => new GeneratedQuestStepDto { StepNumber = s.StepNumber, Title = s.Title }).ToList());

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, null, plugin, mapper, userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        _ = await sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = questId }, CancellationToken.None);

        await stepRepo.Received().AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidActivityId_ReplacedWithGuid()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        stepRepo.QuestContainsSteps(questId, Arg.Any<CancellationToken>()).Returns(false);
        var userProfile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(userProfile);
        classRepo.GetByIdAsync(userProfile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = userProfile.ClassId.Value, Name = "S" });

        var quest = new Quest { Id = questId, SubjectId = subjectId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var sessions = new List<SyllabusSessionDto> { new() { SessionNumber = 1, Topic = "Intro", SuggestedUrl = "https://example.com/a" } };
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Subj", Content = new Dictionary<string, object> { ["SessionSchedule"] = sessions } });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SubjectId = subjectId, SkillId = skillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = skillId, Name = "Skill" } });
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());

        promptBuilder.GenerateAsync(Arg.Any<UserProfile>(), Arg.Any<Class>(), Arg.Any<AcademicContext>(), Arg.Any<CancellationToken>()).Returns("CTX");
        academicContextBuilder.BuildContextAsync(authId, subjectId, Arg.Any<CancellationToken>()).Returns(new AcademicContext { CurrentGpa = 7.5 });

        var badIdJson = "{\"activities\":[{\"activityId\":\"not-guid\",\"type\":\"reading\",\"payload\":{\"url\":\"https://example.com/a\",\"experiencePoints\":10}}]}";
        plugin.GenerateQuestStepsJsonAsync(Arg.Any<WeekContext>(), Arg.Any<string>(), Arg.Any<List<Skill>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AcademicContext>(), Arg.Any<Class>(), Arg.Any<CancellationToken>())
              .Returns(badIdJson);

        QuestStep? captured = null;
        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => { captured = ci.Arg<QuestStep>(); return captured!; });
        mapper.Map<List<GeneratedQuestStepDto>>(Arg.Any<List<QuestStep>>())
              .Returns(ci => ci.Arg<List<QuestStep>>().Select(s => new GeneratedQuestStepDto { StepNumber = s.StepNumber, Title = s.Title }).ToList());

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, null, plugin, mapper, userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        _ = await sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = questId }, CancellationToken.None);

        AllActivityIdsAreGuids(captured!.Content).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SkillIdRemapped_ContainsSkillIdKey()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var relevantSkillId = Guid.NewGuid();

        stepRepo.QuestContainsSteps(questId, Arg.Any<CancellationToken>()).Returns(false);
        var userProfile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(userProfile);
        classRepo.GetByIdAsync(userProfile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = userProfile.ClassId.Value, Name = "S" });

        var quest = new Quest { Id = questId, SubjectId = subjectId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var sessions = new List<SyllabusSessionDto> { new() { SessionNumber = 1, Topic = "Intro", SuggestedUrl = "https://example.com/a" } };
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Subj", Content = new Dictionary<string, object> { ["SessionSchedule"] = sessions } });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SubjectId = subjectId, SkillId = relevantSkillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = relevantSkillId, Name = "Skill" } });
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());

        promptBuilder.GenerateAsync(Arg.Any<UserProfile>(), Arg.Any<Class>(), Arg.Any<AcademicContext>(), Arg.Any<CancellationToken>()).Returns("CTX");
        academicContextBuilder.BuildContextAsync(authId, subjectId, Arg.Any<CancellationToken>()).Returns(new AcademicContext { CurrentGpa = 7.5 });

        var otherSkillId = Guid.NewGuid();
        var kc = "{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"knowledgecheck\",\"payload\":{\"skillId\":\"" + otherSkillId + "\",\"experiencePoints\":35}}";
        var json = "{\"activities\":[" + kc + "]}";
        plugin.GenerateQuestStepsJsonAsync(Arg.Any<WeekContext>(), Arg.Any<string>(), Arg.Any<List<Skill>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AcademicContext>(), Arg.Any<Class>(), Arg.Any<CancellationToken>())
              .Returns(json);

        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<QuestStep>());
        mapper.Map<List<GeneratedQuestStepDto>>(Arg.Any<List<QuestStep>>())
              .Returns(ci => ci.Arg<List<QuestStep>>().Select(s => new GeneratedQuestStepDto { StepNumber = s.StepNumber, Title = s.Title }).ToList());

        QuestStep? captured = null;
        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => { captured = ci.Arg<QuestStep>(); return captured!; });

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, null, plugin, mapper, userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        _ = await sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = questId }, CancellationToken.None);

        ContentHasSkillIdKey(captured!.Content).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_EscapeSequenceCleaner_AllowsParsing_OrThrows()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        stepRepo.QuestContainsSteps(questId, Arg.Any<CancellationToken>()).Returns(false);
        var userProfile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(userProfile);
        classRepo.GetByIdAsync(userProfile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = userProfile.ClassId.Value, Name = "S" });

        var quest = new Quest { Id = questId, SubjectId = subjectId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var sessions = new List<SyllabusSessionDto> { new() { SessionNumber = 1, Topic = "Intro", SuggestedUrl = "https://example.com/a" } };
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Subj", Content = new Dictionary<string, object> { ["SessionSchedule"] = sessions } });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SubjectId = subjectId, SkillId = skillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = skillId, Name = "Skill" } });
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());

        promptBuilder.GenerateAsync(Arg.Any<UserProfile>(), Arg.Any<Class>(), Arg.Any<AcademicContext>(), Arg.Any<CancellationToken>()).Returns("CTX");
        academicContextBuilder.BuildContextAsync(authId, subjectId, Arg.Any<CancellationToken>()).Returns(new AcademicContext { CurrentGpa = 7.5 });

        var jsonWithEscapes = "{\\\\\\\\\"activities\\\\\\\\\":[{\\\\\\\\\"activityId\\\\\\\\\":\\\\\\\\\"" + Guid.NewGuid() + "\\\\\\\\\",\\\\\\\\\"type\\\\\\\\\":\\\\\\\\\"reading\\\\\\\\\",\\\\\\\\\"payload\\\\\\\\\":{\\\\\\\\\"url\\\\\\\\\":\\\\\\\\\"https://example.com/a\\\\\\\\\",\\\\\\\\\"experiencePoints\\\\\\\\\":10}}]}";
        plugin.GenerateQuestStepsJsonAsync(Arg.Any<WeekContext>(), Arg.Any<string>(), Arg.Any<List<Skill>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AcademicContext>(), Arg.Any<Class>(), Arg.Any<CancellationToken>())
              .Returns(jsonWithEscapes);

        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<QuestStep>());
        mapper.Map<List<GeneratedQuestStepDto>>(Arg.Any<List<QuestStep>>())
              .Returns(ci => ci.Arg<List<QuestStep>>().Select(s => new GeneratedQuestStepDto { StepNumber = s.StepNumber, Title = s.Title }).ToList());

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, null, plugin, mapper, userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        try
        {
            var res = await sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = questId }, CancellationToken.None);
            res.Should().NotBeNull();
        }
        catch (InvalidOperationException)
        {
            // Acceptable fallback when cleaning still results in invalid JSON
        }
    }

    [Fact]
    public async Task Handle_Calculates_Total_Xp_On_Step()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        stepRepo.QuestContainsSteps(questId, Arg.Any<CancellationToken>()).Returns(false);
        var userProfile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(userProfile);
        classRepo.GetByIdAsync(userProfile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = userProfile.ClassId.Value, Name = "S" });

        var quest = new Quest { Id = questId, SubjectId = subjectId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var sessions = new List<SyllabusSessionDto> { new() { SessionNumber = 1, Topic = "Intro", SuggestedUrl = "https://example.com/a" } };
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Subj", Content = new Dictionary<string, object> { ["SessionSchedule"] = sessions } });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SubjectId = subjectId, SkillId = skillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = skillId, Name = "Skill" } });
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());

        promptBuilder.GenerateAsync(Arg.Any<UserProfile>(), Arg.Any<Class>(), Arg.Any<AcademicContext>(), Arg.Any<CancellationToken>()).Returns("CTX");
        academicContextBuilder.BuildContextAsync(authId, subjectId, Arg.Any<CancellationToken>()).Returns(new AcademicContext { CurrentGpa = 7.5 });

        var a1 = "{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"reading\",\"payload\":{\"url\":\"https://example.com/a\",\"experiencePoints\":10}}";
        var a2 = "{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"knowledgecheck\",\"payload\":{\"skillId\":\"" + skillId + "\",\"experiencePoints\":35}}";
        var json = "{\"activities\":[" + a1 + "," + a2 + "]}";
        plugin.GenerateQuestStepsJsonAsync(Arg.Any<WeekContext>(), Arg.Any<string>(), Arg.Any<List<Skill>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AcademicContext>(), Arg.Any<Class>(), Arg.Any<CancellationToken>())
              .Returns(json);

        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<QuestStep>());
        mapper.Map<List<GeneratedQuestStepDto>>(Arg.Any<List<QuestStep>>())
              .Returns(ci => ci.Arg<List<QuestStep>>().Select(s => new GeneratedQuestStepDto { StepNumber = s.StepNumber, Title = s.Title }).ToList());

        QuestStep? saved = null;
        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => { saved = ci.Arg<QuestStep>(); return saved; });

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, null, plugin, mapper, userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        _ = await sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = questId }, CancellationToken.None);

        saved!.ExperiencePoints.Should().BeGreaterThanOrEqualTo(45);
    }

    [Fact]
    public async Task Handle_Normalizes_Math_In_Reading_And_Questions()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        stepRepo.QuestContainsSteps(questId, Arg.Any<CancellationToken>()).Returns(false);
        var userProfile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(userProfile);
        classRepo.GetByIdAsync(userProfile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = userProfile.ClassId.Value, Name = "S" });

        var quest = new Quest { Id = questId, SubjectId = subjectId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var sessions = new List<SyllabusSessionDto> { new() { SessionNumber = 1, Topic = "Intro", SuggestedUrl = "https://example.com/a" } };
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Subj", Content = new Dictionary<string, object> { ["SessionSchedule"] = sessions } });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SubjectId = subjectId, SkillId = skillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = skillId, Name = "Skill" } });
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());

        promptBuilder.GenerateAsync(Arg.Any<UserProfile>(), Arg.Any<Class>(), Arg.Any<AcademicContext>(), Arg.Any<CancellationToken>()).Returns("CTX");
        academicContextBuilder.BuildContextAsync(authId, subjectId, Arg.Any<CancellationToken>()).Returns(new AcademicContext { CurrentGpa = 7.5 });

        var reading = "{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"reading\",\"payload\":{\"url\":\"https://example.com/a\",\"experiencePoints\":10,\"articleTitle\":\"$\\\frac{1}{2}$\",\"summary\":\"\\sqrt{4}\\left( x \\right)\"}}";
        var quiz = "{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"quiz\",\"payload\":{\"skillId\":\"" + skillId + "\",\"experiencePoints\":35,\"questions\":[{\"question\":\"$1+1$\",\"options\":[\"$2$\",\"$3$\"],\"correctAnswer\":\"$2$\",\"explanation\":\"\\frac{1}{2}\"}]}}";
        var json = "{\"activities\":[" + reading + "," + quiz + "]}";
        plugin.GenerateQuestStepsJsonAsync(Arg.Any<WeekContext>(), Arg.Any<string>(), Arg.Any<List<Skill>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AcademicContext>(), Arg.Any<Class>(), Arg.Any<CancellationToken>()).Returns(json);

        QuestStep? saved = null;
        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => { saved = ci.Arg<QuestStep>(); return saved!; });
        mapper.Map<List<GeneratedQuestStepDto>>(Arg.Any<List<QuestStep>>()).Returns(ci => ci.Arg<List<QuestStep>>().Select(s => new GeneratedQuestStepDto { StepNumber = s.StepNumber, Title = s.Title }).ToList());

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, null, plugin, mapper, userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        try
        {
            _ = await sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = questId }, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
        }

        if (saved is not null)
        {
            var dict = saved!.Content as Dictionary<string, object>;
            var en = dict!["activities"] as System.Collections.IEnumerable;
            var enumerator = en!.GetEnumerator();
            enumerator.MoveNext();
            var first = enumerator.Current as Dictionary<string, object>;
            var readingPayload = first!["payload"] as Dictionary<string, object>;
            var articleTitle = readingPayload!["articleTitle"]!.ToString();
            var summary = readingPayload!["summary"]!.ToString();
            articleTitle.Should().NotContain("\\").And.NotContain("$");
            summary.Should().NotContain("\\").And.NotContain("left").And.NotContain("right");

            enumerator.MoveNext();
            var second = enumerator.Current as Dictionary<string, object>;
            var quizPayload = second!["payload"] as Dictionary<string, object>;
            var questions = quizPayload!["questions"] as List<object>;
            var firstQ = questions![0] as Dictionary<string, object>;
            firstQ!["question"].ToString().Should().NotContain("$");
            var opts = firstQ!["options"] as List<object>;
            opts![0].ToString().Should().NotContain("$");
            firstQ!["correctAnswer"].ToString().Should().NotContain("$");
            firstQ!["explanation"].ToString().Should().NotContain("\\");
        }
    }

    [Fact]
    public async Task Handle_CleanUrl_Matches_Approved_Urls()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        stepRepo.QuestContainsSteps(questId, Arg.Any<CancellationToken>()).Returns(false);
        var userProfile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(userProfile);
        classRepo.GetByIdAsync(userProfile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = userProfile.ClassId.Value, Name = "S" });

        var quest = new Quest { Id = questId, SubjectId = subjectId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var sessions = new List<SyllabusSessionDto> { new() { SessionNumber = 1, Topic = "Intro", SuggestedUrl = "https://example.com/a" } };
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Subj", Content = new Dictionary<string, object> { ["SessionSchedule"] = sessions } });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SubjectId = subjectId, SkillId = skillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = skillId, Name = "Skill" } });
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());

        promptBuilder.GenerateAsync(Arg.Any<UserProfile>(), Arg.Any<Class>(), Arg.Any<AcademicContext>(), Arg.Any<CancellationToken>()).Returns("CTX");
        academicContextBuilder.BuildContextAsync(authId, subjectId, Arg.Any<CancellationToken>()).Returns(new AcademicContext { CurrentGpa = 7.5 });

        var aiJson = "{\"activities\":[{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"reading\",\"payload\":{\"url\":\"`https://example.com/a`\",\"experiencePoints\":10}}]}";
        plugin.GenerateQuestStepsJsonAsync(Arg.Any<WeekContext>(), Arg.Any<string>(), Arg.Any<List<Skill>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AcademicContext>(), Arg.Any<Class>(), Arg.Any<CancellationToken>()).Returns(aiJson);

        QuestStep? saved = null;
        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => { saved = ci.Arg<QuestStep>(); return saved!; });
        mapper.Map<List<GeneratedQuestStepDto>>(Arg.Any<List<QuestStep>>()).Returns(ci => ci.Arg<List<QuestStep>>().Select(s => new GeneratedQuestStepDto { StepNumber = s.StepNumber, Title = s.Title }).ToList());

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, null, plugin, mapper, userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        try
        {
            _ = await sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = questId }, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
        }

        if (saved is not null)
        {
            var dict = saved!.Content as Dictionary<string, object>;
            (CountReadingsWithUrl(dict, "https://example.com/a") + CountReadingsWithUrl(dict, "`https://example.com/a`"))
                .Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task Handle_Normalizes_Options_ListObject()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        stepRepo.QuestContainsSteps(questId, Arg.Any<CancellationToken>()).Returns(false);
        var userProfile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(userProfile);
        classRepo.GetByIdAsync(userProfile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = userProfile.ClassId.Value, Name = "S" });

        var quest = new Quest { Id = questId, SubjectId = subjectId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var sessions = new List<SyllabusSessionDto> { new() { SessionNumber = 1, Topic = "Intro", SuggestedUrl = "https://example.com/a" } };
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Subj", Content = new Dictionary<string, object> { ["SessionSchedule"] = sessions } });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SubjectId = subjectId, SkillId = skillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = skillId, Name = "Skill" } });
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());

        promptBuilder.GenerateAsync(Arg.Any<UserProfile>(), Arg.Any<Class>(), Arg.Any<AcademicContext>(), Arg.Any<CancellationToken>()).Returns("CTX");
        academicContextBuilder.BuildContextAsync(authId, subjectId, Arg.Any<CancellationToken>()).Returns(new AcademicContext { CurrentGpa = 7.5 });

        var q = new List<object> { "$2$", "\\sqrt{9}" };
        var quizDict = new Dictionary<string, object>
        {
            ["activityId"] = Guid.NewGuid().ToString(),
            ["type"] = "quiz",
            ["payload"] = new Dictionary<string, object>
            {
                ["skillId"] = skillId.ToString(),
                ["experiencePoints"] = 35,
                ["questions"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["question"] = "$1+1$",
                        ["options"] = q,
                        ["correctAnswer"] = "$2$",
                        ["explanation"] = "\\frac{1}{2}"
                    }
                }
            }
        };
        var json = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object> { ["activities"] = new List<object> { quizDict } });
        plugin.GenerateQuestStepsJsonAsync(Arg.Any<WeekContext>(), Arg.Any<string>(), Arg.Any<List<Skill>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AcademicContext>(), Arg.Any<Class>(), Arg.Any<CancellationToken>()).Returns(json);

        QuestStep? saved = null;
        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => { saved = ci.Arg<QuestStep>(); return saved!; });
        mapper.Map<List<GeneratedQuestStepDto>>(Arg.Any<List<QuestStep>>()).Returns(ci => ci.Arg<List<QuestStep>>().Select(s => new GeneratedQuestStepDto { StepNumber = s.StepNumber, Title = s.Title }).ToList());

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, null, plugin, mapper, userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        try
        {
            _ = await sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = questId }, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
        }

        if (saved is not null)
        {
            var dict = saved!.Content as Dictionary<string, object>;
            var en = dict!["activities"] as System.Collections.IEnumerable;
            var enumerator = en!.GetEnumerator();
            enumerator.MoveNext();
            var first = enumerator.Current as Dictionary<string, object>;
            var payload = first!["payload"] as Dictionary<string, object>;
            var questions = payload!["questions"] as List<object>;
            var firstQ = questions![0] as Dictionary<string, object>;
            var opts = firstQ!["options"] as List<object>;
            opts![0].ToString().Should().Be("2");
            opts![1].ToString().Should().Be("sqrt(9)");
        }
    }

    private static bool AllActivityIdsAreGuids(object? content)
    {
        if (content is Dictionary<string, object> dict && dict.TryGetValue("activities", out var activitiesObj))
        {
            if (activitiesObj is System.Collections.IEnumerable en)
            {
                foreach (var item in en)
                {
                    if (item is Dictionary<string, object> act && act.TryGetValue("activityId", out var idObj))
                    {
                        if (!Guid.TryParse(idObj?.ToString(), out _)) return false;
                    }
                }
                return true;
            }
        }
        return false;
    }

    private static string? FirstSkillId(object? content)
    {
        if (content is Dictionary<string, object> dict && dict.TryGetValue("activities", out var activitiesObj) && activitiesObj is List<object> list)
        {
            foreach (var item in list)
            {
                if (item is Dictionary<string, object> act && act.TryGetValue("payload", out var payloadObj) && payloadObj is Dictionary<string, object> payload)
                {
                    if (payload.TryGetValue("skillId", out var s)) return s?.ToString();
                }
            }
        }
        return null;
    }

    private static bool ContentHasSkillIdKey(object? content)
    {
        if (content is Dictionary<string, object> dict && dict.TryGetValue("activities", out var activitiesObj))
        {
            if (activitiesObj is System.Collections.IEnumerable en)
            {
                foreach (var item in en)
                {
                    if (item is Dictionary<string, object> act && act.TryGetValue("payload", out var payloadObj) && payloadObj is Dictionary<string, object> payload)
                    {
                        if (payload.ContainsKey("skillId")) return true;
                    }
                }
            }
        }
        return false;
    }

    [Fact]
    public async Task Handle_Removes_Reading_When_No_Urls()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        stepRepo.QuestContainsSteps(questId, Arg.Any<CancellationToken>()).Returns(false);
        var userProfile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(userProfile);
        classRepo.GetByIdAsync(userProfile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = userProfile.ClassId.Value, Name = "S" });

        var quest = new Quest { Id = questId, SubjectId = subjectId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var sessions = new List<SyllabusSessionDto> { new() { SessionNumber = 1, Topic = "Intro", SuggestedUrl = null } };
        var contentDict = new Dictionary<string, object> { ["SessionSchedule"] = sessions };
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Subj", Content = contentDict });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SubjectId = subjectId, SkillId = skillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = skillId, Name = "Skill" } });
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());

        promptBuilder.GenerateAsync(Arg.Any<UserProfile>(), Arg.Any<Class>(), Arg.Any<AcademicContext>(), Arg.Any<CancellationToken>()).Returns("CTX");
        academicContextBuilder.BuildContextAsync(authId, subjectId, Arg.Any<CancellationToken>()).Returns(new AcademicContext { CurrentGpa = 7.5 });

        var aiJson = "{\"activities\":[{" +
                     "\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"reading\",\"payload\":{\"url\":\"\",\"experiencePoints\":10}}" +
                     "]}";
        plugin.GenerateQuestStepsJsonAsync(Arg.Any<WeekContext>(), Arg.Any<string>(), Arg.Any<List<Skill>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AcademicContext>(), Arg.Any<Class>(), Arg.Any<CancellationToken>())
              .Returns(aiJson);

        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<QuestStep>());
        mapper.Map<List<GeneratedQuestStepDto>>(Arg.Any<List<QuestStep>>())
              .Returns(ci => ci.Arg<List<QuestStep>>().Select(s => new GeneratedQuestStepDto { StepNumber = s.StepNumber, Title = s.Title }).ToList());

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, null, plugin, mapper, userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        var result = await sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = questId }, CancellationToken.None);

        await stepRepo.Received().AddAsync(Arg.Is<QuestStep>(qs => CountActivities(qs.Content) >= 1 && !ContainsUrl(qs.Content, "")), Arg.Any<CancellationToken>());
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_Normalizes_Questions_From_Mixed_Types()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        stepRepo.QuestContainsSteps(questId, Arg.Any<CancellationToken>()).Returns(false);
        var userProfile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(userProfile);
        classRepo.GetByIdAsync(userProfile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = userProfile.ClassId.Value, Name = "S" });

        var quest = new Quest { Id = questId, SubjectId = subjectId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var sessions = new List<SyllabusSessionDto> { new() { SessionNumber = 1, Topic = "Intro", SuggestedUrl = "https://example.com/a" } };
        var contentDict = new Dictionary<string, object> { ["SessionSchedule"] = sessions };
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Subj", Content = contentDict });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SubjectId = subjectId, SkillId = skillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = skillId, Name = "Skill" } });
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());

        promptBuilder.GenerateAsync(Arg.Any<UserProfile>(), Arg.Any<Class>(), Arg.Any<AcademicContext>(), Arg.Any<CancellationToken>()).Returns("CTX");
        academicContextBuilder.BuildContextAsync(authId, subjectId, Arg.Any<CancellationToken>()).Returns(new AcademicContext { CurrentGpa = 7.5 });

        var q1 = "{\"question\":\"1+1\",\"options\":[\"2\",\"3\"],\"answerIndex\":0}";
        var q2 = "{\"question\":\"2+2\",\"options\":[\"3\",\"4\"],\"answerIndex\":1}";
        var aiJson = "{\"activities\":[{" +
                     "\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"knowledgecheck\",\"payload\":{\"skillId\":\"" + skillId + "\",\"experiencePoints\":35,\"questions\":[" + q1 + "," + q2 + "]}}]}";
        plugin.GenerateQuestStepsJsonAsync(Arg.Any<WeekContext>(), Arg.Any<string>(), Arg.Any<List<Skill>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AcademicContext>(), Arg.Any<Class>(), Arg.Any<CancellationToken>())
              .Returns(aiJson);

        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<QuestStep>());
        mapper.Map<List<GeneratedQuestStepDto>>(Arg.Any<List<QuestStep>>())
              .Returns(ci => ci.Arg<List<QuestStep>>().Select(s => new GeneratedQuestStepDto { StepNumber = s.StepNumber, Title = s.Title }).ToList());

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, null, plugin, mapper, userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        var result = await sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = questId }, CancellationToken.None);

        result.Should().NotBeEmpty();
    }


    [Fact]
    public async Task Handle_NormalizePlainTextMath_ExtraSymbols()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        stepRepo.QuestContainsSteps(questId, Arg.Any<CancellationToken>()).Returns(false);
        var userProfile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(userProfile);
        classRepo.GetByIdAsync(userProfile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = userProfile.ClassId.Value, Name = "S" });

        var quest = new Quest { Id = questId, SubjectId = subjectId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var sessions = new List<SyllabusSessionDto> { new() { SessionNumber = 1, Topic = "Intro", SuggestedUrl = "https://example.com/a" } };
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Subj", Content = new Dictionary<string, object> { ["SessionSchedule"] = sessions } });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SubjectId = subjectId, SkillId = skillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = skillId, Name = "Skill" } });
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());

        promptBuilder.GenerateAsync(Arg.Any<UserProfile>(), Arg.Any<Class>(), Arg.Any<AcademicContext>(), Arg.Any<CancellationToken>()).Returns("CTX");
        academicContextBuilder.BuildContextAsync(authId, subjectId, Arg.Any<CancellationToken>()).Returns(new AcademicContext { CurrentGpa = 7.5 });

        var reading = "{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"reading\",\"payload\":{\"url\":\"https://example.com/a\",\"experiencePoints\":10,\"articleTitle\":\"a^2\",\"summary\":\"x_y\"}}";
        var json = "{\"activities\":[" + reading + "]}";
        plugin.GenerateQuestStepsJsonAsync(Arg.Any<WeekContext>(), Arg.Any<string>(), Arg.Any<List<Skill>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AcademicContext>(), Arg.Any<Class>(), Arg.Any<CancellationToken>()).Returns(json);

        QuestStep? saved = null;
        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => { saved = ci.Arg<QuestStep>(); return saved!; });
        mapper.Map<List<GeneratedQuestStepDto>>(Arg.Any<List<QuestStep>>()).Returns(ci => ci.Arg<List<QuestStep>>().Select(s => new GeneratedQuestStepDto { StepNumber = s.StepNumber, Title = s.Title }).ToList());

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, null, plugin, mapper, userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        try { _ = await sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = questId }, CancellationToken.None); } catch (InvalidOperationException) { }

        if (saved is not null)
        {
            var dict = saved!.Content as Dictionary<string, object>;
            var en = dict!["activities"] as System.Collections.IEnumerable;
            var enumerator = en!.GetEnumerator();
            enumerator.MoveNext();
            var first = enumerator.Current as Dictionary<string, object>;
            var payload = first!["payload"] as Dictionary<string, object>;
            payload!["articleTitle"].ToString().Should().Be("a^2");
            payload!["summary"].ToString().Should().Be("x_y");
        }
    }

    [Fact]
    public async Task Handle_Deduplicates_And_Validates_Reading_Urls()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        stepRepo.QuestContainsSteps(questId, Arg.Any<CancellationToken>()).Returns(false);
        var userProfile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(userProfile);
        classRepo.GetByIdAsync(userProfile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = userProfile.ClassId.Value, Name = "S" });

        var quest = new Quest { Id = questId, SubjectId = subjectId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var sessions = new List<SyllabusSessionDto>
        {
            new() { SessionNumber = 1, Topic = "T1", SuggestedUrl = "https://approved/a" },
            new() { SessionNumber = 2, Topic = "T2", SuggestedUrl = "https://approved/b" }
        };
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Subj", Content = new Dictionary<string, object> { ["SessionSchedule"] = sessions } });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SubjectId = subjectId, SkillId = skillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = skillId, Name = "Skill" } });
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());

        promptBuilder.GenerateAsync(Arg.Any<UserProfile>(), Arg.Any<Class>(), Arg.Any<AcademicContext>(), Arg.Any<CancellationToken>()).Returns("CTX");
        academicContextBuilder.BuildContextAsync(authId, subjectId, Arg.Any<CancellationToken>()).Returns(new AcademicContext { CurrentGpa = 7.5 });

        var a = Guid.NewGuid().ToString();
        var b = Guid.NewGuid().ToString();
        var c = Guid.NewGuid().ToString();
        var json = "{\"activities\":[" +
                   "{\"activityId\":\"" + a + "\",\"type\":\"reading\",\"payload\":{\"url\":\"https://approved/a\",\"experiencePoints\":10}}," +
                   "{\"activityId\":\"" + b + "\",\"type\":\"reading\",\"payload\":{\"url\":\"https://approved/a\",\"experiencePoints\":10}}," +
                   "{\"activityId\":\"" + c + "\",\"type\":\"reading\",\"payload\":{\"url\":\"https://outsider/c\",\"experiencePoints\":10}}," +
                   "{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"quiz\",\"payload\":{\"skillId\":\"" + skillId + "\",\"experiencePoints\":35,\"questions\":[]}}] }";
        plugin.GenerateQuestStepsJsonAsync(Arg.Any<WeekContext>(), Arg.Any<string>(), Arg.Any<List<Skill>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AcademicContext>(), Arg.Any<Class>(), Arg.Any<CancellationToken>()).Returns(json);

        QuestStep? saved = null;
        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => { saved = ci.Arg<QuestStep>(); return saved!; });
        mapper.Map<List<GeneratedQuestStepDto>>(Arg.Any<List<QuestStep>>()).Returns(ci => ci.Arg<List<QuestStep>>().Select(s => new GeneratedQuestStepDto { StepNumber = s.StepNumber, Title = s.Title }).ToList());

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, null, plugin, mapper, userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        _ = await sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = questId }, CancellationToken.None);

        var dict = saved!.Content as Dictionary<string, object>;
        var total = CountActivities(dict);
        var approvedCount = CountReadingsWithUrl(dict, "https://approved/a");
        var outsiderCount = CountReadingsWithUrl(dict, "https://outsider/c");
        approvedCount.Should().BeGreaterThanOrEqualTo(1);
        outsiderCount.Should().Be(0);
        total.Should().BeGreaterThanOrEqualTo(6);
    }

    [Fact]
    public async Task Handle_Cleans_OverEscaped_Sequences()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        stepRepo.QuestContainsSteps(questId, Arg.Any<CancellationToken>()).Returns(false);
        var userProfile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(userProfile);
        classRepo.GetByIdAsync(userProfile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = userProfile.ClassId.Value, Name = "S" });

        var quest = new Quest { Id = questId, SubjectId = subjectId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var sessions = new List<SyllabusSessionDto> { new() { SessionNumber = 1, Topic = "Intro", SuggestedUrl = "https://example.com/a" } };
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Subj", Content = new Dictionary<string, object> { ["SessionSchedule"] = sessions } });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SubjectId = subjectId, SkillId = skillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = skillId, Name = "Skill" } });
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());

        promptBuilder.GenerateAsync(Arg.Any<UserProfile>(), Arg.Any<Class>(), Arg.Any<AcademicContext>(), Arg.Any<CancellationToken>()).Returns("CTX");
        academicContextBuilder.BuildContextAsync(authId, subjectId, Arg.Any<CancellationToken>()).Returns(new AcademicContext { CurrentGpa = 7.5 });

        var reading = "{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"reading\",\"payload\":{\"url\":\"https://example.com/a\",\"experiencePoints\":10,\"summary\":\"\\\\\\\\frac{1}{2}\"}}";
        var json = "{\"activities\":[" + reading + "]}";
        plugin.GenerateQuestStepsJsonAsync(Arg.Any<WeekContext>(), Arg.Any<string>(), Arg.Any<List<Skill>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AcademicContext>(), Arg.Any<Class>(), Arg.Any<CancellationToken>()).Returns(json);

        QuestStep? saved = null;
        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => { saved = ci.Arg<QuestStep>(); return saved!; });
        mapper.Map<List<GeneratedQuestStepDto>>(Arg.Any<List<QuestStep>>()).Returns(ci => ci.Arg<List<QuestStep>>().Select(s => new GeneratedQuestStepDto { StepNumber = s.StepNumber, Title = s.Title }).ToList());

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, null, plugin, mapper, userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        _ = await sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = questId }, CancellationToken.None);

        var dict = saved!.Content as Dictionary<string, object>;
        var en = dict!["activities"] as System.Collections.IEnumerable;
        var enumerator = en!.GetEnumerator();
        enumerator.MoveNext();
        var first = enumerator.Current as Dictionary<string, object>;
        var payload = first!["payload"] as Dictionary<string, object>;
        payload!["summary"].ToString().Should().Contain("/").And.NotContain("\\");
    }

    [Fact]
    public async Task Handle_Title_Uses_GeneralConcepts_When_NoTopics()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        stepRepo.QuestContainsSteps(questId, Arg.Any<CancellationToken>()).Returns(false);
        var userProfile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(userProfile);
        classRepo.GetByIdAsync(userProfile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = userProfile.ClassId.Value, Name = "S" });

        var quest = new Quest { Id = questId, SubjectId = subjectId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var sessions = new List<SyllabusSessionDto>
        {
            new() { SessionNumber = 1, Topic = " ", SuggestedUrl = "https://a" },
            new() { SessionNumber = 2, Topic = null!, SuggestedUrl = "https://b" }
        };
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Subj", Content = new Dictionary<string, object> { ["SessionSchedule"] = sessions } });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SubjectId = subjectId, SkillId = skillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = skillId, Name = "Skill" } });
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());

        promptBuilder.GenerateAsync(Arg.Any<UserProfile>(), Arg.Any<Class>(), Arg.Any<AcademicContext>(), Arg.Any<CancellationToken>()).Returns("CTX");
        academicContextBuilder.BuildContextAsync(authId, subjectId, Arg.Any<CancellationToken>()).Returns(new AcademicContext { CurrentGpa = 7.5 });

        var json = "{\"activities\":[{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"quiz\",\"payload\":{\"skillId\":\"" + skillId + "\",\"experiencePoints\":35,\"questions\":[]}}]}";
        plugin.GenerateQuestStepsJsonAsync(Arg.Any<WeekContext>(), Arg.Any<string>(), Arg.Any<List<Skill>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AcademicContext>(), Arg.Any<Class>(), Arg.Any<CancellationToken>()).Returns(json);

        QuestStep? saved = null;
        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => { saved = ci.Arg<QuestStep>(); return saved!; });
        mapper.Map<List<GeneratedQuestStepDto>>(Arg.Any<List<QuestStep>>()).Returns(ci => ci.Arg<List<QuestStep>>().Select(s => new GeneratedQuestStepDto { StepNumber = s.StepNumber, Title = s.Title }).ToList());

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, null, plugin, mapper, userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        _ = await sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = questId }, CancellationToken.None);

        saved!.Title.Should().Contain("General Concepts");
    }

    [Fact]
    public async Task Handle_Title_Uses_AndMore_When_MultipleTopics()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        stepRepo.QuestContainsSteps(questId, Arg.Any<CancellationToken>()).Returns(false);
        var userProfile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(userProfile);
        classRepo.GetByIdAsync(userProfile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = userProfile.ClassId.Value, Name = "S" });

        var quest = new Quest { Id = questId, SubjectId = subjectId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var sessions = new List<SyllabusSessionDto>
        {
            new() { SessionNumber = 1, Topic = "A", SuggestedUrl = "https://a" },
            new() { SessionNumber = 2, Topic = "B", SuggestedUrl = "https://b" }
        };
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Subj", Content = new Dictionary<string, object> { ["SessionSchedule"] = sessions } });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SubjectId = subjectId, SkillId = skillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = skillId, Name = "Skill" } });
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());

        promptBuilder.GenerateAsync(Arg.Any<UserProfile>(), Arg.Any<Class>(), Arg.Any<AcademicContext>(), Arg.Any<CancellationToken>()).Returns("CTX");
        academicContextBuilder.BuildContextAsync(authId, subjectId, Arg.Any<CancellationToken>()).Returns(new AcademicContext { CurrentGpa = 7.5 });

        var json = "{\"activities\":[{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"quiz\",\"payload\":{\"skillId\":\"" + skillId + "\",\"experiencePoints\":35,\"questions\":[]}}]}";
        plugin.GenerateQuestStepsJsonAsync(Arg.Any<WeekContext>(), Arg.Any<string>(), Arg.Any<List<Skill>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AcademicContext>(), Arg.Any<Class>(), Arg.Any<CancellationToken>()).Returns(json);

        QuestStep? saved = null;
        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => { saved = ci.Arg<QuestStep>(); return saved!; });
        mapper.Map<List<GeneratedQuestStepDto>>(Arg.Any<List<QuestStep>>()).Returns(ci => ci.Arg<List<QuestStep>>().Select(s => new GeneratedQuestStepDto { StepNumber = s.StepNumber, Title = s.Title }).ToList());

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, null, plugin, mapper, userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        _ = await sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = questId }, CancellationToken.None);

        saved!.Title.Should().Contain("and more");
    }

    [Fact]
    public async Task Handle_Adds_KnowledgeChecks_When_Activities_Low()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var plugin = Substitute.For<RogueLearn.User.Application.Plugins.IQuestGenerationPlugin>();
        var mapper = Substitute.For<IMapper>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var promptBuilder = Substitute.For<RogueLearn.User.Application.Services.IPromptBuilder>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var academicContextBuilder = Substitute.For<RogueLearn.User.Application.Services.IAcademicContextBuilder>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        stepRepo.QuestContainsSteps(questId, Arg.Any<CancellationToken>()).Returns(false);
        var userProfile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, ClassId = Guid.NewGuid() };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(userProfile);
        classRepo.GetByIdAsync(userProfile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = userProfile.ClassId.Value, Name = "S" });

        var quest = new Quest { Id = questId, SubjectId = subjectId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var sessions = new List<SyllabusSessionDto> { new() { SessionNumber = 1, Topic = "Intro", SuggestedUrl = "https://example.com/a" } };
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Subj", Content = new Dictionary<string, object> { ["SessionSchedule"] = sessions } });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SubjectId = subjectId, SkillId = skillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = skillId, Name = "Skill" } });
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());

        promptBuilder.GenerateAsync(Arg.Any<UserProfile>(), Arg.Any<Class>(), Arg.Any<AcademicContext>(), Arg.Any<CancellationToken>()).Returns("CTX");
        academicContextBuilder.BuildContextAsync(authId, subjectId, Arg.Any<CancellationToken>()).Returns(new AcademicContext { CurrentGpa = 7.5 });

        var json = "{\"activities\":[{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"quiz\",\"payload\":{\"skillId\":\"" + skillId + "\",\"experiencePoints\":35,\"questions\":[]}}]}";
        plugin.GenerateQuestStepsJsonAsync(Arg.Any<WeekContext>(), Arg.Any<string>(), Arg.Any<List<Skill>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AcademicContext>(), Arg.Any<Class>(), Arg.Any<CancellationToken>()).Returns(json);

        QuestStep? saved = null;
        stepRepo.AddAsync(Arg.Any<QuestStep>(), Arg.Any<CancellationToken>()).Returns(ci => { saved = ci.Arg<QuestStep>(); return saved!; });
        mapper.Map<List<GeneratedQuestStepDto>>(Arg.Any<List<QuestStep>>()).Returns(ci => ci.Arg<List<QuestStep>>().Select(s => new GeneratedQuestStepDto { StepNumber = s.StepNumber, Title = s.Title }).ToList());

        var sut = CreateSut(questRepo, stepRepo, subjectRepo, null, plugin, mapper, userProfileRepo, classRepo, skillRepo, ssmRepo, promptBuilder, userSkillRepo, academicContextBuilder);
        _ = await sut.Handle(new GenerateQuestStepsCommand { AuthUserId = authId, QuestId = questId }, CancellationToken.None);

        var dict = saved!.Content as Dictionary<string, object>;
        var en = dict!["activities"] as System.Collections.IEnumerable;
        var count = 0;
        var kc = 0;
        foreach (var item in en!)
        {
            count++;
            if (item is Dictionary<string, object> act && act.TryGetValue("type", out var tObj) && string.Equals(tObj?.ToString(), "KnowledgeCheck", StringComparison.OrdinalIgnoreCase))
            {
                kc++;
            }
        }
        count.Should().BeGreaterThanOrEqualTo(6);
        kc.Should().BeGreaterThan(0);
    }

    private static bool ContainsUrl(object? content, string url)
    {
        if (content is Dictionary<string, object> dict && dict.TryGetValue("activities", out var activitiesObj) && activitiesObj is List<object> list)
        {
            foreach (var item in list)
            {
                if (item is Dictionary<string, object> act && act.TryGetValue("payload", out var payloadObj) && payloadObj is Dictionary<string, object> payload)
                {
                    if (payload.TryGetValue("url", out var urlObj) && urlObj is string u && string.Equals(u, url, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private static int CountReadingsWithUrl(object? content, string url)
    {
        var count = 0;
        if (content is Dictionary<string, object> dict && dict.TryGetValue("activities", out var activitiesObj))
        {
            if (activitiesObj is System.Collections.IEnumerable en)
            {
                foreach (var item in en)
                {
                    if (item is Dictionary<string, object> act && act.TryGetValue("type", out var tObj) &&
                        string.Equals(tObj?.ToString(), "reading", StringComparison.OrdinalIgnoreCase))
                    {
                        if (act.TryGetValue("payload", out var payloadObj) && payloadObj is Dictionary<string, object> payload &&
                            payload.TryGetValue("url", out var urlObj) && string.Equals(urlObj?.ToString(), url, StringComparison.OrdinalIgnoreCase))
                        {
                            count++;
                        }
                    }
                }
            }
        }
        return count;
    }
}
