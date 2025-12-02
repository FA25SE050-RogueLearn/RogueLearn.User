using FluentAssertions;
using RogueLearn.User.Application.Features.QuestFeedback.Commands.ResolveQuestStepFeedback;
using RogueLearn.User.Application.Features.QuestFeedback.Commands.SubmitQuestStepFeedback;

namespace RogueLearn.User.Application.Tests.Features.QuestFeedback.Validators;

public class QuestFeedbackValidatorsTests
{
    [Fact]
    public void Resolve_Invalid_WhenEmptyFeedbackIdOrLongNotes()
    {
        var v = new ResolveQuestStepFeedbackCommandValidator();
        var cmd = new ResolveQuestStepFeedbackCommand { FeedbackId = Guid.Empty, AdminNotes = new string('a', 2001) };
        var res = v.Validate(cmd);
        res.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Submit_Invalid_WhenMissingFieldsOrBadRatingOrCategory()
    {
        var v = new SubmitQuestStepFeedbackCommandValidator();
        var cmd = new SubmitQuestStepFeedbackCommand { AuthUserId = Guid.Empty, QuestId = Guid.Empty, StepId = Guid.Empty, Rating = 6, Category = "Bad" };
        var res = v.Validate(cmd);
        res.IsValid.Should().BeFalse();
    }
}