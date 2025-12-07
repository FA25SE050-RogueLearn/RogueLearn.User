using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.AiTagging.Commands.CommitNoteTagSelections;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.AiTagging.Commands.CommitNoteTagSelections;

public class CommitNoteTagSelectionsCommandHandlerTests
{
    [Fact]
    public async Task Handle_NoteMissingOrForbidden_Throws()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        var sut = new CommitNoteTagSelectionsCommandHandler(noteRepo, tagRepo, noteTagRepo);

        var noteId = Guid.NewGuid();
        var authUserId = Guid.NewGuid();
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns((Note?)null);
        var cmd = new CommitNoteTagSelectionsCommand { NoteId = noteId, AuthUserId = authUserId, SelectedTagIds = new List<Guid>(), NewTagNames = new List<string>() };
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CreatesNewTagsAndAssigns()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        var sut = new CommitNoteTagSelectionsCommandHandler(noteRepo, tagRepo, noteTagRepo);

        var noteId = Guid.NewGuid();
        var authUserId = Guid.NewGuid();
        var note = new Note { Id = noteId, AuthUserId = authUserId };
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(note);

        var existingTag = new Tag { Id = Guid.NewGuid(), AuthUserId = authUserId, Name = "existing" };
        tagRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Tag, bool>>>(), Arg.Any<CancellationToken>())
               .Returns(new[] { existingTag });

        noteTagRepo.GetTagIdsForNoteAsync(noteId, Arg.Any<CancellationToken>()).Returns(new List<Guid> { existingTag.Id });

        var cmd = new CommitNoteTagSelectionsCommand
        {
            NoteId = noteId,
            AuthUserId = authUserId,
            SelectedTagIds = new List<Guid> { Guid.NewGuid(), existingTag.Id },
            NewTagNames = new List<string> { "newtag" }
        };

        var createdTag = new Tag { Id = Guid.NewGuid(), AuthUserId = authUserId, Name = "newtag" };
        tagRepo.AddAsync(Arg.Any<Tag>(), Arg.Any<CancellationToken>()).Returns(createdTag);

        var resp = await sut.Handle(cmd, CancellationToken.None);

        resp.NoteId.Should().Be(noteId);
        resp.AddedTagIds.Should().Contain(existingTag.Id);
        resp.AddedTagIds.Should().Contain(createdTag.Id);
        resp.CreatedTags.Any(ct => ct.Id == createdTag.Id).Should().BeTrue();
        await noteTagRepo.Received().AddAsync(noteId, createdTag.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Removes_Extras_Not_In_Selection()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        var sut = new CommitNoteTagSelectionsCommandHandler(noteRepo, tagRepo, noteTagRepo);

        var noteId = Guid.NewGuid();
        var authUserId = Guid.NewGuid();
        var note = new Note { Id = noteId, AuthUserId = authUserId };
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(note);

        var keptTag = new Tag { Id = Guid.NewGuid(), AuthUserId = authUserId, Name = "keep" };
        var extraTagId = Guid.NewGuid();
        tagRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Tag, bool>>>(), Arg.Any<CancellationToken>())
               .Returns(new[] { keptTag });

        // Current has an extra tag not in desired
        noteTagRepo.GetTagIdsForNoteAsync(noteId, Arg.Any<CancellationToken>()).Returns(new List<Guid> { keptTag.Id, extraTagId });

        var cmd = new CommitNoteTagSelectionsCommand
        {
            NoteId = noteId,
            AuthUserId = authUserId,
            SelectedTagIds = new List<Guid> { keptTag.Id },
            NewTagNames = new List<string>()
        };

        var resp = await sut.Handle(cmd, CancellationToken.None);

        resp.AddedTagIds.Should().Contain(keptTag.Id);
        await noteTagRepo.Received().RemoveAsync(noteId, extraTagId, Arg.Any<CancellationToken>());
    }
}