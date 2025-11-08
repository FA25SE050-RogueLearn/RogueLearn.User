using FluentValidation;

namespace RogueLearn.User.Application.Features.Meetings.Queries.GetMeetingDetails;

public class GetMeetingDetailsQueryValidator : AbstractValidator<GetMeetingDetailsQuery>
{
    public GetMeetingDetailsQueryValidator()
    {
        RuleFor(x => x.MeetingId).NotEqual(Guid.Empty);
    }
}