using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Meetings.DTOs;
using RogueLearn.User.Application.Features.Meetings.Queries.GetMeetingDetails;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Meetings.Queries.GetMeetingDetails;

public class GetMeetingDetailsQueryHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_Success_ReturnsDetails(GetMeetingDetailsQuery query)
    {
        var meetingRepo = Substitute.For<IMeetingRepository>();
        var participantRepo = Substitute.For<IMeetingParticipantRepository>();
        var summaryRepo = Substitute.For<IMeetingSummaryRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new GetMeetingDetailsQueryHandler(meetingRepo, participantRepo, summaryRepo, mapper);

        var meeting = new Meeting { MeetingId = query.MeetingId };
        var participants = new List<MeetingParticipant> { new() { ParticipantId = Guid.NewGuid(), MeetingId = query.MeetingId } };
        var summary = new MeetingSummary { MeetingSummaryId = Guid.NewGuid(), MeetingId = query.MeetingId, SummaryText = "S" };
        meetingRepo.GetByIdAsync(query.MeetingId, Arg.Any<CancellationToken>()).Returns(meeting);
        participantRepo.GetByMeetingAsync(query.MeetingId, Arg.Any<CancellationToken>()).Returns(participants);
        summaryRepo.GetByMeetingAsync(query.MeetingId, Arg.Any<CancellationToken>()).Returns(summary);

        mapper.Map<MeetingDto>(meeting).Returns(new MeetingDto { MeetingId = meeting.MeetingId });
        mapper.Map<MeetingParticipantDto>(participants[0]).Returns(new MeetingParticipantDto { ParticipantId = participants[0].ParticipantId });

        var result = await sut.Handle(query, CancellationToken.None);
        result.Meeting.MeetingId.Should().Be(meeting.MeetingId);
        result.Participants.Should().HaveCount(1);
        result.SummaryText.Should().Be("S");
    }
}