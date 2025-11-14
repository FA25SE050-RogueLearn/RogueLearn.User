using FluentValidation;

namespace RogueLearn.User.Application.Features.Meetings.Queries.GetGuildMeetings;

public class GetGuildMeetingsQueryValidator : AbstractValidator<GetGuildMeetingsQuery>
{
    public GetGuildMeetingsQueryValidator()
    {
        RuleFor(x => x.GuildId).NotEqual(Guid.Empty);
    }
}