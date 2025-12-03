using System.Collections.Generic;
using FluentAssertions;
using RogueLearn.User.Application.Features.Meetings.Commands.ProcessArtifactsAndSummarize;
using RogueLearn.User.Application.Features.Meetings.DTOs;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Meetings.Commands.ProcessArtifactsAndSummarize;

public class ProcessArtifactsAndSummarizeCommandValidatorTests
{
    [Fact]
    public void Valid_Passes()
    {
        var cmd = new ProcessArtifactsAndSummarizeCommand(System.Guid.NewGuid(), new List<ArtifactInputDto> { new ArtifactInputDto { ArtifactType = "note", Url = "https://example.com/1" } });
        var validator = new ProcessArtifactsAndSummarizeCommandValidator();
        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_Url_Fails()
    {
        var cmd = new ProcessArtifactsAndSummarizeCommand(System.Guid.NewGuid(), new List<ArtifactInputDto> { new ArtifactInputDto { ArtifactType = "note", Url = "bad" } });
        var validator = new ProcessArtifactsAndSummarizeCommandValidator();
        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }
}