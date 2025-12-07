using FluentAssertions;
using RogueLearn.User.Application.Features.Meetings.DTOs;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Tests.Features.Meetings.DTOs;

public class MeetingDtoTests
{
    [Fact]
    public void MeetingDto_SetsFields()
    {
        var dto = new MeetingDto
        {
            MeetingId = Guid.NewGuid(),
            PartyId = Guid.NewGuid(),
            GuildId = Guid.NewGuid(),
            Title = "t",
            ScheduledStartTime = DateTimeOffset.UtcNow,
            ScheduledEndTime = DateTimeOffset.UtcNow.AddDays(1),
            ActualStartTime = DateTimeOffset.UtcNow,
            ActualEndTime = DateTimeOffset.UtcNow.AddDays(1),
            OrganizerId = Guid.NewGuid(),
            MeetingLink = "l",
            MeetingCode = "c",
            SpaceName = "s",
            Status = MeetingStatus.Completed,
        };
        dto.Title.Should().Be("t");
        dto.MeetingLink.Should().Be("l");
        dto.MeetingCode.Should().Be("c");
        dto.SpaceName.Should().Be("s");
        dto.Status.Should().Be(MeetingStatus.Completed);
    }
}