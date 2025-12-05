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
}