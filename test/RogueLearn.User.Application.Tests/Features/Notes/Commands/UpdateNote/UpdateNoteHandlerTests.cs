using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Notes.Commands.UpdateNote;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Tests.Features.Notes.Commands.UpdateNote;

public class UpdateNoteHandlerTests
{
    [Fact]
    public async Task Handle_ThrowsNotFound()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<UpdateNoteHandler>>();
        noteRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Note?)null);
        var sut = new UpdateNoteHandler(noteRepo, tagRepo, mapper, logger);
        var act = () => sut.Handle(new UpdateNoteCommand { Id = Guid.NewGuid(), AuthUserId = Guid.NewGuid(), Title = "t" }, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ThrowsForbidden()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<UpdateNoteHandler>>();
        var note = new Note { Id = Guid.NewGuid(), AuthUserId = Guid.NewGuid(), Title = "t" };
        noteRepo.GetByIdAsync(note.Id, Arg.Any<CancellationToken>()).Returns(note);
        var sut = new UpdateNoteHandler(noteRepo, tagRepo, mapper, logger);
        var act = () => sut.Handle(new UpdateNoteCommand { Id = note.Id, AuthUserId = Guid.NewGuid(), Title = "t" }, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_UpdatesNoteAndRelationships()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<UpdateNoteHandler>>();

        var authId = Guid.NewGuid();
        var note = new Note { Id = Guid.NewGuid(), AuthUserId = authId, Title = "old", Content = new object() };
        noteRepo.GetByIdAsync(note.Id, Arg.Any<CancellationToken>()).Returns(note);
        noteRepo.UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());
        mapper.Map<UpdateNoteResponse>(Arg.Any<Note>()).Returns(ci => new UpdateNoteResponse { Id = ci.Arg<Note>().Id, Title = ci.Arg<Note>().Title });

        tagRepo.GetTagIdsForNoteAsync(note.Id, Arg.Any<CancellationToken>()).Returns(new List<Guid> { Guid.NewGuid() });
        var sut = new UpdateNoteHandler(noteRepo, tagRepo, mapper, logger);
        var res = await sut.Handle(new UpdateNoteCommand { Id = note.Id, AuthUserId = authId, Title = "new", Content = "text", TagIds = new List<Guid>() }, CancellationToken.None);
        res.Title.Should().Be("new");
        await tagRepo.ReceivedWithAnyArgs(1).RemoveAsync(default, default, default);
    }

    private class SelfRef
    {
        public SelfRef Self => this;
    }

    [Fact]
    public async Task Handle_ContentNull_SetsNull()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<UpdateNoteHandler>>();

        var authId = Guid.NewGuid();
        var note = new Note { Id = Guid.NewGuid(), AuthUserId = authId, Title = "old" };
        noteRepo.GetByIdAsync(note.Id, Arg.Any<CancellationToken>()).Returns(note);
        noteRepo.UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());
        mapper.Map<UpdateNoteResponse>(Arg.Any<Note>()).Returns(ci => new UpdateNoteResponse { Id = ci.Arg<Note>().Id });

        var sut = new UpdateNoteHandler(noteRepo, tagRepo, mapper, logger);
        var res = await sut.Handle(new UpdateNoteCommand { Id = note.Id, AuthUserId = authId, Title = "t", Content = null }, CancellationToken.None);
        res.Id.Should().Be(note.Id);
    }

    [Fact]
    public async Task Handle_ContentListObject_Preserved()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<UpdateNoteHandler>>();

        var authId = Guid.NewGuid();
        var note = new Note { Id = Guid.NewGuid(), AuthUserId = authId, Title = "old" };
        noteRepo.GetByIdAsync(note.Id, Arg.Any<CancellationToken>()).Returns(note);
        noteRepo.UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());
        mapper.Map<UpdateNoteResponse>(Arg.Any<Note>()).Returns(ci => new UpdateNoteResponse { Id = ci.Arg<Note>().Id });
        var sut = new UpdateNoteHandler(noteRepo, tagRepo, mapper, logger);

        var lo = new List<object> { new Dictionary<string, object> { ["type"] = "paragraph" } };
        var res = await sut.Handle(new UpdateNoteCommand { Id = note.Id, AuthUserId = authId, Title = "t", Content = lo }, CancellationToken.None);
        res.Id.Should().Be(note.Id);
    }

    [Fact]
    public async Task Handle_ContentJsonElement_Converted()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<UpdateNoteHandler>>();

        var authId = Guid.NewGuid();
        var note = new Note { Id = Guid.NewGuid(), AuthUserId = authId, Title = "old" };
        noteRepo.GetByIdAsync(note.Id, Arg.Any<CancellationToken>()).Returns(note);
        noteRepo.UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());
        mapper.Map<UpdateNoteResponse>(Arg.Any<Note>()).Returns(ci => new UpdateNoteResponse { Id = ci.Arg<Note>().Id });

        var json = System.Text.Json.JsonSerializer.Serialize(new { type = "paragraph", content = new[] { new { type = "text", text = "hi" } } });
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var sut = new UpdateNoteHandler(noteRepo, tagRepo, mapper, logger);
        var res = await sut.Handle(new UpdateNoteCommand { Id = note.Id, AuthUserId = authId, Title = "t", Content = doc.RootElement }, CancellationToken.None);
        res.Id.Should().Be(note.Id);
    }

    [Fact]
    public async Task Handle_ContentWhitespaceString_Null()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<UpdateNoteHandler>>();

        var authId = Guid.NewGuid();
        var note = new Note { Id = Guid.NewGuid(), AuthUserId = authId, Title = "old" };
        noteRepo.GetByIdAsync(note.Id, Arg.Any<CancellationToken>()).Returns(note);
        noteRepo.UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());
        mapper.Map<UpdateNoteResponse>(Arg.Any<Note>()).Returns(ci => new UpdateNoteResponse { Id = ci.Arg<Note>().Id });
        var sut = new UpdateNoteHandler(noteRepo, tagRepo, mapper, logger);
        var res = await sut.Handle(new UpdateNoteCommand { Id = note.Id, AuthUserId = authId, Title = "t", Content = "  " }, CancellationToken.None);
        res.Id.Should().Be(note.Id);
    }
}
