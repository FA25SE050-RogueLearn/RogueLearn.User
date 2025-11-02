// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/UpdateQuestStepProgress/UpdateQuestStepProgressCommandValidator.cs
using FluentValidation;

namespace RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestStepProgress;

public class UpdateQuestStepProgressCommandValidator : AbstractValidator<UpdateQuestStepProgressCommand>
{
    public UpdateQuestStepProgressCommandValidator()
    {
        RuleFor(x => x.AuthUserId).NotEmpty();
        RuleFor(x => x.QuestId).NotEmpty();
        RuleFor(x => x.StepId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}