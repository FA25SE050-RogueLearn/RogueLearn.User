using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNoteFromUpload;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNote;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Tests.Features.Notes.Commands.CreateNoteFromUpload;

public class CreateNoteFromUploadHandlerTests
{
    [Fact]
    public async Task Handle_UsesAiSummaryWhenAvailable()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var mapper = Substitute.For<IMapper>();
        var filePlugin = Substitute.For<IFileSummarizationPlugin>();
        var logger = Substitute.For<ILogger<CreateNoteFromUploadCommandHandler>>();

        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());
        mapper.Map<CreateNoteResponse>(Arg.Any<Note>()).Returns(ci => new CreateNoteResponse { Id = ci.Arg<Note>().Id, Title = ci.Arg<Note>().Title });
        filePlugin.SummarizeAsync(Arg.Any<AiFileAttachment>(), Arg.Any<CancellationToken>()).Returns(new { content = "ok" });

        var sut = new CreateNoteFromUploadCommandHandler(noteRepo, mapper, filePlugin, logger);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("file content"));
        var res = await sut.Handle(new CreateNoteFromUploadCommand { AuthUserId = Guid.NewGuid(), FileStream = stream, FileName = "a.txt", ContentType = "text/plain" }, CancellationToken.None);
        res.Title.Should().Be("a");
    }

    [Fact]
    public async Task Handle_FallsBackToPlainText()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var mapper = Substitute.For<IMapper>();
        var filePlugin = Substitute.For<IFileSummarizationPlugin>();
        var logger = Substitute.For<ILogger<CreateNoteFromUploadCommandHandler>>();

        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());
        mapper.Map<CreateNoteResponse>(Arg.Any<Note>()).Returns(ci => new CreateNoteResponse { Id = ci.Arg<Note>().Id, Title = ci.Arg<Note>().Title });
        filePlugin.SummarizeAsync(Arg.Any<AiFileAttachment>(), Arg.Any<CancellationToken>()).Returns((object?)null);

        var sut = new CreateNoteFromUploadCommandHandler(noteRepo, mapper, filePlugin, logger);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("plain text"));
        var res = await sut.Handle(new CreateNoteFromUploadCommand { AuthUserId = Guid.NewGuid(), FileStream = stream, FileName = "b.txt", ContentType = "text/plain" }, CancellationToken.None);
        res.Title.Should().Be("b");
    }

    [Fact]
    public async Task Handle_ThrowsWhenEmptyFile()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var mapper = Substitute.For<IMapper>();
        var filePlugin = Substitute.For<IFileSummarizationPlugin>();
        var logger = Substitute.For<ILogger<CreateNoteFromUploadCommandHandler>>();

        filePlugin.SummarizeAsync(Arg.Any<AiFileAttachment>(), Arg.Any<CancellationToken>()).Returns((object?)null);
        var sut = new CreateNoteFromUploadCommandHandler(noteRepo, mapper, filePlugin, logger);
        using var stream = new MemoryStream(Array.Empty<byte>());
        var act = () => sut.Handle(new CreateNoteFromUploadCommand { AuthUserId = Guid.NewGuid(), FileStream = stream, FileName = "c.txt", ContentType = "text/plain" }, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>();
    }
}