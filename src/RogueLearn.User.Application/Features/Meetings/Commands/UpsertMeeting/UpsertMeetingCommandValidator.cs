using FluentValidation;

namespace RogueLearn.User.Application.Features.Meetings.Commands.UpsertMeeting;

public class UpsertMeetingCommandValidator : AbstractValidator<UpsertMeetingCommand>
{
    public UpsertMeetingCommandValidator()
    {
        RuleFor(x => x.MeetingDto).NotNull();
        RuleFor(x => x.MeetingDto.PartyId).NotEqual(Guid.Empty);
        RuleFor(x => x.MeetingDto.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.MeetingDto.ScheduledStartTime)
            .LessThan(x => x.MeetingDto.ScheduledEndTime)
            .WithMessage("ScheduledStartTime must be before ScheduledEndTime");
        RuleFor(x => x.MeetingDto.OrganizerId).NotEqual(Guid.Empty);
    }
}