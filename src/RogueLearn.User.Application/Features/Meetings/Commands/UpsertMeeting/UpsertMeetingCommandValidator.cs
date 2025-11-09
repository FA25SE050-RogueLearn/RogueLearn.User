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
        // Meeting link is optional, but if provided must be a valid absolute URL and reasonable length
        When(x => !string.IsNullOrWhiteSpace(x.MeetingDto.MeetingLink), () =>
        {
            RuleFor(x => x.MeetingDto.MeetingLink!)
                .MaximumLength(2048)
                .Must(link => Uri.IsWellFormedUriString(link, UriKind.Absolute))
                .WithMessage("MeetingLink must be a well-formed absolute URL");
        });
    }
}