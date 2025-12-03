using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Tags.Commands.AttachTagToNote;
using RogueLearn.User.Application.Features.Tags.DTOs;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Tags.Commands.AttachTagToNote;

public class AttachTagToNoteCommandHandlerTests
{
    [Fact]
    public async Task Handle_NoteNotOwned_Throws()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        var logger = Substitute.For<ILogger<AttachTagToNoteCommandHandler>>();
        var sut = new AttachTagToNoteCommandHandler(noteRepo, tagRepo, noteTagRepo, logger);

        var authId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(new Note { Id = noteId, AuthUserId = Guid.NewGuid() });

        var cmd = new AttachTagToNoteCommand { AuthUserId = authId, NoteId = noteId, TagId = Guid.NewGuid() };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_TagNotOwned_Throws()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        var logger = Substitute.For<ILogger<AttachTagToNoteCommandHandler>>();
        var sut = new AttachTagToNoteCommandHandler(noteRepo, tagRepo, noteTagRepo, logger);

        var authId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(new Note { Id = noteId, AuthUserId = authId });
        tagRepo.GetByIdAsync(tagId, Arg.Any<CancellationToken>()).Returns(new Tag { Id = tagId, AuthUserId = Guid.NewGuid(), Name = "t" });

        var cmd = new AttachTagToNoteCommand { AuthUserId = authId, NoteId = noteId, TagId = tagId };
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Attaches_When_NotAlready()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        var logger = Substitute.For<ILogger<AttachTagToNoteCommandHandler>>();
        var sut = new AttachTagToNoteCommandHandler(noteRepo, tagRepo, noteTagRepo, logger);

        var authId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(new Note { Id = noteId, AuthUserId = authId });
        tagRepo.GetByIdAsync(tagId, Arg.Any<CancellationToken>()).Returns(new Tag { Id = tagId, AuthUserId = authId, Name = "tag" });
        noteTagRepo.GetTagIdsForNoteAsync(noteId, Arg.Any<CancellationToken>()).Returns(new List<Guid>());

        var cmd = new AttachTagToNoteCommand { AuthUserId = authId, NoteId = noteId, TagId = tagId };
        var res = await sut.Handle(cmd, CancellationToken.None);
        res.NoteId.Should().Be(noteId);
        res.Tag.Should().BeEquivalentTo(new TagDto { Id = tagId, Name = "tag" });
        res.AlreadyAttached.Should().BeFalse();
        await noteTagRepo.Received(1).AddAsync(noteId, tagId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyAttached_DoesNotDuplicate()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        var logger = Substitute.For<ILogger<AttachTagToNoteCommandHandler>>();
        var sut = new AttachTagToNoteCommandHandler(noteRepo, tagRepo, noteTagRepo, logger);

        var authId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(new Note { Id = noteId, AuthUserId = authId });
        tagRepo.GetByIdAsync(tagId, Arg.Any<CancellationToken>()).Returns(new Tag { Id = tagId, AuthUserId = authId, Name = "tag" });
        noteTagRepo.GetTagIdsForNoteAsync(noteId, Arg.Any<CancellationToken>()).Returns(new List<Guid> { tagId });

        var cmd = new AttachTagToNoteCommand { AuthUserId = authId, NoteId = noteId, TagId = tagId };
        var res = await sut.Handle(cmd, CancellationToken.None);
        res.AlreadyAttached.Should().BeTrue();
        await noteTagRepo.DidNotReceive().AddAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}