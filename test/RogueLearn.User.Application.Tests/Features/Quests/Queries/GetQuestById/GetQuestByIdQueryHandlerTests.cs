using AutoMapper;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestById;
using RogueLearn.User.Application.Mappings;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Quests.Queries.GetQuestById;

public class GetQuestByIdQueryHandlerTests
{
    private static IMapper CreateMapperStub()
    {
        var mapper = Substitute.For<IMapper>();
        mapper.Map<QuestDetailsDto>(Arg.Any<Quest>())
            .Returns(ci => {
                var q = ci.Arg<Quest>();
                return new QuestDetailsDto { Id = q.Id, Title = q.Title };
            });
        mapper.Map<List<QuestStepDto>>(Arg.Any<List<QuestStep>>())
            .Returns(ci => {
                var steps = ci.Arg<List<QuestStep>>();
                return steps.Select(s => new QuestStepDto { Id = s.Id, StepNumber = s.StepNumber, Title = s.Title }).ToList();
            });
        return mapper;
    }

    [Fact]
    public async Task QuestNotFound_ReturnsNull()
    {
        var sut = new GetQuestByIdQueryHandler(Substitute.For<IQuestRepository>(), Substitute.For<IQuestStepRepository>(), Substitute.For<IUserQuestAttemptRepository>(), CreateMapperStub());
        var res = await sut.Handle(new GetQuestByIdQuery { Id = Guid.NewGuid(), AuthUserId = Guid.NewGuid() }, CancellationToken.None);
        res.Should().BeNull();
    }

    [Fact]
    public async Task UsesAssignedDifficultyFromAttempt()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var quest = new Quest { Id = questId, Title = "Q", ExpectedDifficulty = "Supportive" };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(new UserQuestAttempt { AuthUserId = authId, QuestId = questId, AssignedDifficulty = "Challenging" });
        var steps = new[]
        {
            new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 1, DifficultyVariant = "Challenging" },
            new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 2, DifficultyVariant = "Supportive" }
        };
        stepRepo.GetByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(steps);
        var sut = new GetQuestByIdQueryHandler(questRepo, stepRepo, attemptRepo, CreateMapperStub());
        var res = await sut.Handle(new GetQuestByIdQuery { Id = questId, AuthUserId = authId }, CancellationToken.None);
        res!.Steps.Should().HaveCount(1);
        res.Steps![0].StepNumber.Should().Be(1);
    }

    [Fact]
    public async Task UsesExpectedDifficultyWhenNoAttempt()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var quest = new Quest { Id = questId, Title = "Q", ExpectedDifficulty = "Supportive" };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestAttempt?)null);
        var steps = new[]
        {
            new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 1, DifficultyVariant = "Challenging" },
            new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 2, DifficultyVariant = "Supportive" }
        };
        stepRepo.GetByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(steps);
        var sut = new GetQuestByIdQueryHandler(questRepo, stepRepo, attemptRepo, CreateMapperStub());
        var res = await sut.Handle(new GetQuestByIdQuery { Id = questId, AuthUserId = authId }, CancellationToken.None);
        res!.Steps.Should().HaveCount(1);
        res.Steps![0].StepNumber.Should().Be(2);
    }

    [Fact]
    public async Task OrdersStepsByStepNumber_ForFilteredVariant()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var quest = new Quest { Id = questId, Title = "Q", ExpectedDifficulty = "Supportive" };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestAttempt?)null);

        var s1 = new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 3, Title = "Third", DifficultyVariant = "Supportive" };
        var s2 = new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 1, Title = "First", DifficultyVariant = "Supportive" };
        var s3 = new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 2, Title = "Second", DifficultyVariant = "Supportive" };
        stepRepo.GetByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new[] { s1, s2, s3 });

        var sut = new GetQuestByIdQueryHandler(questRepo, stepRepo, attemptRepo, CreateMapperStub());
        var res = await sut.Handle(new GetQuestByIdQuery { Id = questId, AuthUserId = authId }, CancellationToken.None);
        res!.Steps.Select(x => x.StepNumber).Should().ContainInOrder(1, 2, 3);
    }
}
