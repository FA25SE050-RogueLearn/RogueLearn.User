using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Tags.Commands.CreateTagAndAttachToNote;
using RogueLearn.User.Application.Features.Tags.DTOs;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Tags.Commands.CreateTagAndAttachToNote;

public class CreateTagAndAttachToNoteCommandHandlerTests
{
    [Fact]
    public async Task Handle_NoteNotOwned_Throws()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        var logger = Substitute.For<ILogger<CreateTagAndAttachToNoteCommandHandler>>();
        var sut = new CreateTagAndAttachToNoteCommandHandler(noteRepo, tagRepo, noteTagRepo, logger);

        var authId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(new Note { Id = noteId, AuthUserId = Guid.NewGuid() });

        var cmd = new CreateTagAndAttachToNoteCommand { AuthUserId = authId, NoteId = noteId, Name = "tag" };
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EmptyName_Throws()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        var logger = Substitute.For<ILogger<CreateTagAndAttachToNoteCommandHandler>>();
        var sut = new CreateTagAndAttachToNoteCommandHandler(noteRepo, tagRepo, noteTagRepo, logger);

        var authId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(new Note { Id = noteId, AuthUserId = authId });

        var cmd = new CreateTagAndAttachToNoteCommand { AuthUserId = authId, NoteId = noteId, Name = "   " };
        await Assert.ThrowsAsync<ArgumentException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CreatesAndAttaches_When_NotExisting()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        var logger = Substitute.For<ILogger<CreateTagAndAttachToNoteCommandHandler>>();
        var sut = new CreateTagAndAttachToNoteCommandHandler(noteRepo, tagRepo, noteTagRepo, logger);

        var authId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(new Note { Id = noteId, AuthUserId = authId });
        tagRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Tag, bool>>>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<Tag>());
        Tag? added = null;
        tagRepo.AddAsync(Arg.Any<Tag>(), Arg.Any<CancellationToken>()).Returns(ci => { added = (Tag)ci[0]!; added!.Id = Guid.NewGuid(); return added; });
        noteTagRepo.GetTagIdsForNoteAsync(noteId, Arg.Any<CancellationToken>()).Returns(new List<Guid>());

        var cmd = new CreateTagAndAttachToNoteCommand { AuthUserId = authId, NoteId = noteId, Name = " New Tag " };
        var res = await sut.Handle(cmd, CancellationToken.None);
        res.NoteId.Should().Be(noteId);
        res.CreatedNewTag.Should().BeTrue();
        res.Tag.Should().NotBeNull();
        await noteTagRepo.Received(1).AddAsync(noteId, res.Tag.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AttachesExisting_When_Found()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        var logger = Substitute.For<ILogger<CreateTagAndAttachToNoteCommandHandler>>();
        var sut = new CreateTagAndAttachToNoteCommandHandler(noteRepo, tagRepo, noteTagRepo, logger);

        var authId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        var existing = new Tag { Id = Guid.NewGuid(), AuthUserId = authId, Name = "Existing Tag" };

        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(new Note { Id = noteId, AuthUserId = authId });
        tagRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Tag, bool>>>(), Arg.Any<CancellationToken>()).Returns(new[] { existing });
        noteTagRepo.GetTagIdsForNoteAsync(noteId, Arg.Any<CancellationToken>()).Returns(new List<Guid>());

        var cmd = new CreateTagAndAttachToNoteCommand { AuthUserId = authId, NoteId = noteId, Name = "Existing Tag" };

        var res = await sut.Handle(cmd, CancellationToken.None);
        res.CreatedNewTag.Should().BeFalse();
        res.Tag.Should().BeEquivalentTo(new TagDto { Id = existing.Id, Name = existing.Name });
        await noteTagRepo.Received(1).AddAsync(noteId, existing.Id, Arg.Any<CancellationToken>());
    }
}