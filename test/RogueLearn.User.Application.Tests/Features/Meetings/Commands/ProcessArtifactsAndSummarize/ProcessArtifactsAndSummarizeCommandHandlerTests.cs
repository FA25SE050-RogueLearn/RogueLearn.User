using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Features.Meetings.Commands.ProcessArtifactsAndSummarize;
using RogueLearn.User.Application.Features.Meetings.DTOs;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Meetings.Commands.ProcessArtifactsAndSummarize;

public class ProcessArtifactsAndSummarizeCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_SummarizesAndStores(ProcessArtifactsAndSummarizeCommand cmd)
    {
        var plugin = Substitute.For<IFileSummarizationPlugin>();
        var repo = Substitute.For<IMeetingSummaryRepository>();
        var sut = new ProcessArtifactsAndSummarizeCommandHandler(plugin, repo);

        cmd = new ProcessArtifactsAndSummarizeCommand(cmd.MeetingId, new List<ArtifactInputDto> {
            new ArtifactInputDto { ArtifactType = "note", Url = "https://example.com/a" },
            new ArtifactInputDto { ArtifactType = "file", Url = "https://example.com/b" }
        });
        plugin.SummarizeAsync(Arg.Any<AiFileAttachment>(), Arg.Any<CancellationToken>()).Returns(new { summary = "text" });
        repo.GetByMeetingAsync(cmd.MeetingId, Arg.Any<CancellationToken>()).Returns((MeetingSummary?)null);

        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).AddAsync(Arg.Any<MeetingSummary>(), Arg.Any<CancellationToken>());
    }
}