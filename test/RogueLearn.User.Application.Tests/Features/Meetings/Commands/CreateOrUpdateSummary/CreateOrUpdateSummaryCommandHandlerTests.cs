using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Features.Meetings.Commands.CreateOrUpdateSummary;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Meetings.Commands.CreateOrUpdateSummary;

public class CreateOrUpdateSummaryCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_AddsWhenMissing(CreateOrUpdateSummaryCommand cmd)
    {
        var repo = Substitute.For<IMeetingSummaryRepository>();
        var sut = new CreateOrUpdateSummaryCommandHandler(repo);
        repo.GetByMeetingAsync(cmd.MeetingId, Arg.Any<CancellationToken>()).Returns((MeetingSummary?)null);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).AddAsync(Arg.Is<MeetingSummary>(s => s.MeetingId == cmd.MeetingId && s.SummaryText == cmd.Content), Arg.Any<CancellationToken>());
    }

    [Theory]
    [AutoData]
    public async Task Handle_UpdatesWhenExists(CreateOrUpdateSummaryCommand cmd)
    {
        var repo = Substitute.For<IMeetingSummaryRepository>();
        var sut = new CreateOrUpdateSummaryCommandHandler(repo);
        var existing = new MeetingSummary { MeetingId = cmd.MeetingId, SummaryText = "old" };
        repo.GetByMeetingAsync(cmd.MeetingId, Arg.Any<CancellationToken>()).Returns(existing);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).UpdateAsync(Arg.Is<MeetingSummary>(s => s.SummaryText == cmd.Content), Arg.Any<CancellationToken>());
    }
}