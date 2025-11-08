using FluentValidation;

namespace RogueLearn.User.Application.Features.Meetings.Commands.UpsertParticipants;

public class UpsertParticipantsCommandValidator : AbstractValidator<UpsertParticipantsCommand>
{
    public UpsertParticipantsCommandValidator()
    {
        RuleFor(x => x.MeetingId).NotEqual(Guid.Empty);
        RuleFor(x => x.Participants).NotNull().NotEmpty();
        RuleForEach(x => x.Participants).ChildRules(p =>
        {
            p.RuleFor(y => y.UserId).NotEqual(Guid.Empty);
            p.RuleFor(y => y.RoleInMeeting).NotEmpty().MaximumLength(50);
            p.RuleFor(y => y.JoinTime).LessThanOrEqualTo(y => y.LeaveTime!).When(y => y.LeaveTime.HasValue);
        });
    }
}