using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Quests.Queries.GetAdminQuestDetails;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Quests.Queries.GetAdminQuestDetails;

public class GetAdminQuestDetailsQueryHandlerTests
{
    [Fact]
    public async Task Handle_QuestNotFound_ReturnsNull()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GetAdminQuestDetailsQueryHandler>>();

        questRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Quest?)null);
        var sut = new GetAdminQuestDetailsQueryHandler(questRepo, stepRepo, subjectRepo, logger);
        var res = await sut.Handle(new GetAdminQuestDetailsQuery { QuestId = Guid.NewGuid() }, CancellationToken.None);
        res.Should().BeNull();
    }

    [Fact]
    public async Task Handle_SubjectAndStepsGrouped_ContentParsed()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GetAdminQuestDetailsQueryHandler>>();

        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var quest = new Quest { Id = questId, Title = "T", Description = "D", SubjectId = subjectId, IsActive = true };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectCode = "CODE", SubjectName = "NAME" });

        var st1 = new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 2, ModuleNumber = 1, Title = "B", Description = "b", ExperiencePoints = 10, DifficultyVariant = "Supportive", Content = new Dictionary<string, object> { ["x"] = 1 } };
        var st2 = new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 1, ModuleNumber = 1, Title = "A", Description = "a", ExperiencePoints = 5, DifficultyVariant = "Standard", Content = "{\"a\":1}" };
        var st3 = new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 3, ModuleNumber = 2, Title = "C", Description = "c", ExperiencePoints = 20, DifficultyVariant = "Challenging", Content = "invalid json" };
        stepRepo.GetByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new[] { st1, st2, st3 });

        var sut = new GetAdminQuestDetailsQueryHandler(questRepo, stepRepo, subjectRepo, logger);
        var res = await sut.Handle(new GetAdminQuestDetailsQuery { QuestId = questId }, CancellationToken.None);

        res.Should().NotBeNull();
        res!.SubjectCode.Should().Be("CODE");
        res.SubjectName.Should().Be("NAME");
        res.StandardSteps.Should().HaveCount(1);
        res.SupportiveSteps.Should().HaveCount(1);
        res.ChallengingSteps.Should().HaveCount(1);

        res.StandardSteps[0].Content.Should().NotBeNull();
        res.StandardSteps[0].Content.Should().NotBeOfType<string>();
        res.SupportiveSteps[0].Content.Should().BeAssignableTo<Dictionary<string, object>>();
        res.ChallengingSteps[0].Content.Should().Be("invalid json");
        res.StandardSteps[0].StepNumber.Should().Be(1);
    }
}
