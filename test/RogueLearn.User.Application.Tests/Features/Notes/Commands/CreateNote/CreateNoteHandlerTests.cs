using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNote;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using AutoMapper;

namespace RogueLearn.User.Application.Tests.Features.Notes.Commands.CreateNote;

public class CreateNoteHandlerTests
{
    [Fact]
    public async Task Handle_CreatesNoteAndNormalizesContent()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<CreateNoteHandler>>();

        Note? captured = null;
        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>())
            .Returns(ci => { captured = ci.Arg<Note>(); return captured!; });

        mapper.Map<CreateNoteResponse>(Arg.Any<Note>())
            .Returns(ci =>
            {
                var n = ci.Arg<Note>();
                return new CreateNoteResponse { Id = n.Id, AuthUserId = n.AuthUserId, Title = n.Title, Content = n.Content, IsPublic = n.IsPublic, CreatedAt = n.CreatedAt, UpdatedAt = n.UpdatedAt };
            });

        var sut = new CreateNoteHandler(noteRepo, tagRepo, mapper, logger);
        var cmd = new CreateNoteCommand { AuthUserId = Guid.NewGuid(), Title = "Title", Content = "hello", IsPublic = true, TagIds = new List<Guid> { Guid.NewGuid() } };
        var res = await sut.Handle(cmd, CancellationToken.None);

        captured!.Content.Should().NotBeNull();
        captured!.Content.Should().BeAssignableTo<List<object>>();
        res.Title.Should().Be("Title");
        await tagRepo.ReceivedWithAnyArgs(1).AddAsync(default, default, default);
    }

    private class SelfRef
    {
        public SelfRef Self => this;
    }

    [Fact]
    public async Task Handle_ContentNull_StoresNull()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<CreateNoteHandler>>();
        Note? captured = null;
        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => { captured = ci.Arg<Note>(); return captured!; });
        mapper.Map<CreateNoteResponse>(Arg.Any<Note>()).Returns(ci => new CreateNoteResponse { Id = ci.Arg<Note>().Id });
        var sut = new CreateNoteHandler(noteRepo, tagRepo, mapper, logger);

        var cmd = new CreateNoteCommand { AuthUserId = Guid.NewGuid(), Title = "T", Content = null, IsPublic = false };
        await sut.Handle(cmd, CancellationToken.None);
        captured!.Content.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ContentListObject_Preserved()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<CreateNoteHandler>>();
        Note? captured = null;
        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => { captured = ci.Arg<Note>(); return captured!; });
        mapper.Map<CreateNoteResponse>(Arg.Any<Note>()).Returns(ci => new CreateNoteResponse { Id = ci.Arg<Note>().Id });
        var sut = new CreateNoteHandler(noteRepo, tagRepo, mapper, logger);

        var lo = new List<object> { new Dictionary<string, object> { ["type"] = "paragraph" } };
        var cmd = new CreateNoteCommand { AuthUserId = Guid.NewGuid(), Title = "T", Content = lo };
        await sut.Handle(cmd, CancellationToken.None);
        captured!.Content.Should().BeSameAs(lo);
    }

    [Fact]
    public async Task Handle_ContentJsonElement_Converted()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<CreateNoteHandler>>();
        Note? captured = null;
        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => { captured = ci.Arg<Note>(); return captured!; });
        mapper.Map<CreateNoteResponse>(Arg.Any<Note>()).Returns(ci => new CreateNoteResponse { Id = ci.Arg<Note>().Id });
        var sut = new CreateNoteHandler(noteRepo, tagRepo, mapper, logger);

        var json = System.Text.Json.JsonSerializer.Serialize(new { type = "paragraph", content = new[] { new { type = "text", text = "hi" } } });
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var cmd = new CreateNoteCommand { AuthUserId = Guid.NewGuid(), Title = "T", Content = doc.RootElement };
        await sut.Handle(cmd, CancellationToken.None);
        captured!.Content.Should().BeAssignableTo<List<object>>();
    }

    [Fact]
    public async Task Handle_ContentWhitespaceString_Null()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<CreateNoteHandler>>();
        Note? captured = null;
        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => { captured = ci.Arg<Note>(); return captured!; });
        mapper.Map<CreateNoteResponse>(Arg.Any<Note>()).Returns(ci => new CreateNoteResponse { Id = ci.Arg<Note>().Id });
        var sut = new CreateNoteHandler(noteRepo, tagRepo, mapper, logger);

        var cmd = new CreateNoteCommand { AuthUserId = Guid.NewGuid(), Title = "T", Content = "   " };
        await sut.Handle(cmd, CancellationToken.None);
        captured!.Content.Should().BeNull();
    }

    [Fact]
    public async Task Handle_StoredPreviewSerializationError_Caught()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<CreateNoteHandler>>();
        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci =>
        {
            var n = ci.Arg<Note>();
            n.Content = new List<object> { new SelfRef() };
            return n;
        });
        mapper.Map<CreateNoteResponse>(Arg.Any<Note>()).Returns(ci => new CreateNoteResponse { Id = ci.Arg<Note>().Id });
        var sut = new CreateNoteHandler(noteRepo, tagRepo, mapper, logger);

        var cmd = new CreateNoteCommand { AuthUserId = Guid.NewGuid(), Title = "T", Content = "plain" };
        var res = await sut.Handle(cmd, CancellationToken.None);
        res.Should().NotBeNull();
    }
}
