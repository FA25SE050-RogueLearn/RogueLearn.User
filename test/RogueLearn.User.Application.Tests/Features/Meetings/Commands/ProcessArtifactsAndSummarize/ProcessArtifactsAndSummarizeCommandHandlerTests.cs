using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Features.Meetings.Commands.ProcessArtifactsAndSummarize;
using RogueLearn.User.Application.Features.Meetings.DTOs;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Xunit;
using OpenAI.Audio;
using Microsoft.SemanticKernel;

namespace RogueLearn.User.Application.Tests.Features.Meetings.Commands.ProcessArtifactsAndSummarize;

public class ProcessArtifactsAndSummarizeCommandHandlerTests
{
    [Fact]
    public async Task Handle_SummarizesAndStores()
    {
        var repo = Substitute.For<IMeetingSummaryRepository>();
        var logger = Substitute.For<ILogger<ProcessArtifactsAndSummarizeCommandHandler>>();
        var audioClient = Substitute.For<AudioClient>();
        var kernel = Substitute.For<Kernel>();
        var sut = new ProcessArtifactsAndSummarizeCommandHandler(repo, logger, audioClient, kernel);

        var cmd = new ProcessArtifactsAndSummarizeCommand(System.Guid.NewGuid(), new List<ArtifactInputDto> {
            new ArtifactInputDto { ArtifactType = "recording", FileId = "file-1" }
        }, "token");
        repo.GetByMeetingAsync(cmd.MeetingId, Arg.Any<CancellationToken>()).Returns((MeetingSummary?)null);

        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).AddAsync(Arg.Any<MeetingSummary>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdatesExistingSummary_WhenPresent()
    {
        var repo = Substitute.For<IMeetingSummaryRepository>();
        var logger = Substitute.For<ILogger<ProcessArtifactsAndSummarizeCommandHandler>>();
        var audioClient = Substitute.For<AudioClient>();
        var kernel = Substitute.For<Kernel>();
        var sut = new ProcessArtifactsAndSummarizeCommandHandler(repo, logger, audioClient, kernel);

        var meetingId = System.Guid.NewGuid();
        var cmd = new ProcessArtifactsAndSummarizeCommand(meetingId, new List<ArtifactInputDto> {
            new ArtifactInputDto { ArtifactType = "recording", FileId = "file-1" }
        }, "token");
        var existing = new MeetingSummary { MeetingId = meetingId, SummaryText = "old" };
        repo.GetByMeetingAsync(meetingId, Arg.Any<CancellationToken>()).Returns(existing);

        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).UpdateAsync(Arg.Is<MeetingSummary>(m => m.MeetingId == meetingId && !string.IsNullOrEmpty(m.SummaryText)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyArtifacts_NoSummarizationOrStorage()
    {
        var repo = Substitute.For<IMeetingSummaryRepository>();
        var logger = Substitute.For<ILogger<ProcessArtifactsAndSummarizeCommandHandler>>();
        var audioClient = Substitute.For<AudioClient>();
        var kernel = Substitute.For<Kernel>();
        var sut = new ProcessArtifactsAndSummarizeCommandHandler(repo, logger, audioClient, kernel);

        var cmd = new ProcessArtifactsAndSummarizeCommand(System.Guid.NewGuid(), new List<ArtifactInputDto>(), "token");
        await sut.Handle(cmd, CancellationToken.None);

        await repo.DidNotReceive().AddAsync(Arg.Any<MeetingSummary>(), Arg.Any<CancellationToken>());
        await repo.DidNotReceive().UpdateAsync(Arg.Any<MeetingSummary>(), Arg.Any<CancellationToken>());
    }
}
