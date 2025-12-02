using FluentAssertions;
using RogueLearn.User.Application.Features.Meetings.Commands.CreateOrUpdateSummary;
using RogueLearn.User.Application.Features.Meetings.Queries.GetGuildMeetings;
using RogueLearn.User.Application.Features.Meetings.Queries.GetMeetingDetails;

namespace RogueLearn.User.Application.Tests.Features.Meetings.Validators;

public class MeetingsValidatorsTests
{
    [Fact]
    public void CreateOrUpdateSummary_Invalid_WhenEmptyContentOrEmptyMeetingId()
    {
        var validator = new CreateOrUpdateSummaryCommandValidator();
        var invalidId = validator.Validate(new CreateOrUpdateSummaryCommand(Guid.Empty, "content"));
        invalidId.IsValid.Should().BeFalse();
        invalidId.Errors.Any(e => e.PropertyName == nameof(CreateOrUpdateSummaryCommand.MeetingId)).Should().BeTrue();

        var invalidContent = validator.Validate(new CreateOrUpdateSummaryCommand(Guid.NewGuid(), ""));
        invalidContent.IsValid.Should().BeFalse();
        invalidContent.Errors.Any(e => e.PropertyName == nameof(CreateOrUpdateSummaryCommand.Content)).Should().BeTrue();
    }

    [Fact]
    public void GetGuildMeetings_Invalid_WhenEmptyGuildId()
    {
        var validator = new GetGuildMeetingsQueryValidator();
        var result = validator.Validate(new GetGuildMeetingsQuery(Guid.Empty));
        result.IsValid.Should().BeFalse();
        result.Errors.Any(e => e.PropertyName == nameof(GetGuildMeetingsQuery.GuildId)).Should().BeTrue();
    }

    [Fact]
    public void GetMeetingDetails_Invalid_WhenEmptyMeetingId()
    {
        var validator = new GetMeetingDetailsQueryValidator();
        var result = validator.Validate(new GetMeetingDetailsQuery(Guid.Empty));
        result.IsValid.Should().BeFalse();
        result.Errors.Any(e => e.PropertyName == nameof(GetMeetingDetailsQuery.MeetingId)).Should().BeTrue();
    }
}