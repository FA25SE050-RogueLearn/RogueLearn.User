using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.QuestSubmissions.Commands.SubmitQuizAnswer;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Features.QuestSubmissions.Services;

namespace RogueLearn.User.Application.Tests.Features.QuestSubmissions.Commands.SubmitQuizAnswer;

public class SubmitQuizAnswerCommandHandlerTests
{
    private static string BuildContentJson(Guid activityId, bool includeActivitiesArray = true, bool emptyString = false)
    {
        if (emptyString) return string.Empty;
        if (!includeActivitiesArray) return "{}";
        return $"{{\"activities\":[{{\"activityId\":\"{activityId}\",\"type\":\"Quiz\"}}]}}";
    }

    [Fact]
    public async Task Handle_NullStepContent_LogsAndThrowsNotFound()
    {
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var quiz = Substitute.For<IQuizValidationService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitQuizAnswerCommandHandler>>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId });
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = null });
        var sut = new SubmitQuizAnswerCommandHandler(submissionRepo, stepRepo, questRepo, attemptRepo, quiz, logger);
        var act = () => sut.Handle(new SubmitQuizAnswerCommand { AuthUserId = authId, QuestId = questId, StepId = stepId, ActivityId = Guid.NewGuid(), CorrectAnswerCount = 0, TotalQuestions = 1 }, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
    }

    private class CustomJObject
    {
        public override string ToString() => string.Empty;
    }

    [Fact]
    public async Task Handle_EmptyJsonAfterConversion_ThrowsNotFound()
    {
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var quiz = Substitute.For<IQuizValidationService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitQuizAnswerCommandHandler>>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId });
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = new CustomJObject() });
        var sut = new SubmitQuizAnswerCommandHandler(submissionRepo, stepRepo, questRepo, attemptRepo, quiz, logger);
        var act = () => sut.Handle(new SubmitQuizAnswerCommand { AuthUserId = authId, QuestId = questId, StepId = stepId, ActivityId = Guid.NewGuid(), CorrectAnswerCount = 0, TotalQuestions = 1 }, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
    }

    private class UnsupportedType { }

    [Fact]
    public async Task Handle_UnsupportedContentType_ThrowsNotFound()
    {
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var quiz = Substitute.For<IQuizValidationService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitQuizAnswerCommandHandler>>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId });
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = new UnsupportedType() });
        var sut = new SubmitQuizAnswerCommandHandler(submissionRepo, stepRepo, questRepo, attemptRepo, quiz, logger);
        var act = () => sut.Handle(new SubmitQuizAnswerCommand { AuthUserId = authId, QuestId = questId, StepId = stepId, ActivityId = Guid.NewGuid(), CorrectAnswerCount = 0, TotalQuestions = 1 }, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
    }

    [Fact]
    public async Task Handle_InvalidJson_ThrowsNotFound()
    {
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var quiz = Substitute.For<IQuizValidationService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitQuizAnswerCommandHandler>>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId });
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = "{" });
        var sut = new SubmitQuizAnswerCommandHandler(submissionRepo, stepRepo, questRepo, attemptRepo, quiz, logger);
        var act = () => sut.Handle(new SubmitQuizAnswerCommand { AuthUserId = authId, QuestId = questId, StepId = stepId, ActivityId = Guid.NewGuid(), CorrectAnswerCount = 0, TotalQuestions = 1 }, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
    }

    [Fact]
    public async Task Handle_ActivitiesContainsNonObject_SkipsAndThrowsNotFound()
    {
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var quiz = Substitute.For<IQuizValidationService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitQuizAnswerCommandHandler>>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId });
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        var stepContent = "{\"activities\":[\"string-activity\"]}";
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = stepContent });
        var sut = new SubmitQuizAnswerCommandHandler(submissionRepo, stepRepo, questRepo, attemptRepo, quiz, logger);
        var act = () => sut.Handle(new SubmitQuizAnswerCommand { AuthUserId = authId, QuestId = questId, StepId = stepId, ActivityId = Guid.NewGuid(), CorrectAnswerCount = 0, TotalQuestions = 1 }, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
    }

    [Fact]
    public async Task Handle_InvalidActivityId_SkipsAndThrowsNotFound()
    {
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var quiz = Substitute.For<IQuizValidationService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitQuizAnswerCommandHandler>>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId });
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        var stepContent = "{\"activities\":[{\"activityId\":\"not-a-guid\",\"type\":\"quiz\"}]}";
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = stepContent });
        var sut = new SubmitQuizAnswerCommandHandler(submissionRepo, stepRepo, questRepo, attemptRepo, quiz, logger);
        var act = () => sut.Handle(new SubmitQuizAnswerCommand { AuthUserId = authId, QuestId = questId, StepId = stepId, ActivityId = Guid.NewGuid(), CorrectAnswerCount = 0, TotalQuestions = 1 }, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
    }
    [Fact]
    public async Task Handle_StringContent_ParsesAndReturnsResponse()
    {
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var quizService = Substitute.For<IQuizValidationService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitQuizAnswerCommandHandler>>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId });
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = BuildContentJson(activityId) });
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId });
        submissionRepo.AddAsync(Arg.Any<QuestSubmission>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<QuestSubmission>());
        quizService.EvaluateQuizSubmission(Arg.Any<int>(), Arg.Any<int>()).Returns((true, 100m));

        var sut = new SubmitQuizAnswerCommandHandler(submissionRepo, stepRepo, questRepo, attemptRepo, quizService, logger);
        var res = await sut.Handle(new SubmitQuizAnswerCommand { AuthUserId = authId, QuestId = questId, StepId = stepId, ActivityId = activityId, CorrectAnswerCount = 3, TotalQuestions = 3, Answers = new Dictionary<string, string>() }, CancellationToken.None);
        res.IsPassed.Should().BeTrue();
        res.ScorePercentage.Should().Be(100m);
    }

    [Fact]
    public async Task Handle_EmptyStringContent_WarnsAndThrowsNotFound()
    {
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var quizService = Substitute.For<IQuizValidationService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitQuizAnswerCommandHandler>>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId });
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = BuildContentJson(activityId, emptyString: true) });
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId });

        var sut = new SubmitQuizAnswerCommandHandler(submissionRepo, stepRepo, questRepo, attemptRepo, quizService, logger);
        var act = () => sut.Handle(new SubmitQuizAnswerCommand { AuthUserId = authId, QuestId = questId, StepId = stepId, ActivityId = activityId, CorrectAnswerCount = 1, TotalQuestions = 1, Answers = new Dictionary<string, string>() }, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
    }

    [Fact]
    public async Task Handle_NoActivitiesProperty_ThrowsNotFound()
    {
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var quizService = Substitute.For<IQuizValidationService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitQuizAnswerCommandHandler>>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId });
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = BuildContentJson(activityId, includeActivitiesArray: false) });
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId });

        var sut = new SubmitQuizAnswerCommandHandler(submissionRepo, stepRepo, questRepo, attemptRepo, quizService, logger);
        var act = () => sut.Handle(new SubmitQuizAnswerCommand { AuthUserId = authId, QuestId = questId, StepId = stepId, ActivityId = activityId, CorrectAnswerCount = 1, TotalQuestions = 1, Answers = new Dictionary<string, string>() }, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
    }

    [Fact]
    public async Task Handle_UnexpectedErrorExtractingActivityType_ThrowsNotFound()
    {
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var quizService = Substitute.For<IQuizValidationService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitQuizAnswerCommandHandler>>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId });

        var jObj = new JObject();
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = jObj });
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId });

        var sut = new SubmitQuizAnswerCommandHandler(submissionRepo, stepRepo, questRepo, attemptRepo, quizService, logger);
        var act = () => sut.Handle(new SubmitQuizAnswerCommand { AuthUserId = authId, QuestId = questId, StepId = stepId, ActivityId = activityId, CorrectAnswerCount = 1, TotalQuestions = 1, Answers = new Dictionary<string, string>() }, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
    }

    private class JObject
    {
        public override string ToString() => throw new Exception("boom");
    }

    [Fact]
    public async Task Handle_JObjectType_EmptyToString_ThrowsNotFound()
    {
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var quizService = Substitute.For<IQuizValidationService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitQuizAnswerCommandHandler>>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId });
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId });

        var dynJObject = CreateDynamicJObject("");
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = dynJObject });

        var sut = new SubmitQuizAnswerCommandHandler(submissionRepo, stepRepo, questRepo, attemptRepo, quizService, logger);
        var act = () => sut.Handle(new SubmitQuizAnswerCommand { AuthUserId = authId, QuestId = questId, StepId = stepId, ActivityId = activityId, CorrectAnswerCount = 1, TotalQuestions = 1, Answers = new Dictionary<string, string>() }, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
    }

    [Fact]
    public async Task Handle_JObjectType_ActivityNotFound_ThrowsNotFound()
    {
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var quizService = Substitute.For<IQuizValidationService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitQuizAnswerCommandHandler>>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId });
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId });

        var json = "{\"activities\":[{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"Quiz\"}]}";
        var dynJObject = CreateDynamicJObject(json);
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = dynJObject });

        var sut = new SubmitQuizAnswerCommandHandler(submissionRepo, stepRepo, questRepo, attemptRepo, quizService, logger);
        var act = () => sut.Handle(new SubmitQuizAnswerCommand { AuthUserId = authId, QuestId = questId, StepId = stepId, ActivityId = activityId, CorrectAnswerCount = 1, TotalQuestions = 1, Answers = new Dictionary<string, string>() }, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
    }

    private static object CreateDynamicJObject(string toStringValue)
    {
        var asmName = new System.Reflection.AssemblyName("DynJObjectAsm");
        var asmBuilder = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(asmName, System.Reflection.Emit.AssemblyBuilderAccess.Run);
        var moduleBuilder = asmBuilder.DefineDynamicModule("MainModule");
        var typeBuilder = moduleBuilder.DefineType("JObject", System.Reflection.TypeAttributes.Public);
        var methodBuilder = typeBuilder.DefineMethod("ToString", System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Virtual, typeof(string), Type.EmptyTypes);
        var il = methodBuilder.GetILGenerator();
        il.Emit(System.Reflection.Emit.OpCodes.Ldstr, toStringValue);
        il.Emit(System.Reflection.Emit.OpCodes.Ret);
        var createdType = typeBuilder.CreateType();
        return Activator.CreateInstance(createdType)!;
    }

    [Fact]
    public async Task Handle_OuterCatch_ErrorProcessingQuizSubmission_Rethrows()
    {
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var quizService = Substitute.For<IQuizValidationService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitQuizAnswerCommandHandler>>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId });
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = BuildContentJson(activityId) });
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId });
        quizService.When(q => q.EvaluateQuizSubmission(Arg.Any<int>(), Arg.Any<int>())).Do(_ => throw new Exception("grading failed"));

        var sut = new SubmitQuizAnswerCommandHandler(submissionRepo, stepRepo, questRepo, attemptRepo, quizService, logger);
        var act = () => sut.Handle(new SubmitQuizAnswerCommand { AuthUserId = authId, QuestId = questId, StepId = stepId, ActivityId = activityId, CorrectAnswerCount = 1, TotalQuestions = 1, Answers = new Dictionary<string, string>() }, CancellationToken.None);
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Handle_QuestNotFound_ThrowsNotFound()
    {
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var quizService = Substitute.For<IQuizValidationService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitQuizAnswerCommandHandler>>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns((Quest?)null);

        var sut = new SubmitQuizAnswerCommandHandler(submissionRepo, stepRepo, questRepo, attemptRepo, quizService, logger);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.NotFoundException>(() => sut.Handle(new SubmitQuizAnswerCommand { AuthUserId = authId, QuestId = questId, StepId = stepId, ActivityId = activityId, CorrectAnswerCount = 1, TotalQuestions = 1, Answers = new Dictionary<string, string>() }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_StepWrongQuest_ThrowsNotFound()
    {
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var quizService = Substitute.For<IQuizValidationService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitQuizAnswerCommandHandler>>();

        var questId = Guid.NewGuid();
        var otherQuestId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId });
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = otherQuestId, Content = BuildContentJson(activityId) });

        var sut = new SubmitQuizAnswerCommandHandler(submissionRepo, stepRepo, questRepo, attemptRepo, quizService, logger);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.NotFoundException>(() => sut.Handle(new SubmitQuizAnswerCommand { AuthUserId = authId, QuestId = questId, StepId = stepId, ActivityId = activityId, CorrectAnswerCount = 1, TotalQuestions = 1, Answers = new Dictionary<string, string>() }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AttemptMissing_CreatesAttemptAndSaves()
    {
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var quizService = Substitute.For<IQuizValidationService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitQuizAnswerCommandHandler>>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId });
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = BuildContentJson(activityId) });
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestAttempt?)null);
        attemptRepo.AddAsync(Arg.Any<UserQuestAttempt>(), Arg.Any<CancellationToken>()).Returns(ci => { var a = ci.Arg<UserQuestAttempt>(); a.Id = Guid.NewGuid(); return a; });
        submissionRepo.AddAsync(Arg.Any<QuestSubmission>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<QuestSubmission>());
        quizService.EvaluateQuizSubmission(Arg.Any<int>(), Arg.Any<int>()).Returns((true, 80m));

        var sut = new SubmitQuizAnswerCommandHandler(submissionRepo, stepRepo, questRepo, attemptRepo, quizService, logger);
        var res = await sut.Handle(new SubmitQuizAnswerCommand { AuthUserId = authId, QuestId = questId, StepId = stepId, ActivityId = activityId, CorrectAnswerCount = 4, TotalQuestions = 5, Answers = new Dictionary<string, string>() }, CancellationToken.None);
        res.IsPassed.Should().BeTrue();
        await attemptRepo.Received(1).AddAsync(Arg.Any<UserQuestAttempt>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DictionaryContent_ParsesAndReturnsResponse()
    {
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var quizService = Substitute.For<IQuizValidationService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitQuizAnswerCommandHandler>>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId });
        var content = new Dictionary<string, object> { ["activities"] = new List<Dictionary<string, object>> { new() { ["activityId"] = activityId.ToString(), ["type"] = "Quiz" } } };
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = content });
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId });
        submissionRepo.AddAsync(Arg.Any<QuestSubmission>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<QuestSubmission>());
        quizService.EvaluateQuizSubmission(Arg.Any<int>(), Arg.Any<int>()).Returns((false, 60m));

        var sut = new SubmitQuizAnswerCommandHandler(submissionRepo, stepRepo, questRepo, attemptRepo, quizService, logger);
        var res = await sut.Handle(new SubmitQuizAnswerCommand { AuthUserId = authId, QuestId = questId, StepId = stepId, ActivityId = activityId, CorrectAnswerCount = 3, TotalQuestions = 5, Answers = new Dictionary<string, string>() }, CancellationToken.None);
        res.IsPassed.Should().BeFalse();
        res.Message.Should().Contain("70%");
    }

    [Fact]
    public async Task Handle_ActivityNotFoundInArray_ThrowsNotFound()
    {
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var quizService = Substitute.For<IQuizValidationService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitQuizAnswerCommandHandler>>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId });
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, QuestId = questId, Content = "{\"activities\":[{\"activityId\":\"00000000-0000-0000-0000-000000000000\",\"type\":\"Quiz\"}]}" });
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId });

        var sut = new SubmitQuizAnswerCommandHandler(submissionRepo, stepRepo, questRepo, attemptRepo, quizService, logger);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.NotFoundException>(() => sut.Handle(new SubmitQuizAnswerCommand { AuthUserId = authId, QuestId = questId, StepId = stepId, ActivityId = activityId, CorrectAnswerCount = 1, TotalQuestions = 1, Answers = new Dictionary<string, string>() }, CancellationToken.None));
    }
}
