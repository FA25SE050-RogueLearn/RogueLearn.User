using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Meetings.DTOs;
using RogueLearn.User.Application.Features.Meetings.Queries.GetPartyMeetings;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Meetings.Queries.GetPartyMeetings;

public class GetPartyMeetingsQueryHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_ReturnsMapped(GetPartyMeetingsQuery query)
    {
        var meetingRepo = Substitute.For<IMeetingRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new GetPartyMeetingsQueryHandler(meetingRepo, mapper);

        var meetings = new List<Meeting> { new() { MeetingId = System.Guid.NewGuid() } };
        meetingRepo.GetByPartyAsync(query.PartyId, Arg.Any<CancellationToken>()).Returns(meetings);
        mapper.Map<MeetingDto>(meetings.First()).Returns(new MeetingDto { MeetingId = meetings.First().MeetingId });

        var result = await sut.Handle(query, CancellationToken.None);
        result.Count.Should().Be(1);
        result[0].MeetingId.Should().Be(meetings.First().MeetingId);
    }
}