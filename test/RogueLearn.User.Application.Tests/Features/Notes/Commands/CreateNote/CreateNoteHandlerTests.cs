using FluentAssertions;
using NSubstitute;
using AutoMapper;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNote;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Application.Tests.Features.Notes.Commands.CreateNote;

public class CreateNoteHandlerTests
{
    [Fact]
    public async Task Handle_NumberJsonElement_ConvertsUsingTryGetInt32()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CreateNoteHandler>>();

        Note created = default!;
        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci =>
        {
            created = ci.Arg<Note>();
            MutableThrower.ThrowNow = true;
            return created;
        });
        mapper.Map<CreateNoteResponse>(Arg.Any<Note>()).Returns(new CreateNoteResponse { Id = Guid.NewGuid() });

        var sut = new CreateNoteHandler(noteRepo, tagRepo, mapper, logger);
        var element = JsonSerializer.Deserialize<JsonElement>("42");
        await sut.Handle(new CreateNoteCommand { AuthUserId = Guid.NewGuid(), Title = "t", Content = element }, CancellationToken.None);

        created.Content.Should().BeOfType<List<object>>();
        ((List<object>)created.Content!)[0].Should().Be(42);
    }

    [Fact]
    public async Task Handle_ListObjectContent_ReturnsSameList()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CreateNoteHandler>>();

        Note created = default!;
        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => { created = ci.Arg<Note>(); return created; });
        mapper.Map<CreateNoteResponse>(Arg.Any<Note>()).Returns(new CreateNoteResponse { Id = Guid.NewGuid() });

        var sut = new CreateNoteHandler(noteRepo, tagRepo, mapper, logger);
        var list = new List<object> { 1, "text" };
        await sut.Handle(new CreateNoteCommand { AuthUserId = Guid.NewGuid(), Title = "t", Content = list }, CancellationToken.None);

        created.Content.Should().BeSameAs(list);
    }

    [Fact]
    public async Task Handle_EmptyStringContent_SetsNull()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CreateNoteHandler>>();

        Note created = default!;
        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => { created = ci.Arg<Note>(); return created; });
        mapper.Map<CreateNoteResponse>(Arg.Any<Note>()).Returns(new CreateNoteResponse { Id = Guid.NewGuid() });

        var sut = new CreateNoteHandler(noteRepo, tagRepo, mapper, logger);
        await sut.Handle(new CreateNoteCommand { AuthUserId = Guid.NewGuid(), Title = "t", Content = "   " }, CancellationToken.None);

        created.Content.Should().BeNull();
    }

    [Fact]
    public async Task Handle_StringJsonObject_WrappedAsList()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CreateNoteHandler>>();

        Note created = default!;
        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => { created = ci.Arg<Note>(); return created; });
        mapper.Map<CreateNoteResponse>(Arg.Any<Note>()).Returns(new CreateNoteResponse { Id = Guid.NewGuid() });

        var sut = new CreateNoteHandler(noteRepo, tagRepo, mapper, logger);
        await sut.Handle(new CreateNoteCommand { AuthUserId = Guid.NewGuid(), Title = "t", Content = "{\"a\":1}" }, CancellationToken.None);

        created.Content.Should().BeEquivalentTo(new List<object> { new Dictionary<string, object> { ["a"] = 1 } });
    }

    [Fact]
    public async Task Handle_FalseAndNullJsonElement_ConvertsProperly()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CreateNoteHandler>>();

        Note created = default!;
        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => { created = ci.Arg<Note>(); return created; });
        mapper.Map<CreateNoteResponse>(Arg.Any<Note>()).Returns(new CreateNoteResponse { Id = Guid.NewGuid() });

        var sut = new CreateNoteHandler(noteRepo, tagRepo, mapper, logger);

        var falseEl = JsonSerializer.Deserialize<JsonElement>("false");
        await sut.Handle(new CreateNoteCommand { AuthUserId = Guid.NewGuid(), Title = "t", Content = falseEl }, CancellationToken.None);
        created.Content.Should().BeEquivalentTo(new List<object> { false });

        var nullEl = JsonSerializer.Deserialize<JsonElement>("null");
        await sut.Handle(new CreateNoteCommand { AuthUserId = Guid.NewGuid(), Title = "t", Content = nullEl }, CancellationToken.None);
        created.Content.Should().BeNull();
    }

    private class ThrowingContent
    {
        public int Boom => throw new Exception("boom");
    }

    private class MutableThrower
    {
        public static bool ThrowNow;
        public int Boom => ThrowNow ? throw new Exception("boom") : 1;
    }


    [Fact]
    public async Task Handle_BooleanTrue_ConvertsToListWithTrue()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CreateNoteHandler>>();

        Note created = default!;
        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => { created = ci.Arg<Note>(); return created; });
        mapper.Map<CreateNoteResponse>(Arg.Any<Note>()).Returns(new CreateNoteResponse { Id = Guid.NewGuid() });

        var sut = new CreateNoteHandler(noteRepo, tagRepo, mapper, logger);
        var element = JsonSerializer.Deserialize<JsonElement>("true");
        await sut.Handle(new CreateNoteCommand { AuthUserId = Guid.NewGuid(), Title = "t", Content = element }, CancellationToken.None);

        created.Content.Should().BeEquivalentTo(new List<object> { true });
    }

    [Fact]
    public async Task Handle_DictionaryContent_WrappedInList()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CreateNoteHandler>>();

        Note created = default!;
        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => { created = ci.Arg<Note>(); return created; });
        mapper.Map<CreateNoteResponse>(Arg.Any<Note>()).Returns(new CreateNoteResponse { Id = Guid.NewGuid() });

        var sut = new CreateNoteHandler(noteRepo, tagRepo, mapper, logger);
        var dict = new Dictionary<string, object> { ["k"] = "v" };
        await sut.Handle(new CreateNoteCommand { AuthUserId = Guid.NewGuid(), Title = "t", Content = dict }, CancellationToken.None);
        created.Content.Should().BeEquivalentTo(new List<object> { dict });
    }

    [Fact]
    public async Task Handle_JsonArrayString_ReturnsConvertedListDirectly()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CreateNoteHandler>>();

        Note created = default!;
        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => { created = ci.Arg<Note>(); return created; });
        mapper.Map<CreateNoteResponse>(Arg.Any<Note>()).Returns(new CreateNoteResponse { Id = Guid.NewGuid() });

        var sut = new CreateNoteHandler(noteRepo, tagRepo, mapper, logger);
        var json = "[{\"type\":\"paragraph\"}]";
        await sut.Handle(new CreateNoteCommand { AuthUserId = Guid.NewGuid(), Title = "t", Content = json }, CancellationToken.None);

        created.Content.Should().BeOfType<List<object>>();
        ((List<object>)created.Content!).Count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_StringPlainText_WrappedIntoParagraphStructure()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CreateNoteHandler>>();

        Note created = default!;
        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => { created = ci.Arg<Note>(); return created; });
        mapper.Map<CreateNoteResponse>(Arg.Any<Note>()).Returns(new CreateNoteResponse { Id = Guid.NewGuid(), AuthUserId = Guid.NewGuid(), Title = "t", Content = new List<object> { new { type = "paragraph", children = new List<object> { new { text = "hello" } } } }, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, IsPublic = true });

        var sut = new CreateNoteHandler(noteRepo, tagRepo, mapper, logger);
        await sut.Handle(new CreateNoteCommand { AuthUserId = Guid.NewGuid(), Title = "t", Content = "hello" }, CancellationToken.None);

        created.Content.Should().BeOfType<List<object>>();
        ((List<object>)created.Content!).Count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NullContent_SetsNullAndAddsTags()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CreateNoteHandler>>();

        Note created = default!;
        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => { created = ci.Arg<Note>(); return created; });
        mapper.Map<CreateNoteResponse>(Arg.Any<Note>()).Returns(new CreateNoteResponse { Id = Guid.NewGuid() });

        var tag1 = Guid.NewGuid();
        var tag2 = Guid.NewGuid();

        var sut = new CreateNoteHandler(noteRepo, tagRepo, mapper, logger);
        await sut.Handle(new CreateNoteCommand { AuthUserId = Guid.NewGuid(), Title = "t", Content = null, TagIds = new List<Guid> { tag1, tag2, tag1 } }, CancellationToken.None);

        created.Content.Should().BeNull();
        await tagRepo.Received(1).AddAsync(Arg.Any<Guid>(), Arg.Is<Guid>(g => g == tag1), Arg.Any<CancellationToken>());
        await tagRepo.Received(1).AddAsync(Arg.Any<Guid>(), Arg.Is<Guid>(g => g == tag2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PostInsertPreviewSerializationThrows_DoesNotCrash()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CreateNoteHandler>>();

        Note created = default!;
        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci =>
        {
            created = ci.Arg<Note>();
            MutableThrower.ThrowNow = true;
            return created;
        });
        mapper.Map<CreateNoteResponse>(Arg.Any<Note>()).Returns(new CreateNoteResponse { Id = Guid.NewGuid() });

        var sut = new CreateNoteHandler(noteRepo, tagRepo, mapper, logger);
        var content = new List<object> { new MutableThrower() };
        var res = await sut.Handle(new CreateNoteCommand { AuthUserId = Guid.NewGuid(), Title = "t", Content = content }, CancellationToken.None);

        res.Should().NotBeNull();
        created.Content.Should().BeSameAs(content);
    }

    [Fact]
    public async Task Handle_UndefinedJsonElement_FallbacksToStringValue()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CreateNoteHandler>>();

        Note created = default!;
        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => { created = ci.Arg<Note>(); return created; });
        mapper.Map<CreateNoteResponse>(Arg.Any<Note>()).Returns(new CreateNoteResponse { Id = Guid.NewGuid() });

        var sut = new CreateNoteHandler(noteRepo, tagRepo, mapper, logger);
        var undefined = default(JsonElement);
        await sut.Handle(new CreateNoteCommand { AuthUserId = Guid.NewGuid(), Title = "t", Content = undefined }, CancellationToken.None);

        created.Content.Should().BeOfType<List<object>>();
        ((List<object>)created.Content!)[0].Should().Be(string.Empty);
    }
}
