using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.AiTagging.Queries.SuggestNoteTags;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.AiTagging.Queries.SuggestNoteTags;

public class SuggestNoteTagsQueryHandlerTests
{
    [Fact]
    public async Task Handle_MissingInputs_Throws()
    {
        var service = Substitute.For<ITaggingSuggestionService>();
        var noteRepo = Substitute.For<INoteRepository>();
        var sut = new SuggestNoteTagsQueryHandler(service, noteRepo);
        var q = new SuggestNoteTagsQuery { AuthUserId = Guid.NewGuid(), RawText = null, NoteId = null, MaxTags = 10 };
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => sut.Handle(q, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoteNotFoundOrForbidden_Throws()
    {
        var authUserId = Guid.NewGuid();
        var service = Substitute.For<ITaggingSuggestionService>();
        var noteRepo = Substitute.For<INoteRepository>();
        var sut = new SuggestNoteTagsQueryHandler(service, noteRepo);
        var noteId = Guid.NewGuid();
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(new Note { Id = noteId, AuthUserId = Guid.NewGuid() });
        var q = new SuggestNoteTagsQuery { AuthUserId = authUserId, RawText = null, NoteId = noteId, MaxTags = 10 };
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => sut.Handle(q, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RawTextProvided_ReturnsSuggestions()
    {
        var authUserId = Guid.NewGuid();
        var service = Substitute.For<ITaggingSuggestionService>();
        var noteRepo = Substitute.For<INoteRepository>();
        var suggestions = new List<TagSuggestionDto> { new TagSuggestionDto { Label = "AI", Confidence = 0.9 }, new TagSuggestionDto { Label = "ML", Confidence = 0.8 } };
        service.SuggestAsync(authUserId, "hello", 10, Arg.Any<CancellationToken>()).Returns(suggestions);
        var sut = new SuggestNoteTagsQueryHandler(service, noteRepo);
        var q = new SuggestNoteTagsQuery { AuthUserId = authUserId, RawText = "hello", NoteId = null, MaxTags = 10 };
        var res = await sut.Handle(q, CancellationToken.None);
        res.Suggestions.Should().BeEquivalentTo(suggestions);
    }

    [Fact]
    public async Task Handle_ContentPlainString_UsesPlainText()
    {
        var authUserId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        var service = Substitute.For<ITaggingSuggestionService>();
        var noteRepo = Substitute.For<INoteRepository>();
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(new Note { Id = noteId, AuthUserId = authUserId, Content = "plain text" });
        service.SuggestAsync(authUserId, "plain text", 5, Arg.Any<CancellationToken>()).Returns(new List<TagSuggestionDto>());
        var sut = new SuggestNoteTagsQueryHandler(service, noteRepo);
        var q = new SuggestNoteTagsQuery { AuthUserId = authUserId, RawText = null, NoteId = noteId, MaxTags = 5 };
        await sut.Handle(q, CancellationToken.None);
        await service.Received(1).SuggestAsync(authUserId, "plain text", 5, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ContentJsonString_ExtractsInnerString()
    {
        var authUserId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        var service = Substitute.For<ITaggingSuggestionService>();
        var noteRepo = Substitute.For<INoteRepository>();
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(new Note { Id = noteId, AuthUserId = authUserId, Content = "\"inner\"" });
        var sut = new SuggestNoteTagsQueryHandler(service, noteRepo);
        var q = new SuggestNoteTagsQuery { AuthUserId = authUserId, RawText = null, NoteId = noteId, MaxTags = 3 };
        await sut.Handle(q, CancellationToken.None);
        await service.Received(1).SuggestAsync(authUserId, "inner", 3, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ContentJsonElementString_ExtractsString()
    {
        var authUserId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        var service = Substitute.For<ITaggingSuggestionService>();
        var noteRepo = Substitute.For<INoteRepository>();
        var el = JsonSerializer.Deserialize<JsonElement>("\"hello\"");
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(new Note { Id = noteId, AuthUserId = authUserId, Content = el });
        var sut = new SuggestNoteTagsQueryHandler(service, noteRepo);
        var q = new SuggestNoteTagsQuery { AuthUserId = authUserId, RawText = null, NoteId = noteId, MaxTags = 2 };
        await sut.Handle(q, CancellationToken.None);
        await service.Received(1).SuggestAsync(authUserId, "hello", 2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ContentObject_FallbacksToJson()
    {
        var authUserId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        var service = Substitute.For<ITaggingSuggestionService>();
        var noteRepo = Substitute.For<INoteRepository>();
        var obj = new { a = 1, b = "x" };
        var json = JsonSerializer.Serialize(obj);
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(new Note { Id = noteId, AuthUserId = authUserId, Content = obj });
        var sut = new SuggestNoteTagsQueryHandler(service, noteRepo);
        var q = new SuggestNoteTagsQuery { AuthUserId = authUserId, RawText = null, NoteId = noteId, MaxTags = 4 };
        await sut.Handle(q, CancellationToken.None);
        await service.Received(1).SuggestAsync(authUserId, json, 4, Arg.Any<CancellationToken>());
    }
}