using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Meetings.DTOs;
using RogueLearn.User.Application.Features.Meetings.Queries.GetGuildMeetings;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Meetings.Queries.GetGuildMeetings;

public class GetGuildMeetingsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMapped()
    {
        var query = new GetGuildMeetingsQuery(System.Guid.NewGuid());
        var meetingRepo = Substitute.For<IMeetingRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new GetGuildMeetingsQueryHandler(meetingRepo, mapper);

        var meetings = new List<Meeting> { new() { MeetingId = System.Guid.NewGuid() } };
        meetingRepo.GetByGuildAsync(query.GuildId, Arg.Any<CancellationToken>()).Returns(meetings);
        mapper.Map<MeetingDto>(meetings.First()).Returns(new MeetingDto { MeetingId = meetings.First().MeetingId });

        var result = await sut.Handle(query, CancellationToken.None);
        result.Count.Should().Be(1);
        result[0].MeetingId.Should().Be(meetings.First().MeetingId);
    }

    [Fact]
    public async Task Handle_Empty_ReturnsEmpty()
    {
        var query = new GetGuildMeetingsQuery(System.Guid.NewGuid());
        var meetingRepo = Substitute.For<IMeetingRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new GetGuildMeetingsQueryHandler(meetingRepo, mapper);

        meetingRepo.GetByGuildAsync(query.GuildId, Arg.Any<CancellationToken>()).Returns(new List<Meeting>());

        var result = await sut.Handle(query, CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MapsMultipleItems()
    {
        var query = new GetGuildMeetingsQuery(System.Guid.NewGuid());
        var meetingRepo = Substitute.For<IMeetingRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new GetGuildMeetingsQueryHandler(meetingRepo, mapper);

        var m1 = new Meeting { MeetingId = System.Guid.NewGuid() };
        var m2 = new Meeting { MeetingId = System.Guid.NewGuid() };
        var meetings = new List<Meeting> { m1, m2 };
        meetingRepo.GetByGuildAsync(query.GuildId, Arg.Any<CancellationToken>()).Returns(meetings);
        mapper.Map<MeetingDto>(m1).Returns(new MeetingDto { MeetingId = m1.MeetingId });
        mapper.Map<MeetingDto>(m2).Returns(new MeetingDto { MeetingId = m2.MeetingId });

        var result = await sut.Handle(query, CancellationToken.None);
        result.Count.Should().Be(2);
        result.Select(x => x.MeetingId).Should().Contain(new[] { m1.MeetingId, m2.MeetingId });
    }
}
