using FluentAssertions;
using NSubstitute;
using AutoMapper;
using RogueLearn.User.Application.Features.Notes.Commands.UpdateNote;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Application.Tests.Features.Notes.Commands.UpdateNote;

public class UpdateNoteHandlerTests
{
    [Fact]
    public async Task Handle_ToAddLoop_AddsMissingTags_AndConvertsContent()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateNoteHandler>>();

        var noteId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existing = new Note { Id = noteId, AuthUserId = userId, Title = "t", Content = null };
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(existing);
        noteRepo.UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());

        tagRepo.GetTagIdsForNoteAsync(noteId, Arg.Any<CancellationToken>()).Returns(new[] { Guid.NewGuid() });
        mapper.Map<UpdateNoteResponse>(Arg.Any<Note>()).Returns(new UpdateNoteResponse { Id = noteId });

        var element = JsonSerializer.Deserialize<JsonElement>("7");
        var cmd = new UpdateNoteCommand { Id = noteId, AuthUserId = userId, Title = "updated", Content = element, TagIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() } };

        var sut = new UpdateNoteHandler(noteRepo, tagRepo, mapper, logger);
        var res = await sut.Handle(cmd, CancellationToken.None);

        res.Id.Should().Be(noteId);
        await tagRepo.ReceivedWithAnyArgs(2).AddAsync(default!, default!, default);
        existing.Content.Should().BeEquivalentTo(new List<object> { 7 });
    }

    [Fact]
    public async Task Handle_StringJsonElement_ConvertsToListWithString()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateNoteHandler>>();

        var noteId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existing = new Note { Id = noteId, AuthUserId = userId, Title = "t", Content = null };
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(existing);
        noteRepo.UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());
        mapper.Map<UpdateNoteResponse>(Arg.Any<Note>()).Returns(new UpdateNoteResponse { Id = noteId });

        var element = JsonSerializer.Deserialize<JsonElement>("\"hello\"");
        var cmd = new UpdateNoteCommand { Id = noteId, AuthUserId = userId, Title = "updated", Content = element, TagIds = new List<Guid>() };
        var sut = new UpdateNoteHandler(noteRepo, tagRepo, mapper, logger);
        await sut.Handle(cmd, CancellationToken.None);

        existing.Content.Should().BeEquivalentTo(new List<object> { "hello" });
    }

    [Fact]
    public async Task Handle_BooleanTrue_ConvertsToListWithTrue()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateNoteHandler>>();

        var noteId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existing = new Note { Id = noteId, AuthUserId = userId, Title = "t", Content = null };
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(existing);
        noteRepo.UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());
        mapper.Map<UpdateNoteResponse>(Arg.Any<Note>()).Returns(new UpdateNoteResponse { Id = noteId });

        var element = JsonSerializer.Deserialize<JsonElement>("true");
        var cmd = new UpdateNoteCommand { Id = noteId, AuthUserId = userId, Title = "updated", Content = element, TagIds = new List<Guid>() };
        var sut = new UpdateNoteHandler(noteRepo, tagRepo, mapper, logger);
        var res = await sut.Handle(cmd, CancellationToken.None);

        res.Id.Should().Be(noteId);
        existing.Content.Should().BeEquivalentTo(new List<object> { true });
    }

    [Fact]
    public async Task Handle_ObjectContent_WrappedAsList()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateNoteHandler>>();

        var noteId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existing = new Note { Id = noteId, AuthUserId = userId, Title = "t", Content = null };
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(existing);
        noteRepo.UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());
        mapper.Map<UpdateNoteResponse>(Arg.Any<Note>()).Returns(new UpdateNoteResponse { Id = noteId });

        var cmd = new UpdateNoteCommand { Id = noteId, AuthUserId = userId, Title = "updated", Content = new Dictionary<string, object> { ["k"] = "v" }, TagIds = new List<Guid>() };
        var sut = new UpdateNoteHandler(noteRepo, tagRepo, mapper, logger);
        await sut.Handle(cmd, CancellationToken.None);

        existing.Content.Should().BeEquivalentTo(new List<object> { new Dictionary<string, object> { ["k"] = "v" } });
    }

    [Fact]
    public async Task Handle_NoteNotFound_ThrowsNotFound()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateNoteHandler>>();

        var noteId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns((Note?)null);

        var sut = new UpdateNoteHandler(noteRepo, tagRepo, mapper, logger);
        var act = () => sut.Handle(new UpdateNoteCommand { Id = noteId, AuthUserId = userId, Title = "t" }, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
    }

    [Fact]
    public async Task Handle_ForbiddenUser_ThrowsForbidden()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateNoteHandler>>();

        var noteId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existing = new Note { Id = noteId, AuthUserId = Guid.NewGuid() };
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(existing);

        var sut = new UpdateNoteHandler(noteRepo, tagRepo, mapper, logger);
        var act = () => sut.Handle(new UpdateNoteCommand { Id = noteId, AuthUserId = userId, Title = "t" }, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.ForbiddenException>();
    }

    [Fact]
    public async Task Handle_ToRemoveLoop_RemovesObsoleteTags()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateNoteHandler>>();

        var noteId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existing = new Note { Id = noteId, AuthUserId = userId, Title = "t", Content = null };
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(existing);
        noteRepo.UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());
        mapper.Map<UpdateNoteResponse>(Arg.Any<Note>()).Returns(new UpdateNoteResponse { Id = noteId });

        var existingTag = Guid.NewGuid();
        tagRepo.GetTagIdsForNoteAsync(noteId, Arg.Any<CancellationToken>()).Returns(new[] { existingTag });

        var cmd = new UpdateNoteCommand { Id = noteId, AuthUserId = userId, Title = "updated", Content = "[]", TagIds = new List<Guid> { } };
        var sut = new UpdateNoteHandler(noteRepo, tagRepo, mapper, logger);
        await sut.Handle(cmd, CancellationToken.None);

        await tagRepo.Received(1).RemoveAsync(noteId, existingTag, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StringPlainText_WrapsAsParagraphBlock()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateNoteHandler>>();

        var noteId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existing = new Note { Id = noteId, AuthUserId = userId };
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(existing);
        noteRepo.UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());
        mapper.Map<UpdateNoteResponse>(Arg.Any<Note>()).Returns(new UpdateNoteResponse { Id = noteId });

        var cmd = new UpdateNoteCommand { Id = noteId, AuthUserId = userId, Title = "t", Content = "hello world", TagIds = new List<Guid>() };
        var sut = new UpdateNoteHandler(noteRepo, tagRepo, mapper, logger);
        await sut.Handle(cmd, CancellationToken.None);

        existing.Content.Should().BeEquivalentTo(new List<object>
        {
            new Dictionary<string, object?>
            {
                ["type"] = "paragraph",
                ["content"] = new List<object>
                {
                    new Dictionary<string, object?>
                    {
                        ["type"] = "text",
                        ["text"] = "hello world"
                    }
                }
            }
        });
    }

    [Fact]
    public async Task Handle_StringJsonObject_WrappedAsList()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateNoteHandler>>();

        var noteId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existing = new Note { Id = noteId, AuthUserId = userId };
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(existing);
        noteRepo.UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());
        mapper.Map<UpdateNoteResponse>(Arg.Any<Note>()).Returns(new UpdateNoteResponse { Id = noteId });

        var cmd = new UpdateNoteCommand { Id = noteId, AuthUserId = userId, Title = "t", Content = "{\"a\":1}", TagIds = new List<Guid>() };
        var sut = new UpdateNoteHandler(noteRepo, tagRepo, mapper, logger);
        await sut.Handle(cmd, CancellationToken.None);

        existing.Content.Should().BeEquivalentTo(new List<object> { new Dictionary<string, object> { ["a"] = 1 } });
    }

    [Fact]
    public async Task Handle_EmptyStringContent_BecomesNull()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateNoteHandler>>();

        var noteId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existing = new Note { Id = noteId, AuthUserId = userId };
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(existing);
        noteRepo.UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());
        mapper.Map<UpdateNoteResponse>(Arg.Any<Note>()).Returns(new UpdateNoteResponse { Id = noteId });

        var cmd = new UpdateNoteCommand { Id = noteId, AuthUserId = userId, Title = "t", Content = "  ", TagIds = new List<Guid>() };
        var sut = new UpdateNoteHandler(noteRepo, tagRepo, mapper, logger);
        await sut.Handle(cmd, CancellationToken.None);

        existing.Content.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NullContent_RemainsNull()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateNoteHandler>>();

        var noteId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existing = new Note { Id = noteId, AuthUserId = userId };
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(existing);
        noteRepo.UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());
        mapper.Map<UpdateNoteResponse>(Arg.Any<Note>()).Returns(new UpdateNoteResponse { Id = noteId });

        var cmd = new UpdateNoteCommand { Id = noteId, AuthUserId = userId, Title = "t", Content = null, TagIds = new List<Guid>() };
        var sut = new UpdateNoteHandler(noteRepo, tagRepo, mapper, logger);
        await sut.Handle(cmd, CancellationToken.None);

        existing.Content.Should().BeNull();
    }

    [Fact]
    public async Task Handle_FalseAndNullJsonKinds_ConvertProperly()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateNoteHandler>>();

        var noteId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existing = new Note { Id = noteId, AuthUserId = userId };
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(existing);
        noteRepo.UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());
        mapper.Map<UpdateNoteResponse>(Arg.Any<Note>()).Returns(new UpdateNoteResponse { Id = noteId });

        var falseEl = JsonSerializer.Deserialize<JsonElement>("false");
        var cmdFalse = new UpdateNoteCommand { Id = noteId, AuthUserId = userId, Title = "t", Content = falseEl };
        var sut = new UpdateNoteHandler(noteRepo, tagRepo, mapper, logger);
        await sut.Handle(cmdFalse, CancellationToken.None);
        existing.Content.Should().BeEquivalentTo(new List<object> { false });

        var nullEl = JsonSerializer.Deserialize<JsonElement>("null");
        var cmdNull = new UpdateNoteCommand { Id = noteId, AuthUserId = userId, Title = "t", Content = nullEl };
        await sut.Handle(cmdNull, CancellationToken.None);
        existing.Content.Should().BeNull();
    }

    [Fact]
    public async Task Handle_TagIdsNull_DoesNotTouchTags()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateNoteHandler>>();

        var noteId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existing = new Note { Id = noteId, AuthUserId = userId };
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(existing);
        noteRepo.UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());
        mapper.Map<UpdateNoteResponse>(Arg.Any<Note>()).Returns(new UpdateNoteResponse { Id = noteId });

        var cmd = new UpdateNoteCommand { Id = noteId, AuthUserId = userId, Title = "t", Content = new List<object>(), TagIds = null };
        var sut = new UpdateNoteHandler(noteRepo, tagRepo, mapper, logger);
        await sut.Handle(cmd, CancellationToken.None);

        await tagRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default!, default);
        await tagRepo.DidNotReceiveWithAnyArgs().RemoveAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_JsonElementUndefined_WrappedAsEmptyString()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateNoteHandler>>();

        var noteId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existing = new Note { Id = noteId, AuthUserId = userId };
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(existing);
        noteRepo.UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());
        mapper.Map<UpdateNoteResponse>(Arg.Any<Note>()).Returns(new UpdateNoteResponse { Id = noteId });

        var element = default(JsonElement);
        var cmd = new UpdateNoteCommand { Id = noteId, AuthUserId = userId, Title = "t", Content = element, TagIds = new List<Guid>() };
        var sut = new UpdateNoteHandler(noteRepo, tagRepo, mapper, logger);
        await sut.Handle(cmd, CancellationToken.None);

        existing.Content.Should().BeEquivalentTo(new List<object> { string.Empty });
    }
}
