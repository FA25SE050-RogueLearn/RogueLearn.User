using FluentValidation;

namespace RogueLearn.User.Application.Features.Meetings.Queries.GetPartyMeetings;

public class GetPartyMeetingsQueryValidator : AbstractValidator<GetPartyMeetingsQuery>
{
    public GetPartyMeetingsQueryValidator()
    {
        RuleFor(x => x.PartyId).NotEqual(Guid.Empty);
    }
}