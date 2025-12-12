using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.QuestProgress.Queries.GetCompletedActivities;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.QuestProgress.Queries.GetCompletedActivities;

public class GetCompletedActivitiesQueryHandlerTests
{
    private static object BuildContentArray(params (Guid id, string type)[] items)
    {
        return items.Select(x => new { activityId = x.id.ToString(), type = x.type, payload = new { experiencePoints = 10 } }).ToArray();
    }

    private static object BuildContentObject(params (Guid id, string type)[] items)
    {
        return new { activities = items.Select(x => new { activityId = x.id.ToString(), type = x.type, payload = new { experiencePoints = 10 } }).ToArray() };
    }

    [Fact]
    public async Task StepNotFound_ThrowsNotFound()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        questStepRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((QuestStep?)null);
        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, Substitute.For<ILogger<GetCompletedActivitiesQueryHandler>>());
        var act = () => sut.Handle(new GetCompletedActivitiesQuery { AuthUserId = Guid.NewGuid(), QuestId = Guid.NewGuid(), StepId = Guid.NewGuid() }, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task StepQuestMismatch_ThrowsNotFound()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        questStepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = Guid.NewGuid(), Content = BuildContentArray((Guid.NewGuid(), "Reading")) });
        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, Substitute.For<ILogger<GetCompletedActivitiesQueryHandler>>());
        var act = () => sut.Handle(new GetCompletedActivitiesQuery { AuthUserId = Guid.NewGuid(), QuestId = Guid.NewGuid(), StepId = stepId }, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task NoAttempt_ReturnsEmptyWithTotalCount_FromObjectContent()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var content = BuildContentObject((ids[0], "Reading"), (ids[1], "Quiz"), (ids[2], "Coding"));
        questStepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = content });
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestAttempt?)null);
        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, Substitute.For<ILogger<GetCompletedActivitiesQueryHandler>>());
        var res = await sut.Handle(new GetCompletedActivitiesQuery { AuthUserId = authId, QuestId = questId, StepId = stepId }, CancellationToken.None);
        res.TotalCount.Should().Be(3);
        res.CompletedCount.Should().Be(0);
    }

    [Fact]
    public async Task NoStepProgress_ReturnsEmptyWithTotalCount_FromArrayContent()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var content = BuildContentArray((ids[0], "Reading"), (ids[1], "Quiz"));
        questStepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = content });
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestStepProgress?)null);
        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, Substitute.For<ILogger<GetCompletedActivitiesQueryHandler>>());
        var res = await sut.Handle(new GetCompletedActivitiesQuery { AuthUserId = authId, QuestId = questId, StepId = stepId }, CancellationToken.None);
        res.TotalCount.Should().Be(2);
        res.CompletedCount.Should().Be(0);
    }

    [Fact]
    public async Task CompletedIds_MapToCompletedActivities()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var content = BuildContentObject((ids[0], "Reading"), (ids[1], "Quiz"), (ids[2], "Coding"));
        questStepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = content });
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { AttemptId = attempt.Id, StepId = stepId, CompletedActivityIds = new[] { ids[0], ids[2] } };
        stepRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);
        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, Substitute.For<ILogger<GetCompletedActivitiesQueryHandler>>());
        var res = await sut.Handle(new GetCompletedActivitiesQuery { AuthUserId = authId, QuestId = questId, StepId = stepId }, CancellationToken.None);
        res.TotalCount.Should().Be(3);
        res.CompletedCount.Should().Be(2);
        res.Activities!.Count(a => a.IsCompleted).Should().Be(2);
    }

    [Fact]
    public async Task NullContent_ReturnsZeroCounts()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        questStepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = null });
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestStepProgress?)null);
        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetCompletedActivitiesQueryHandler>>());
        var res = await sut.Handle(new GetCompletedActivitiesQuery { AuthUserId = authId, QuestId = questId, StepId = stepId }, CancellationToken.None);
        res.TotalCount.Should().Be(0);
        res.CompletedCount.Should().Be(0);
    }

    [Fact]
    public async Task StringContent_MissingActivitiesProperty_ReturnsZero()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var content = "{ \"data\": [] }";
        questStepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = content });
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(new UserQuestStepProgress { AttemptId = attempt.Id, StepId = stepId });
        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetCompletedActivitiesQueryHandler>>());
        var res = await sut.Handle(new GetCompletedActivitiesQuery { AuthUserId = authId, QuestId = questId, StepId = stepId }, CancellationToken.None);
        res.TotalCount.Should().Be(0);
        res.CompletedCount.Should().Be(0);
    }

    [Fact]
    public async Task JsonElementContent_ArrayWithInvalidItem_SkipsInvalid()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var validId = Guid.NewGuid();
        using var doc = System.Text.Json.JsonDocument.Parse($"[{{\"activityId\":\"{validId}\",\"type\":\"Reading\",\"payload\":{{}}}},{{\"type\":\"Quiz\"}}]");
        var content = doc.RootElement; // JsonElement path
        questStepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = content });
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(new UserQuestStepProgress { AttemptId = attempt.Id, StepId = stepId, CompletedActivityIds = new[] { validId } });
        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetCompletedActivitiesQueryHandler>>());
        var res = await sut.Handle(new GetCompletedActivitiesQuery { AuthUserId = authId, QuestId = questId, StepId = stepId }, CancellationToken.None);
        res.TotalCount.Should().Be(1);
        res.CompletedCount.Should().Be(1);
    }

    [Fact]
    public async Task JsonElementObject_ActivitiesArray_Parsed()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        using var doc = System.Text.Json.JsonDocument.Parse($"{{\"activities\":[{{\"activityId\":\"{id1}\",\"type\":\"Quiz\",\"payload\":{{\"experiencePoints\":1}}}},{{\"activityId\":\"{id2}\",\"type\":\"Reading\",\"payload\":{{\"articleTitle\":\"Article\",\"experiencePoints\":2}}}}]}}");
        var content = doc.RootElement;

        questStepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = content });
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(new UserQuestStepProgress { AttemptId = attempt.Id, StepId = stepId, CompletedActivityIds = new[] { id1 } });

        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetCompletedActivitiesQueryHandler>>());
        var res = await sut.Handle(new GetCompletedActivitiesQuery { AuthUserId = authId, QuestId = questId, StepId = stepId }, CancellationToken.None);

        res.TotalCount.Should().Be(2);
        res.Activities!.First(a => a.ActivityId == id1).IsCompleted.Should().BeTrue();
        res.Activities!.First(a => a.ActivityId == id2).Title.Should().Be("Article");
    }

    [Fact]
    public async Task MalformedJson_ReturnsZero()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var content = "{"; // malformed
        questStepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = content });
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(new UserQuestStepProgress { AttemptId = attempt.Id, StepId = stepId });
        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetCompletedActivitiesQueryHandler>>());
        var res = await sut.Handle(new GetCompletedActivitiesQuery { AuthUserId = authId, QuestId = questId, StepId = stepId }, CancellationToken.None);
        res.TotalCount.Should().Be(0);
        res.CompletedCount.Should().Be(0);
    }

    [Fact]
    public async Task ActivitiesPropertyExistsButNotArray_ReturnsZero()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var content = "{ \"activities\": {} }"; // activities present but not an array
        questStepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = content });
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(new UserQuestStepProgress { AttemptId = attempt.Id, StepId = stepId });

        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetCompletedActivitiesQueryHandler>>());
        var res = await sut.Handle(new GetCompletedActivitiesQuery { AuthUserId = authId, QuestId = questId, StepId = stepId }, CancellationToken.None);
        res.TotalCount.Should().Be(0);
        res.CompletedCount.Should().Be(0);
    }

    [Fact]
    public async Task JObjectContent_UsesToString_ParsesActivities_WithSkillAndTitles()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var j = new Newtonsoft.Json.Linq.JObject
        {
            ["activities"] = new Newtonsoft.Json.Linq.JArray
            {
                new Newtonsoft.Json.Linq.JObject
                {
                    ["activityId"] = id1.ToString(),
                    ["type"] = "KnowledgeCheck",
                    ["payload"] = new Newtonsoft.Json.Linq.JObject { ["topic"] = "Functions", ["skillId"] = skillId.ToString(), ["experiencePoints"] = 5 }
                },
                new Newtonsoft.Json.Linq.JObject
                {
                    ["activityId"] = id2.ToString(),
                    ["type"] = "Coding",
                    ["payload"] = new Newtonsoft.Json.Linq.JObject { ["topic"] = "Loops", ["experiencePoints"] = 7 }
                },
                new Newtonsoft.Json.Linq.JObject
                {
                    ["activityId"] = id3.ToString(),
                    ["type"] = "Other",
                    ["payload"] = new Newtonsoft.Json.Linq.JObject { ["experiencePoints"] = 3 }
                }
            }
        };

        questStepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = j });
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(new UserQuestStepProgress { AttemptId = attempt.Id, StepId = stepId, CompletedActivityIds = new[] { id2 } });

        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetCompletedActivitiesQueryHandler>>());
        var res = await sut.Handle(new GetCompletedActivitiesQuery { AuthUserId = authId, QuestId = questId, StepId = stepId }, CancellationToken.None);

        res.TotalCount.Should().Be(3);
        var a1 = res.Activities!.First(a => a.ActivityId == id1);
        a1.Title.Should().Be("Functions");
        a1.SkillId.Should().Be(skillId);
        var a2 = res.Activities!.First(a => a.ActivityId == id2);
        a2.Title.Should().Be("Loops");
        a2.IsCompleted.Should().BeTrue();
        var a3 = res.Activities!.First(a => a.ActivityId == id3);
        a3.Title.Should().Be("Activity");
    }
}
