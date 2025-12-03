using FluentAssertions;
using RogueLearn.User.Application.Features.Meetings.Queries.GetPartyMeetings;

namespace RogueLearn.User.Application.Tests.Features.Meetings.Queries.GetPartyMeetings;

public class GetPartyMeetingsQueryValidatorTests
{
    [Fact]
    public void Invalid_WhenEmptyPartyId()
    {
        var validator = new GetPartyMeetingsQueryValidator();
        var result = validator.Validate(new GetPartyMeetingsQuery(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Valid_WhenPartyIdProvided()
    {
        var validator = new GetPartyMeetingsQueryValidator();
        var result = validator.Validate(new GetPartyMeetingsQuery(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }
}