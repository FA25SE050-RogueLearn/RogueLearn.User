using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Tags.Commands.RemoveTagFromNote;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Tags.Commands.RemoveTagFromNote;

public class RemoveTagFromNoteCommandHandlerTests
{
    [Fact]
    public async Task Handle_NoteNotOwned_DoesNothing()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        var logger = Substitute.For<ILogger<RemoveTagFromNoteCommandHandler>>();
        var sut = new RemoveTagFromNoteCommandHandler(noteRepo, tagRepo, noteTagRepo, logger);

        var authId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(new Note { Id = noteId, AuthUserId = Guid.NewGuid() });

        var cmd = new RemoveTagFromNoteCommand { AuthUserId = authId, NoteId = noteId, TagId = Guid.NewGuid() };
        await sut.Handle(cmd, CancellationToken.None);

        await noteTagRepo.DidNotReceive().RemoveAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TagNotOwned_DoesNothing()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        var logger = Substitute.For<ILogger<RemoveTagFromNoteCommandHandler>>();
        var sut = new RemoveTagFromNoteCommandHandler(noteRepo, tagRepo, noteTagRepo, logger);

        var authId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(new Note { Id = noteId, AuthUserId = authId });
        tagRepo.GetByIdAsync(tagId, Arg.Any<CancellationToken>()).Returns(new Tag { Id = tagId, AuthUserId = Guid.NewGuid(), Name = "t" });

        var cmd = new RemoveTagFromNoteCommand { AuthUserId = authId, NoteId = noteId, TagId = tagId };
        await sut.Handle(cmd, CancellationToken.None);

        await noteTagRepo.DidNotReceive().RemoveAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Removes_When_Owned()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        var logger = Substitute.For<ILogger<RemoveTagFromNoteCommandHandler>>();
        var sut = new RemoveTagFromNoteCommandHandler(noteRepo, tagRepo, noteTagRepo, logger);

        var authId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(new Note { Id = noteId, AuthUserId = authId });
        tagRepo.GetByIdAsync(tagId, Arg.Any<CancellationToken>()).Returns(new Tag { Id = tagId, AuthUserId = authId, Name = "t" });

        var cmd = new RemoveTagFromNoteCommand { AuthUserId = authId, NoteId = noteId, TagId = tagId };
        await sut.Handle(cmd, CancellationToken.None);

        await noteTagRepo.Received(1).RemoveAsync(noteId, tagId, Arg.Any<CancellationToken>());
    }
}