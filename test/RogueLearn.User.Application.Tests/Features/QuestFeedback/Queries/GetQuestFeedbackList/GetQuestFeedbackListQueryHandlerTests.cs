using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.QuestFeedback.Queries.GetQuestFeedbackList;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.QuestFeedback.Queries.GetQuestFeedbackList;

public class GetQuestFeedbackListQueryHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_BySubject_UnresolvedOnly_Filters(GetQuestFeedbackListQuery query)
    {
        query.SubjectId = System.Guid.NewGuid();
        query.UnresolvedOnly = true;

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
            return input.Select(x => new QuestFeedbackDto { Id = x.Id, IsResolved = x.IsResolved }).ToList();
        });

        var sut = new GetQuestFeedbackListQueryHandler(repo, mapper);
        var res = await sut.Handle(query, CancellationToken.None);
        res.Should().HaveCount(1);
        res[0].IsResolved.Should().BeFalse();
    }
}