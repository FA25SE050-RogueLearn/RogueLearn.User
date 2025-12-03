using System;
using FluentAssertions;
using RogueLearn.User.Application.Features.Meetings.Commands.UpsertMeeting;
using RogueLearn.User.Application.Features.Meetings.DTOs;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Meetings.Commands.UpsertMeeting;

public class UpsertMeetingCommandValidatorTests
{
    [Fact]
    public void Valid_Passes()
    {
        var dto = new MeetingDto { GuildId = Guid.NewGuid(), Title = "T", ScheduledStartTime = DateTimeOffset.UtcNow, ScheduledEndTime = DateTimeOffset.UtcNow.AddHours(1), OrganizerId = Guid.NewGuid(), MeetingLink = "https://example.com/meet" };
        var cmd = new UpsertMeetingCommand(dto);
        var validator = new UpsertMeetingCommandValidator();
        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_Link_Fails()
    {
        var dto = new MeetingDto { GuildId = Guid.NewGuid(), Title = "T", ScheduledStartTime = DateTimeOffset.UtcNow, ScheduledEndTime = DateTimeOffset.UtcNow.AddHours(1), OrganizerId = Guid.NewGuid(), MeetingLink = "bad" };
        var cmd = new UpsertMeetingCommand(dto);
        var validator = new UpsertMeetingCommandValidator();
        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }
}