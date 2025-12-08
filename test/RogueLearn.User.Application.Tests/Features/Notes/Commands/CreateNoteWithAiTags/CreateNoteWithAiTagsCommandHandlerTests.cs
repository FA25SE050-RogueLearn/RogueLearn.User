using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNoteWithAiTags;
using RogueLearn.User.Application.Features.AiTagging.Commands.CommitNoteTagSelections;
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

    [Fact]
    public async Task Handle_CreatesNoteFromRawText()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var sugg = Substitute.For<ITaggingSuggestionService>();
        var summarizer = Substitute.For<ISummarizationPlugin>();
        var fileSummarizer = Substitute.For<IFileSummarizationPlugin>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var logger = Substitute.For<ILogger<CreateNoteWithAiTagsCommandHandler>>();

        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());
        summarizer.SummarizeTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new { content = "ok" });
        sugg.SuggestAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new List<Application.Models.TagSuggestionDto>());

        var sut = new CreateNoteWithAiTagsCommandHandler(noteRepo, sugg, summarizer, fileSummarizer, mediator, logger);
        var res = await sut.Handle(new CreateNoteWithAiTagsCommand { AuthUserId = Guid.NewGuid(), RawText = "hello", Title = null, ApplySuggestions = false }, CancellationToken.None);
        res.Title.Should().Be("New Note");
        res.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_FileUpload_AppliesSuggestions()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var sugg = Substitute.For<ITaggingSuggestionService>();
        var summarizer = Substitute.For<ISummarizationPlugin>();
        var fileSummarizer = Substitute.For<IFileSummarizationPlugin>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var logger = Substitute.For<ILogger<CreateNoteWithAiTagsCommandHandler>>();

        var noteId = Guid.NewGuid();
        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci =>
        {
            var n = ci.Arg<Note>();
            n.Id = noteId;
            return n;
        });

        fileSummarizer.SummarizeAsync(Arg.Any<AiFileAttachment>(), Arg.Any<CancellationToken>())
                      .Returns(new { content = "ok" });

        var matchedId = Guid.NewGuid();
        var suggestions = new List<TagSuggestionDto>
        {
            new TagSuggestionDto { Label = "existing", Confidence = 0.9, MatchedTagId = matchedId, MatchedTagName = "existing" },
            new TagSuggestionDto { Label = "new", Confidence = 0.7 }
        };
        sugg.SuggestFromFileAsync(Arg.Any<Guid>(), Arg.Any<AiFileAttachment>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(suggestions);

        var createdTagId = Guid.NewGuid();
        mediator.Send(Arg.Any<CommitNoteTagSelectionsCommand>(), Arg.Any<CancellationToken>())
                .Returns(new CommitNoteTagSelectionsResponse
                {
                    NoteId = noteId,
                    AddedTagIds = new[] { matchedId, createdTagId },
                    CreatedTags = new[] { new CreatedTagDto { Id = createdTagId, Name = "new" } },
                    TotalTagsAssigned = 2
                });

        var sut = new CreateNoteWithAiTagsCommandHandler(noteRepo, sugg, summarizer, fileSummarizer, mediator, logger);

        using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        var cmd = new CreateNoteWithAiTagsCommand
        {
            AuthUserId = Guid.NewGuid(),
            FileStream = ms,
            FileLength = ms.Length,
            ContentType = "application/pdf",
            FileName = "doc.pdf",
            ApplySuggestions = true,
            MaxTags = 5
        };

        var res = await sut.Handle(cmd, CancellationToken.None);

        res.NoteId.Should().Be(noteId);
        res.Title.Should().Be("doc");
        res.Suggestions.Should().HaveCount(2);
        res.AppliedTagIds.Should().BeEquivalentTo(new[] { matchedId, createdTagId });
        res.CreatedTags.Should().HaveCount(1);
        res.TotalTagsAssigned.Should().Be(2);
    }
}
