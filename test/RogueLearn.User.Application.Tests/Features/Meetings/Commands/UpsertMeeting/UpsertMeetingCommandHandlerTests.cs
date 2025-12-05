using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Meetings.Commands.UpsertMeeting;
using RogueLearn.User.Application.Features.Meetings.DTOs;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Meetings.Commands.UpsertMeeting;

public class UpsertMeetingCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesWhenMissing()
    {
        var repo = Substitute.For<IMeetingRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var meetingNotificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IMeetingNotificationService>();
        var sut = new UpsertMeetingCommandHandler(repo, mapper, meetingNotificationService);

        var dto = new MeetingDto { PartyId = Guid.NewGuid(), Title = "T", ScheduledStartTime = DateTimeOffset.UtcNow, ScheduledEndTime = DateTimeOffset.UtcNow.AddHours(1), OrganizerId = Guid.NewGuid() };
        var cmd = new UpsertMeetingCommand(dto);
        mapper.Map<Meeting>(dto).Returns(new Meeting { MeetingId = Guid.Empty, PartyId = dto.PartyId, Title = dto.Title, ScheduledStartTime = dto.ScheduledStartTime, ScheduledEndTime = dto.ScheduledEndTime, OrganizerId = dto.OrganizerId });
        repo.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        repo.AddAsync(Arg.Any<Meeting>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Meeting>());
        mapper.Map<MeetingDto>(Arg.Any<Meeting>()).Returns(dto);

        var result = await sut.Handle(cmd, CancellationToken.None);
        result.Title.Should().Be("T");
    }

    [Fact]
    public async Task Handle_UpdatesWhenExists()
    {
        var repo = Substitute.For<IMeetingRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var meetingNotificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IMeetingNotificationService>();
        var sut = new UpsertMeetingCommandHandler(repo, mapper, meetingNotificationService);

        var dto = new MeetingDto { MeetingId = Guid.NewGuid(), GuildId = Guid.NewGuid(), Title = "T", ScheduledStartTime = DateTimeOffset.UtcNow, ScheduledEndTime = DateTimeOffset.UtcNow.AddHours(1), OrganizerId = Guid.NewGuid() };
        var cmd = new UpsertMeetingCommand(dto);
        var entity = new Meeting { MeetingId = dto.MeetingId, GuildId = dto.GuildId, Title = dto.Title };
        mapper.Map<Meeting>(dto).Returns(entity);
        repo.ExistsAsync(dto.MeetingId, Arg.Any<CancellationToken>()).Returns(true);
        repo.UpdateAsync(Arg.Any<Meeting>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        mapper.Map<MeetingDto>(Arg.Any<Meeting>()).Returns(dto);

        var result = await sut.Handle(cmd, CancellationToken.None);
        result.MeetingId.Should().Be(dto.MeetingId);
    }
}