using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.QuestFeedback.Queries.GetQuestFeedbackList;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.QuestFeedback.Queries.GetQuestFeedbackList;

public class GetQuestFeedbackListQueryHandlerTests
{
    [Fact]
    public async Task Handle_BySubject_UnresolvedOnly_Filters()
    {
        var query = new GetQuestFeedbackListQuery
        {
            QuestId = System.Guid.NewGuid(),
            SubjectId = System.Guid.NewGuid(),
            UnresolvedOnly = true
        };

        var repo = Substitute.For<IUserQuestStepFeedbackRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();

        var items = new List<UserQuestStepFeedback>
        {
            new UserQuestStepFeedback { Id = System.Guid.NewGuid(), IsResolved = false },
            new UserQuestStepFeedback { Id = System.Guid.NewGuid(), IsResolved = true }
        };

        repo.GetBySubjectIdAsync(query.SubjectId.Value, Arg.Any<CancellationToken>()).Returns(items);
        mapper.Map<List<QuestFeedbackDto>>(Arg.Any<IEnumerable<UserQuestStepFeedback>>()).Returns(ci =>
        {
            var input = ((IEnumerable<UserQuestStepFeedback>)ci[0]).ToList();
            return input.Select(x => new QuestFeedbackDto { Id = x.Id, IsResolved = x.IsResolved, QuestId = x.QuestId, SubjectId = x.SubjectId, AuthUserId = x.AuthUserId, Category = x.Category, Comment = x.Comment, CreatedAt = x.CreatedAt, Rating = x.Rating, StepId = x.StepId }).ToList();
        });

        var sut = new GetQuestFeedbackListQueryHandler(repo, mapper);
        var res = await sut.Handle(query, CancellationToken.None);
        res.Should().HaveCount(1);
        res[0].IsResolved.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ByQuest_IncludesResolved_WhenRequested()
    {
        var qId = Guid.NewGuid();
        var query = new GetQuestFeedbackListQuery
        {
            QuestId = qId,
            UnresolvedOnly = false
        };

        var repo = Substitute.For<IUserQuestStepFeedbackRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();

        var items = new List<UserQuestStepFeedback>
        {
            new UserQuestStepFeedback { Id = Guid.NewGuid(), QuestId = qId, IsResolved = false },
            new UserQuestStepFeedback { Id = Guid.NewGuid(), QuestId = qId, IsResolved = true }
        };

        repo.GetByQuestIdAsync(qId, Arg.Any<CancellationToken>()).Returns(items);
        mapper.Map<List<QuestFeedbackDto>>(Arg.Any<IEnumerable<UserQuestStepFeedback>>()).Returns(ci =>
        {
            var input = ((IEnumerable<UserQuestStepFeedback>)ci[0]).ToList();
            return input.Select(x => new QuestFeedbackDto { Id = x.Id, IsResolved = x.IsResolved }).ToList();
        });

        var sut = new GetQuestFeedbackListQueryHandler(repo, mapper);
        var res = await sut.Handle(query, CancellationToken.None);
        res.Should().HaveCount(2);
        res.Any(r => r.IsResolved).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UnresolvedOnly_Global_PullsUnresolved()
    {
        var query = new GetQuestFeedbackListQuery { UnresolvedOnly = true };

        var repo = Substitute.For<IUserQuestStepFeedbackRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();

        var items = new List<UserQuestStepFeedback>
        {
            new UserQuestStepFeedback { Id = Guid.NewGuid(), IsResolved = false },
            new UserQuestStepFeedback { Id = Guid.NewGuid(), IsResolved = false }
        };

        repo.GetUnresolvedAsync(Arg.Any<CancellationToken>()).Returns(items);
        mapper.Map<List<QuestFeedbackDto>>(Arg.Any<IEnumerable<UserQuestStepFeedback>>()).Returns(ci =>
        {
            var input = ((IEnumerable<UserQuestStepFeedback>)ci[0]).ToList();
            return input.Select(x => new QuestFeedbackDto { Id = x.Id, IsResolved = x.IsResolved }).ToList();
        });

        var sut = new GetQuestFeedbackListQueryHandler(repo, mapper);
        var res = await sut.Handle(query, CancellationToken.None);
        res.Should().HaveCount(2);
        await repo.Received(1).GetUnresolvedAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GetAll_Fallback_ReturnsAll()
    {
        var query = new GetQuestFeedbackListQuery { UnresolvedOnly = false };

        var repo = Substitute.For<IUserQuestStepFeedbackRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();

        var items = new List<UserQuestStepFeedback>
        {
            new UserQuestStepFeedback { Id = Guid.NewGuid(), IsResolved = false },
            new UserQuestStepFeedback { Id = Guid.NewGuid(), IsResolved = true }
        };

        repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(items);
        mapper.Map<List<QuestFeedbackDto>>(Arg.Any<IEnumerable<UserQuestStepFeedback>>()).Returns(ci =>
        {
            var input = ((IEnumerable<UserQuestStepFeedback>)ci[0]).ToList();
            return input.Select(x => new QuestFeedbackDto { Id = x.Id, IsResolved = x.IsResolved }).ToList();
        });

        var sut = new GetQuestFeedbackListQueryHandler(repo, mapper);
        var res = await sut.Handle(query, CancellationToken.None);
        res.Should().HaveCount(2);
        await repo.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }
}
