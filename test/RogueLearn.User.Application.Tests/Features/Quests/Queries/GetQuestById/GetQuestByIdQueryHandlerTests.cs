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


}