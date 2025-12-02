using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Quests.Commands.GenerateQuestSteps;

public class GenerateQuestStepsCommandHandlerTests
{
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
}