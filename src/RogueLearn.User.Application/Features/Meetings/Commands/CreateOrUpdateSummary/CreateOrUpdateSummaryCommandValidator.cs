using FluentValidation;

namespace RogueLearn.User.Application.Features.Meetings.Commands.CreateOrUpdateSummary;

public class CreateOrUpdateSummaryCommandValidator : AbstractValidator<CreateOrUpdateSummaryCommand>
{
    public CreateOrUpdateSummaryCommandValidator()
    {
        RuleFor(x => x.MeetingId).NotEqual(Guid.Empty);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(100_000);
    }
}