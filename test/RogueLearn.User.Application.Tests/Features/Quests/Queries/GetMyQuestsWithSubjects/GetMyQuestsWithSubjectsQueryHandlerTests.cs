using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Quests.Queries.GetMyQuestsWithSubjects;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Quests.Queries.GetMyQuestsWithSubjects;

public class GetMyQuestsWithSubjectsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsEmptyWhenNoQuests()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var sut = new GetMyQuestsWithSubjectsQueryHandler(questRepo, subjectRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetMyQuestsWithSubjectsQueryHandler>>());
        var res = await sut.Handle(new GetMyQuestsWithSubjectsQuery { AuthUserId = Guid.NewGuid() }, CancellationToken.None);
        res.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsMappedItems()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var userId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Quest> { new Quest { Id = Guid.NewGuid(), CreatedBy = userId, SubjectId = subjectId, Title = "Q", IsActive = true } });
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Subject> { new Subject { Id = subjectId, SubjectName = "S", SubjectCode = "SC", Credits = 3 } });

        var sut = new GetMyQuestsWithSubjectsQueryHandler(questRepo, subjectRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetMyQuestsWithSubjectsQueryHandler>>());
        var res = await sut.Handle(new GetMyQuestsWithSubjectsQuery { AuthUserId = userId }, CancellationToken.None);
        res.Should().ContainSingle(x => x.SubjectName == "S" && x.Title == "Q");
    }
}