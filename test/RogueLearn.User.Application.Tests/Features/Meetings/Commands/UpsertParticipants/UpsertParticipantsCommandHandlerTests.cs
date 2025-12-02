using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Meetings.Commands.UpsertParticipants;
using RogueLearn.User.Application.Features.Meetings.DTOs;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Meetings.Commands.UpsertParticipants;

public class UpsertParticipantsCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_MissingIdentity_Throws(UpsertParticipantsCommand cmd)
    {
        var repo = Substitute.For<IMeetingParticipantRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new UpsertParticipantsCommandHandler(repo, mapper);

        var participants = new List<MeetingParticipantDto> { new() { DisplayName = "" } };
        cmd = new UpsertParticipantsCommand(Guid.NewGuid(), participants);
        await Assert.ThrowsAsync<UnprocessableEntityException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_AddsAndUpdates(UpsertParticipantsCommand cmd)
    {
        var repo = Substitute.For<IMeetingParticipantRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new UpsertParticipantsCommandHandler(repo, mapper);

        var p1 = new MeetingParticipantDto { UserId = Guid.NewGuid(), RoleInMeeting = "Attendee" };
        var p2 = new MeetingParticipantDto { ParticipantId = Guid.NewGuid(), DisplayName = "Someone", RoleInMeeting = "Host" };
        var list = new List<MeetingParticipantDto> { p1, p2 };
        cmd = new UpsertParticipantsCommand(Guid.NewGuid(), list);

        var e1 = new MeetingParticipant { ParticipantId = Guid.NewGuid(), UserId = p1.UserId, RoleInMeeting = p1.RoleInMeeting };
        var e2 = new MeetingParticipant { ParticipantId = p2.ParticipantId, DisplayName = p2.DisplayName, RoleInMeeting = p2.RoleInMeeting };
        mapper.Map<MeetingParticipant>(p1).Returns(e1);
        mapper.Map<MeetingParticipant>(p2).Returns(e2);
        repo.ExistsAsync(e1.ParticipantId, Arg.Any<CancellationToken>()).Returns(false);
        repo.ExistsAsync(e2.ParticipantId, Arg.Any<CancellationToken>()).Returns(true);
        repo.AddAsync(Arg.Any<MeetingParticipant>(), Arg.Any<CancellationToken>()).Returns(e1);
        repo.UpdateAsync(Arg.Any<MeetingParticipant>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        mapper.Map<MeetingParticipantDto>(e1).Returns(p1);
        mapper.Map<MeetingParticipantDto>(e2).Returns(p2);

        var result = await sut.Handle(cmd, CancellationToken.None);
        result.Should().HaveCount(2);
    }
}