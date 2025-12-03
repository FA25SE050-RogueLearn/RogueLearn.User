using System;
using System.Collections.Generic;
using FluentAssertions;
using RogueLearn.User.Application.Features.Meetings.Commands.UpsertParticipants;
using RogueLearn.User.Application.Features.Meetings.DTOs;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Meetings.Commands.UpsertParticipants;

public class UpsertParticipantsCommandValidatorTests
{
    [Fact]
    public void Valid_Passes()
    {
        var participants = new List<MeetingParticipantDto> { new() { UserId = Guid.NewGuid(), RoleInMeeting = "Attendee" } };
        var cmd = new UpsertParticipantsCommand(Guid.NewGuid(), participants);
        var validator = new UpsertParticipantsCommandValidator();
        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_Times_Fails()
    {
        var participants = new List<MeetingParticipantDto> { new() { UserId = Guid.NewGuid(), RoleInMeeting = "Attendee", JoinTime = DateTimeOffset.UtcNow, LeaveTime = DateTimeOffset.UtcNow.AddHours(-1) } };
        var cmd = new UpsertParticipantsCommand(Guid.NewGuid(), participants);
        var validator = new UpsertParticipantsCommandValidator();
        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }
}