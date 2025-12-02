using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestById;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using AutoMapper;

namespace RogueLearn.User.Application.Tests.Features.Quests.Queries.GetQuestById;

public class GetQuestByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsNullWhenMissing()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var mapper = Substitute.For<IMapper>();
        var sut = new GetQuestByIdQueryHandler(questRepo, stepRepo, mapper, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetQuestByIdQueryHandler>>());
        var res = await sut.Handle(new GetQuestByIdQuery { Id = Guid.NewGuid() }, CancellationToken.None);
        res.Should().BeNull();
    }

    [Fact]
    public async Task Handle_OrdersStepsBeforeMapping()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var mapper = Substitute.For<IMapper>();

        var questId = Guid.NewGuid();
        var quest = new Quest { Id = questId, Title = "Q" };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);
        stepRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<QuestStep, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<QuestStep> { new QuestStep { StepNumber = 2 }, new QuestStep { StepNumber = 1 } });

        mapper.Map<QuestDetailsDto>(quest).Returns(new QuestDetailsDto { Id = questId, Title = "Q" });

        var sut = new GetQuestByIdQueryHandler(questRepo, stepRepo, mapper, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetQuestByIdQueryHandler>>());
        await sut.Handle(new GetQuestByIdQuery { Id = questId }, CancellationToken.None);

        mapper.Received(1).Map<List<QuestStepDto>>(Arg.Is<IEnumerable<QuestStep>>(s => s.First().StepNumber == 1));
    }
}