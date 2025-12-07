using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNoteWithAiTags;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Notes.Commands.CreateNoteWithAiTags;

public class CreateNoteWithAiTagsCommandHandlerTests
{
    [Fact]
    public async Task Handle_NoContentProvided_ThrowsValidation()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var suggestion = Substitute.For<ITaggingSuggestionService>();
        var summarization = Substitute.For<ISummarizationPlugin>();
        var fileSummarization = Substitute.For<IFileSummarizationPlugin>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var logger = Substitute.For<ILogger<CreateNoteWithAiTagsCommandHandler>>();

        var sut = new CreateNoteWithAiTagsCommandHandler(noteRepo, suggestion, summarization, fileSummarization, mediator, logger);
        var cmd = new CreateNoteWithAiTagsCommand { AuthUserId = Guid.NewGuid(), RawText = " ", FileStream = null };
        await Assert.ThrowsAsync<ValidationException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RawSummarizationFails_ThrowsValidation()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var suggestion = Substitute.For<ITaggingSuggestionService>();
        var summarization = Substitute.For<ISummarizationPlugin>();
        var fileSummarization = Substitute.For<IFileSummarizationPlugin>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var logger = Substitute.For<ILogger<CreateNoteWithAiTagsCommandHandler>>();

        summarization.SummarizeTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((object?)null);
        var sut = new CreateNoteWithAiTagsCommandHandler(noteRepo, suggestion, summarization, fileSummarization, mediator, logger);

        var cmd = new CreateNoteWithAiTagsCommand { AuthUserId = Guid.NewGuid(), RawText = "hello world" };
        await Assert.ThrowsAsync<ValidationException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_FileSummarizationFails_ThrowsValidation()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var suggestion = Substitute.For<ITaggingSuggestionService>();
        var summarization = Substitute.For<ISummarizationPlugin>();
        var fileSummarization = Substitute.For<IFileSummarizationPlugin>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var logger = Substitute.For<ILogger<CreateNoteWithAiTagsCommandHandler>>();

        fileSummarization.SummarizeAsync(Arg.Any<AiFileAttachment>(), Arg.Any<CancellationToken>()).Returns((object?)null);
        var sut = new CreateNoteWithAiTagsCommandHandler(noteRepo, suggestion, summarization, fileSummarization, mediator, logger);

        var ms = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var cmd = new CreateNoteWithAiTagsCommand { AuthUserId = Guid.NewGuid(), FileStream = ms, FileLength = ms.Length, ContentType = "text/plain", FileName = "a.txt" };
        await Assert.ThrowsAsync<ValidationException>(() => sut.Handle(cmd, CancellationToken.None));
    }
}
