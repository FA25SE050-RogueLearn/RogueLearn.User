using AutoMapper;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestById;
using RogueLearn.User.Application.Mappings;
using RogueLearn.User.Application.Services; // For IQuestDifficultyResolver
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
        // Fix: Pass missing dependencies: IStudentSemesterSubjectRepository, IQuestDifficultyResolver
        var sut = new GetQuestByIdQueryHandler(
            Substitute.For<IQuestRepository>(),
            Substitute.For<IQuestStepRepository>(),
            Substitute.For<IUserQuestAttemptRepository>(),
            Substitute.For<IStudentSemesterSubjectRepository>(),
            Substitute.For<IQuestDifficultyResolver>(),
            CreateMapperStub());

        var res = await sut.Handle(new GetQuestByIdQuery { Id = Guid.NewGuid(), AuthUserId = Guid.NewGuid() }, CancellationToken.None);
        res.Should().BeNull();
    }

    [Fact]
    public async Task UsesExpectedDifficultyFromQuest_WhenFilteringSteps()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var studentRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var diffResolver = Substitute.For<IQuestDifficultyResolver>();

        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        // Quest dictates the expected difficulty based on prior user analysis
        var quest = new Quest { Id = questId, Title = "Q", ExpectedDifficulty = "Supportive" };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(new UserQuestAttempt { AuthUserId = authId, QuestId = questId });

        var steps = new[]
        {
            new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 1, DifficultyVariant = "Challenging" },
            new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 2, DifficultyVariant = "Supportive" }
        };
        stepRepo.GetByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(steps);

        // Fix: Pass updated dependencies
        var sut = new GetQuestByIdQueryHandler(questRepo, stepRepo, attemptRepo, studentRepo, diffResolver, CreateMapperStub());
        var res = await sut.Handle(new GetQuestByIdQuery { Id = questId, AuthUserId = authId }, CancellationToken.None);

        // Expect only the "Supportive" step to be returned
        res!.Steps.Should().HaveCount(1);
        res.Steps![0].StepNumber.Should().Be(2);
    }
}